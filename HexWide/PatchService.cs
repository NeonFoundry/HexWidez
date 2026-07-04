using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;

namespace HexWide;

public interface IFileSystem
{
    bool FileExists(string path);
    string? GetDirectoryName(string path);
    string? GetFileNameWithoutExtension(string path);
    string? GetExtension(string path);
    byte[] ReadAllBytes(string path);
    void WriteAllBytes(string path, byte[] bytes);
    void Copy(string sourcePath, string destinationPath);
}

public sealed class FileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);

    public string? GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);

    public string? GetExtension(string path) => Path.GetExtension(path);

    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

    public void WriteAllBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);

    public void Copy(string sourcePath, string destinationPath) => File.Copy(sourcePath, destinationPath, overwrite: false);
}

public sealed record HexPatchOperation(string Id, string FindHex, string ReplaceHex);

public sealed record PatchResult(int ReplacementCount, string BackupPath);

public interface IPatchService
{
    PatchResult ApplyPatch(string filePath, string resolutionHex, bool includeFovFix);
    PatchResult ApplyPatch(byte[] bytes, string findHex, string replaceHex);
    Task<PatchResult> ApplyPatchAsync(string filePath, string resolutionHex, bool includeFovFix, IProgress<int>? progress = null, CancellationToken? cancellationToken = null);
    Task<PatchResult> ApplyPatchAsync(byte[] bytes, string findHex, string replaceHex, IProgress<int>? progress = null, CancellationToken? cancellationToken = null);
}

public sealed class PatchService : IPatchService
{
    private readonly IFileSystem _fileSystem;
    private readonly IReadOnlyList<HexPatchOperation> _operations;

    public PatchService(IFileSystem? fileSystem = null, IEnumerable<HexPatchOperation>? operations = null)
    {
        _fileSystem = fileSystem ?? new FileSystem();
        _operations = (operations ?? CreateDefaultOperations()).ToList();
    }

    public PatchResult ApplyPatch(string filePath, string resolutionHex, bool includeFovFix)
    {
        return ApplyPatchAsync(filePath, resolutionHex, includeFovFix).GetAwaiter().GetResult();
    }

    public PatchResult ApplyPatch(byte[] bytes, string findHex, string replaceHex)
    {
        return ApplyPatchAsync(bytes, findHex, replaceHex).GetAwaiter().GetResult();
    }

    public async Task<PatchResult> ApplyPatchAsync(string filePath, string resolutionHex, bool includeFovFix, IProgress<int>? progress = null, CancellationToken? cancellationToken = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolutionHex);

        if (!_fileSystem.FileExists(filePath))
        {
            Log.Warning("Patch requested for missing file {FilePath}", filePath);
            throw new FileNotFoundException("The selected file could not be found.", filePath);
        }

        cancellationToken?.ThrowIfCancellationRequested();

        string backupPath = CreateBackup(filePath);
        byte[] originalBytes = _fileSystem.ReadAllBytes(filePath);
        byte[] patchedBytes = (byte[])originalBytes.Clone();

        var settings = AppSettingsLoader.Load();

        var operations = new List<HexPatchOperation>(_operations)
        {
            new("aspect-ratio", settings.AspectFindHex ?? "3B 8E E3 3F", resolutionHex)
        };

        if (includeFovFix)
        {
            operations.Add(new("fov-compensation", settings.FovFindHex ?? "35 FA 0E 3C AC C5 27 37 6F", settings.FovReplaceHex ?? "35 FA 3E 3C AC C5 27 37 6F"));
        }

        int total = operations.Count();
        int completed = 0;
        int replacementCount = 0;

        foreach (var op in operations)
        {
            cancellationToken?.ThrowIfCancellationRequested();
            completed++;
            var subProgress = new Progress<int>(p => progress?.Report((int)((completed - 1 + p / 100.0) / total * 100)));
            PatchResult result = await ApplyPatchAsync(patchedBytes, op.FindHex, op.ReplaceHex, subProgress, cancellationToken);
            replacementCount += result.ReplacementCount;
        }

        _fileSystem.WriteAllBytes(filePath, patchedBytes);
        progress?.Report(100);
        Log.Information("Patch applied to {FilePath} with {ReplacementCount} replacements and backup {BackupPath}", filePath, replacementCount, backupPath);
        return new PatchResult(replacementCount, backupPath);
    }

    public async Task<PatchResult> ApplyPatchAsync(byte[] bytes, string findHex, string replaceHex, IProgress<int>? progress = null, CancellationToken? cancellationToken = null)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        cancellationToken?.ThrowIfCancellationRequested();

        byte[] findBytes = ConvertHexStringToByteArray(NormalizeHex(findHex));
        byte[] replaceBytes = ConvertHexStringToByteArray(NormalizeHex(replaceHex));

        if (findBytes.Length != replaceBytes.Length)
        {
            throw new ArgumentException("The replacement bytes must match the original length.");
        }

        int replacements = await Task.Run(() => ApplyReplacement(bytes, findBytes, replaceBytes), cancellationToken ?? CancellationToken.None);
        progress?.Report(100);
        return new PatchResult(replacements, string.Empty);
    }

    private string CreateBackup(string sourcePath)
    {
        string directory = _fileSystem.GetDirectoryName(sourcePath) ?? AppDomain.CurrentDomain.BaseDirectory;
        string fileName = _fileSystem.GetFileNameWithoutExtension(sourcePath) ?? Path.GetFileNameWithoutExtension(sourcePath);
        string extension = _fileSystem.GetExtension(sourcePath) ?? Path.GetExtension(sourcePath);
        string backupPath = Path.Combine(directory, $"{fileName}.bak{extension}");

        int suffix = 1;
        while (_fileSystem.FileExists(backupPath))
        {
            backupPath = Path.Combine(directory, $"{fileName}.bak{suffix}{extension}");
            suffix++;
        }

        _fileSystem.Copy(sourcePath, backupPath);
        return backupPath;
    }

    private static int ApplyReplacement(byte[] bytes, byte[] find, byte[] replace)
    {
        int totalReplacements = 0;
        foreach (int index in Search(bytes, find))
        {
            for (int i = 0; i < replace.Length; i++)
            {
                if (index + i >= bytes.Length) break;
                bytes[index + i] = replace[i];
            }

            totalReplacements++;
        }

        return totalReplacements;
    }

    private static int[] Search(byte[] src, byte[] pattern)
    {
        var indexes = new List<int>();
        int maxFirstCharSlot = src.Length - pattern.Length + 1;
        for (int i = 0; i < maxFirstCharSlot; i++)
        {
            if (src[i] != pattern[0]) continue;

            bool match = true;
            for (int j = 1; j < pattern.Length; j++)
            {
                if (src[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                indexes.Add(i);
            }
        }

        return indexes.ToArray();
    }

    private static byte[] ConvertHexStringToByteArray(string hexString)
    {
        if (hexString.Length % 2 != 0)
        {
            throw new ArgumentException($"The binary key should have even number of digits : {hexString}");
        }

        byte[] data = new byte[hexString.Length / 2];
        for (int index = 0; index < data.Length; index++)
        {
            string byteValue = hexString.Substring(index * 2, 2);
            data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return data;
    }

    private static string NormalizeHex(string hexString)
    {
        return Regex.Replace(hexString, @"0x|[\s,]", string.Empty).Normalize().Trim();
    }

    private static IReadOnlyList<HexPatchOperation> CreateDefaultOperations()
    {
        return new List<HexPatchOperation>
        {
            new("aspect-ratio", "3B 8E E3 3F", "3B 8E E3 3F")
        };
    }
}
