using System.Windows;

namespace LatexVault.Views;

public enum ConfirmResult
{
    Primary,
    Secondary,
    Cancel
}

public partial class ConfirmDialog : Window
{
    public ConfirmResult Result { get; private set; } = ConfirmResult.Cancel;

    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public static ConfirmResult Show(
        string title,
        string message,
        string primaryText = "Save",
        string? secondaryText = "Don't Save",
        string cancelText = "Cancel",
        Window? owner = null)
    {
        var dlg = new ConfirmDialog
        {
            Owner = owner ?? Application.Current?.MainWindow
        };
        dlg.Title = title;
        dlg.TitleText.Text = title;
        dlg.MessageText.Text = message;
        dlg.PrimaryButton.Content = primaryText;
        dlg.CancelButton.Content = cancelText;

        if (string.IsNullOrEmpty(secondaryText))
        {
            dlg.SecondaryButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            dlg.SecondaryButton.Content = secondaryText;
            dlg.SecondaryButton.Visibility = Visibility.Visible;
        }

        dlg.ShowDialog();
        return dlg.Result;
    }

    private void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        Result = ConfirmResult.Primary;
        DialogResult = true;
        Close();
    }

    private void SecondaryButton_Click(object sender, RoutedEventArgs e)
    {
        Result = ConfirmResult.Secondary;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = ConfirmResult.Cancel;
        DialogResult = false;
        Close();
    }
}