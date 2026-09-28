using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LatexVault.Models;
using LatexVault.Services;

namespace LatexVault.ViewModels;

public partial class LibraryTreeViewModel : ObservableObject, IDisposable
{
    private readonly LibraryService _library = new();
    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _debounceCts;
    private Action? _afterRefresh;

    [ObservableProperty] private string _rootPath = "";
    [ObservableProperty] private string _rootName = "No library";
    [ObservableProperty] private string _filterText = "";
    [ObservableProperty] private LibraryNode? _selectedNode;

    public ObservableCollection<LibraryNode> Nodes { get; } = new();

    public LibraryService Service => _library;

    public void SetAfterRefresh(Action? callback) => _afterRefresh = callback;

    public void Load(string root)
    {
        root = Path.GetFullPath(root);
        Nodes.Clear();
        if (Directory.Exists(root))
        {
            foreach (var n in _library.BuildTree(root))
                Nodes.Add(n);
        }
        RootPath = root;
        RootName = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(RootName))
            RootName = root;
        ApplyFilter();
        AttachWatcher(root);
    }

    public void Refresh()
    {
        if (!string.IsNullOrWhiteSpace(RootPath) && Directory.Exists(RootPath))
            Load(RootPath);
        else
            Nodes.Clear();
        _afterRefresh?.Invoke();
    }

    partial void OnFilterTextChanged(string value) => ApplyFilter();

    public void ApplyFilter()
    {
        foreach (var n in Nodes)
            ApplyFilterRecursive(n, FilterText);
    }

    private static bool ApplyFilterRecursive(LibraryNode node, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            node.IsVisible = true;
            foreach (var c in node.Children)
                ApplyFilterRecursive(c, filter);
            return true;
        }

        var selfMatch = node.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);
        var childMatch = false;
        foreach (var c in node.Children)
            childMatch |= ApplyFilterRecursive(c, filter);
        node.IsVisible = selfMatch || childMatch;
        return node.IsVisible;
    }

    public string? GetCreateParentDirectory()
    {
        if (string.IsNullOrWhiteSpace(RootPath) || !Directory.Exists(RootPath))
            return null;
        if (SelectedNode == null)
            return RootPath;
        return SelectedNode.IsDirectory
            ? SelectedNode.FullPath
            : (Path.GetDirectoryName(SelectedNode.FullPath) ?? RootPath);
    }

    private void AttachWatcher(string root)
    {
        DetachWatcher();
        if (!Directory.Exists(root))
            return;

        _watcher = new FileSystemWatcher(root)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName
                           | NotifyFilters.DirectoryName
                           | NotifyFilters.LastWrite
        };
        _watcher.Created += OnFsEvent;
        _watcher.Changed += OnFsEvent;
        _watcher.Deleted += OnFsEvent;
        _watcher.Renamed += OnFsEvent;
        _watcher.EnableRaisingEvents = true;
    }

    private void DetachWatcher()
    {
        if (_watcher == null) return;
        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnFsEvent;
        _watcher.Changed -= OnFsEvent;
        _watcher.Deleted -= OnFsEvent;
        _watcher.Renamed -= OnFsEvent;
        _watcher.Dispose();
        _watcher = null;
    }

    private void OnFsEvent(object sender, FileSystemEventArgs e)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;
        _ = DebounceRefreshAsync(token);
    }

    private async Task DebounceRefreshAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(200, token).ConfigureAwait(false);
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null) return;
            await dispatcher.InvokeAsync(() =>
            {
                if (!string.IsNullOrWhiteSpace(RootPath) && Directory.Exists(RootPath))
                {
                    var selected = SelectedNode?.FullPath;
                    Nodes.Clear();
                    foreach (var n in _library.BuildTree(RootPath))
                        Nodes.Add(n);
                    ApplyFilter();
                    if (!string.IsNullOrEmpty(selected))
                        SelectedNode = FindNode(Nodes, selected);
                }
                _afterRefresh?.Invoke();
            }, DispatcherPriority.Background);
        }
        catch (TaskCanceledException)
        {
            // newer event superseded this refresh
        }
    }

    private static LibraryNode? FindNode(IEnumerable<LibraryNode> nodes, string path)
    {
        foreach (var n in nodes)
        {
            if (string.Equals(n.FullPath, path, StringComparison.OrdinalIgnoreCase))
                return n;
            var child = FindNode(n.Children, path);
            if (child != null) return child;
        }
        return null;
    }

    public bool TryMove(string sourcePath, string destParentDirectory, out string? error)
    {
        error = null;
        if (!LibraryPathRules.CanMove(sourcePath, destParentDirectory))
        {
            error = "Cannot move into its own subfolder.";
            return false;
        }
        try
        {
            _library.Move(sourcePath, destParentDirectory);
            Refresh();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public void Dispose()
    {
        _debounceCts?.Cancel();
        DetachWatcher();
    }
}