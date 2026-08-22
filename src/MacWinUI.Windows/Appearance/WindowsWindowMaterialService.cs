using MacWinUI.Core.Dock;
using MacWinUI.Core.Interfaces;
using MacWinUI.Core.Models;
using MacWinUI.Windows.Native;
using Microsoft.Extensions.Logging;

namespace MacWinUI.Windows.Appearance;

public sealed class WindowsWindowMaterialService(
    ISystemThemeService systemThemeService,
    ILogger<WindowsWindowMaterialService> logger) : IWindowMaterialService
{
    private const int AttributeSize = sizeof(int);

    public bool TryApply(
        nint windowHandle,
        WindowMaterial material,
        DockTheme requestedTheme,
        bool enabled)
    {
        if (windowHandle == nint.Zero)
        {
            return false;
        }

        if (!enabled)
        {
            Clear(windowHandle);
            return false;
        }

        try
        {
            var resolvedTheme = requestedTheme is DockTheme.Auto
                ? systemThemeService.GetSystemTheme()
                : requestedTheme;
            var useDarkMode = resolvedTheme is DockTheme.Dark ? 1 : 0;
            var darkModeResult = DwmApi.DwmSetWindowAttribute(
                windowHandle,
                DwmApi.DwmWindowAttribute.UseImmersiveDarkMode,
                ref useDarkMode,
                AttributeSize);

            var cornerPreference = material is WindowMaterial.MenuBar
                ? (int)DwmApi.DwmWindowCornerPreference.DoNotRound
                : (int)DwmApi.DwmWindowCornerPreference.Round;
            var cornerResult = DwmApi.DwmSetWindowAttribute(
                windowHandle,
                DwmApi.DwmWindowAttribute.WindowCornerPreference,
                ref cornerPreference,
                AttributeSize);

            var backdropType = (int)DwmApi.DwmSystemBackdropType.TransientWindow;
            var backdropResult = DwmApi.DwmSetWindowAttribute(
                windowHandle,
                DwmApi.DwmWindowAttribute.SystemBackdropType,
                ref backdropType,
                AttributeSize);

            return darkModeResult == 0 || cornerResult == 0 || backdropResult == 0;
        }
        catch (DllNotFoundException exception)
        {
            logger.LogDebug(exception, "DWM material APIs are unavailable; using XAML fallback material.");
            return false;
        }
        catch (EntryPointNotFoundException exception)
        {
            logger.LogDebug(exception, "DWM material attributes are unavailable; using XAML fallback material.");
            return false;
        }
    }

    public void Clear(nint windowHandle)
    {
        if (windowHandle == nint.Zero)
        {
            return;
        }

        try
        {
            var backdropType = (int)DwmApi.DwmSystemBackdropType.None;
            _ = DwmApi.DwmSetWindowAttribute(
                windowHandle,
                DwmApi.DwmWindowAttribute.SystemBackdropType,
                ref backdropType,
                AttributeSize);

            var cornerPreference = (int)DwmApi.DwmWindowCornerPreference.Default;
            _ = DwmApi.DwmSetWindowAttribute(
                windowHandle,
                DwmApi.DwmWindowAttribute.WindowCornerPreference,
                ref cornerPreference,
                AttributeSize);
        }
        catch (DllNotFoundException)
        {
            // The XAML fallback remains active on systems without DWM APIs.
        }
        catch (EntryPointNotFoundException)
        {
            // The XAML fallback remains active on older Windows builds.
        }
    }
}
