namespace MacWinUI.Core.Dock;

public sealed record PinnedDockApplicationsSnapshot
{
    public const int CurrentSchemaVersion = 4;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public IReadOnlyList<PinnedDockApplication> Applications { get; init; } = [];

    public IReadOnlyList<string> HiddenDefaultItemIds { get; init; } = [];

    public IReadOnlyList<string> ItemOrder { get; init; } = [];
}
