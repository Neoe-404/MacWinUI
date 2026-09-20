using System.Windows;
using System.Windows.Input;

namespace MacWinUI.App.Dialogs;

public partial class AppDialogWindow : Window
{
    public AppDialogWindow(
        string title,
        string message,
        string primaryButtonText,
        string cancelButtonText,
        bool showCancel)
    {
        InitializeComponent();
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        PrimaryButton.Content = primaryButtonText;
        CancelButton.Content = cancelButtonText;
        CancelButton.Visibility = showCancel ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnPrimaryClick(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Escape)
        {
            DialogResult = false;
            e.Handled = true;
        }
    }
}
