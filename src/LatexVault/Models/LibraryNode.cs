using System.Collections.ObjectModel;

namespace LatexVault.Models;

public sealed class LibraryNode
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public bool IsDirectory { get; init; }
    public ObservableCollection<LibraryNode> Children { get; } = new();
}
