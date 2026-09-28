using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using LatexVault.Models;
using LatexVault.Services;
using LatexVault.ViewModels;

namespace LatexVault.Views
{
public partial class WorkspaceView : UserControl
{
    private Point _dragStart;
    private LibraryNode? _dragNode;
    private ShellViewModel? _subscribedVm;

    public WorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += WorkspaceView_DataContextChanged;
        Loaded += (_, _) =>
        {
            ApplyPreviewColumn();
            WireHandlers();
            _ = EnsurePdfViewerAsync();
        };
    }


    private void WireHandlers()
    {
        LibraryTree.SelectedItemChanged -= LibraryTree_SelectedItemChanged;
        LibraryTree.SelectedItemChanged += LibraryTree_SelectedItemChanged;
        LibraryTree.MouseDoubleClick -= LibraryTree_MouseDoubleClick;
        LibraryTree.MouseDoubleClick += LibraryTree_MouseDoubleClick;
        LibraryTree.PreviewMouseLeftButtonDown -= LibraryTree_PreviewMouseLeftButtonDown;
        LibraryTree.PreviewMouseLeftButtonDown += LibraryTree_PreviewMouseLeftButtonDown;
        LibraryTree.PreviewMouseMove -= LibraryTree_PreviewMouseMove;
        LibraryTree.PreviewMouseMove += LibraryTree_PreviewMouseMove;
        LibraryTree.DragOver -= LibraryTree_DragOver;
        LibraryTree.DragOver += LibraryTree_DragOver;
        LibraryTree.Drop -= LibraryTree_Drop;
        LibraryTree.Drop += LibraryTree_Drop;
if (CompileLogBox != null)
        {
            CompileLogBox.TargetUpdated -= CompileLogBox_TargetUpdated;
            CompileLogBox.TargetUpdated += CompileLogBox_TargetUpdated;
        }
    }

    private ShellViewModel? Vm => DataContext as ShellViewModel;

