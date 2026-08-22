using MacWinUI.Core.Utilities;

namespace MacWinUI.Core.Dock;

public sealed class DockAppearanceSettings : ObservableObject
{
    private const double DefaultCornerRadius = 22;
    private const double DefaultIconSize = 48;
    private const double DefaultMaxScale = 1.5;
    private const double DefaultMaterialIntensity = 0.85;
    private const double DefaultOpacity = 0.82;

    private double _cornerRadius = DefaultCornerRadius;
    private bool _enableBlur = true;
    private bool _autoHideDock;
    private bool _enableMagnification = true;
    private double _iconSize = DefaultIconSize;
    private double _maxScale = DefaultMaxScale;
    private double _materialIntensity = DefaultMaterialIntensity;
    private double _opacity = DefaultOpacity;
    private bool _reduceMotion;
    private bool _reserveMenuBarSpace = true;
    private DockDisplayMode _displayMode = DockDisplayMode.FollowCursor;
    private bool _showBatteryStatus = true;
    private bool _showNetworkStatus = true;
    private bool _showVolumeStatus = true;
    private bool _use24HourClock = true;
    private bool _showActiveIndicator = true;
    private bool _showRunningIndicators = true;
    private DockPresentationStyle _style = DockPresentationStyle.Floating;
    private DockTheme _theme = DockTheme.BigSur;

    public DockTheme Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    public DockPresentationStyle Style
    {
        get => _style;
        set => SetProperty(ref _style, value);
    }

    public double IconSize
    {
        get => _iconSize;
        set => SetProperty(ref _iconSize, ClampFinite(value, 32, 64, DefaultIconSize));
    }

    public double MaxScale
    {
        get => _maxScale;
        set
        {
            if (SetProperty(ref _maxScale, ClampFinite(value, 1, 2, DefaultMaxScale)))
            {
                OnPropertyChanged(nameof(MaxScaleBoost));
            }
        }
    }

    public double MaxScaleBoost => MaxScale - 1;

    public double Opacity
    {
        get => _opacity;
        set => SetProperty(ref _opacity, ClampFinite(value, 0.55, 1, DefaultOpacity));
    }

    public double CornerRadius
    {
        get => _cornerRadius;
        set => SetProperty(ref _cornerRadius, ClampFinite(value, 12, 36, DefaultCornerRadius));
    }

    public bool EnableBlur
    {
        get => _enableBlur;
        set => SetProperty(ref _enableBlur, value);
    }

    public bool AutoHideDock
    {
        get => _autoHideDock;
        set => SetProperty(ref _autoHideDock, value);
    }

    public double MaterialIntensity
    {
        get => _materialIntensity;
        set => SetProperty(
            ref _materialIntensity,
            ClampFinite(value, 0.35, 1, DefaultMaterialIntensity));
    }

    public bool ShowRunningIndicators
    {
        get => _showRunningIndicators;
        set => SetProperty(ref _showRunningIndicators, value);
    }

    public bool ShowActiveIndicator
    {
        get => _showActiveIndicator;
        set => SetProperty(ref _showActiveIndicator, value);
    }

    public bool EnableMagnification
    {
        get => _enableMagnification;
        set => SetProperty(ref _enableMagnification, value);
    }

    public bool ReduceMotion
    {
        get => _reduceMotion;
        set => SetProperty(ref _reduceMotion, value);
    }

    public bool ReserveMenuBarSpace
    {
        get => _reserveMenuBarSpace;
        set => SetProperty(ref _reserveMenuBarSpace, value);
    }

    public DockDisplayMode DisplayMode
    {
        get => _displayMode;
        set => SetProperty(ref _displayMode, value);
    }

    public bool Use24HourClock
    {
        get => _use24HourClock;
        set => SetProperty(ref _use24HourClock, value);
    }

    public bool ShowNetworkStatus
    {
        get => _showNetworkStatus;
        set => SetProperty(ref _showNetworkStatus, value);
    }

    public bool ShowVolumeStatus
    {
        get => _showVolumeStatus;
        set => SetProperty(ref _showVolumeStatus, value);
    }

    public bool ShowBatteryStatus
    {
        get => _showBatteryStatus;
        set => SetProperty(ref _showBatteryStatus, value);
    }

    public DockAppearanceSnapshot CreateSnapshot() => new()
    {
        Theme = Theme,
        Style = Style,
        IconSize = IconSize,
        MaxScale = MaxScale,
        Opacity = Opacity,
        CornerRadius = CornerRadius,
        EnableBlur = EnableBlur,
        AutoHideDock = AutoHideDock,
        MaterialIntensity = MaterialIntensity,
        ShowRunningIndicators = ShowRunningIndicators,
        ShowActiveIndicator = ShowActiveIndicator,
        EnableMagnification = EnableMagnification,
        ReduceMotion = ReduceMotion,
        ReserveMenuBarSpace = ReserveMenuBarSpace,
        DisplayMode = DisplayMode,
        Use24HourClock = Use24HourClock,
        ShowNetworkStatus = ShowNetworkStatus,
        ShowVolumeStatus = ShowVolumeStatus,
        ShowBatteryStatus = ShowBatteryStatus
    };

    public void Apply(DockAppearanceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        Theme = snapshot.Theme;
        Style = snapshot.Style;
        IconSize = snapshot.IconSize;
        MaxScale = snapshot.MaxScale;
        Opacity = snapshot.Opacity;
        CornerRadius = snapshot.CornerRadius;
        EnableBlur = snapshot.EnableBlur;
        AutoHideDock = snapshot.AutoHideDock;
        MaterialIntensity = snapshot.MaterialIntensity;
        ShowRunningIndicators = snapshot.ShowRunningIndicators;
        ShowActiveIndicator = snapshot.ShowActiveIndicator;
        EnableMagnification = snapshot.EnableMagnification;
        ReduceMotion = snapshot.ReduceMotion;
        ReserveMenuBarSpace = snapshot.ReserveMenuBarSpace;
        DisplayMode = Enum.IsDefined(snapshot.DisplayMode)
            ? snapshot.DisplayMode
            : DockDisplayMode.FollowCursor;
        Use24HourClock = snapshot.Use24HourClock;
        ShowNetworkStatus = snapshot.ShowNetworkStatus;
        ShowVolumeStatus = snapshot.ShowVolumeStatus;
        ShowBatteryStatus = snapshot.ShowBatteryStatus;
    }

    public void Reset() => Apply(new DockAppearanceSnapshot());

    private static double ClampFinite(
        double value,
        double minimum,
        double maximum,
        double fallback)
    {
        return double.IsFinite(value)
            ? Math.Clamp(value, minimum, maximum)
            : fallback;
    }
}
