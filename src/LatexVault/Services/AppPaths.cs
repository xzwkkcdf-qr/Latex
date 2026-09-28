namespace LatexVault.Services;

public static class AppPaths
{
    public static string DefaultLibraryRoot =>
        Path.Combine(AppContext.BaseDirectory, "Library");

    public static string EnsureDefaultLibrary()
    {
        var root = DefaultLibraryRoot;
        Directory.CreateDirectory(root);
        return root;
    }
}
