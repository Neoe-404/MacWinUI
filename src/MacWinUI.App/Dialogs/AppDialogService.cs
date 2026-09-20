using System.Windows;
using System.Windows.Markup;
using MacWinUI.App.Localization;

namespace MacWinUI.App.Dialogs;

public sealed class AppDialogService(IAppLocalizationService localization) : IAppDialogService
{
    public void ShowInfo(Window owner, string title, string message) =>
        Show(owner, title, message, localization.GetString("String.Dialog.OK"), false);

    public void ShowWarning(Window owner, string title, string message) =>
        Show(owner, title, message, localization.GetString("String.Dialog.Close"), false);

    public bool ShowConfirmation(
        Window owner,
        string title,
        string message,
        string confirmButtonText) =>
        Show(owner, title, message, confirmButtonText, true);

    private bool Show(
        Window owner,
        string title,
        string message,
        string primaryButtonText,
        bool showCancel)
    {
        ArgumentNullException.ThrowIfNull(owner);
        var dialog = new AppDialogWindow(
            title,
            message,
            primaryButtonText,
            localization.GetString("String.Dialog.Cancel"),
            showCancel)
        {
            Owner = owner,
            Language = XmlLanguage.GetLanguage(localization.EffectiveCulture.IetfLanguageTag)
        };
        return dialog.ShowDialog() is true;
    }
}
