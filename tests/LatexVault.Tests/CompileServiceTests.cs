using System.Diagnostics;
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
        Assert.Equal("latexmk.exe", psi.FileName);
        Assert.Contains("-pdf", psi.Arguments);
        Assert.Contains("main.tex", psi.Arguments);
        Assert.Equal(@"C:\tex", psi.WorkingDirectory);
        Assert.True(psi.RedirectStandardOutput);
        Assert.False(psi.CreateNoWindow);
        Assert.False(psi.UseShellExecute);
        Assert.Equal(ProcessWindowStyle.Hidden, psi.WindowStyle);
    }

    [Fact]
    public void BuildProcessStart_xelatex_interaction_nonstop()
    {
        var psi = CompileService.BuildProcessStart(
            CompileEngine.XeLatex,
            @"D:\a\b.tex",
            "xelatex");
        Assert.Equal("xelatex.exe", psi.FileName);
        Assert.Contains("-interaction=nonstopmode", psi.Arguments);
        Assert.Contains("b.tex", psi.Arguments);
        Assert.False(psi.CreateNoWindow);
    }

    [Fact]
    public void NormalizeEnginePath_adds_exe_suffix()
    {
        Assert.Equal("xelatex.exe", CompileService.NormalizeEnginePath("xelatex"));
        Assert.Equal(@"C:\tex\xelatex.exe", CompileService.NormalizeEnginePath(@"C:\tex\xelatex.exe"));
    }

    [Fact]
    public void ResolvePdfPath_same_stem()
    {
        Assert.Equal(
            @"C:\tex\main.pdf",
            CompileService.ResolvePdfPath(@"C:\tex\main.tex"));
    }
}
