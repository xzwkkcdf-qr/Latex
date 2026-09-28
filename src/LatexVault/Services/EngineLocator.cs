using LatexVault.Models;

namespace LatexVault.Services;

public static class EngineLocator
{
    public static void ApplyDetectedPaths(AppSettings settings)
    {
        settings.LatexMkPath ??= FindExecutable("latexmk.exe", "latexmk");
        settings.XeLatexPath ??= FindExecutable("xelatex.exe", "xelatex");
        settings.PdfLatexPath ??= FindExecutable("pdflatex.exe", "pdflatex");
    }

    public static string? FindExecutable(params string[] names)
    {
        foreach (var name in names)
        {
            var fromPath = FindOnPath(name);
            if (fromPath != null)
                return fromPath;
        }

        foreach (var dir in CandidateDirectories())
        {
            foreach (var name in names)
            {
                var file = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    ? name
                    : name + ".exe";
                var full = Path.Combine(dir, file);
                if (File.Exists(full))
                    return full;
            }
        }

        return null;
    }

    private static string? FindOnPath(string fileName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv))
            return null;

        var file = fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : fileName + ".exe";

        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var full = Path.Combine(dir.Trim('"'), file);
                if (File.Exists(full))
                    return Path.GetFullPath(full);
            }
            catch
            {
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidateDirectories()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        yield return Path.Combine(local, "Programs", "MiKTeX", "miktex", "bin", "x64");
        yield return Path.Combine(programFiles, "MiKTeX", "miktex", "bin", "x64");
        yield return @"E:\texlive\2025\bin\windows";
        yield return @"E:\texlive\2024\bin\windows";
        yield return @"E:\texlive\2023\bin\windows";
        yield return @"C:\texlive\2025\bin\windows";
        yield return @"C:\texlive\2024\bin\windows";
        yield return @"C:\texlive\2023\bin\windows";
    }
}
