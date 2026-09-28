using LatexVault.Models;

namespace LatexVault.Services;

public sealed class LibraryService
{
    public IReadOnlyList<LibraryNode> BuildTree(string root)
    {
        root = Path.GetFullPath(root);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException(root);

        return BuildChildren(root);
    }

    private static List<LibraryNode> BuildChildren(string dir)
    {
        var list = new List<LibraryNode>();
        foreach (var sub in Directory.GetDirectories(dir).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            var node = new LibraryNode
            {
                Name = Path.GetFileName(sub),
                FullPath = sub,
                IsDirectory = true
            };
            foreach (var child in BuildChildren(sub))
                node.Children.Add(child);
            list.Add(node);
        }

        foreach (var file in Directory.GetFiles(dir, "*.tex").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            list.Add(new LibraryNode
            {
                Name = Path.GetFileName(file),
                FullPath = file,
                IsDirectory = false
            });
        }

        return list;
    }

    public string CreateTexFile(string parentDirectory, string baseName)
    {
        if (!baseName.EndsWith(".tex", StringComparison.OrdinalIgnoreCase))
            baseName += ".tex";
        var path = Path.Combine(parentDirectory, baseName);
        if (File.Exists(path))
            throw new IOException("File already exists: " + path);
        const string stub =
            "\\documentclass{article}\n\\begin{document}\n\n\\end{document}\n";
        File.WriteAllText(path, stub);
        return path;
    }

    public string CreateFolder(string parentDirectory, string name)
    {
        var path = Path.Combine(parentDirectory, name);
        Directory.CreateDirectory(path);
        return path;
    }

    public void Rename(string path, string newName)
    {
        var parent = Path.GetDirectoryName(path) ?? throw new InvalidOperationException();
        var dest = Path.Combine(parent, newName);
        if (Directory.Exists(path))
            Directory.Move(path, dest);
        else
            File.Move(path, dest);
    }

    public void Move(string sourcePath, string destParentDirectory)
    {
        if (!LibraryPathRules.CanMove(sourcePath, destParentDirectory))
            throw new InvalidOperationException("Illegal move into descendant.");
        var name = Path.GetFileName(sourcePath);
        var dest = Path.Combine(destParentDirectory, name);
        if (Directory.Exists(sourcePath))
            Directory.Move(sourcePath, dest);
        else
            File.Move(sourcePath, dest);
    }

    public void Delete(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
        else if (File.Exists(path))
            File.Delete(path);
    }
}
