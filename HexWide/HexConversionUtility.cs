using System;

namespace HexWide;

/// <summary>
/// Utility for converting between floating-point values and hex string representations.
/// </summary>
public static class HexConversionUtility
{
    /// <summary>
    /// Converts a float value to a hex string representation (space-separated bytes, little-endian).
    /// Example: 1.7777778 -> "26 B4 17 40"
    /// </summary>
    public static string FloatToHex(float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        return string.Join(" ", Array.ConvertAll(bytes, b => b.ToString("X2")));
    }

    /// <summary>
    /// Converts a hex string to a float value.
    /// Example: "26 B4 17 40" -> 1.7777778
    /// </summary>
    public static float? HexToFloat(string hexString)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hexString))
                return null;

            string[] parts = hexString.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
                return null;

            byte[] bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                if (!byte.TryParse(parts[i], System.Globalization.NumberStyles.HexNumber, null, out bytes[i]))
                    return null;
            }

            return BitConverter.ToSingle(bytes, 0);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Calculates the aspect ratio hex from X and Y resolution.
    /// Example: X=2560, Y=1440 -> "26 B4 17 40" (aspect ratio 16:9)
    /// </summary>
    public static string ResolutionToHex(int x, int y)
    {
        if (x <= 0 || y <= 0)
            throw new ArgumentException("Resolution values must be positive.");

        float aspectRatio = (float)x / y;
        return FloatToHex(aspectRatio);
    }

    /// <summary>
    /// Extracts the aspect ratio from a resolution hex value.
    /// </summary>
    public static (int? X, int? Y) HexToAspectRatio(string hexString)
    {
        float? aspectRatio = HexToFloat(hexString);
        if (aspectRatio == null)
            return (null, null);

        // Normalize to a common resolution height (e.g., 1440)
        const int normalizedHeight = 1440;
        int calculatedWidth = (int)Math.Round(aspectRatio.Value * normalizedHeight);

        return (calculatedWidth, normalizedHeight);
    }
}
