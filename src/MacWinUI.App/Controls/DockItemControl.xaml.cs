using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MacWinUI.Core.Models;

namespace MacWinUI.App.Controls;

public partial class DockItemControl : UserControl
{
    public const string DockItemDataFormat = "MacWinUI.DockItemId";

    public event EventHandler<DockFilesDroppedEventArgs>? FilesDropped;

    public event EventHandler<DockItemContextRequestedEventArgs>? ContextRequested;

    public event EventHandler<DockItemInteractionEventArgs>? ItemInvoked;

    public event EventHandler<DockItemInteractionEventArgs>? DraggedOutside;

    public event EventHandler<DockItemReorderEventArgs>? ReorderRequested;

    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
        nameof(IconSize),
        typeof(double),
        typeof(DockItemControl),
        new PropertyMetadata(48d, OnIconSizeChanged));

    public static readonly DependencyProperty ShowRunningIndicatorsProperty =
        DependencyProperty.Register(
            nameof(ShowRunningIndicators),
            typeof(bool),
            typeof(DockItemControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowActiveIndicatorProperty =
        DependencyProperty.Register(
            nameof(ShowActiveIndicator),
            typeof(bool),
            typeof(DockItemControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ReduceMotionProperty =
        DependencyProperty.Register(
            nameof(ReduceMotion),
            typeof(bool),
            typeof(DockItemControl),
            new PropertyMetadata(false, OnReduceMotionChanged));

    private const double AnimationResponsiveness = 18;
    private double _currentScale = 1;
    private Point _dragStart;
    private bool _dragCancelled;
    private bool _isDragging;

    public DockItemControl()
    {
        InitializeComponent();
        UpdateSlotSize(IconSize);
    }

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public bool ShowRunningIndicators
    {
        get => (bool)GetValue(ShowRunningIndicatorsProperty);
        set => SetValue(ShowRunningIndicatorsProperty, value);
    }

    public bool ShowActiveIndicator
    {
        get => (bool)GetValue(ShowActiveIndicatorProperty);
        set => SetValue(ShowActiveIndicatorProperty, value);
    }

    public bool ReduceMotion
    {
        get => (bool)GetValue(ReduceMotionProperty);
        set => SetValue(ReduceMotionProperty, value);
    }

    public double TargetScale { get; set; } = 1;

    public double GetCenterX(UIElement relativeTo)
    {
        return TranslatePoint(new Point(ActualWidth / 2, ActualHeight / 2), relativeTo).X;
    }

    public bool AdvanceAnimation(double elapsedSeconds)
    {
        var difference = TargetScale - _currentScale;
        if (Math.Abs(difference) < 0.001)
        {
            _currentScale = TargetScale;
            ApplyTransform();
            return false;
        }

        var boundedElapsed = Math.Clamp(elapsedSeconds, 1d / 240d, 0.05);
        var interpolation = 1 - Math.Exp(-AnimationResponsiveness * boundedElapsed);
        _currentScale += difference * interpolation;
        ApplyTransform();
        return true;
    }

    public void ResetScaleImmediately()
    {
        TargetScale = 1;
        _currentScale = 1;
        ApplyTransform();
    }

    private static void OnIconSizeChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is DockItemControl control
            && eventArgs.NewValue is double iconSize)
        {
            control.UpdateSlotSize(iconSize);
        }
    }

    private static void OnReduceMotionChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is DockItemControl control
            && eventArgs.NewValue is true)
        {
            control.ResetScaleImmediately();
            control.ClickTranslate.BeginAnimation(
                System.Windows.Media.TranslateTransform.YProperty,
                null);
            control.ClickTranslate.Y = 0;
        }
    }

    private void OnItemClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DockItem item || _isDragging)
        {
            return;
        }

        if (ReduceMotion)
        {
            ClickTranslate.Y = 0;
        }
        else
        {
            var storyboard = (Storyboard)FindResource("DockItemBounceStoryboard");
            storyboard.Begin(this, HandoffBehavior.SnapshotAndReplace, true);
        }

        ItemInvoked?.Invoke(this, new DockItemInteractionEventArgs(item));
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _dragCancelled = false;
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging
            || e.LeftButton is not MouseButtonState.Pressed
            || DataContext is not DockItem item)
        {
            return;
        }

        var current = e.GetPosition(this);
        if (Math.Abs(current.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(current.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        _isDragging = true;
        _dragCancelled = false;
        try
        {
            var data = new DataObject();
            data.SetData(DockItemDataFormat, item.Id);
            var result = DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
            if (result is DragDropEffects.None && !_dragCancelled)
            {
                DraggedOutside?.Invoke(this, new DockItemInteractionEventArgs(item));
            }
        }
        finally
        {
            _isDragging = false;
        }
    }

    private void OnQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
    {
        if (e.EscapePressed)
        {
            _dragCancelled = true;
        }
    }

    private void OnContextRequested(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not DockItem item)
        {
            return;
        }

        e.Handled = true;
        ContextRequested?.Invoke(this, new DockItemContextRequestedEventArgs(item));
    }

    private void OnFileDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DockItemDataFormat))
        {
            e.Effects = DragDropEffects.Move;
            FileDropHighlight.Visibility = Visibility.Visible;
            e.Handled = true;
            return;
        }

        if (DataContext is not DockItem { AcceptsFileDrops: true }
            || !TryGetFilePaths(e.Data, out _))
        {
            FileDropHighlight.Visibility = Visibility.Collapsed;
            return;
        }

        e.Effects = DragDropEffects.Link;
        FileDropHighlight.Visibility = Visibility.Visible;
        e.Handled = true;
    }

    private void OnFileDragLeave(object sender, DragEventArgs e)
    {
        FileDropHighlight.Visibility = Visibility.Collapsed;
    }

    private void OnFileDrop(object sender, DragEventArgs e)
    {
        FileDropHighlight.Visibility = Visibility.Collapsed;
        if (DataContext is DockItem targetItem
            && e.Data.GetData(DockItemDataFormat) is string sourceItemId)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
            ReorderRequested?.Invoke(
                this,
                new DockItemReorderEventArgs(sourceItemId, targetItem));
            return;
        }

        if (DataContext is not DockItem { AcceptsFileDrops: true } application
            || !TryGetFilePaths(e.Data, out var filePaths))
        {
            return;
        }

        e.Effects = DragDropEffects.Link;
        e.Handled = true;
        FilesDropped?.Invoke(
            this,
            new DockFilesDroppedEventArgs(application, filePaths));
    }

    private static bool TryGetFilePaths(
        IDataObject data,
        out IReadOnlyList<string> filePaths)
    {
        if (data.GetDataPresent(DataFormats.FileDrop)
            && data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths)
        {
            filePaths = paths;
            return true;
        }

        filePaths = [];
        return false;
    }

    private void UpdateSlotSize(double iconSize)
    {
        Width = iconSize + 16;
        Height = iconSize + 16;
    }

    private void ApplyTransform()
    {
        IconScale.ScaleX = _currentScale;
        IconScale.ScaleY = _currentScale;
        IconTranslate.Y = -((_currentScale - 1) * 4);
    }
}
