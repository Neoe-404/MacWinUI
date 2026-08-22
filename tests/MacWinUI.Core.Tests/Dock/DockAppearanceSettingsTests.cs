using MacWinUI.Core.Dock;
using Xunit;

namespace MacWinUI.Core.Tests.Dock;

public sealed class DockAppearanceSettingsTests
{
    [Fact]
    public void ThemeValues_PreservePersistedLegacyOrdering()
    {
        Assert.Equal(0, (int)DockTheme.Auto);
        Assert.Equal(1, (int)DockTheme.Light);
        Assert.Equal(2, (int)DockTheme.Dark);
        Assert.Equal(3, (int)DockTheme.BigSur);
    }

    [Fact]
    public void Defaults_AreSafeFloatingDockValues()
    {
        var settings = new DockAppearanceSettings();

        Assert.Equal(DockTheme.BigSur, settings.Theme);
        Assert.Equal(DockPresentationStyle.Floating, settings.Style);
        Assert.Equal(48, settings.IconSize);
        Assert.Equal(1.5, settings.MaxScale);
        Assert.Equal(0.82, settings.Opacity);
        Assert.Equal(0.85, settings.MaterialIntensity);
        Assert.Equal(22, settings.CornerRadius);
        Assert.True(settings.EnableMagnification);
        Assert.False(settings.AutoHideDock);
        Assert.True(settings.ShowRunningIndicators);
        Assert.True(settings.ShowActiveIndicator);
        Assert.False(settings.ReduceMotion);
        Assert.Equal(DockDisplayMode.FollowCursor, settings.DisplayMode);
        Assert.True(settings.Use24HourClock);
        Assert.True(settings.ShowNetworkStatus);
        Assert.True(settings.ShowVolumeStatus);
        Assert.True(settings.ShowBatteryStatus);
        Assert.True(settings.ReserveMenuBarSpace);
        Assert.Equal(
            DockAppearanceSnapshot.CurrentSchemaVersion,
            settings.CreateSnapshot().SchemaVersion);
    }

    [Fact]
    public void NumericValues_AreClampedToSupportedRanges()
    {
        var settings = new DockAppearanceSettings
        {
            IconSize = 500,
            MaxScale = 8,
            Opacity = -1,
            CornerRadius = 2
        };

        Assert.Equal(64, settings.IconSize);
        Assert.Equal(2, settings.MaxScale);
        Assert.Equal(0.55, settings.Opacity);
        Assert.Equal(12, settings.CornerRadius);
    }

    [Fact]
    public void Snapshot_RoundTripsAllAppearancePreferences()
    {
        var settings = new DockAppearanceSettings
        {
            Theme = DockTheme.Light,
            IconSize = 58,
            MaxScale = 1.7,
            Opacity = 0.68,
            MaterialIntensity = 0.64,
            CornerRadius = 28,
            EnableBlur = false,
            ShowRunningIndicators = false,
            ShowActiveIndicator = false,
            EnableMagnification = false,
            AutoHideDock = true,
            ReduceMotion = true,
            DisplayMode = DockDisplayMode.Primary,
            Use24HourClock = false,
            ShowNetworkStatus = false,
            ShowVolumeStatus = false,
            ShowBatteryStatus = false,
            ReserveMenuBarSpace = false
        };

        var restored = new DockAppearanceSettings();
        restored.Apply(settings.CreateSnapshot());

        Assert.Equal(settings.CreateSnapshot(), restored.CreateSnapshot());
    }

    [Fact]
    public void Reset_RestoresEveryPreferenceToCurrentDefaults()
    {
        var settings = new DockAppearanceSettings
        {
            Theme = DockTheme.Dark,
            IconSize = 64,
            Opacity = 0.55,
            ReduceMotion = true,
            DisplayMode = DockDisplayMode.Primary,
            Use24HourClock = false,
            ShowNetworkStatus = false,
            ShowVolumeStatus = false,
            ShowBatteryStatus = false
        };

        settings.Reset();

        Assert.Equal(new DockAppearanceSnapshot(), settings.CreateSnapshot());
    }

    [Fact]
    public void Apply_ClampsInvalidPersistedNumericValues()
    {
        var settings = new DockAppearanceSettings();

        settings.Apply(new DockAppearanceSnapshot
        {
            IconSize = double.PositiveInfinity,
            MaxScale = -5,
            Opacity = 4,
            MaterialIntensity = 0,
            CornerRadius = 100,
            DisplayMode = (DockDisplayMode)99
        });

        Assert.Equal(48, settings.IconSize);
        Assert.Equal(1, settings.MaxScale);
        Assert.Equal(1, settings.Opacity);
        Assert.Equal(0.35, settings.MaterialIntensity);
        Assert.Equal(36, settings.CornerRadius);
        Assert.Equal(DockDisplayMode.FollowCursor, settings.DisplayMode);
    }
}
