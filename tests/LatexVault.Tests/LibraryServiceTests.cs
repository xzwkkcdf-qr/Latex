using LatexVault.Services;

namespace LatexVault.Tests;

public class LibraryServiceTests
{
    [Fact]
    public void BuildTree_lists_folders_and_tex_only()
    {
        var root = Path.Combine(Path.GetTempPath(), "LVLib_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "notes"));
        File.WriteAllText(Path.Combine(root, "notes", "a.tex"), "x");
        File.WriteAllText(Path.Combine(root, "notes", "a.pdf"), "y");
        try
        {
            var svc = new LibraryService();
            var tree = svc.BuildTree(root);
            Assert.Single(tree);
            Assert.Equal("notes", tree[0].Name);
            Assert.True(tree[0].IsDirectory);
            Assert.Single(tree[0].Children);
            Assert.Equal("a.tex", tree[0].Children[0].Name);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void CreateTexFile_writes_utf8_stub()
    {
        var root = Path.Combine(Path.GetTempPath(), "LVLib_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = new LibraryService().CreateTexFile(root, "hello");
            Assert.True(File.Exists(path));
            Assert.Contains("documentclass", File.ReadAllText(path));
        }
        finally { Directory.Delete(root, true); }
    }
}
