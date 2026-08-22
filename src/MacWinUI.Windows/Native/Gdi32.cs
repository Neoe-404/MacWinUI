using System.Runtime.InteropServices;

namespace MacWinUI.Windows.Native;

internal static class Gdi32
{
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteObject(nint objectHandle);
}
