using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using MacWinUI.App.ViewModels;
using MacWinUI.App.Lifecycle;
using MacWinUI.Core.Accessibility;
using MacWinUI.Core.Display;
using MacWinUI.Core.Dock;
using MacWinUI.Core.Interfaces;
using MacWinUI.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace MacWinUI.App.Views;

public partial class MenuBarWindow : Window
{
    private readonly ControlCenterWindow _controlCenterWindow;
    private readonly IAccessibilityPreferencesService _accessibilityPreferencesService;
    private readonly IDisplayWorkAreaService _displayWorkAreaService;
    private readonly DockAppearanceSettings _appearanceSettings;
    private readonly DockViewModel _dockViewModel;
    private readonly IApplicationLauncher _applicationLauncher;
    private readonly ISystemSettingsLauncher _systemSettingsLauncher;
    private readonly IScreenWorkAreaReservationService _screenReservationService;
    private readonly IWindowMaterialService _windowMaterialService;
    private readonly ILogger<MenuBarWindow> _logger;
    private HwndSource? _windowSource;
    private bool _reservationRefreshQueued;

    public MenuBarWindow(
        MenuBarViewModel viewModel,
        DockViewModel dockViewModel,
        ControlCenterWindow controlCenterWindow,
        IApplicationLauncher applicationLauncher,
        IAccessibilityPreferencesService accessibilityPreferencesService,
        DockAppearanceSettings appearanceSettings,
        IDisplayWorkAreaService displayWorkAreaService,
        IWindowMaterialService windowMaterialService,
        IScreenWorkAreaReservationService screenReservationService,
        ISystemSettingsLauncher systemSettingsLauncher,
        ILogger<MenuBarWindow> logger)
    {
        InitializeComponent();
        DataContext = viewModel;
        _controlCenterWindow = controlCenterWindow;
        _dockViewModel = dockViewModel;
        _applicationLauncher = applicationLauncher;
        _accessibilityPreferencesService = accessibilityPreferencesService;
        _appearanceSettings = appearanceSettings;
        _displayWorkAreaService = displayWorkAreaService;
        _windowMaterialService = windowMaterialService;
        _screenReservationService = screenReservationService;
        _systemSettingsLauncher = systemSettingsLauncher;
        _logger = logger;
        _controlCenterWindow.IsVisibleChanged += OnControlCenterVisibilityChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        _windowSource?.AddHook(WindowProcedure);
        RefreshMaterial();
    }

    public void RefreshMaterial()
    {
        _windowMaterialService.TryApply(
            new WindowInteropHelper(this).Handle,
            WindowMaterial.MenuBar,
            _appearanceSettings.Theme,
            AccessibilityBehavior.CanUseWindowMaterial(
                _appearanceSettings.EnableBlur,
                _accessibilityPreferencesService.GetCurrent()));
    }

    protected override void OnClosed(EventArgs e)
    {
        _controlCenterWindow.IsVisibleChanged -= OnControlCenterVisibilityChanged;
        _windowSource?.RemoveHook(WindowProcedure);
        _windowSource = null;
        var windowHandle = new WindowInteropHelper(this).Handle;
        _screenReservationService.Release(windowHandle);
        _windowMaterialService.Clear(windowHandle);
        base.OnClosed(e);
    }

    private nint WindowProcedure(
        nint windowHandle,
        int message,
        nint wordParameter,
        nint longParameter,
        ref bool handled)
    {
        var unsignedMessage = unchecked((uint)message);
        if (unsignedMessage == _screenReservationService.ShellRestartedMessage)
        {
            _screenReservationService.Invalidate(windowHandle);
            QueueReservationRefresh();
        }
        else if (unsignedMessage == _screenReservationService.PositionChangedMessage
                 && wordParameter.ToInt64() == 1)
        {
            QueueReservationRefresh();
        }

        return nint.Zero;
    }

    private void QueueReservationRefresh()
    {
        if (_reservationRefreshQueued)
        {
            return;
        }

        _reservationRefreshQueued = true;
        Dispatcher.BeginInvoke(
            () =>
            {
                _reservationRefreshQueued = false;
                Reposition();
            },
            System.Windows.Threading.DispatcherPriority.Background);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Reposition();
        UpdateViewMenuState();
    }

    private void OnDpiChanged(object sender, DpiChangedEventArgs e)
    {
        Reposition();
    }

    public void Reposition()
    {
        var windowHandle = new WindowInteropHelper(this).Handle;
        if (_appearanceSettings.ReserveMenuBarSpace
            && _screenReservationService.ReserveTop(
                windowHandle,
                _appearanceSettings.DisplayMode,
                Height))
        {
            return;
        }

        _screenReservationService.Release(windowHandle);
        var workArea = _appearanceSettings.DisplayMode is DockDisplayMode.Primary
            ? _displayWorkAreaService.GetPrimaryWorkArea()
            : _displayWorkAreaService.GetActiveWorkArea();
        var bounds = WindowPlacement.MenuBar(
            workArea,
            Height);
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
    }

