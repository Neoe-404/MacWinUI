using MacWinUI.Core.Models;

namespace MacWinUI.Core.Interfaces;

public interface IApplicationLauncher
{
    Task LaunchAsync(
        DockItem item,
        CancellationToken cancellationToken = default);

    Task ActivateOrLaunchAsync(
        DockItem item,
        CancellationToken cancellationToken = default);

    Task LaunchWithFilesAsync(
        DockItem item,
        IReadOnlyList<string> filePaths,
        CancellationToken cancellationToken = default);

    Task OpenContainingFolderAsync(
        DockItem item,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplicationWindowInfo>> GetOpenWindowsAsync(
        DockItem item,
        CancellationToken cancellationToken = default);

    Task ActivateWindowAsync(
        nint windowHandle,
        CancellationToken cancellationToken = default);
}
