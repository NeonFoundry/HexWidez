using System;
using System.Collections.Generic;
using HexWide;
using Xunit;

namespace HexWide.Tests;

public class SettingsViewModelTests
{

    [Fact]
    public void LoadSettings_PopulatesResolutions_FromAppSettings()
    {
        var viewModel = new SettingsViewModel();

        viewModel.LoadSettings();

        // Should load default resolutions from AppSettings
        Assert.NotEmpty(viewModel.Resolutions);
        Assert.Contains(viewModel.Resolutions, r => r.Name == "2560x1440");
    }

    [Fact]
    public void LoadSettings_PopulatesHexValues_FromAppSettings()
    {
        var viewModel = new SettingsViewModel();

        viewModel.LoadSettings();

        // Should load default hex values
        Assert.False(string.IsNullOrEmpty(viewModel.AspectFindHex));
        Assert.False(string.IsNullOrEmpty(viewModel.FovFindHex));
        Assert.False(string.IsNullOrEmpty(viewModel.FovReplaceHex));
    }

    [Fact]
    public void LoadSettings_SortsResolutionsByName()
    {
        var viewModel = new SettingsViewModel();

        viewModel.LoadSettings();

        // Verify resolutions are sorted alphabetically
        var resolutionNames = viewModel.Resolutions.Select(r => r.Name).ToList();
        var sortedNames = resolutionNames.OrderBy(n => n).ToList();
        Assert.Equal(sortedNames, resolutionNames);
    }

    [Fact]
    public void AddResolution_AddsNewResolutionWithDefaults()
    {
        var viewModel = new SettingsViewModel();
        viewModel.LoadSettings();
        int initialCount = viewModel.Resolutions.Count;

        viewModel.AddResolution();

        Assert.Equal(initialCount + 1, viewModel.Resolutions.Count);
        Assert.Equal("new-resolution", viewModel.Resolutions[^1].Name);
        Assert.Equal("00 00 00 00", viewModel.Resolutions[^1].Hex);
    }

    [Fact]
    public void AddResolution_AddsNewResolutionWithCustomValues()
    {
        var viewModel = new SettingsViewModel();
        viewModel.LoadSettings();

        viewModel.AddResolution("custom-res", "FF FF FF FF");

        Assert.Contains(viewModel.Resolutions, r => r.Name == "custom-res" && r.Hex == "FF FF FF FF");
    }

    [Fact]
    public void RemoveResolution_RemovesExistingResolution()
    {
        var viewModel = new SettingsViewModel();
        viewModel.LoadSettings();
        viewModel.AddResolution("to-remove", "00 00 00 00");
        var itemToRemove = viewModel.Resolutions[^1];
        int initialCount = viewModel.Resolutions.Count;

        bool result = viewModel.RemoveResolution(itemToRemove);

        Assert.True(result);
        Assert.Equal(initialCount - 1, viewModel.Resolutions.Count);
        Assert.DoesNotContain(viewModel.Resolutions, r => r.Name == "to-remove");
    }

    [Fact]
    public void RemoveResolution_ReturnsFalse_WhenItemIsNull()
    {
        var viewModel = new SettingsViewModel();
        viewModel.LoadSettings();
        int initialCount = viewModel.Resolutions.Count;

        bool result = viewModel.RemoveResolution(null!);

        Assert.False(result);
        Assert.Equal(initialCount, viewModel.Resolutions.Count);
    }

    [Fact]
    public void RemoveResolution_ReturnsFalse_WhenItemNotFound()
    {
        var viewModel = new SettingsViewModel();
        viewModel.LoadSettings();
        var itemNotInList = new SettingsViewModel.ResolutionItem { Name = "nonexistent", Hex = "00 00 00 00" };
        int initialCount = viewModel.Resolutions.Count;

        bool result = viewModel.RemoveResolution(itemNotInList);

        Assert.False(result);
        Assert.Equal(initialCount, viewModel.Resolutions.Count);
    }

