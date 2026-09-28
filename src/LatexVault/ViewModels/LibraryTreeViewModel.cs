using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LatexVault.Models;
using LatexVault.Services;

namespace LatexVault.ViewModels;

public partial class LibraryTreeViewModel : ObservableObject
{
    private readonly LibraryService _library = new();

    [ObservableProperty] private string _rootPath = "";
    [ObservableProperty] private string _rootName = "No library";
    [ObservableProperty] private string _filterText = "";
    [ObservableProperty] private LibraryNode? _selectedNode;

    public ObservableCollection<LibraryNode> Nodes { get; } = new();

    public void Load(string root)
    {
        root = Path.GetFullPath(root);
        Nodes.Clear();
        foreach (var n in _library.BuildTree(root))
            Nodes.Add(n);
        RootPath = root;
        RootName = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(RootName))
            RootName = root;
    }

    public void Refresh()
    {
        if (!string.IsNullOrWhiteSpace(RootPath) && Directory.Exists(RootPath))
            Load(RootPath);
    }

    public LibraryService Service => _library;
}