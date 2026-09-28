using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexVault.Models;

public partial class LibraryNode : ObservableObject
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public bool IsDirectory { get; init; }
    public ObservableCollection<LibraryNode> Children { get; } = new();

    [ObservableProperty] private bool _isVisible = true;
}