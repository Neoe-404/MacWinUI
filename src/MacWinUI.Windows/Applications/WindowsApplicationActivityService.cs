using System.Diagnostics;
using System.IO;
using MacWinUI.Core.Interfaces;
using MacWinUI.Core.Models;
using MacWinUI.Windows.Native;
using Microsoft.Extensions.Logging;

namespace MacWinUI.Windows.Applications;

public sealed class WindowsApplicationActivityService(
    ILogger<WindowsApplicationActivityService> logger) : IApplicationActivityService
{
    public Task<IReadOnlyDictionary<string, ApplicationActivityState>> GetActivityAsync(
        IReadOnlyCollection<DockItem> dockItems,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dockItems);

        return Task.Run(
            () => CaptureActivity(dockItems, cancellationToken),
            cancellationToken);
    }

    private IReadOnlyDictionary<string, ApplicationActivityState> CaptureActivity(
        IReadOnlyCollection<DockItem> dockItems,
        CancellationToken cancellationToken)
    {
        var processCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var pathCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var foregroundProcessId = GetForegroundProcessId();
        string? foregroundProcessName = null;
        string? foregroundProcessPath = null;
        var processes = Process.GetProcesses();

        try
        {
            foreach (var process in processes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var processName = process.ProcessName;
                    processCounts[processName] = processCounts.GetValueOrDefault(processName) + 1;
                    string? processPath = null;
                    try
                    {
                        processPath = process.MainModule?.FileName;
                        if (!string.IsNullOrWhiteSpace(processPath))
                        {
                            pathCounts[processPath] = pathCounts.GetValueOrDefault(processPath) + 1;
                        }
                    }
                    catch (Exception pathException) when (
                        pathException is System.ComponentModel.Win32Exception
                        or NotSupportedException
                        or InvalidOperationException)
                    {
                        logger.LogDebug(pathException, "Process executable path was unavailable.");
                    }

                    if (process.Id == foregroundProcessId)
                    {
                        foregroundProcessName = processName;
                        foregroundProcessPath = processPath;
                    }
                }
                catch (Exception exception) when (
                    exception is InvalidOperationException
                    or System.ComponentModel.Win32Exception
                    or NotSupportedException)
                {
                    logger.LogDebug(
                        exception,
                        "A process disappeared or denied access while capturing Dock activity.");
                }
            }
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }

        return dockItems.ToDictionary(
            item => item.Id,
            item => CreateActivityState(
                item,
                processCounts,
                pathCounts,
                foregroundProcessName,
                foregroundProcessPath),
            StringComparer.Ordinal);
    }

    private static ApplicationActivityState CreateActivityState(
        DockItem item,
        IReadOnlyDictionary<string, int> processCounts,
        IReadOnlyDictionary<string, int> pathCounts,
        string? foregroundProcessName,
        string? foregroundProcessPath)
    {
        if (item.LaunchType is LaunchType.Executable
            && Path.IsPathFullyQualified(item.LaunchTarget))
        {
            var pathInstanceCount = pathCounts.GetValueOrDefault(item.LaunchTarget);
            if (pathInstanceCount > 0)
            {
                return new ApplicationActivityState(
                    pathInstanceCount,
                    string.Equals(
                        item.LaunchTarget,
                        foregroundProcessPath,
                        StringComparison.OrdinalIgnoreCase));
            }
        }

        if (string.IsNullOrWhiteSpace(item.ProcessName))
        {
            return default;
        }

        var instanceCount = processCounts.GetValueOrDefault(item.ProcessName);
        var isActive = instanceCount > 0
            && string.Equals(
                item.ProcessName,
                foregroundProcessName,
                StringComparison.OrdinalIgnoreCase);

        return new ApplicationActivityState(instanceCount, isActive);
    }

    private static int GetForegroundProcessId()
    {
        var foregroundWindow = User32.GetForegroundWindow();
        if (foregroundWindow == 0)
        {
            return 0;
        }

        User32.GetWindowThreadProcessId(foregroundWindow, out var processId);
        return processId <= int.MaxValue ? (int)processId : 0;
    }
}
