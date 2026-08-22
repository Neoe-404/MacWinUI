using System.Security.Cryptography;
using System.Text;
using MacWinUI.Core.Models;

namespace MacWinUI.Core.Dock;

public sealed record PinnedDockApplication
{
    public required string DisplayName { get; init; }

    public required string ExecutablePath { get; init; }

    public PinnedDockItemKind Kind { get; init; } = PinnedDockItemKind.Application;

    public static PinnedDockApplication Create(
        string executablePath,
        string? displayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        var normalizedPath = Path.GetFullPath(executablePath.Trim());
        var kind = string.Equals(
            Path.GetExtension(normalizedPath),
            ".exe",
            StringComparison.OrdinalIgnoreCase)
            ? PinnedDockItemKind.Application
            : PinnedDockItemKind.File;

        var normalizedName = string.IsNullOrWhiteSpace(displayName)
            ? Path.GetFileNameWithoutExtension(normalizedPath)
            : displayName.Trim();

        return new PinnedDockApplication
        {
            DisplayName = normalizedName,
            ExecutablePath = normalizedPath,
            Kind = kind
        };
    }

    public DockItem CreateDockItem()
    {
        return new DockItem
        {
            Id = CreateStableId(ExecutablePath),
            DisplayName = DisplayName,
            LaunchType = Kind is PinnedDockItemKind.Application
                ? LaunchType.Executable
                : LaunchType.Shell,
            LaunchTarget = ExecutablePath,
            ProcessName = Kind is PinnedDockItemKind.Application
                ? Path.GetFileNameWithoutExtension(ExecutablePath)
                : null,
            IconSourcePath = ExecutablePath,
            IsCustom = true,
            AcceptsFileDrops = Kind is PinnedDockItemKind.Application,
            PlaceholderGlyph = Kind is PinnedDockItemKind.Application
                ? "\uE7C3"
                : "\uE8A5",
            AccentColor = "#64748B"
        };
    }

    public static string CreateStableId(string targetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        var normalizedPath = Path.GetFullPath(targetPath.Trim()).ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath));
        return $"custom-{Convert.ToHexString(hash.AsSpan(0, 10)).ToLowerInvariant()}";
    }
}
