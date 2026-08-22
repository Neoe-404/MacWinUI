using System.Runtime.InteropServices;

namespace MacWinUI.Windows.Native;

internal static class Kernel32
{
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetSystemPowerStatus(out SystemPowerStatus status);

    [StructLayout(LayoutKind.Sequential)]
    internal struct SystemPowerStatus
    {
        internal byte AcLineStatus;
        internal byte BatteryFlag;
        internal byte BatteryLifePercent;
        internal byte SystemStatusFlag;
        internal uint BatteryLifeTime;
        internal uint BatteryFullLifeTime;
    }
}
