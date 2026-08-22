using MacWinUI.Core.Dock;
using MacWinUI.Core.Models;
using Xunit;

namespace MacWinUI.Core.Tests.Dock;

public sealed class PinnedDockApplicationTests
{
    [Fact]
    public void Snapshot_DefaultsKeepAllBuiltInItemsVisible()
    {
        var snapshot = new PinnedDockApplicationsSnapshot();

        Assert.Equal(4, snapshot.SchemaVersion);
        Assert.Empty(snapshot.HiddenDefaultItemIds);
        Assert.Empty(snapshot.ItemOrder);
    }

    [Fact]
    public void Create_NormalizesExecutableAndBuildsDockItem()
    {
        var executablePath = Path.Combine(
            Path.GetTempPath(),
            "MacWinUI Tests",
            "Sample App.exe");

        var application = PinnedDockApplication.Create(executablePath, "  Sample App  ");
        var item = application.CreateDockItem();

        Assert.Equal("Sample App", application.DisplayName);
        Assert.Equal(Path.GetFullPath(executablePath), application.ExecutablePath);
        Assert.Equal(LaunchType.Executable, item.LaunchType);
        Assert.Equal(application.ExecutablePath, item.LaunchTarget);
        Assert.Equal(application.ExecutablePath, item.IconSourcePath);
        Assert.Equal("Sample App", item.ProcessName);
        Assert.True(item.IsCustom);
        Assert.True(item.AcceptsFileDrops);
    }

    [Fact]
    public void CreateStableId_IsCaseInsensitiveAndDeterministic()
    {
        var executablePath = Path.Combine(Path.GetTempPath(), "Example", "App.exe");

        var first = PinnedDockApplication.CreateStableId(executablePath);
        var second = PinnedDockApplication.CreateStableId(executablePath.ToUpperInvariant());

        Assert.Equal(first, second);
        Assert.StartsWith("custom-", first, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_CreatesShellItemForAFile()
    {
        var documentPath = Path.Combine(Path.GetTempPath(), "document.txt");

        var pinnedFile = PinnedDockApplication.Create(documentPath);
        var item = pinnedFile.CreateDockItem();

        Assert.Equal(PinnedDockItemKind.File, pinnedFile.Kind);
        Assert.Equal(LaunchType.Shell, item.LaunchType);
        Assert.False(item.AcceptsFileDrops);
        Assert.Null(item.ProcessName);
    }
}
