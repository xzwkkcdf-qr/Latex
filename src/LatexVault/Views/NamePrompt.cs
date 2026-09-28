using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LatexVault.Views;

internal static class NamePrompt
{
    public static string? Ask(string title, string defaultName)
    {
        var box = new TextBox
        {
            Text = defaultName,
            MinWidth = 280,
            Margin = new Thickness(16, 16, 16, 8)
        };
        var ok = new Button
        {
            Content = "OK",
            IsDefault = true,
            Width = 72,
            Margin = new Thickness(0, 0, 8, 0),
            Style = Application.Current?.TryFindResource("LatexVaultPrimaryActionStyle") as Style
        };
        var cancel = new Button
        {
            Content = "Cancel",
            IsCancel = true,
            Width = 72,
            Style = Application.Current?.TryFindResource("LatexVaultSecondaryActionStyle") as Style
        };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(16, 8, 16, 16)
        };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);

        var panel = new StackPanel();
        panel.Children.Add(box);
        panel.Children.Add(buttons);

        var win = new Window
        {
            Title = title,
            Content = panel,
            SizeToContent = SizeToContent.WidthAndHeight,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current?.MainWindow,
            Background = Application.Current?.TryFindResource("LatexVaultPaperBrush") as System.Windows.Media.Brush
        };

        string? result = null;
        ok.Click += (_, _) =>
        {
            result = box.Text?.Trim();
            win.DialogResult = true;
        };
        box.SelectAll();
        box.Focus();
        Keyboard.Focus(box);
        return win.ShowDialog() == true ? result : null;
    }
}