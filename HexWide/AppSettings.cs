using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Serilog;

namespace HexWide;

public sealed class AppSettings
{
    public Dictionary<string, string> Resolutions { get; set; } = new();
    public string DefaultResolution { get; set; } = "2560x1440";
    public bool EnableFovCompensationByDefault { get; set; }
    public string LogFilePath { get; set; } = "logs/hexwide-.log";
    public int MaxLogFileCount { get; set; } = 7;
    public string AspectFindHex { get; set; } = "39 8E E3 3F";
    public string FovFindHex { get; set; } = "35 FA 0E 3C AC C5 27 37 6F";
    public string FovReplaceHex { get; set; } = "35 FA 3E 3C AC C5 27 37 6F";
    public bool ShowPatchWarning { get; set; } = true;
}

public static class AppSettingsLoader
{
    private const string FileName = "appsettings.json";

    public static AppSettings Load()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, FileName);
            if (!File.Exists(path))
            {
                return CreateDefaultSettings();
            }

            string json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return settings ?? CreateDefaultSettings();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load application settings, falling back to defaults.");
            return CreateDefaultSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, FileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to save application settings.");
            throw;
        }
    }

    public static AppSettings CreateDefaultSettings() => new()
    {
        Resolutions = new Dictionary<string, string>
        {
            ["1440x900"] = "CD CC CC 3F",
            ["1280x1024"] = "00 00 A0 3F",
            ["2560x1440"] = "26 B4 17 40",
            ["3440x1440"] = "8E E3 18 40",
            ["3840x1080"] = "39 8E 63 40",
            ["3840x1600"] = "9A 99 19 40",
            ["4120x1024"] = "00 00 A0 3F",
            ["5120x1440"] = "39 8E 63 40",
            ["5292x1050"] = "AE 47 A1 40",
            ["7680x1440"] = "AB AA AA 40"
        },
        DefaultResolution = "2560x1440",
        EnableFovCompensationByDefault = false,
        LogFilePath = "logs/hexwide-.log",
        MaxLogFileCount = 7
        ,ShowPatchWarning = true
    };
}
