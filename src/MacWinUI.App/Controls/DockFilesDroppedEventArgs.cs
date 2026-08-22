using MacWinUI.Core.Models;

namespace MacWinUI.App.Controls;

public sealed class DockFilesDroppedEventArgs(
    DockItem application,
    IReadOnlyList<string> filePaths) : EventArgs
{
    public DockItem Application { get; } = application;

    public IReadOnlyList<string> FilePaths { get; } = filePaths;
}
