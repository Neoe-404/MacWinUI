using MacWinUI.Core.Models;

namespace MacWinUI.Core.Interfaces;

public interface IApplicationActivityService
{
    Task<IReadOnlyDictionary<string, ApplicationActivityState>> GetActivityAsync(
        IReadOnlyCollection<DockItem> dockItems,
        CancellationToken cancellationToken = default);
}
