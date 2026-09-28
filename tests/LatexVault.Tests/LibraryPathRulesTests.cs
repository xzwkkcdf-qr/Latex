using LatexVault.Services;

namespace LatexVault.Tests;

public class LibraryPathRulesTests
{
    [Fact]
    public void CanMove_rejects_into_own_descendant()
    {
        var src = @"C:\lib\paper";
        var destParent = @"C:\lib\paper\chapters";
        Assert.False(LibraryPathRules.CanMove(src, destParent));
    }

    [Fact]
    public void CanMove_allows_sibling()
    {
        Assert.True(LibraryPathRules.CanMove(@"C:\lib\a", @"C:\lib\b"));
    }

    [Fact]
    public void IsTexFile_requires_tex_extension()
    {
        Assert.True(LibraryPathRules.IsTexFile(@"C:\a\main.tex"));
        Assert.False(LibraryPathRules.IsTexFile(@"C:\a\main.pdf"));
    }

    [Fact]
    public void Relativize_returns_path_under_root()
    {
        var rel = LibraryPathRules.Relativize(@"G:\lib", @"G:\lib\notes\a.tex");
        Assert.Equal(Path.Combine("notes", "a.tex"), rel);
    }
}