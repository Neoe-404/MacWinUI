namespace MacWinUI.Core.Models;

public readonly record struct ApplicationActivityState(
    int RunningInstanceCount,
    bool IsActive);
