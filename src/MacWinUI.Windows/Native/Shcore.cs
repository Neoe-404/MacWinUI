using System.Runtime.InteropServices;

namespace MacWinUI.Windows.Native;

internal static class Shcore
{
    [DllImport("shcore.dll")]
    internal static extern int GetDpiForMonitor(
        nint monitorHandle,
        MonitorDpiType dpiType,
        out uint dpiX,
        out uint dpiY);

    internal enum MonitorDpiType
    {
        Effective = 0
    }
}
