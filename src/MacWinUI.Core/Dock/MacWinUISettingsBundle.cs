namespace MacWinUI.Core.Dock;

public sealed record MacWinUISettingsBundle
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public DockAppearanceSnapshot Appearance { get; init; } = new();

    public PinnedDockApplicationsSnapshot DockItems { get; init; } = new();
}
