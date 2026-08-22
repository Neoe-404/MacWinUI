using MacWinUI.Core.Models;

namespace MacWinUI.Core.Interfaces;

public interface IIconService
{
    Task<byte[]?> GetIconPngAsync(
        DockItem item,
        CancellationToken cancellationToken = default);
}
