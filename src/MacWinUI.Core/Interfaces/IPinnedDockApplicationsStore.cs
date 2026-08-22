using MacWinUI.Core.Dock;

namespace MacWinUI.Core.Interfaces;

public interface IPinnedDockApplicationsStore
{
    Task<PinnedDockApplicationsSnapshot?> LoadAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        PinnedDockApplicationsSnapshot snapshot,
        CancellationToken cancellationToken = default);
}
