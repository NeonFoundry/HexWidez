using HexWide;

namespace HexWide.Tests;

public class PatchViewModelTests
{
    [Fact]
    public void ValidatePatchRequest_ReturnsError_WhenExecutableMissing()
    {
        var viewModel = new PatchViewModel(new PatchService(), ResolutionOptions.CreateDefaults());

        var result = viewModel.ValidatePatchRequest();

        Assert.False(result.IsValid);
        Assert.Equal("Select an executable first.", result.ErrorMessage);
    }

    [Fact]
    public void ValidatePatchRequest_ReturnsError_WhenResolutionMissing()
    {
        var viewModel = new PatchViewModel(new PatchService(), ResolutionOptions.CreateDefaults())
        {
            ExecutablePath = "C:/temp/test.exe"
        };

        var result = viewModel.ValidatePatchRequest();

        Assert.False(result.IsValid);
        Assert.Equal("Pick a supported resolution first.", result.ErrorMessage);
    }
}
