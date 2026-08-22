using System.IO;
using MacWinUI.Core.Dock;
using MacWinUI.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace MacWinUI.Windows.Themes;

public sealed class WindowsSystemThemeService(
    ILogger<WindowsSystemThemeService> logger) : ISystemThemeService
{
    private const string PersonalizeKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public DockTheme GetSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int lightThemeEnabled && lightThemeEnabled != 0
                ? DockTheme.Light
                : DockTheme.Dark;
        }
        catch (Exception exception) when (
            exception is UnauthorizedAccessException
            or System.Security.SecurityException
            or IOException)
        {
            logger.LogWarning(
                exception,
                "Could not read the Windows app theme. Falling back to Dark.");
            return DockTheme.Dark;
        }
    }
}
