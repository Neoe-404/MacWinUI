using System.Runtime.InteropServices;

namespace MacWinUI.Windows.Native;

internal static class DwmApi
{
    [DllImport("dwmapi.dll")]
    internal static extern int DwmSetWindowAttribute(
        nint windowHandle,
        DwmWindowAttribute attribute,
        ref int attributeValue,
        int attributeSize);

    internal enum DwmWindowAttribute
    {
        UseImmersiveDarkMode = 20,
        WindowCornerPreference = 33,
        SystemBackdropType = 38
    }

    internal enum DwmWindowCornerPreference
    {
        Default = 0,
        DoNotRound = 1,
        Round = 2,
        RoundSmall = 3
    }

    internal enum DwmSystemBackdropType
    {
        Auto = 0,
        None = 1,
        MainWindow = 2,
        TransientWindow = 3,
        TabbedWindow = 4
    }
}
