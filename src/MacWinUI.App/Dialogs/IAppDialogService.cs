using System.Windows;

namespace MacWinUI.App.Dialogs;

public interface IAppDialogService
{
    void ShowInfo(Window owner, string title, string message);

    void ShowWarning(Window owner, string title, string message);

    bool ShowConfirmation(
        Window owner,
        string title,
        string message,
        string confirmButtonText);
}
