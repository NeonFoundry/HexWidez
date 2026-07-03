using System.Collections.ObjectModel;
using System.Linq;

namespace HexWide;

public sealed record SettingsValidationResult(bool IsValid, string? ErrorMessage);

public sealed class SettingsViewModel
{
    private ObservableCollection<ResolutionItem> _resolutions = new();
    private AppSettings? _currentSettings;

    public ObservableCollection<ResolutionItem> Resolutions
    {
        get => _resolutions;
        private set => _resolutions = value;
    }

    public string AspectFindHex { get; set; } = string.Empty;
    public string FovFindHex { get; set; } = string.Empty;
    public string FovReplaceHex { get; set; } = string.Empty;

    public void LoadSettings()
    {
        _currentSettings = AppSettingsLoader.Load();
        Resolutions = new ObservableCollection<ResolutionItem>(
            _currentSettings.Resolutions.Select(kv => 
                new ResolutionItem { Name = kv.Key, Hex = kv.Value }
            ).OrderBy(r => r.Name)
        );

        AspectFindHex = _currentSettings.AspectFindHex ?? string.Empty;
        FovFindHex = _currentSettings.FovFindHex ?? string.Empty;
        FovReplaceHex = _currentSettings.FovReplaceHex ?? string.Empty;
    }

    public void AddResolution(string name = "new-resolution", string hex = "00 00 00 00")
    {
        var newItem = new ResolutionItem { Name = name, Hex = hex };
        Resolutions.Add(newItem);
    }

    public bool RemoveResolution(ResolutionItem item)
    {
        if (item == null)
            return false;

        return Resolutions.Remove(item);
    }

    public bool RemoveResolutionAt(int index)
    {
        if (index < 0 || index >= Resolutions.Count)
            return false;

        Resolutions.RemoveAt(index);
        return true;
    }

    public SettingsValidationResult ValidateSettings()
    {
        if (Resolutions.Count == 0)
        {
            return new SettingsValidationResult(false, "At least one resolution is required.");
        }

        foreach (var resolution in Resolutions)
        {
            if (string.IsNullOrWhiteSpace(resolution.Name))
            {
                return new SettingsValidationResult(false, "Resolution names cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(resolution.Hex))
            {
                return new SettingsValidationResult(false, "Resolution hex values cannot be empty.");
            }

            if (!IsValidHexString(resolution.Hex))
            {
                return new SettingsValidationResult(false, $"Invalid hex format in resolution '{resolution.Name}'. Use format like '00 00 00 00'.");
            }
        }

        if (!string.IsNullOrWhiteSpace(AspectFindHex) && !IsValidHexString(AspectFindHex))
        {
            return new SettingsValidationResult(false, "Invalid hex format in Aspect Find value.");
        }

        if (!string.IsNullOrWhiteSpace(FovFindHex) && !IsValidHexString(FovFindHex))
        {
            return new SettingsValidationResult(false, "Invalid hex format in FOV Find value.");
        }

        if (!string.IsNullOrWhiteSpace(FovReplaceHex) && !IsValidHexString(FovReplaceHex))
        {
            return new SettingsValidationResult(false, "Invalid hex format in FOV Replace value.");
        }

        return new SettingsValidationResult(true, null);
    }

    public void SaveSettings()
    {
        var validation = ValidateSettings();
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage ?? "Settings validation failed.");
        }

        var settings = AppSettingsLoader.Load();
        settings.Resolutions = Resolutions.ToDictionary(r => r.Name, r => r.Hex);
        settings.AspectFindHex = string.IsNullOrWhiteSpace(AspectFindHex) ? settings.AspectFindHex : AspectFindHex;
        settings.FovFindHex = string.IsNullOrWhiteSpace(FovFindHex) ? settings.FovFindHex : FovFindHex;
        settings.FovReplaceHex = string.IsNullOrWhiteSpace(FovReplaceHex) ? settings.FovReplaceHex : FovReplaceHex;

        AppSettingsLoader.Save(settings);
        _currentSettings = settings;
    }

    public void DiscardChanges()
    {
        LoadSettings();
    }

    private static bool IsValidHexString(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return true;

        // Valid hex string should be space-separated pairs like "00 00 00 00"
        var parts = hex.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            if (part.Length != 2 || !System.Byte.TryParse(part, System.Globalization.NumberStyles.HexNumber, null, out _))
            {
                return false;
            }
        }

        return parts.Length > 0;
    }

    public sealed class ResolutionItem
    {
        public string Name { get; set; } = string.Empty;
        public string Hex { get; set; } = string.Empty;
    }
}
