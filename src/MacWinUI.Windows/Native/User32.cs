using System.Runtime.InteropServices;

namespace MacWinUI.Windows.Native;

internal static partial class User32
{
    [DllImport("user32.dll")]
    internal static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(
        nint windowHandle,
        out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(nint iconHandle);
}
