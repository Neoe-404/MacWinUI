using System.Diagnostics;
using MacWinUI.Core.Interfaces;
using MacWinUI.Core.Models;
using Microsoft.Extensions.Logging;

namespace MacWinUI.Windows.Applications;

public sealed class WindowsSystemSettingsLauncher(
    ILogger<WindowsSystemSettingsLauncher> logger) : ISystemSettingsLauncher
{
    public async Task OpenAsync(
        SystemSettingsPage page,
        CancellationToken cancellationToken = default)
    {
        var target = page switch
        {
            SystemSettingsPage.Network => "ms-settings:network-status",
            SystemSettingsPage.Bluetooth => "ms-settings:bluetooth",
            SystemSettingsPage.Sound => "ms-settings:sound",
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };

        try
        {
            await Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Process.Start(
                        new ProcessStartInfo(target)
                        {
                            UseShellExecute = true
                        });
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Could not open Windows Settings page {SettingsPage}.",
                page);
        }
    }
}