    private async void OnControlCenterClick(object sender, RoutedEventArgs e)
    {
        await _controlCenterWindow.ToggleAsync();
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        var version = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "0.2.9";
        MessageBox.Show(
            this,
            $"MacWinUI {version}\n\nA safe, macOS-inspired Windows desktop enhancement.\nThe native Windows taskbar and Explorer remain unchanged.",
            "About MacWinUI",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void OnShowControlCenterClick(object sender, RoutedEventArgs e)
    {
        if (!_controlCenterWindow.IsVisible)
        {
            await _controlCenterWindow.ToggleAsync();
        }
        else
        {
            _controlCenterWindow.Activate();
        }
    }

    private void OnQuitClick(object sender, RoutedEventArgs e)
    {
        ApplicationExitCoordinator.ConfirmAndExit(this);
    }

    private async void OnOpenExplorerClick(object sender, RoutedEventArgs e)
    {
        await LaunchTargetAsync("explorer.exe", "File Explorer");
    }

    private async void OnAddDockItemClick(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog
        {
            Title = "Add applications or files to the Dock",
            Filter = "All files (*.*)|*.*|Windows applications (*.exe)|*.exe",
            CheckFileExists = true,
            Multiselect = true
        };

        if (picker.ShowDialog(this) is true)
        {
            await _dockViewModel.AddDroppedItemsAsync(picker.FileNames);
        }
    }

    private async void OnOpenSoundSettingsClick(object sender, RoutedEventArgs e)
    {
        await _systemSettingsLauncher.OpenAsync(SystemSettingsPage.Sound);
    }

    private void OnViewMenuOpened(object sender, RoutedEventArgs e)
    {
        UpdateViewMenuState();
    }

    private void OnThemeClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: DockTheme theme })
        {
            _appearanceSettings.Theme = theme;
            UpdateViewMenuState();
        }
    }

    private void OnToggleMagnificationClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            _appearanceSettings.EnableMagnification = menuItem.IsChecked;
        }
    }

    private async void OnOpenFolderClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string folderKey })
        {
            return;
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var folderPath = folderKey switch
        {
            "Desktop" => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "Downloads" => Path.Combine(userProfile, "Downloads"),
            _ => userProfile
        };
        await LaunchTargetAsync(folderPath, folderKey);
    }

    private void OnShowDockClick(object sender, RoutedEventArgs e)
    {
        var dockWindow = Application.Current.Windows
            .OfType<DockWindow>()
            .FirstOrDefault();
        if (dockWindow is null)
        {
            return;
        }

        if (!dockWindow.IsVisible)
        {
            dockWindow.Show();
        }

        dockWindow.Reposition();
    }

    private void OnRepositionWindowsClick(object sender, RoutedEventArgs e)
    {
        Reposition();
        foreach (var window in Application.Current.Windows.OfType<Window>())
        {
            switch (window)
            {
                case DockWindow dockWindow:
                    dockWindow.Reposition();
                    break;
                case ControlCenterWindow controlCenterWindow when controlCenterWindow.IsVisible:
                    controlCenterWindow.Reposition();
                    break;
            }
        }
    }

    private void OnShowTipsClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            this,
            "• Drag an application or file onto an empty Dock area to pin it.\n" +
            "• Drag files onto a compatible application icon to open them.\n" +
            "• Open Control Center to remove custom items or adjust appearance.\n" +
            "• Press Escape to close Control Center.",
            "Dock Drag & Drop Tips",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OnShowDiagnosticsClick(object sender, RoutedEventArgs e)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var workArea = _appearanceSettings.DisplayMode is DockDisplayMode.Primary
            ? _displayWorkAreaService.GetPrimaryWorkArea()
            : _displayWorkAreaService.GetActiveWorkArea();
        var renderingTier = RenderCapability.Tier >> 16;
        MessageBox.Show(
            this,
            $"Version: {typeof(App).Assembly.GetName().Version?.ToString(3)}\n" +
            $"Rendering tier: {renderingTier} (2 = hardware accelerated)\n" +
            $"DPI scale: {dpi.DpiScaleX:0.##} × {dpi.DpiScaleY:0.##}\n" +
            $"Work area: {workArea.Width:0} × {workArea.Height:0} at {workArea.Left:0},{workArea.Top:0}\n" +
            $"MenuBar reservation: {_screenReservationService.HasActiveReservation}\n" +
            $"Display mode: {_appearanceSettings.DisplayMode}\n" +
            $"Reduce motion: {_dockViewModel.EffectiveReduceMotion}",
            "MacWinUI Runtime Diagnostics",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async Task LaunchTargetAsync(string target, string displayName)
    {
        try
        {
            await _applicationLauncher.LaunchAsync(new DockItem
            {
                Id = $"menu-{displayName.ToLowerInvariant().Replace(' ', '-')}",
                DisplayName = displayName,
                LaunchType = LaunchType.Shell,
                LaunchTarget = target
            });
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Could not launch menu target {MenuTarget}.",
                target);
        }
    }

    private void UpdateViewMenuState()
    {
        BigSurThemeMenuItem.IsChecked = _appearanceSettings.Theme is DockTheme.BigSur;
        AutoThemeMenuItem.IsChecked = _appearanceSettings.Theme is DockTheme.Auto;
        LightThemeMenuItem.IsChecked = _appearanceSettings.Theme is DockTheme.Light;
        DarkThemeMenuItem.IsChecked = _appearanceSettings.Theme is DockTheme.Dark;
        MagnificationMenuItem.IsChecked = _appearanceSettings.EnableMagnification;
    }

    private void OnControlCenterVisibilityChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (_controlCenterWindow.IsVisible)
        {
            ControlCenterButton.SetResourceReference(
                Control.BackgroundProperty,
                "MenuBarHoverBrush");
        }
        else
        {
            ControlCenterButton.ClearValue(Control.BackgroundProperty);
        }
    }
}