    [Fact]
    public void RemoveResolutionAt_RemovesAtValidIndex()
    {
        var viewModel = new SettingsViewModel();
        viewModel.LoadSettings();
        viewModel.AddResolution("to-remove", "AA BB CC DD");
        int indexToRemove = viewModel.Resolutions.Count - 1;
        int initialCount = viewModel.Resolutions.Count;

        bool result = viewModel.RemoveResolutionAt(indexToRemove);

        Assert.True(result);
        Assert.Equal(initialCount - 1, viewModel.Resolutions.Count);
    }

    [Fact]
    public void RemoveResolutionAt_ReturnsFalse_WhenIndexOutOfRange()
    {
        var viewModel = new SettingsViewModel();
        viewModel.LoadSettings();

        bool resultNegative = viewModel.RemoveResolutionAt(-1);
        bool resultTooHigh = viewModel.RemoveResolutionAt(viewModel.Resolutions.Count + 10);

        Assert.False(resultNegative);
        Assert.False(resultTooHigh);
    }

    [Fact]
    public void ValidateSettings_ReturnsFalse_WhenNoResolutions()
    {
        var viewModel = new SettingsViewModel();
        viewModel.Resolutions.Clear();

        var result = viewModel.ValidateSettings();

        Assert.False(result.IsValid);
        Assert.Contains("At least one resolution", result.ErrorMessage);
    }

