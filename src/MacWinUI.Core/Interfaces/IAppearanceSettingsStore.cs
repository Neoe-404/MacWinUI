using MacWinUI.Core.Dock;

namespace MacWinUI.Core.Interfaces;

public interface IAppearanceSettingsStore
{
    Task<DockAppearanceSnapshot?> LoadAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        DockAppearanceSnapshot snapshot,
        CancellationToken cancellationToken = default);
}
