using System.Diagnostics;
using System.IO;
using MacWinUI.Core.Interfaces;
using MacWinUI.Core.Models;
using MacWinUI.Windows.Native;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MacWinUI.Windows.Applications;

public sealed class WindowsApplicationLauncher(
    ILogger<WindowsApplicationLauncher> logger) : IApplicationLauncher
{
    public async Task LaunchAsync(
        DockItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        try
        {
            await Task.Run(
                () => StartProcess(item, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to launch dock item {DockItemId} with target {LaunchTarget}.",
                item.Id,
                item.LaunchTarget);
        }
    }

    public async Task ActivateOrLaunchAsync(
        DockItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!string.IsNullOrWhiteSpace(item.ProcessName))
        {
            var activated = await Task.Run(
                () => TryActivateWindow(item.ProcessName, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            if (activated)
            {
                return;
            }
        }

        await LaunchAsync(item, cancellationToken).ConfigureAwait(false);
    }

    public async Task LaunchWithFilesAsync(
        DockItem item,
        IReadOnlyList<string> filePaths,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(filePaths);

        if (!item.AcceptsFileDrops || filePaths.Count == 0)
        {
            return;
        }

        try
        {
            await Task.Run(
                () => StartProcessWithFiles(item, filePaths, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to open {FileCount} dropped files with dock item {DockItemId}.",
                filePaths.Count,
                item.Id);
        }
    }

    public async Task OpenContainingFolderAsync(
        DockItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var sourcePath = item.IconSourcePath ?? item.LaunchTarget;
        try
        {
            var fullPath = Path.GetFullPath(sourcePath);
            var folderPath = Directory.Exists(fullPath)
                ? fullPath
                : Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return;
            }

            await Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = folderPath,
                        UseShellExecute = true
                    });
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or System.ComponentModel.Win32Exception)
        {
            logger.LogWarning(
                exception,
                "Could not open the containing folder for dock item {DockItemId}.",
                item.Id);
        }
    }

    public Task<IReadOnlyList<ApplicationWindowInfo>> GetOpenWindowsAsync(
        DockItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        return Task.Run(
            () => (IReadOnlyList<ApplicationWindowInfo>)CaptureWindows(
                item.ProcessName,
                cancellationToken),
            cancellationToken);
    }

    public Task ActivateWindowAsync(
        nint windowHandle,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (User32.IsIconic(windowHandle))
                {
                    User32.ShowWindow(windowHandle, User32.ShowWindowRestore);
                }

                User32.SetForegroundWindow(windowHandle);
            },
            cancellationToken);
    }

    private static void StartProcess(
        DockItem item,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (item.LaunchType is LaunchType.AppUserModelId)
        {
            var appStartInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true
            };
            appStartInfo.ArgumentList.Add($"shell:AppsFolder\\{item.LaunchTarget}");
            Process.Start(appStartInfo);
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = item.LaunchTarget,
            Arguments = item.Arguments ?? string.Empty,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    private static bool TryActivateWindow(
        string processName,
        CancellationToken cancellationToken)
    {
        var processIds = Process.GetProcessesByName(processName)
            .Select(process =>
            {
                try
                {
                    return process.Id;
                }
                finally
                {
                    process.Dispose();
                }
            })
            .ToHashSet();
        if (processIds.Count == 0)
        {
            return false;
        }

        nint matchingWindow = 0;
        User32.EnumWindows(
            (windowHandle, _) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!User32.IsWindowVisible(windowHandle))
                {
                    return true;
                }

                User32.GetWindowThreadProcessId(windowHandle, out var processId);
                if (!processIds.Contains((int)processId))
                {
                    return true;
                }

                matchingWindow = windowHandle;
                return false;
            },
            nint.Zero);
        if (matchingWindow == 0)
        {
            return false;
        }

        if (User32.IsIconic(matchingWindow))
        {
            User32.ShowWindow(matchingWindow, User32.ShowWindowRestore);
        }

        return User32.SetForegroundWindow(matchingWindow);
    }

    private static ApplicationWindowInfo[] CaptureWindows(
        string? processName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return [];
        }

        var processIds = Process.GetProcessesByName(processName)
            .Select(process =>
            {
                try
                {
                    return process.Id;
                }
                finally
                {
                    process.Dispose();
                }
            })
            .ToHashSet();
        var windows = new List<ApplicationWindowInfo>();
        User32.EnumWindows(
            (windowHandle, _) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!User32.IsWindowVisible(windowHandle))
                {
                    return true;
                }

                User32.GetWindowThreadProcessId(windowHandle, out var processId);
                if (!processIds.Contains((int)processId))
                {
                    return true;
                }

                var titleLength = User32.GetWindowTextLength(windowHandle);
                if (titleLength <= 0)
                {
                    return true;
                }

                var title = new StringBuilder(titleLength + 1);
                User32.GetWindowText(windowHandle, title, title.Capacity);
                windows.Add(new ApplicationWindowInfo(windowHandle, title.ToString()));
                return true;
            },
            nint.Zero);
        return windows.ToArray();
    }

    private static void StartProcessWithFiles(
        DockItem item,
        IReadOnlyList<string> filePaths,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = item.LaunchTarget,
            UseShellExecute = true
        };
        foreach (var filePath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            startInfo.ArgumentList.Add(filePath);
        }

        Process.Start(startInfo);
    }
}
