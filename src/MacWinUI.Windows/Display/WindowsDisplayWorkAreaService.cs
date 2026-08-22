using System.Runtime.InteropServices;
using System.Windows;
using MacWinUI.Core.Display;
using MacWinUI.Core.Interfaces;
using MacWinUI.Windows.Native;

namespace MacWinUI.Windows.Display;

public sealed class WindowsDisplayWorkAreaService : IDisplayWorkAreaService
{
    private const double DefaultDpi = 96;

    public DisplayWorkArea GetActiveWorkArea()
    {
        try
        {
            if (!User32.GetCursorPos(out var cursor))
            {
                return GetPrimaryWorkArea();
            }

            var monitor = User32.MonitorFromPoint(
                cursor,
                User32.MonitorDefaultToNearest);
            if (monitor == nint.Zero)
            {
                return GetPrimaryWorkArea();
            }

            var monitorInfo = new User32.MonitorInfo
            {
                Size = (uint)Marshal.SizeOf<User32.MonitorInfo>()
            };
            if (!User32.GetMonitorInfo(monitor, ref monitorInfo))
            {
                return GetPrimaryWorkArea();
            }

            var scale = GetDipScale(monitor);
            var work = monitorInfo.Work;
            return new DisplayWorkArea(
                work.Left * scale,
                work.Top * scale,
                (work.Right - work.Left) * scale,
                (work.Bottom - work.Top) * scale);
        }
        catch (DllNotFoundException)
        {
            return GetPrimaryWorkArea();
        }
        catch (EntryPointNotFoundException)
        {
            return GetPrimaryWorkArea();
        }
    }

    private static double GetDipScale(nint monitor)
    {
        try
        {
            var result = Shcore.GetDpiForMonitor(
                monitor,
                Shcore.MonitorDpiType.Effective,
                out var dpiX,
                out _);
            return result == 0 && dpiX > 0
                ? DefaultDpi / dpiX
                : 1;
        }
        catch (DllNotFoundException)
        {
            return 1;
        }
        catch (EntryPointNotFoundException)
        {
            return 1;
        }
    }

    public DisplayWorkArea GetPrimaryWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        return new DisplayWorkArea(
            workArea.Left,
            workArea.Top,
            workArea.Width,
            workArea.Height);
    }
}
