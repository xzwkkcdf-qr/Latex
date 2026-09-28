namespace LatexVault.Models;

public sealed class AppSettings
{
    public string LibraryRoot { get; set; } = "";
    public CompileEngine DefaultEngine { get; set; } = CompileEngine.LatexMk;
    public bool AutoCompileOnSave { get; set; }
    public bool PreviewVisible { get; set; } = true;
    public string? LatexMkPath { get; set; }
    public string? XeLatexPath { get; set; }
    public string? PdfLatexPath { get; set; }
    public double LibraryPaneWidth { get; set; } = 280;
    public double PreviewPaneWidth { get; set; } = 360;
    public double WindowWidth { get; set; } = 1280;
    public double WindowHeight { get; set; } = 900;
    public bool WindowMaximized { get; set; } = true;
}

