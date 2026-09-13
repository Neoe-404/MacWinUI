using System.Windows;

namespace MacWinUI.App.Lifecycle;

public interface IApplicationExitCoordinator
{
    bool ConfirmAndExit(Window owner);
}
