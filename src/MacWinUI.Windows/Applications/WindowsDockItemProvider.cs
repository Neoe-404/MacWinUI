using System.IO;
using MacWinUI.Core.Interfaces;
using MacWinUI.Core.Models;

namespace MacWinUI.Windows.Applications;

public sealed class WindowsDockItemProvider : IDockItemProvider
{
    public IReadOnlyList<DockItem> GetDefaultItems()
    {
        var edgePath = FindEdge();
        var terminalPath = FindWindowsTerminal();
        var codePath = FindVisualStudioCode();

        return
        [
            CreateExplorerItem(),
            CreateEdgeItem(edgePath),
            CreateTerminalItem(terminalPath),
            CreateCodeItem(codePath),
            CreateSettingsItem()
        ];
    }

    private static DockItem CreateExplorerItem()
    {
        return new DockItem
        {
            Id = "explorer",
            DisplayName = "File Explorer",
            LaunchType = LaunchType.Shell,
            LaunchTarget = "explorer.exe",
            ProcessName = "explorer",
            IconSourcePath = FindWindowsFile("explorer.exe"),
            PlaceholderGlyph = "\uE8B7",
            AccentColor = "#EAB308"
        };
    }

    private static DockItem CreateEdgeItem(string? edgePath)
    {
        return new DockItem
        {
            Id = "edge",
            DisplayName = "Microsoft Edge",
            LaunchType = edgePath is null ? LaunchType.Uri : LaunchType.Executable,
            LaunchTarget = edgePath ?? "microsoft-edge:",
            ProcessName = "msedge",
            IconSourcePath = edgePath,
            AcceptsFileDrops = edgePath is not null,
            PlaceholderGlyph = "\uE774",
            AccentColor = "#0EA5A4"
        };
    }

    private static DockItem CreateTerminalItem(string? terminalPath)
    {
        var isTerminalAvailable = terminalPath is not null;
        return new DockItem
        {
            Id = isTerminalAvailable ? "windows-terminal" : "command-prompt-fallback",
            DisplayName = isTerminalAvailable
                ? "Windows Terminal"
                : "Command Prompt (Terminal fallback)",
            LaunchType = isTerminalAvailable ? LaunchType.Executable : LaunchType.Shell,
            LaunchTarget = terminalPath ?? "cmd.exe",
            ProcessName = isTerminalAvailable ? "WindowsTerminal" : "cmd",
            IconSourcePath = terminalPath ?? FindWindowsFile("System32", "cmd.exe"),
            PlaceholderGlyph = "\uE756",
            AccentColor = "#111827"
        };
    }

    private static DockItem CreateCodeItem(string? codePath)
    {
        var isCodeAvailable = codePath is not null;
        return new DockItem
        {
            Id = isCodeAvailable ? "visual-studio-code" : "notepad-fallback",
            DisplayName = isCodeAvailable
                ? "Visual Studio Code"
                : "Notepad (VS Code fallback)",
            LaunchType = isCodeAvailable ? LaunchType.Executable : LaunchType.Shell,
            LaunchTarget = codePath ?? "notepad.exe",
            ProcessName = isCodeAvailable ? "Code" : "notepad",
            IconSourcePath = codePath ?? FindWindowsFile("System32", "notepad.exe"),
            AcceptsFileDrops = true,
            PlaceholderGlyph = isCodeAvailable ? "\uE943" : "\uE70B",
            AccentColor = isCodeAvailable ? "#3B82F6" : "#2563EB"
        };
    }

    private static DockItem CreateSettingsItem()
    {
        return new DockItem
        {
            Id = "windows-settings",
            DisplayName = "Windows Settings",
            LaunchType = LaunchType.Uri,
            LaunchTarget = "ms-settings:",
            ProcessName = "SystemSettings",
            IconSourcePath = FindWindowsFile("ImmersiveControlPanel", "SystemSettings.exe"),
            PlaceholderGlyph = "\uE713",
            AccentColor = "#64748B"
        };
    }

    private static string? FindEdge()
    {
        return FindFirstExistingPath(
            CombineSpecialFolder(Environment.SpecialFolder.LocalApplicationData, "Microsoft", "Edge", "Application", "msedge.exe"),
            CombineSpecialFolder(Environment.SpecialFolder.ProgramFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"),
            CombineSpecialFolder(Environment.SpecialFolder.ProgramFiles, "Microsoft", "Edge", "Application", "msedge.exe"));
    }

    private static string? FindWindowsTerminal()
    {
        return FindFirstExistingPath(
            CombineSpecialFolder(Environment.SpecialFolder.LocalApplicationData, "Microsoft", "WindowsApps", "wt.exe"));
    }

    private static string? FindVisualStudioCode()
    {
        return FindFirstExistingPath(
            CombineSpecialFolder(Environment.SpecialFolder.LocalApplicationData, "Programs", "Microsoft VS Code", "Code.exe"),
            CombineSpecialFolder(Environment.SpecialFolder.ProgramFiles, "Microsoft VS Code", "Code.exe"),
            CombineSpecialFolder(Environment.SpecialFolder.ProgramFilesX86, "Microsoft VS Code", "Code.exe"));
    }

    private static string? CombineSpecialFolder(
        Environment.SpecialFolder specialFolder,
        params string[] pathParts)
    {
        var basePath = Environment.GetFolderPath(specialFolder);
        return string.IsNullOrWhiteSpace(basePath)
            ? null
            : Path.Combine([basePath, .. pathParts]);
    }

    private static string? FindWindowsFile(params string[] pathParts)
    {
        return FindFirstExistingPath(
            CombineSpecialFolder(Environment.SpecialFolder.Windows, pathParts));
    }

    private static string? FindFirstExistingPath(params string?[] paths)
    {
        return paths.FirstOrDefault(path => path is not null && File.Exists(path));
    }
}
