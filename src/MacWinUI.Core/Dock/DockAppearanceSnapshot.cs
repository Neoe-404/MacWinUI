namespace MacWinUI.Core.Dock;

public sealed record DockAppearanceSnapshot
{
    public const int CurrentSchemaVersion = 5;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public DockTheme Theme { get; init; } = DockTheme.BigSur;

    public DockPresentationStyle Style { get; init; } = DockPresentationStyle.Floating;

    public double IconSize { get; init; } = 48;

    public double MaxScale { get; init; } = 1.5;

    public double Opacity { get; init; } = 0.82;

    public double CornerRadius { get; init; } = 22;

    public bool EnableBlur { get; init; } = true;

    public bool AutoHideDock { get; init; }

    public double MaterialIntensity { get; init; } = 0.85;

    public bool ShowRunningIndicators { get; init; } = true;

    public bool ShowActiveIndicator { get; init; } = true;

    public bool EnableMagnification { get; init; } = true;

    public bool ReduceMotion { get; init; }

    public bool ReserveMenuBarSpace { get; init; } = true;

    public DockDisplayMode DisplayMode { get; init; } = DockDisplayMode.FollowCursor;

    public bool Use24HourClock { get; init; } = true;

    public bool ShowNetworkStatus { get; init; } = true;

    public bool ShowVolumeStatus { get; init; } = true;

    public bool ShowBatteryStatus { get; init; } = true;
}
