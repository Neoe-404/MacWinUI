using MacWinUI.Core.Models;

namespace MacWinUI.Core.Interfaces;

public interface ISystemSettingsLauncher
{
    Task OpenAsync(
        SystemSettingsPage page,
        CancellationToken cancellationToken = default);
}
