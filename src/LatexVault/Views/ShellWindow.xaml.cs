using System.Windows;
using LatexVault.ViewModels;

namespace LatexVault.Views;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
        DataContext = new ShellViewModel();
        WorkspaceHost.Content = new WorkspaceView();
        StateChanged += (_, _) => UpdateMaximizeGlyph();
        UpdateMaximizeGlyph();
    }

    private void MinimizeWindowButton_Click(object sender, RoutedEventArgs e)
        => SystemCommands.MinimizeWindow(this);

    private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
        => SystemCommands.CloseWindow(this);

    private void MaximizeWindowButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            SystemCommands.RestoreWindow(this);
        else
            SystemCommands.MaximizeWindow(this);
    }

    private void UpdateMaximizeGlyph()
    {
        bool isMaximized = WindowState == WindowState.Maximized;
        MaximizeWindowGlyph.Visibility = isMaximized
            ? Visibility.Collapsed : Visibility.Visible;
        RestoreWindowGlyph.Visibility = isMaximized
            ? Visibility.Visible : Visibility.Collapsed;
    }
}