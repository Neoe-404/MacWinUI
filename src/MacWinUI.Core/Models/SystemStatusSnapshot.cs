namespace MacWinUI.Core.Models;

public readonly record struct SystemStatusSnapshot(
    bool IsNetworkAvailable,
    int? BatteryPercentage,
    bool IsCharging);
