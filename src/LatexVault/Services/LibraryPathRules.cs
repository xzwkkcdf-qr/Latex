namespace LatexVault.Services;

public static class LibraryPathRules
{
    public static bool IsTexFile(string path) =>
        string.Equals(Path.GetExtension(path), ".tex", StringComparison.OrdinalIgnoreCase);

    public static string Relativize(string root, string fullPath)
    {
        var rootFull = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(fullPath);
        if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Path is outside library root.");
        return full.Substring(rootFull.Length);
    }

    public static bool CanMove(string sourcePath, string destParentDirectory)
    {
        var source = Path.GetFullPath(sourcePath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var dest = Path.GetFullPath(destParentDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.Equals(source, dest, StringComparison.OrdinalIgnoreCase))
            return false;

        var prefix = source + Path.DirectorySeparatorChar;
        if (dest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}