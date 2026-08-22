using MacWinUI.Core.Dock;
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
}
