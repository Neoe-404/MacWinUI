using System.Runtime.InteropServices;

namespace MacWinUI.Windows.Native;

internal static partial class User32
{
    internal const uint MonitorDefaultToPrimary = 1;
    internal const uint SetWindowPositionNoActivate = 0x0010;
    internal const uint SetWindowPositionShowWindow = 0x0040;
    internal static readonly nint WindowInsertAfterTopmost = new(-1);

    [DllImport("user32.dll")]
    internal static extern nint MonitorFromWindow(
        nint windowHandle,
        uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern uint RegisterWindowMessage(string messageName);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowPos(
        nint windowHandle,
        nint windowInsertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