    private void WorkspaceView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_subscribedVm != null)
        {
            _subscribedVm.Preview.PropertyChanged -= Preview_PropertyChanged;
            _subscribedVm.PropertyChanged -= Shell_PropertyChanged;
            _subscribedVm.Compile.PropertyChanged -= Compile_PropertyChanged;
            _subscribedVm = null;
        }

        if (e.NewValue is ShellViewModel vm)
        {
            _subscribedVm = vm;
            vm.Preview.PropertyChanged += Preview_PropertyChanged;
            vm.PropertyChanged += Shell_PropertyChanged;
            vm.Compile.PropertyChanged += Compile_PropertyChanged;
            ApplyPreviewColumn();
            ApplyLibraryColumn();
        }
    }

    private void Preview_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PreviewViewModel.IsVisible) or null)
            ApplyPreviewColumn();
    }

    private void Shell_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ShellViewModel.PreviewPaneWidth) or nameof(ShellViewModel.LibraryPaneWidth) or null)
        {
            ApplyPreviewColumn();
            ApplyLibraryColumn();
        }
    }


    public void CapturePaneWidths()
    {
        if (Vm == null) return;
        if (LibraryCol.Width.IsAbsolute && LibraryCol.Width.Value > 0)
            Vm.LibraryPaneWidth = LibraryCol.Width.Value;
        if (Vm.Preview.IsVisible && PreviewCol.Width.IsAbsolute && PreviewCol.Width.Value > 40)
            Vm.PreviewPaneWidth = PreviewCol.Width.Value;
    }

    private void Compile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CompileViewModel.LastLog)
            or nameof(CompileViewModel.LogCaretIndex)
            or nameof(CompileViewModel.IsLogOpen)
            or null)
        {
            Dispatcher.BeginInvoke(ScrollCompileLogToError, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    public void CompileLogBox_TargetUpdated(object? sender, System.Windows.Data.DataTransferEventArgs e)
    {
        if (e.Property == TextBox.TextProperty)
            ScrollCompileLogToError();
    }

    private void ScrollCompileLogToError()
    {
        if (Vm == null || CompileLogBox == null) return;
        var text = CompileLogBox.Text ?? "";
        if (text.Length == 0) return;
        var idx = Math.Clamp(Vm.Compile.LogCaretIndex, 0, text.Length);
        CompileLogBox.CaretIndex = idx;
        try
        {
            var line = CompileLogBox.GetLineIndexFromCharacterIndex(idx);
            CompileLogBox.ScrollToLine(line);
        }
        catch
        {
            // Ignore layout timing issues.
        }
    }
    private void ApplyLibraryColumn()
    {
        if (Vm == null) return;
        var w = Vm.LibraryPaneWidth > 0 ? Vm.LibraryPaneWidth : 280;
        LibraryCol.Width = new GridLength(w);
    }

    private void ApplyPreviewColumn()
    {
        if (Vm == null) return;

        if (Vm.Preview.IsVisible)
        {
            var w = Vm.PreviewPaneWidth > 0 ? Vm.PreviewPaneWidth : 360;
            PreviewCol.MinWidth = 160;
            PreviewCol.Width = new GridLength(w);
        }
        else
        {
            if (PreviewCol.Width.IsAbsolute && PreviewCol.Width.Value > 40)
                Vm.PreviewPaneWidth = PreviewCol.Width.Value;
            PreviewCol.MinWidth = 0;
            PreviewCol.Width = new GridLength(0);
        }
    }

    public void LibraryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
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

    public void LibraryTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        Vm?.OpenSelectedNodeCommand.Execute(null);
    }

    public void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DocumentTab tab })
            Vm?.Editor.CloseTab(tab);
        e.Handled = true;
    }

    public void CompileStatus_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        Vm?.Compile.ToggleLogCommand.Execute(null);
    }

    public void LibraryTree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(null);
        _dragNode = FindNode(e.OriginalSource as DependencyObject);
    }

    public void LibraryTree_PreviewMouseMove(object sender, MouseEventArgs e)
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

    public void LibraryTree_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(LibraryNode))
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    public void LibraryTree_Drop(object sender, DragEventArgs e)
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

        var oldPath = source.FullPath;
        if (!Vm.Library.TryMove(oldPath, destDir, out var error))
        {
            if (!string.IsNullOrEmpty(error) &&
                error.Contains("subfolder", StringComparison.OrdinalIgnoreCase))
                Vm.ReportIllegalMove();
            else
                Vm.ReportStatus(error ?? "Move failed.");
            return;
        }

        var newPath = Path.Combine(destDir, Path.GetFileName(oldPath));
        Vm.Editor.RemapPath(oldPath, newPath);
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




    private PreviewViewModel? _previewVm;
    private Action? _pdfNavHandler;
    private bool _pdfReady;

    private async Task EnsurePdfViewerAsync()
    {
        try
        {
            if (!_pdfReady)
            {
                await PdfWebView.EnsureCoreWebView2Async();
                PdfWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                PdfWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                PdfWebView.CoreWebView2.Settings.HiddenPdfToolbarItems =
                    CoreWebView2PdfToolbarItems.Save
                    | CoreWebView2PdfToolbarItems.SaveAs
                    | CoreWebView2PdfToolbarItems.Print
                    | CoreWebView2PdfToolbarItems.MoreSettings;
                _pdfReady = true;
            }
        }
        catch (Exception ex)
        {
            if (Vm != null)
                Vm.Preview.Hint = "WebView2 Runtime missing. Install Evergreen WebView2. " + ex.Message;
            return;
        }

        BindPreviewNavigation();
        NavigatePdf();
    }

    private void BindPreviewNavigation()
    {
        if (_previewVm != null && _pdfNavHandler != null)
        {
            _previewVm.ViewerNavigateRequested -= _pdfNavHandler;
            _previewVm.PropertyChanged -= Preview_ViewerUriChanged;
        }

        _previewVm = Vm?.Preview;
        if (_previewVm == null)
            return;

        _pdfNavHandler = NavigatePdf;
        _previewVm.ViewerNavigateRequested += _pdfNavHandler;
        _previewVm.PropertyChanged += Preview_ViewerUriChanged;
    }

    private void Preview_ViewerUriChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PreviewViewModel.ViewerUri) or nameof(PreviewViewModel.Zoom) or "ViewerUri" or "Zoom")
            NavigatePdf();
    }

    private void NavigatePdf()
    {
        if (!_pdfReady || PdfWebView.CoreWebView2 == null)
            return;

        var preview = Vm?.Preview;
        var uri = preview?.ViewerUri;
        if (string.IsNullOrWhiteSpace(uri))
        {
            PdfWebView.CoreWebView2.Navigate("about:blank");
            return;
        }

        try
        {
            PdfWebView.ZoomFactor = Math.Clamp(preview!.Zoom, 0.3, 4.0);
            // Force reload even if path same (compile overwrite): bump via navigation to blank then file.
            PdfWebView.CoreWebView2.Navigate("about:blank");
            PdfWebView.CoreWebView2.Navigate(uri);
        }
        catch (Exception ex)
        {
            if (preview != null)
                preview.Hint = "PDF navigate failed: " + ex.Message;
        }
    }

    public void PreviewScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Vm == null) return;
        if (Keyboard.Modifiers != ModifierKeys.Control)
            return;
        Vm.Preview.ZoomByWheel(e.Delta);
        e.Handled = true;
    }
}
}
