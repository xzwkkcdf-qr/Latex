using LatexVault.Models;
using LatexVault.Services;

namespace LatexVault.Tests;

public class CompileServiceTests
{
    [Fact]
    public void BuildProcessStart_latexmk_uses_pdf_and_file()
    {
        var psi = CompileService.BuildProcessStart(
            CompileEngine.LatexMk,
            @"C:\tex\main.tex",
            executableOverride: "latexmk");
        Assert.Equal("latexmk", psi.FileName);
        Assert.Contains("-pdf", psi.Arguments);
        Assert.Contains("main.tex", psi.Arguments);
        Assert.Equal(@"C:\tex", psi.WorkingDirectory);
        Assert.True(psi.RedirectStandardOutput);
    }

    [Fact]
    public void BuildProcessStart_xelatex_interaction_nonstop()
    {
        var psi = CompileService.BuildProcessStart(
            CompileEngine.XeLatex,
            @"D:\a\b.tex",
            "xelatex");
        Assert.Equal("xelatex", psi.FileName);
        Assert.Contains("-interaction=nonstopmode", psi.Arguments);
        Assert.Contains("b.tex", psi.Arguments);
    }

    [Fact]
    public void ResolvePdfPath_same_stem()
    {
        Assert.Equal(
            @"C:\tex\main.pdf",
            CompileService.ResolvePdfPath(@"C:\tex\main.tex"));
    }
}
