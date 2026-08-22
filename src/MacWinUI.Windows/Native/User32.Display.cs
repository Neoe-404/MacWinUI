using System.Runtime.InteropServices;

namespace MacWinUI.Windows.Native;

internal static partial class User32
{
    internal const uint MonitorDefaultToNearest = 2;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    internal static extern nint MonitorFromPoint(
        Point point,
        uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetMonitorInfo(
        nint monitorHandle,
        ref MonitorInfo monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Point(int x, int y)
    {
        internal readonly int X = x;
        internal readonly int Y = y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Rect(
        int left,
        int top,
        int right,
        int bottom)
    {
        internal readonly int Left = left;
        internal readonly int Top = top;
        internal readonly int Right = right;
        internal readonly int Bottom = bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct MonitorInfo
    {
        internal uint Size;
        internal Rect Monitor;
        internal Rect Work;
        internal uint Flags;
    }
}
