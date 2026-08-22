using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using MacWinUI.App.ViewModels;
using MacWinUI.App.Lifecycle;
using MacWinUI.Core.Accessibility;
using MacWinUI.Core.Display;
using MacWinUI.Core.Dock;
using MacWinUI.Core.Interfaces;
using MacWinUI.Core.Models;

namespace MacWinUI.App.Views;

public partial class ControlCenterWindow : Window
{
    private static readonly TimeSpan ToggleDebounce = TimeSpan.FromMilliseconds(250);
    private readonly DockAppearanceSettings _appearanceSettings;
    private readonly IAccessibilityPreferencesService _accessibilityPreferencesService;
    private readonly IDisplayWorkAreaService _displayWorkAreaService;
    private readonly ControlCenterViewModel _viewModel;
    private readonly DockViewModel _dockViewModel;
    private readonly ISettingsTransferService _settingsTransferService;
    private readonly IWindowMaterialService _windowMaterialService;
    private readonly IScreenWorkAreaReservationService _screenReservationService;
    private DateTimeOffset _lastDeactivatedHide = DateTimeOffset.MinValue;
    private bool _suppressDeactivation;

    public ControlCenterWindow(
        ControlCenterViewModel viewModel,
        DockViewModel dockViewModel,
        IAccessibilityPreferencesService accessibilityPreferencesService,
        DockAppearanceSettings appearanceSettings,
        IDisplayWorkAreaService displayWorkAreaService,
        IScreenWorkAreaReservationService screenReservationService,
        ISettingsTransferService settingsTransferService,
        IWindowMaterialService windowMaterialService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _dockViewModel = dockViewModel;
        _accessibilityPreferencesService = accessibilityPreferencesService;
        _appearanceSettings = appearanceSettings;
        _displayWorkAreaService = displayWorkAreaService;
        _screenReservationService = screenReservationService;
        _settingsTransferService = settingsTransferService;
        _windowMaterialService = windowMaterialService;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        RefreshMaterial();
    }

    public void RefreshMaterial()
    {
        _windowMaterialService.TryApply(
            new WindowInteropHelper(this).Handle,
            WindowMaterial.ControlCenter,
            _appearanceSettings.Theme,
            AccessibilityBehavior.CanUseWindowMaterial(
                _appearanceSettings.EnableBlur,
                _accessibilityPreferencesService.GetCurrent()));
    }

    protected override void OnClosed(EventArgs e)
    {
        _windowMaterialService.Clear(new WindowInteropHelper(this).Handle);
        base.OnClosed(e);
    }

    public async Task ToggleAsync()
    {
        if (IsVisible)
        {
            Hide();
            return;
        }

        if (DateTimeOffset.UtcNow - _lastDeactivatedHide < ToggleDebounce)
        {
            return;
        }

        await _viewModel.RefreshAsync();
        ResetConfirmation.Visibility = Visibility.Collapsed;
        Reposition();
        Show();
        Activate();
        Focus();
        if (ShouldReduceMotion())
        {
            ControlCenterRoot.Opacity = 1;
            ControlCenterTranslate.Y = 0;
        }
        else
        {
            BeginStoryboard(
                (Storyboard)FindResource("ControlCenterAppearStoryboard"),
                HandoffBehavior.SnapshotAndReplace,
                true);
        }

        ControlCenterScrollViewer.MoveFocus(
            new TraversalRequest(FocusNavigationDirection.First));
    }

    public void Reposition()
    {
        var workArea = _appearanceSettings.DisplayMode is DockDisplayMode.Primary
            ? _displayWorkAreaService.GetPrimaryWorkArea()
            : _displayWorkAreaService.GetActiveWorkArea();
        MaxHeight = Math.Max(200, Math.Min(720, workArea.Height - 8));
        ControlCenterScrollViewer.MaxHeight = Math.Max(140, MaxHeight - 54);
        Measure(new Size(Width, double.PositiveInfinity));
        var measuredHeight = Math.Max(1, DesiredSize.Height);
        var bounds = WindowPlacement.TopRight(
            workArea,
            Width,
            Math.Min(measuredHeight, MaxHeight),
            _screenReservationService.HasActiveReservation ? 0 : 30,
            12);
        Left = bounds.Left;
        Top = bounds.Top;
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (!IsVisible || _suppressDeactivation)
        {
            return;
        }

        _lastDeactivatedHide = DateTimeOffset.UtcNow;
        ResetConfirmation.Visibility = Visibility.Collapsed;
        Hide();
    }

