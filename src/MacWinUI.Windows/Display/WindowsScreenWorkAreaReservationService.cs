using System.Runtime.InteropServices;
using MacWinUI.Core.Dock;
using MacWinUI.Core.Interfaces;
using MacWinUI.Windows.Native;
using Microsoft.Extensions.Logging;

namespace MacWinUI.Windows.Display;

public sealed class WindowsScreenWorkAreaReservationService(
    ILogger<WindowsScreenWorkAreaReservationService> logger) :
    IScreenWorkAreaReservationService,
    IDisposable
{
    private const double DefaultDpi = 96;
    private readonly HashSet<nint> _registeredWindows = [];
    private readonly uint _callbackMessage = User32.RegisterWindowMessage(
        "MacWinUI.ScreenWorkAreaReservation");
    private readonly uint _shellRestartedMessage = User32.RegisterWindowMessage(
        "TaskbarCreated");
    private bool _disposed;

    public uint PositionChangedMessage => _callbackMessage;

    public uint ShellRestartedMessage => _shellRestartedMessage;

    public bool HasActiveReservation => _registeredWindows.Count > 0;

    public bool ReserveTop(
        nint windowHandle,
        DockDisplayMode displayMode,
        double heightDip)
    {
        if (_disposed
            || windowHandle == nint.Zero
            || !double.IsFinite(heightDip)
            || heightDip <= 0)
        {
            return false;
        }

        try
        {
            if (!_registeredWindows.Contains(windowHandle))
            {
                var registration = CreateAppBarData(windowHandle);
                if (Shell32.SHAppBarMessage(
                        Shell32.AppBarMessageNew,
                        ref registration) == 0)
                {
                    logger.LogWarning("Windows Shell rejected the MenuBar AppBar registration.");
                    return false;
                }

                _registeredWindows.Add(windowHandle);
            }

            var monitor = GetTargetMonitor(windowHandle, displayMode);
            if (monitor == nint.Zero)
            {
                return false;
            }

            var monitorInfo = new User32.MonitorInfo
            {
                Size = (uint)Marshal.SizeOf<User32.MonitorInfo>()
            };
            if (!User32.GetMonitorInfo(monitor, ref monitorInfo))
            {
                return false;
            }

            var monitorBounds = monitorInfo.Monitor;
            var data = CreateAppBarData(windowHandle);
            data.Edge = Shell32.AppBarEdgeTop;
            data.Rect = new Shell32.AppBarRect
            {
                Left = monitorBounds.Left,
                Top = monitorBounds.Top,
                Right = monitorBounds.Right,
                Bottom = monitorBounds.Bottom
            };
            _ = Shell32.SHAppBarMessage(
                Shell32.AppBarMessageQueryPosition,
                ref data);

            var heightPixels = Math.Max(
                1,
                (int)Math.Round(heightDip * GetDpiScale(monitor)));
            data.Rect.Bottom = Math.Min(
                monitorBounds.Bottom,
                data.Rect.Top + heightPixels);
            _ = Shell32.SHAppBarMessage(
                Shell32.AppBarMessageSetPosition,
                ref data);

            return User32.SetWindowPos(
                windowHandle,
                User32.WindowInsertAfterTopmost,
                data.Rect.Left,
                data.Rect.Top,
                Math.Max(1, data.Rect.Right - data.Rect.Left),
                Math.Max(1, data.Rect.Bottom - data.Rect.Top),
                User32.SetWindowPositionNoActivate
                | User32.SetWindowPositionShowWindow);
        }
        catch (Exception exception) when (
            exception is DllNotFoundException
            or EntryPointNotFoundException)
        {
            logger.LogWarning(
                exception,
                "Windows AppBar APIs are unavailable; MenuBar will use overlay placement.");
            Release(windowHandle);
            return false;
        }
    }

    public void Release(nint windowHandle)
    {
        if (windowHandle == nint.Zero || !_registeredWindows.Remove(windowHandle))
        {
            return;
        }

        try
        {
            var data = CreateAppBarData(windowHandle);
            _ = Shell32.SHAppBarMessage(
                Shell32.AppBarMessageRemove,
                ref data);
        }
        catch (Exception exception) when (
            exception is DllNotFoundException
            or EntryPointNotFoundException)
        {
            logger.LogDebug(exception, "Could not release the Windows AppBar reservation.");
        }
    }

    public void Invalidate(nint windowHandle)
    {
        _registeredWindows.Remove(windowHandle);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var windowHandle in _registeredWindows.ToArray())
        {
            Release(windowHandle);
        }

        _disposed = true;
    }

    private Shell32.AppBarData CreateAppBarData(nint windowHandle) => new()
    {
        Size = (uint)Marshal.SizeOf<Shell32.AppBarData>(),
        WindowHandle = windowHandle,
        CallbackMessage = _callbackMessage
    };

    private nint GetTargetMonitor(
        nint windowHandle,
        DockDisplayMode displayMode)
    {
        if (displayMode is DockDisplayMode.FollowCursor
            && User32.GetCursorPos(out var cursor))
        {
            return User32.MonitorFromPoint(cursor, User32.MonitorDefaultToNearest);
        }

        return User32.MonitorFromPoint(
            new User32.Point(0, 0),
            User32.MonitorDefaultToPrimary);
    }

    private static double GetDpiScale(nint monitor)
    {
        try
        {
            var result = Shcore.GetDpiForMonitor(
                monitor,
                Shcore.MonitorDpiType.Effective,
                out var dpiX,
                out _);
            return result == 0 && dpiX > 0
                ? dpiX / DefaultDpi
                : 1;
        }
        catch (Exception exception) when (
            exception is DllNotFoundException
            or EntryPointNotFoundException)
        {
            return 1;
        }
    }
}
