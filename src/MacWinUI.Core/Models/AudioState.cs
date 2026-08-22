namespace MacWinUI.Core.Models;

public readonly record struct AudioState(
    bool IsAvailable,
    double VolumePercent,
    bool IsMuted);
