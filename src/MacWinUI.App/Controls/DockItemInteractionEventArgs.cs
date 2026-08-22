using MacWinUI.Core.Models;

namespace MacWinUI.App.Controls;

public sealed class DockItemInteractionEventArgs(DockItem item) : EventArgs
{
    public DockItem Item { get; } = item;
}

public sealed class DockItemReorderEventArgs(
    string sourceItemId,
    DockItem targetItem) : EventArgs
{
    public string SourceItemId { get; } = sourceItemId;

    public DockItem TargetItem { get; } = targetItem;
}
