using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MacWinUI.App.Controls;
using MacWinUI.App.Lifecycle;
using MacWinUI.App.ViewModels;
using MacWinUI.Core.Accessibility;
using MacWinUI.Core.Display;
using MacWinUI.Core.Dock;
using MacWinUI.Core.Interfaces;
using MacWinUI.Core.Models;
using Microsoft.Win32;

namespace MacWinUI.App.Views;

public partial class DockWindow : Window
{
    private const double BottomMargin = 12;
    private readonly IDisplayWorkAreaService _displayWorkAreaService;
    private readonly IAccessibilityPreferencesService _accessibilityPreferencesService;
    private readonly DockMagnificationEngine _magnificationEngine;
    private readonly IWindowMaterialService _windowMaterialService;
    private readonly DockViewModel _viewModel;
    private readonly ControlCenterWindow _controlCenterWindow;
    private readonly DispatcherTimer _autoHideTimer;
    private TimeSpan? _lastRenderingTime;
    private bool _isRendering;
    private bool _isAutoHidden;
    private bool _contextMenuOpen;

    public DockWindow(
        DockViewModel viewModel,
        ControlCenterWindow controlCenterWindow,
        DockMagnificationEngine magnificationEngine,
        IAccessibilityPreferencesService accessibilityPreferencesService,
        IDisplayWorkAreaService displayWorkAreaService,
        IWindowMaterialService windowMaterialService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _controlCenterWindow = controlCenterWindow;
        _magnificationEngine = magnificationEngine;
        _accessibilityPreferencesService = accessibilityPreferencesService;
        _displayWorkAreaService = displayWorkAreaService;
        _windowMaterialService = windowMaterialService;
        _autoHideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(650)
        };
        _autoHideTimer.Tick += OnAutoHideTimerTick;
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
            WindowMaterial.Dock,
            _viewModel.Appearance.Theme,
            AccessibilityBehavior.CanUseWindowMaterial(
                _viewModel.Appearance.EnableBlur,
                _accessibilityPreferencesService.GetCurrent()));
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.Appearance.PropertyChanged += OnAppearancePropertyChanged;
        Reposition();
        if (_viewModel.EffectiveReduceMotion)
        {
            DockRoot.Opacity = 1;
            DockTranslate.Y = 0;
        }
        else
        {
            BeginStoryboard(
                (Storyboard)FindResource("DockAppearStoryboard"),
                HandoffBehavior.SnapshotAndReplace,
                true);
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsLoaded)
        {
            Reposition();
        }
    }

    private void OnDpiChanged(object sender, DpiChangedEventArgs e)
    {
        Reposition();
    }

    protected override void OnClosed(EventArgs e)
    {
        _autoHideTimer.Stop();
        _autoHideTimer.Tick -= OnAutoHideTimerTick;
        _viewModel.Appearance.PropertyChanged -= OnAppearancePropertyChanged;
        _windowMaterialService.Clear(new WindowInteropHelper(this).Handle);
        CompositionTarget.Rendering -= OnRendering;
        _isRendering = false;
        base.OnClosed(e);
    }

    public void Reposition()
    {
        var bounds = WindowPlacement.Dock(
            GetSelectedWorkArea(),
            ActualWidth,
            ActualHeight,
            BottomMargin);
        Left = bounds.Left;
        Top = _isAutoHidden
            ? GetSelectedWorkArea().Bottom - 5
            : bounds.Top;
    }

    private MacWinUI.Core.Display.DisplayWorkArea GetSelectedWorkArea() =>
        _viewModel.Appearance.DisplayMode is DockDisplayMode.Primary
            ? _displayWorkAreaService.GetPrimaryWorkArea()
            : _displayWorkAreaService.GetActiveWorkArea();

    private void OnDockMouseMove(object sender, MouseEventArgs e)
    {
        if (_viewModel.EffectiveReduceMotion)
        {
            ResetImmediately();
            return;
        }

        if (!_viewModel.Appearance.EnableMagnification)
        {
            ResetTargets();
            StartRendering();
            return;
        }

        var mouseX = e.GetPosition(DockSurface).X;
        var sigma = _viewModel.Appearance.IconSize * 1.35;
        foreach (var itemControl in FindVisualChildren<DockItemControl>(DockSurface))
        {
            itemControl.TargetScale = _magnificationEngine.CalculateScale(
                mouseX,
                itemControl.GetCenterX(DockSurface),
                _viewModel.Appearance.MaxScaleBoost,
                sigma);
        }

        StartRendering();
    }

    private void OnDockMouseLeave(object sender, MouseEventArgs e)
    {
        if (_viewModel.EffectiveReduceMotion)
        {
            ResetImmediately();
            return;
        }

        ResetTargets();
        StartRendering();
    }

    private void OnDockWindowMouseEnter(object sender, MouseEventArgs e)
    {
        _autoHideTimer.Stop();
        if (_isAutoHidden)
        {
            ShowFromAutoHide();
        }
    }

    private void OnDockWindowMouseLeave(object sender, MouseEventArgs e)
    {
        if (_viewModel.Appearance.AutoHideDock && !_contextMenuOpen)
        {
            _autoHideTimer.Stop();
            _autoHideTimer.Start();
        }
    }

    private void OnAutoHideTimerTick(object? sender, EventArgs e)
    {
        _autoHideTimer.Stop();
        if (_viewModel.Appearance.AutoHideDock
            && !_contextMenuOpen
            && !IsMouseOver)
        {
            HideForAutoHide();
        }
    }

    private void HideForAutoHide()
    {
        if (_isAutoHidden)
        {
            return;
        }

        if (_viewModel.EffectiveReduceMotion)
        {
            DockRoot.Opacity = 0.12;
            DockTranslate.Y = 8;
            _isAutoHidden = true;
            Reposition();
            return;
        }

        var fade = new DoubleAnimation(0.12, TimeSpan.FromMilliseconds(150));
        var slide = new DoubleAnimation(8, TimeSpan.FromMilliseconds(150));
        slide.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
        fade.Completed += (_, _) =>
        {
            _isAutoHidden = true;
            Reposition();
        };
        DockRoot.BeginAnimation(OpacityProperty, fade);
        DockTranslate.BeginAnimation(TranslateTransform.YProperty, slide);
    }

    private void ShowFromAutoHide()
    {
        _isAutoHidden = false;
        Reposition();
        if (_viewModel.EffectiveReduceMotion)
        {
            DockRoot.Opacity = 1;
            DockTranslate.Y = 0;
            return;
        }

        var fade = new DoubleAnimation(1, TimeSpan.FromMilliseconds(180));
        var slide = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        DockRoot.BeginAnimation(OpacityProperty, fade);
        DockTranslate.BeginAnimation(TranslateTransform.YProperty, slide);
    }

    private void OnAppearancePropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DockAppearanceSettings.AutoHideDock)
            && !_viewModel.Appearance.AutoHideDock)
        {
            _autoHideTimer.Stop();
            ShowFromAutoHide();
        }
    }

    private void OnDockDragOver(object sender, DragEventArgs e)
    {
        if (!TryGetFilePaths(e.Data, out _))
        {
            e.Effects = DragDropEffects.None;
            DockDropHighlight.Visibility = Visibility.Collapsed;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Copy;
        DockDropHighlight.Visibility = Visibility.Visible;
        e.Handled = true;
    }

    private void OnDockDragLeave(object sender, DragEventArgs e)
    {
        DockDropHighlight.Visibility = Visibility.Collapsed;
    }

    private async void OnDockDrop(object sender, DragEventArgs e)
    {
        DockDropHighlight.Visibility = Visibility.Collapsed;
        if (!TryGetFilePaths(e.Data, out var paths))
        {
            return;
        }

        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
        await _viewModel.AddDroppedItemsAsync(paths);
    }

    private async void OnDockItemFilesDropped(
        object? sender,
        DockFilesDroppedEventArgs e)
    {
        DockDropHighlight.Visibility = Visibility.Collapsed;
        await _viewModel.OpenDroppedFilesAsync(e.Application, e.FilePaths);
    }

    private async void OnDockItemInvoked(
        object? sender,
        DockItemInteractionEventArgs e)
    {
        await _viewModel.LaunchItemAsync(e.Item);
    }

    private async void OnDockItemReorderRequested(
        object? sender,
        DockItemReorderEventArgs e)
    {
        await _viewModel.MoveDockItemAsync(e.SourceItemId, e.TargetItem);
    }

    private async void OnDockItemDraggedOutside(
        object? sender,
        DockItemInteractionEventArgs e)
    {
        await _viewModel.RemoveDockItemAsync(e.Item);
    }

    private async void OnDockItemContextRequested(
        object? sender,
        DockItemContextRequestedEventArgs e)
    {
        if (sender is not FrameworkElement placementTarget)
        {
            return;
        }

        ResetTargets();
        StartRendering();

        var contextMenu = CreateContextMenu();
        contextMenu.Items.Add(CreateMenuItem(
            Localized("String.Dock.Open"),
            async () => await _viewModel.LaunchItemAsync(e.Item)));

        var openWindows = await _viewModel.GetOpenWindowsAsync(e.Item);
        foreach (var window in openWindows.Take(6))
        {
            var title = window.Title.Length > 46
                ? $"{window.Title[..43]}…"
                : window.Title;
            contextMenu.Items.Add(CreateMenuItem(
                $"↗ {title}",
                async () => await _viewModel.ActivateWindowAsync(window.WindowHandle)));
        }

        var showInExplorer = CreateMenuItem(
            Localized("String.Dock.ShowExplorer"),
            async () => await _viewModel.OpenContainingFolderAsync(e.Item));
        showInExplorer.IsEnabled = IsLocalPath(e.Item.IconSourcePath ?? e.Item.LaunchTarget);
        contextMenu.Items.Add(showInExplorer);

        contextMenu.Items.Add(CreateMenuItem(
            Localized("String.Dock.Remove"),
            async () => await _viewModel.RemoveDockItemAsync(e.Item)));

        contextMenu.Items.Add(CreateMenuItem(
            Localized("String.Dock.Settings"),
            ShowControlCenterAsync));
        OpenContextMenu(contextMenu, placementTarget);
    }

    private void OnDockBackgroundRightClick(object sender, MouseButtonEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        e.Handled = true;
        ResetTargets();
        StartRendering();

        var contextMenu = CreateContextMenu();
        contextMenu.Items.Add(CreateMenuItem(
            Localized("String.Dock.Add"),
            PickAndAddDockItemsAsync));
        contextMenu.Items.Add(CreateMenuItem(
            Localized("String.Dock.Settings"),
            ShowControlCenterAsync));

        var magnificationItem = CreateMenuItem(
            Localized("String.Dock.Magnification"),
            () =>
            {
                _viewModel.Appearance.EnableMagnification =
                    !_viewModel.Appearance.EnableMagnification;
                return Task.CompletedTask;
            });
        magnificationItem.IsCheckable = true;
        magnificationItem.IsChecked = _viewModel.Appearance.EnableMagnification;
        contextMenu.Items.Add(magnificationItem);

        var autoHideItem = CreateMenuItem(
            Localized("String.Dock.AutoHide"),
            () =>
            {
                _viewModel.Appearance.AutoHideDock =
                    !_viewModel.Appearance.AutoHideDock;
                return Task.CompletedTask;
            });
        autoHideItem.IsCheckable = true;
        autoHideItem.IsChecked = _viewModel.Appearance.AutoHideDock;
        contextMenu.Items.Add(autoHideItem);

        contextMenu.Items.Add(CreateMenuItem(
            Localized("String.Dock.Reposition"),
            () =>
            {
                Reposition();
                return Task.CompletedTask;
            }));

        var restoreDefaultsItem = CreateMenuItem(
            Localized("String.Dock.Restore"),
            async () => await _viewModel.RestoreDefaultItemsAsync());
        restoreDefaultsItem.IsEnabled = _viewModel.HasHiddenDefaultItems;
        contextMenu.Items.Add(restoreDefaultsItem);
        contextMenu.Items.Add(CreateMenuItem(
            Localized("String.Dock.Quit"),
            () =>
            {
                ApplicationExitCoordinator.ConfirmAndExit(this);
                return Task.CompletedTask;
            }));
        OpenContextMenu(contextMenu, DockRoot);
    }

    private ContextMenu CreateContextMenu()
    {
        return new ContextMenu
        {
            Style = (Style)FindResource("DockContextMenuStyle")
        };
    }

    private string Localized(string resourceKey) =>
        TryFindResource(resourceKey) as string ?? resourceKey;

    private MenuItem CreateMenuItem(string header, Func<Task> action)
    {
        var menuItem = new MenuItem
        {
            Header = header,
            Style = (Style)FindResource("DockContextMenuItemStyle")
        };
        menuItem.Click += async (_, _) => await action();
        return menuItem;
    }

    private void OpenContextMenu(
        ContextMenu contextMenu,
        FrameworkElement placementTarget)
    {
        _autoHideTimer.Stop();
        _contextMenuOpen = true;
        contextMenu.Closed += (_, _) =>
        {
            _contextMenuOpen = false;
            if (_viewModel.Appearance.AutoHideDock && !IsMouseOver)
            {
                _autoHideTimer.Start();
            }
        };
        contextMenu.PlacementTarget = placementTarget;
        contextMenu.IsOpen = true;
    }

    private async Task PickAndAddDockItemsAsync()
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
            await _viewModel.AddDroppedItemsAsync(picker.FileNames);
        }
    }

    private async Task ShowControlCenterAsync()
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

    private static bool IsLocalPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            return Path.IsPathFullyQualified(path);
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or NotSupportedException)
        {
            return false;
        }
    }

    private static bool TryGetFilePaths(
        IDataObject data,
        out IReadOnlyList<string> paths)
    {
        if (data.GetDataPresent(DataFormats.FileDrop)
            && data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } filePaths)
        {
            paths = filePaths;
            return true;
        }

        paths = [];
        return false;
    }

    private void ResetTargets()
    {
        foreach (var itemControl in FindVisualChildren<DockItemControl>(DockSurface))
        {
            itemControl.TargetScale = 1;
        }
    }

    private void ResetImmediately()
    {
        foreach (var itemControl in FindVisualChildren<DockItemControl>(DockSurface))
        {
            itemControl.ResetScaleImmediately();
        }

        if (_isRendering)
        {
            CompositionTarget.Rendering -= OnRendering;
            _isRendering = false;
            _lastRenderingTime = null;
        }
    }

    private void StartRendering()
    {
        if (_isRendering)
        {
            return;
        }

        _lastRenderingTime = null;
        CompositionTarget.Rendering += OnRendering;
        _isRendering = true;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var renderingTime = e is RenderingEventArgs renderingEventArgs
            ? renderingEventArgs.RenderingTime
            : TimeSpan.Zero;
        var elapsedSeconds = _lastRenderingTime is { } lastRenderingTime
            ? (renderingTime - lastRenderingTime).TotalSeconds
            : 1d / 60d;
        _lastRenderingTime = renderingTime;

        var animationActive = false;
        foreach (var itemControl in FindVisualChildren<DockItemControl>(DockSurface))
        {
            animationActive |= itemControl.AdvanceAnimation(elapsedSeconds);
        }

        if (!animationActive)
        {
            CompositionTarget.Rendering -= OnRendering;
            _isRendering = false;
            _lastRenderingTime = null;
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
