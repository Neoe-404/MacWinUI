using MacWinUI.Core.Models;

namespace MacWinUI.Core.Interfaces;

public interface IDockItemProvider
{
    IReadOnlyList<DockItem> GetDefaultItems();
}
