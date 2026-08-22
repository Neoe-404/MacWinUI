using MacWinUI.Core.Models;

namespace MacWinUI.Core.Interfaces;

public interface ISystemStatusService
{
    Task<SystemStatusSnapshot> GetStatusAsync(
        CancellationToken cancellationToken = default);
}
