using System.Windows;

namespace MacWinUI.App.Lifecycle;

public static class ApplicationExitCoordinator
{
    public static bool ConfirmAndExit(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var message = Application.Current.TryFindResource("String.Exit.Message") as string
            ?? "Quit MacWinUI? Your settings will be saved before the application closes.";
        var title = Application.Current.TryFindResource("String.Exit.Title") as string
            ?? "Quit MacWinUI";
        var confirmed = MessageBox.Show(
            owner,
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No) is MessageBoxResult.Yes;
        if (confirmed)
        {
            Application.Current.Shutdown();
        }

        return confirmed;
    }
}
