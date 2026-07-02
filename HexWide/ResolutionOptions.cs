using System.Collections.Generic;

namespace HexWide;

public static class ResolutionOptions
{
    public static IReadOnlyDictionary<string, string> CreateDefaults()
    {
        return AppSettingsLoader.Load().Resolutions;
    }
}
