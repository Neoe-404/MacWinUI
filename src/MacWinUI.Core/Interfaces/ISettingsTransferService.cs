using MacWinUI.Core.Dock;

namespace MacWinUI.Core.Interfaces;

public interface ISettingsTransferService
{
    Task ExportAsync(
        string path,
        MacWinUISettingsBundle bundle,
        CancellationToken cancellationToken = default);

    Task<MacWinUISettingsBundle?> ImportAsync(
        string path,
        CancellationToken cancellationToken = default);
}
