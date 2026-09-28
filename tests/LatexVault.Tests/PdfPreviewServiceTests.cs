using LatexVault.Services;

namespace LatexVault.Tests;

public class PdfPreviewServiceTests
{
    [Fact]
    public void StageForPreview_copies_to_temp()
    {
        var src = Path.Combine(Path.GetTempPath(), "lv-stage-src-" + Guid.NewGuid().ToString("N") + ".pdf");
        File.WriteAllText(src, "%PDF-1.1 stub");
        try
        {
            var staged = PdfPreviewService.StageForPreview(src);
            Assert.True(File.Exists(staged));
            Assert.NotEqual(Path.GetFullPath(src), Path.GetFullPath(staged));
            Assert.Equal(File.ReadAllText(src), File.ReadAllText(staged));
            File.Delete(staged);
        }
        finally
        {
            try { File.Delete(src); } catch { }
        }
    }
}
