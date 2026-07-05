using System;
using HexWide;
using Xunit;

namespace HexWide.Tests;

public class HexConversionUtilityTests
{
    [Theory]
    [InlineData(1.7777778f, "39 8E E3 3F")]  // 2560x1440 aspect ratio
    [InlineData(3.5555556f, "39 8E 63 40")]  // 3840x1080 aspect ratio
    [InlineData(2.3703704f, "26 B4 17 40")]  // 3440x1440 aspect ratio
    public void FloatToHex_ConvertsFloatCorrectly(float value, string expectedHex)
    {
        var result = HexConversionUtility.FloatToHex(value);
        Assert.Equal(expectedHex, result);
    }

    [Theory]
    [InlineData("39 8E E3 3F", 1.7777778f)]
    [InlineData("39 8E 63 40", 3.5555556f)]
    public void HexToFloat_ConvertsHexCorrectly(string hex, float expectedValue)
    {
        var result = HexConversionUtility.HexToFloat(hex);
        Assert.NotNull(result);
        Assert.True(Math.Abs(result.Value - expectedValue) < 0.0001, $"Expected ~{expectedValue}, got {result}");
    }

    [Theory]
    [InlineData(2560, 1440, "39 8E E3 3F")]  // 16:9
    [InlineData(3840, 1080, "39 8E 63 40")]  // 32:9
    [InlineData(3440, 1440, "8E E3 18 40")]  // 21:9
    [InlineData(1920, 1080, "39 8E E3 3F")]  // 16:9 (different resolution, same aspect)
    [InlineData(5120, 1440, "39 8E 63 40")]  // 32:9 (different resolution, same aspect)
    public void ResolutionToHex_CalculatesCorrectAspectRatio(int x, int y, string expectedHex)
    {
        var result = HexConversionUtility.ResolutionToHex(x, y);
        Assert.Equal(expectedHex, result);
    }

    [Theory]
    [InlineData(0, 1080)]
    [InlineData(1920, 0)]
    [InlineData(-1920, 1080)]
    [InlineData(1920, -1080)]
    public void ResolutionToHex_ThrowsOnInvalidInput(int x, int y)
    {
        Assert.Throws<ArgumentException>(() => HexConversionUtility.ResolutionToHex(x, y));
    }

    [Fact]
    public void HexToFloat_ReturnsNull_OnInvalidHex()
    {
        var result1 = HexConversionUtility.HexToFloat("INVALID");
        var result2 = HexConversionUtility.HexToFloat("GG HH II JJ");
        var result3 = HexConversionUtility.HexToFloat("");
        var result4 = HexConversionUtility.HexToFloat("26 B4");  // Too few bytes

        Assert.Null(result1);
        Assert.Null(result2);
        Assert.Null(result3);
        Assert.Null(result4);
    }

    [Fact]
    public void HexToAspectRatio_CalculatesCorrectly()
    {
        var result = HexConversionUtility.HexToAspectRatio("39 8E E3 3F");  // 2560x1440 hex
        
        Assert.NotNull(result.X);
        Assert.NotNull(result.Y);
        Assert.Equal(1440, result.Y);  // Normalized height
        // X should be approximately 2560 (the actual width for 16:9 at height 1440)
        Assert.InRange(result.X.Value, 2550, 2570);
    }

    [Fact]
    public void HexToAspectRatio_ReturnsNullValues_OnInvalidHex()
    {
        var result = HexConversionUtility.HexToAspectRatio("INVALID");
        
        Assert.Null(result.X);
        Assert.Null(result.Y);
    }

    [Fact]
    public void FloatToHex_RoundTrip_PreservesValue()
    {
        float originalValue = 2.4f;
        
        string hex = HexConversionUtility.FloatToHex(originalValue);
        float? roundTripValue = HexConversionUtility.HexToFloat(hex);

        Assert.NotNull(roundTripValue);
        Assert.Equal(originalValue, roundTripValue.Value);
    }

    [Fact]
    public void ResolutionToHex_RoundTrip_PreservesAspectRatio()
    {
        int originalX = 3440;
        int originalY = 1440;
        
        string hex = HexConversionUtility.ResolutionToHex(originalX, originalY);
        var (calculatedX, calculatedY) = HexConversionUtility.HexToAspectRatio(hex);

        Assert.NotNull(calculatedX);
        Assert.NotNull(calculatedY);
        
        // Check aspect ratio is preserved (allowing small rounding differences)
        float originalAspect = (float)originalX / originalY;
        float calculatedAspect = (float)calculatedX.Value / calculatedY.Value;
        Assert.True(Math.Abs(originalAspect - calculatedAspect) < 0.001);
    }
}
