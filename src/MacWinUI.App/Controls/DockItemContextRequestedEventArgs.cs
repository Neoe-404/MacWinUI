using MacWinUI.Core.Models;

namespace MacWinUI.App.Controls;

public sealed class DockItemContextRequestedEventArgs(DockItem item) : EventArgs
{
    public DockItem Item { get; } = item;
}
