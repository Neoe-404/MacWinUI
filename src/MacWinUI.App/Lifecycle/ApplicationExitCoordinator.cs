using System.Windows;
using Microsoft.Extensions.Logging;

namespace MacWinUI.App.Lifecycle;

public sealed class ApplicationExitCoordinator(ILogger<ApplicationExitCoordinator> logger) : IApplicationExitCoordinator
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
            var message = Application.Current.TryFindResource("String.Exit.Message") as string
                ?? "Quit MacWinUI? Your settings will be saved before the application closes.";
            var title = Application.Current.TryFindResource("String.Exit.Title") as string
                ?? "Quit MacWinUI";
            var confirmed = MessageBox.Show(owner, message, title, MessageBoxButton.YesNo,
                MessageBoxImage.Question, MessageBoxResult.No) is MessageBoxResult.Yes;
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
