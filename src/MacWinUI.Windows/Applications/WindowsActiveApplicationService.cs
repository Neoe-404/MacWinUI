using System.Diagnostics;
using MacWinUI.Core.Interfaces;
using MacWinUI.Windows.Native;

namespace MacWinUI.Windows.Applications;

public sealed class WindowsActiveApplicationService : IActiveApplicationService
{
    public Task<string> GetActiveApplicationNameAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () => GetActiveApplicationName(cancellationToken),
            cancellationToken);
    }

    private static string GetActiveApplicationName(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var foregroundWindow = User32.GetForegroundWindow();
        if (foregroundWindow == 0)
        {
            return "MacWinUI";
        }

        User32.GetWindowThreadProcessId(foregroundWindow, out var processId);
        if (processId == 0)
        {
            return "MacWinUI";
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            var description = process.MainModule?.FileVersionInfo.FileDescription;
            return string.IsNullOrWhiteSpace(description)
                ? process.ProcessName
                : description.Trim();
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or InvalidOperationException
            or System.ComponentModel.Win32Exception
            or NotSupportedException)
        {
            return "MacWinUI";
        }
    }
}
