using HexWide;

namespace HexWide.Tests;

public class PatchServiceTests
{
    [Fact]
    public void ApplyPatch_ReturnsZero_WhenPatternIsNotFound()
    {
        var service = new PatchService();
        var source = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
        var result = service.ApplyPatch(source, "3B 8E E3 3F", "26 B4 17 40");

        Assert.Equal(0, result.ReplacementCount);
        Assert.Equal(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 }, source);
    }

    [Fact]
    public void ApplyPatch_WritesReplacement_WhenPatternExists()
    {
        var service = new PatchService();
        var source = new byte[] { 0x3B, 0x8E, 0xE3, 0x3F, 0x00, 0x01 };
        var result = service.ApplyPatch(source, "3B 8E E3 3F", "26 B4 17 40");

        Assert.Equal(1, result.ReplacementCount);
        Assert.Equal(new byte[] { 0x26, 0xB4, 0x17, 0x40, 0x00, 0x01 }, source);
    }
}
