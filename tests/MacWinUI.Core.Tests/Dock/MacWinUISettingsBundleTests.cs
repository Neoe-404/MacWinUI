using MacWinUI.Core.Dock;
using MacWinUI.Core.Localization;
using System.Text.Json;
using Xunit;

namespace MacWinUI.Core.Tests.Dock;

public sealed class MacWinUISettingsBundleTests
{
    [Fact]
    public void Defaults_AreVersionedAndRecoverable()
    {
        var bundle = new MacWinUISettingsBundle();

        Assert.Equal(MacWinUISettingsBundle.CurrentSchemaVersion, bundle.SchemaVersion);
        Assert.True(bundle.Appearance.ReserveMenuBarSpace);
        Assert.False(bundle.Appearance.AutoHideDock);
        Assert.Empty(bundle.DockItems.Applications);
        Assert.Empty(bundle.DockItems.ItemOrder);
    }

    [Fact]
    public void JsonRoundTrip_PreservesVersionSixLanguageSelection()
    {
        var bundle = new MacWinUISettingsBundle
        {
            Appearance = new DockAppearanceSnapshot
            {
                Language = AppLanguage.SimplifiedChinese
            }
        };

        var restored = JsonSerializer.Deserialize<MacWinUISettingsBundle>(
            JsonSerializer.Serialize(bundle));

        Assert.NotNull(restored);
        Assert.Equal(DockAppearanceSnapshot.CurrentSchemaVersion, restored.Appearance.SchemaVersion);
        Assert.Equal(AppLanguage.SimplifiedChinese, restored.Appearance.Language);
    }
}
