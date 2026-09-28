using LatexVault.Models;
using LatexVault.Services;

namespace LatexVault.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void Save_then_Load_roundtrips_library_root_and_engine()
    {
        var dir = Path.Combine(Path.GetTempPath(), "LatexVaultTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var store = new SettingsStore(dir);
            store.Save(new AppSettings
            {
                LibraryRoot = @"G:\texlib",
                DefaultEngine = CompileEngine.XeLatex,
                AutoCompileOnSave = false,
                PreviewVisible = true
            });

            var loaded = store.Load();
            Assert.Equal(@"G:\texlib", loaded.LibraryRoot);
            Assert.Equal(CompileEngine.XeLatex, loaded.DefaultEngine);
            Assert.True(loaded.PreviewVisible);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Load_missing_file_returns_defaults()
    {
        var dir = Path.Combine(Path.GetTempPath(), "LatexVaultTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var loaded = new SettingsStore(dir).Load();
            Assert.Equal(CompileEngine.LatexMk, loaded.DefaultEngine);
            Assert.True(string.IsNullOrEmpty(loaded.LibraryRoot));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}

