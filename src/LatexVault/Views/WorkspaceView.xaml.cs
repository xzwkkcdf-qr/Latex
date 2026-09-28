using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LatexVault.Models;
using LatexVault.Services;
using LatexVault.ViewModels;

namespace LatexVault.Views;

public partial class WorkspaceView : UserControl
{
    public WorkspaceView()
    {
        InitializeComponent();
    }

    private ShellViewModel? Vm => DataContext as ShellViewModel;

    private void LibraryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (Vm == null) return;
        Vm.Library.SelectedNode = e.NewValue as LibraryNode;
        if (e.NewValue is LibraryNode node &&
            !node.IsDirectory &&
            LibraryPathRules.IsTexFile(node.FullPath))
        {
            Vm.OpenSelectedNodeCommand.Execute(null);
        }
    }

    private void LibraryTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        Vm?.OpenSelectedNodeCommand.Execute(null);
    }

    private void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DocumentTab tab })
            Vm?.Editor.CloseTab(tab);
        e.Handled = true;
    }
}