    [Fact]
    public void ValidateSettings_ReturnsFalse_WhenResolutionNameIsEmpty()
    {
        var viewModel = new SettingsViewModel();
        viewModel.Resolutions.Add(new SettingsViewModel.ResolutionItem { Name = string.Empty, Hex = "00 00 00 00" });

        var result = viewModel.ValidateSettings();

        Assert.False(result.IsValid);
        Assert.Contains("names cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public void ValidateSettings_ReturnsFalse_WhenResolutionHexIsEmpty()
    {
        var viewModel = new SettingsViewModel();
        viewModel.Resolutions.Add(new SettingsViewModel.ResolutionItem { Name = "test-res", Hex = string.Empty });

        var result = viewModel.ValidateSettings();

        Assert.False(result.IsValid);
        Assert.Contains("hex values cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public void ValidateSettings_ReturnsFalse_WhenHexFormatIsInvalid()
    {
        var viewModel = new SettingsViewModel();
        viewModel.Resolutions.Add(new SettingsViewModel.ResolutionItem { Name = "test-res", Hex = "INVALID HEX" });

        var result = viewModel.ValidateSettings();

        Assert.False(result.IsValid);
        Assert.Contains("Invalid hex format", result.ErrorMessage);
    }

    [Fact]
    public void ValidateSettings_ReturnsTrue_WithValidData()
    {
        var viewModel = new SettingsViewModel();
        viewModel.Resolutions.Add(new SettingsViewModel.ResolutionItem { Name = "1440x900", Hex = "CD CC CC 3F" });
        viewModel.AspectFindHex = "3B 8E E3 3F";

        var result = viewModel.ValidateSettings();

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateSettings_AllowsEmptyOptionalHexFields()
    {
        var viewModel = new SettingsViewModel();
        viewModel.Resolutions.Add(new SettingsViewModel.ResolutionItem { Name = "2560x1440", Hex = "26 B4 17 40" });
        viewModel.AspectFindHex = string.Empty;
        viewModel.FovFindHex = string.Empty;
        viewModel.FovReplaceHex = string.Empty;

        var result = viewModel.ValidateSettings();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateSettings_ReturnsFalse_WhenAspectFindHexIsInvalid()
    {
        var viewModel = new SettingsViewModel();
        viewModel.Resolutions.Add(new SettingsViewModel.ResolutionItem { Name = "test-res", Hex = "00 00 00 00" });
        viewModel.AspectFindHex = "ZZ ZZ ZZ ZZ";

        var result = viewModel.ValidateSettings();

        Assert.False(result.IsValid);
        Assert.Contains("Aspect Find", result.ErrorMessage);
    }

    [Fact]
    public void ValidateSettings_ReturnsFalse_WhenFovFindHexIsInvalid()
    {
        var viewModel = new SettingsViewModel();
        viewModel.Resolutions.Add(new SettingsViewModel.ResolutionItem { Name = "test-res", Hex = "00 00 00 00" });
        viewModel.FovFindHex = "not hex";

        var result = viewModel.ValidateSettings();

        Assert.False(result.IsValid);
        Assert.Contains("FOV Find", result.ErrorMessage);
    }

    [Fact]
    public void ValidateSettings_ReturnsFalse_WhenFovReplaceHexIsInvalid()
    {
        var viewModel = new SettingsViewModel();
        viewModel.Resolutions.Add(new SettingsViewModel.ResolutionItem { Name = "test-res", Hex = "00 00 00 00" });
        viewModel.FovReplaceHex = "GG GG GG";

        var result = viewModel.ValidateSettings();

        Assert.False(result.IsValid);
        Assert.Contains("FOV Replace", result.ErrorMessage);
    }

    [Fact]
    public void SaveSettings_ThrowsWhenValidationFails()
    {
        var viewModel = new SettingsViewModel();
        viewModel.Resolutions.Clear();

        var ex = Assert.Throws<InvalidOperationException>(() => viewModel.SaveSettings());
        Assert.Contains("At least one resolution", ex.Message);
    }

    [Fact]
    public void SaveSettings_PersistsChangesToSettings()
    {
        var viewModel = new SettingsViewModel();
        viewModel.LoadSettings();

        // Make changes
        viewModel.AddResolution("new-custom", "AA BB CC DD");
        viewModel.AspectFindHex = "FF FF FF FF";
        viewModel.SaveSettings();

        // Reload and verify
        var newViewModel = new SettingsViewModel();
        newViewModel.LoadSettings();
        Assert.Contains(newViewModel.Resolutions, r => r.Name == "new-custom" && r.Hex == "AA BB CC DD");
        Assert.Equal("FF FF FF FF", newViewModel.AspectFindHex);
    }

    [Fact]
    public void SaveSettings_PreservesExistingSettingsWhenFieldsEmpty()
    {
        var viewModel = new SettingsViewModel();
        viewModel.LoadSettings();
        string originalAspect = viewModel.AspectFindHex;

        viewModel.AspectFindHex = string.Empty;
        viewModel.SaveSettings();

        var newViewModel = new SettingsViewModel();
        newViewModel.LoadSettings();
        // Should preserve original value when field is empty
        Assert.Equal(originalAspect, newViewModel.AspectFindHex);
    }

    [Fact]
    public void DiscardChanges_RevertsToPreviousSettings()
    {
        var viewModel = new SettingsViewModel();
        viewModel.LoadSettings();
        int originalCount = viewModel.Resolutions.Count;
        string originalAspect = viewModel.AspectFindHex;

        viewModel.AddResolution("unsaved", "00 00 00 00");
        viewModel.AspectFindHex = "AA AA AA AA";
        viewModel.DiscardChanges();

        Assert.Equal(originalCount, viewModel.Resolutions.Count);
        Assert.Equal(originalAspect, viewModel.AspectFindHex);
        Assert.DoesNotContain(viewModel.Resolutions, r => r.Name == "unsaved");
    }

    [Theory]
    [InlineData("00 00 00 00", true)]
    [InlineData("FF FF FF FF", true)]
    [InlineData("AA BB CC DD", true)]
    [InlineData("00", true)]  // Single byte is valid
    [InlineData("GGGG", false)]
    [InlineData("00 00 00", true)]
    [InlineData("00 000 00 00", false)]  // Triple digit is invalid
    [InlineData("", true)]  // Empty is valid (optional field)
    [InlineData("00-00-00-00", false)]  // Wrong separator
    public void ValidateSettings_ValidatesHexFormatsCorrectly(string hexValue, bool shouldBeValid)
    {
        var viewModel = new SettingsViewModel();
        viewModel.Resolutions.Add(new SettingsViewModel.ResolutionItem { Name = "test", Hex = "00 00 00 00" });
        viewModel.AspectFindHex = hexValue;

        var result = viewModel.ValidateSettings();

        Assert.Equal(shouldBeValid, result.IsValid);
    }
}
