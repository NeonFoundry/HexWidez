using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Serilog;

namespace HexWide;

public sealed record PatchValidationResult(bool IsValid, string? ErrorMessage, string? ResolutionHex);

public sealed class PatchViewModel
{
    private readonly IPatchService _patchService;
    private readonly IReadOnlyDictionary<string, string> _resolutionOptions;

    public PatchViewModel(IPatchService patchService, IReadOnlyDictionary<string, string> resolutionOptions)
    {
        _patchService = patchService;
        _resolutionOptions = resolutionOptions;
    }

    public string? ExecutablePath { get; set; }

    public string? SelectedResolution { get; set; }

    public bool IncludeFovFix { get; set; }

    public IReadOnlyList<string> AvailableResolutions => _resolutionOptions.Keys.OrderBy(name => name).ToList();

    public PatchValidationResult ValidatePatchRequest()
    {
        if (string.IsNullOrWhiteSpace(ExecutablePath))
        {
            return new PatchValidationResult(false, "Select an executable first.", null);
        }

        if (string.IsNullOrWhiteSpace(SelectedResolution) || !_resolutionOptions.TryGetValue(SelectedResolution, out string? resolutionHex))
        {
            return new PatchValidationResult(false, "Pick a supported resolution first.", null);
        }

        if (!File.Exists(ExecutablePath))
        {
            return new PatchValidationResult(false, "The selected file could not be found.", null);
        }

        return new PatchValidationResult(true, null, resolutionHex);
    }

    public async Task<PatchResult> ApplyPatchAsync(IProgress<int>? progress = null, CancellationToken? cancellationToken = null)
    {
        var validation = ValidatePatchRequest();
        if (!validation.IsValid)
        {
            var error = new InvalidOperationException(validation.ErrorMessage ?? "Patch request is invalid.");
            Log.Warning(error, "Patch request validation failed.");
            throw error;
        }

        Log.Information("Applying patch to {ExecutablePath} with resolution {Resolution}", ExecutablePath, SelectedResolution);
        return await _patchService.ApplyPatchAsync(ExecutablePath!, validation.ResolutionHex!, IncludeFovFix, progress, cancellationToken);
    }
}
