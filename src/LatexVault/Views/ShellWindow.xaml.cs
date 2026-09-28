using System.ComponentModel;
using System.Windows;
using LatexVault.ViewModels;

namespace LatexVault.Views;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
        var vm = new ShellViewModel();
        DataContext = vm;
        WorkspaceHost.Content = new WorkspaceView();
        StateChanged += (_, _) => UpdateMaximizeGlyph();
        Loaded += ShellWindow_Loaded;
        Closing += ShellWindow_Closing;
        UpdateMaximizeGlyph();
        vm.ApplyWindowGeometry(this);
    }

    private ShellViewModel? Vm => DataContext as ShellViewModel;

    private void ShellWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Vm?.EnsureLibraryOnStartup();
    }

    private void ShellWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (Vm == null) return;

        if (WorkspaceHost.Content is WorkspaceView workspace)
            workspace.CapturePaneWidths();

        if (!Vm.ConfirmCloseSession())
        {
            e.Cancel = true;
            return;
        }

        Vm.PersistSession(this);
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