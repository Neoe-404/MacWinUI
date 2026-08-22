using MacWinUI.Core.Interfaces;
using MacWinUI.Windows.Applications;
using MacWinUI.Windows.Accessibility;
using MacWinUI.Windows.Appearance;
using MacWinUI.Windows.Audio;
using MacWinUI.Windows.Display;
using MacWinUI.Windows.Icons;
using MacWinUI.Windows.Settings;
using MacWinUI.Windows.SystemStatus;
using MacWinUI.Windows.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace MacWinUI.Windows.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMacWinUIWindows(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IApplicationLauncher, WindowsApplicationLauncher>();
        services.AddSingleton<IActiveApplicationService, WindowsActiveApplicationService>();
        services.AddSingleton<IAccessibilityPreferencesService, WindowsAccessibilityPreferencesService>();
        services.AddSingleton<IAppearanceSettingsStore, WindowsAppearanceSettingsStore>();
        services.AddSingleton<IPinnedDockApplicationsStore, WindowsPinnedDockApplicationsStore>();
        services.AddSingleton<IAudioService, WindowsAudioService>();
        services.AddSingleton<IApplicationActivityService, WindowsApplicationActivityService>();
        services.AddSingleton<IDockItemProvider, WindowsDockItemProvider>();
        services.AddSingleton<IDisplayWorkAreaService, WindowsDisplayWorkAreaService>();
        services.AddSingleton<IScreenWorkAreaReservationService, WindowsScreenWorkAreaReservationService>();
        services.AddSingleton<ISettingsTransferService, WindowsSettingsTransferService>();
        services.AddSingleton<IDisplayChangeService, WindowsDisplayChangeService>();
        services.AddSingleton<IIconService, WindowsIconService>();
        services.AddSingleton<ISystemStatusService, WindowsSystemStatusService>();
        services.AddSingleton<ISystemSettingsLauncher, WindowsSystemSettingsLauncher>();
        services.AddSingleton<ISystemThemeService, WindowsSystemThemeService>();
        services.AddSingleton<IWindowMaterialService, WindowsWindowMaterialService>();
        return services;
    }
}
