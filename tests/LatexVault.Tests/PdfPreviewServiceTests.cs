using LatexVault.Services;

namespace LatexVault.Tests;

public class PdfPreviewServiceTests
{
    [Fact]
    public void StageForPreview_copies_to_temp_and_returns_new_path()
    {
        var src = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
        File.WriteAllText(src, "%PDF-1.4 fake");
        try
        {
            var staged = PdfPreviewService.StageForPreview(src);
            Assert.NotEqual(Path.GetFullPath(src), Path.GetFullPath(staged));
            Assert.True(File.Exists(staged));
            Assert.Contains("LatexVault", staged, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(src);
        }
    }
}