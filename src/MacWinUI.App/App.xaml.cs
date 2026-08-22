using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Globalization;
using MacWinUI.App.Lifecycle;
using MacWinUI.App.Theming;
using MacWinUI.App.ViewModels;
using MacWinUI.App.Views;
using MacWinUI.Core.Dock;
using MacWinUI.Core.Interfaces;
using MacWinUI.Windows.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MacWinUI.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private IAppearanceSettingsStore? _appearanceSettingsStore;
    private IAccessibilityPreferencesService? _accessibilityPreferencesService;
    private IDisplayChangeService? _displayChangeService;
    private SingleInstanceGuard? _singleInstanceGuard;
    private DockAppearanceSettings? _appearanceSettings;
    private DockThemeManager? _themeManager;
    private DockViewModel? _dockViewModel;
    private MenuBarViewModel? _menuBarViewModel;
    private ControlCenterViewModel? _controlCenterViewModel;
    private ControlCenterWindow? _controlCenterWindow;
    private DockWindow? _dockWindow;
    private MenuBarWindow? _menuBarWindow;
    private CancellationTokenSource? _settingsSaveCancellation;
    private Task? _settingsSaveTask;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RenderOptions.ProcessRenderMode = RenderMode.Default;
        ApplyLocalizationResources();

        _singleInstanceGuard = SingleInstanceGuard.Acquire("Local\\MacWinUI");
        if (!_singleInstanceGuard.IsPrimaryInstance)
        {
            Shutdown();
            return;
        }

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Information));
        services.AddMacWinUIWindows();
        services.AddSingleton<DockAppearanceSettings>();
        services.AddSingleton<DockMagnificationEngine>();
        services.AddSingleton<DockThemeManager>();
        services.AddSingleton<DockViewModel>();
        services.AddSingleton<DockWindow>();
        services.AddSingleton<ControlCenterViewModel>();
        services.AddSingleton<ControlCenterWindow>();
        services.AddSingleton<MenuBarViewModel>();
        services.AddSingleton<MenuBarWindow>();

        _serviceProvider = services.BuildServiceProvider(validateScopes: true);
        _appearanceSettings = _serviceProvider.GetRequiredService<DockAppearanceSettings>();
        _accessibilityPreferencesService = _serviceProvider.GetRequiredService<IAccessibilityPreferencesService>();
        _displayChangeService = _serviceProvider.GetRequiredService<IDisplayChangeService>();
        _appearanceSettingsStore = _serviceProvider.GetRequiredService<IAppearanceSettingsStore>();
        var savedAppearance = _appearanceSettingsStore
            .LoadAsync()
            .GetAwaiter()
            .GetResult();
        if (savedAppearance is not null)
        {
            _appearanceSettings.Apply(savedAppearance);
        }

        _themeManager = _serviceProvider.GetRequiredService<DockThemeManager>();
        _themeManager.Apply(
            Resources,
            _appearanceSettings.Theme,
            _accessibilityPreferencesService.GetCurrent().HighContrastEnabled);
        _themeManager.ApplyMaterialIntensity(
            Resources,
            _appearanceSettings.MaterialIntensity,
            _accessibilityPreferencesService.GetCurrent().HighContrastEnabled);
        _appearanceSettings.PropertyChanged += OnAppearanceSettingsChanged;

        _dockViewModel = _serviceProvider.GetRequiredService<DockViewModel>();
        _menuBarViewModel = _serviceProvider.GetRequiredService<MenuBarViewModel>();
        _controlCenterViewModel = _serviceProvider.GetRequiredService<ControlCenterViewModel>();
        _dockWindow = _serviceProvider.GetRequiredService<DockWindow>();
        _menuBarWindow = _serviceProvider.GetRequiredService<MenuBarWindow>();
        _controlCenterWindow = _serviceProvider.GetRequiredService<ControlCenterWindow>();
        _accessibilityPreferencesService.PreferencesChanged += OnAccessibilityPreferencesChanged;
        _displayChangeService.DisplayChanged += OnDisplayChanged;

        MainWindow = _dockWindow;
        _menuBarWindow.Show();
        _dockWindow.Show();
        _dockViewModel.Start(Dispatcher);
        _menuBarViewModel.Start(Dispatcher);
    }

    private void ApplyLocalizationResources()
    {
        var language = CultureInfo.CurrentUICulture.Name.StartsWith(
            "zh",
            StringComparison.OrdinalIgnoreCase)
            ? "zh-CN"
            : "en-US";
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri($"Resources/Strings.{language}.xaml", UriKind.Relative)
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        FlushAppearanceSettings();

        if (_accessibilityPreferencesService is not null)
        {
            _accessibilityPreferencesService.PreferencesChanged -= OnAccessibilityPreferencesChanged;
        }

        if (_displayChangeService is not null)
        {
            _displayChangeService.DisplayChanged -= OnDisplayChanged;
        }

        if (_appearanceSettings is not null)
        {
            _appearanceSettings.PropertyChanged -= OnAppearanceSettingsChanged;
        }

        if (_dockViewModel is not null)
        {
            _dockViewModel.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        if (_menuBarViewModel is not null)
        {
            _menuBarViewModel.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        if (_controlCenterViewModel is not null)
        {
            _controlCenterViewModel.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        _serviceProvider?.Dispose();
        _singleInstanceGuard?.Dispose();
        base.OnExit(e);
    }

    private void OnAppearanceSettingsChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        QueueAppearanceSettingsSave();

        if (e.PropertyName == nameof(DockAppearanceSettings.DisplayMode))
        {
            _dockWindow?.Reposition();
            _menuBarWindow?.Reposition();
            if (_controlCenterWindow?.IsVisible is true)
            {
                _controlCenterWindow.Reposition();
            }
        }


        if (e.PropertyName == nameof(DockAppearanceSettings.ReserveMenuBarSpace))
        {
            _menuBarWindow?.Reposition();
            _dockWindow?.Reposition();
            if (_controlCenterWindow?.IsVisible is true)
            {
                _controlCenterWindow.Reposition();
            }
        }

        if ((e.PropertyName == nameof(DockAppearanceSettings.Theme)
             || e.PropertyName == nameof(DockAppearanceSettings.EnableBlur)
             || e.PropertyName == nameof(DockAppearanceSettings.MaterialIntensity))
            && _appearanceSettings is not null
            && _themeManager is not null)
        {
            if (e.PropertyName == nameof(DockAppearanceSettings.Theme))
            {
                _themeManager.Apply(
                    Resources,
                    _appearanceSettings.Theme,
                    _accessibilityPreferencesService?.GetCurrent().HighContrastEnabled is true);
            }


            _themeManager.ApplyMaterialIntensity(
                Resources,
                _appearanceSettings.MaterialIntensity,
                _accessibilityPreferencesService?.GetCurrent().HighContrastEnabled is true);

            _dockWindow?.RefreshMaterial();
            _menuBarWindow?.RefreshMaterial();
            _controlCenterWindow?.RefreshMaterial();
        }
    }

    private void OnAccessibilityPreferencesChanged(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (_appearanceSettings is null
                || _themeManager is null
                || _accessibilityPreferencesService is null)
            {
                return;
            }

            _themeManager.Apply(
                Resources,
                _appearanceSettings.Theme,
                _accessibilityPreferencesService.GetCurrent().HighContrastEnabled);
            _themeManager.ApplyMaterialIntensity(
                Resources,
                _appearanceSettings.MaterialIntensity,
                _accessibilityPreferencesService.GetCurrent().HighContrastEnabled);
            _dockWindow?.RefreshMaterial();
            _menuBarWindow?.RefreshMaterial();
            _controlCenterWindow?.RefreshMaterial();
        });
    }

    private void OnDisplayChanged(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _dockWindow?.Reposition();
            _menuBarWindow?.Reposition();
            if (_controlCenterWindow?.IsVisible is true)
            {
                _controlCenterWindow.Reposition();
            }
        });
    }

    private void QueueAppearanceSettingsSave()
    {
        if (_appearanceSettings is null || _appearanceSettingsStore is null)
        {
            return;
        }

        _settingsSaveCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        _settingsSaveCancellation = cancellation;
        _settingsSaveTask = SaveAppearanceAfterDelayAsync(
            _appearanceSettings.CreateSnapshot(),
            cancellation,
            cancellation.Token);
    }

    private async Task SaveAppearanceAfterDelayAsync(
        DockAppearanceSnapshot snapshot,
        CancellationTokenSource cancellation,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(300, cancellationToken).ConfigureAwait(false);
            if (_appearanceSettingsStore is not null)
            {
                await _appearanceSettingsStore
                    .SaveAsync(snapshot, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A newer appearance value superseded this save.
        }
        finally
        {
            cancellation.Dispose();
            if (ReferenceEquals(_settingsSaveCancellation, cancellation))
            {
                _settingsSaveCancellation = null;
            }
        }
    }

    private void FlushAppearanceSettings()
    {
        _settingsSaveCancellation?.Cancel();
        if (_settingsSaveTask is not null)
        {
            try
            {
                _settingsSaveTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Expected while flushing the newest snapshot.
            }
        }

        if (_appearanceSettingsStore is not null && _appearanceSettings is not null)
        {
            _appearanceSettingsStore
                .SaveAsync(_appearanceSettings.CreateSnapshot())
                .GetAwaiter()
                .GetResult();
        }
    }
}