    private void OnDpiChanged(object sender, DpiChangedEventArgs e)
    {
        Reposition();
    }

    private bool ShouldReduceMotion() => AccessibilityBehavior.ShouldReduceMotion(
        _appearanceSettings.ReduceMotion,
        _accessibilityPreferencesService.GetCurrent());

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Escape)
        {
            return;
        }

        if (ResetConfirmation.Visibility == Visibility.Visible)
        {
            ResetConfirmation.Visibility = Visibility.Collapsed;
            Reposition();
            e.Handled = true;
            return;
        }

        Hide();
        e.Handled = true;
    }

    private void OnResetSettingsClick(object sender, RoutedEventArgs e)
    {
        ResetConfirmation.Visibility = Visibility.Visible;
        Reposition();
    }

    private void OnCancelResetClick(object sender, RoutedEventArgs e)
    {
        ResetConfirmation.Visibility = Visibility.Collapsed;
        Reposition();
    }

    private void OnConfirmResetClick(object sender, RoutedEventArgs e)
    {
        _appearanceSettings.Reset();
        _viewModel.NotifyAppearancePreferencesChanged();
        ResetConfirmation.Visibility = Visibility.Collapsed;
        Reposition();
    }

    private async void OnAddDockApplicationClick(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog
        {
            Title = "Add an application or file to the Dock",
            Filter = "All files (*.*)|*.*|Windows applications (*.exe)|*.exe",
            CheckFileExists = true,
            Multiselect = false
        };

        _suppressDeactivation = true;
        try
        {
            if (picker.ShowDialog(this) is true)
            {
                await _viewModel.AddDockApplicationAsync(picker.FileName);
                Reposition();
            }
        }
        finally
        {
            _suppressDeactivation = false;
            if (IsVisible)
            {
                Activate();
            }
        }
    }

    private async void OnExportSettingsClick(object sender, RoutedEventArgs e)
    {
        var picker = new SaveFileDialog
        {
            Title = "Export MacWinUI settings",
            Filter = "MacWinUI settings (*.macwinui.json)|*.macwinui.json",
            DefaultExt = ".macwinui.json",
            FileName = "MacWinUI-settings.macwinui.json",
            AddExtension = true
        };
        if (picker.ShowDialog(this) is not true)
        {
            return;
        }

        var bundle = new MacWinUISettingsBundle
        {
            Appearance = _appearanceSettings.CreateSnapshot(),
            DockItems = await _dockViewModel.CreatePinnedSnapshotAsync()
        };
        await _settingsTransferService.ExportAsync(picker.FileName, bundle);
    }

    private async void OnImportSettingsClick(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog
        {
            Title = "Import MacWinUI settings",
            Filter = "MacWinUI settings (*.macwinui.json)|*.macwinui.json|JSON files (*.json)|*.json",
            CheckFileExists = true,
            Multiselect = false
        };
        if (picker.ShowDialog(this) is not true)
        {
            return;
        }

        var bundle = await _settingsTransferService.ImportAsync(picker.FileName);
        if (bundle is null)
        {
            MessageBox.Show(
                this,
                "The selected settings file is unsupported or invalid.",
                "MacWinUI Settings",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _appearanceSettings.Apply(bundle.Appearance);
        _viewModel.NotifyAppearancePreferencesChanged();
        await _dockViewModel.ApplyPinnedSnapshotAsync(bundle.DockItems);
        Reposition();
    }

    private void OnQuitApplicationClick(object sender, RoutedEventArgs e)
    {
        ApplicationExitCoordinator.ConfirmAndExit(this);
    }
}
