using System.Windows;
using MacWinUI.App.Dialogs;
using MacWinUI.App.Localization;
using Microsoft.Extensions.Logging;

namespace MacWinUI.App.Lifecycle;

public sealed class ApplicationExitCoordinator(
    IAppDialogService dialogs,
    IAppLocalizationService localization,
    ILogger<ApplicationExitCoordinator> logger) : IApplicationExitCoordinator
{
    private int _exitInProgress;

    public bool ConfirmAndExit(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (Interlocked.CompareExchange(ref _exitInProgress, 1, 0) != 0)
        {
            logger.LogDebug("Ignoring duplicate application exit request.");
            return false;
        }

        try
        {
            var confirmed = dialogs.ShowConfirmation(
                owner,
                localization.GetString("String.Exit.Title"),
                localization.GetString("String.Exit.Message"),
                localization.GetString("String.Dialog.Quit"));
            if (!confirmed)
            {
                Volatile.Write(ref _exitInProgress, 0);
                logger.LogInformation("Application exit canceled by the user.");
                return false;
            }

            logger.LogInformation("Application exit confirmed.");
            Application.Current.Shutdown();
            return true;
        }
        catch
        {
            Volatile.Write(ref _exitInProgress, 0);
            throw;
        }
    }
}
