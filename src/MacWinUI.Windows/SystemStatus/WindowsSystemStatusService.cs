using System.Net.NetworkInformation;
using MacWinUI.Core.Interfaces;
using MacWinUI.Core.Models;
using MacWinUI.Windows.Native;

namespace MacWinUI.Windows.SystemStatus;

public sealed class WindowsSystemStatusService : ISystemStatusService
{
    public Task<SystemStatusSnapshot> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.Run(CaptureStatus, cancellationToken);
    }

    private static SystemStatusSnapshot CaptureStatus()
    {
        var networkAvailable = NetworkInterface.GetIsNetworkAvailable();
        if (!Kernel32.GetSystemPowerStatus(out var powerStatus))
        {
            return new SystemStatusSnapshot(networkAvailable, null, false);
        }

        const byte noBattery = 128;
        const byte unknownPercentage = 255;
        var hasBattery = powerStatus.BatteryFlag != noBattery
            && powerStatus.BatteryLifePercent != unknownPercentage;
        int? batteryPercentage = hasBattery
            ? Math.Clamp((int)powerStatus.BatteryLifePercent, 0, 100)
            : null;
        var isCharging = hasBattery && (powerStatus.BatteryFlag & 8) != 0;

        return new SystemStatusSnapshot(
            networkAvailable,
            batteryPercentage,
            isCharging);
    }
}
