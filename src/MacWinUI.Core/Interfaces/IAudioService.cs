using MacWinUI.Core.Models;

namespace MacWinUI.Core.Interfaces;

public interface IAudioService
{
    Task<AudioState> GetStateAsync(
        CancellationToken cancellationToken = default);

    Task SetVolumeAsync(
        double volumePercent,
        CancellationToken cancellationToken = default);

    Task SetMutedAsync(
        bool isMuted,
        CancellationToken cancellationToken = default);
}
