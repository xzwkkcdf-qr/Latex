using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LatexVault.Models;
using LatexVault.Services;
using LatexVault.ViewModels;

namespace LatexVault.Views;

public partial class WorkspaceView : UserControl
{
    private Point _dragStart;
    private LibraryNode? _dragNode;

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

    private void LibraryTree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(null);
        _dragNode = FindNode(e.OriginalSource as DependencyObject);
    }

    private void LibraryTree_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragNode == null)
            return;

        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        DragDrop.DoDragDrop(LibraryTree, _dragNode, DragDropEffects.Move);
        _dragNode = null;
    }

    private void LibraryTree_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(LibraryNode))
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void LibraryTree_Drop(object sender, DragEventArgs e)
    {
        if (Vm == null) return;
        if (e.Data.GetData(typeof(LibraryNode)) is not LibraryNode source)
            return;

        var target = FindNode(e.OriginalSource as DependencyObject);
        string? destDir;
        if (target == null)
            destDir = Vm.Library.RootPath;
        else if (target.IsDirectory)
            destDir = target.FullPath;
        else
            destDir = Path.GetDirectoryName(target.FullPath);

        if (string.IsNullOrWhiteSpace(destDir))
            return;

        if (string.Equals(Path.GetDirectoryName(source.FullPath), destDir, StringComparison.OrdinalIgnoreCase))
            return;

        if (!Vm.Library.TryMove(source.FullPath, destDir, out var error))
        {
            if (!string.IsNullOrEmpty(error) &&
                error.Contains("subfolder", StringComparison.OrdinalIgnoreCase))
                Vm.ReportIllegalMove();
            else
                Vm.ReportStatus(error ?? "Move failed.");
            return;
        }

        Vm.ReportStatus("Moved " + source.Name);
        e.Handled = true;
    }

    private static LibraryNode? FindNode(DependencyObject? origin)
    {
        while (origin != null)
        {
            if (origin is FrameworkElement { DataContext: LibraryNode node })
                return node;
            origin = VisualTreeHelper.GetParent(origin);
        }
        return null;
    }
}