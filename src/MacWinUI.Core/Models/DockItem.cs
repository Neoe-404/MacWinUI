using MacWinUI.Core.Utilities;

namespace MacWinUI.Core.Models;

public sealed class DockItem : ObservableObject
{
    private bool _isActive;
    private bool _isRunning;
    private byte[]? _iconPngData;
    private int _runningInstanceCount;

    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required LaunchType LaunchType { get; init; }

    public required string LaunchTarget { get; init; }

    public string? Arguments { get; init; }

    public string? ProcessName { get; init; }

    public string? IconSourcePath { get; init; }

    public bool IsPinned { get; init; } = true;

    public bool IsCustom { get; init; }

    public bool AcceptsFileDrops { get; init; }

    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    public int RunningInstanceCount
    {
        get => _runningInstanceCount;
        set => SetProperty(ref _runningInstanceCount, Math.Max(0, value));
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public byte[]? IconPngData
    {
        get => _iconPngData;
        set => SetProperty(ref _iconPngData, value);
    }

    public string PlaceholderGlyph { get; init; } = "\uE7C3";

    public string AccentColor { get; init; } = "#4A5568";
}
