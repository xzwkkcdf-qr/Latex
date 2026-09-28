namespace LatexVault.Services;

/// <summary>Stages PDF files for WebView2. No rasterization.</summary>
public static class PdfPreviewService
{
    public static string StageForPreview(string pdfPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "LatexVault");
        Directory.CreateDirectory(root);
        var dest = Path.Combine(root, Guid.NewGuid().ToString("N") + ".pdf");
        File.Copy(pdfPath, dest, overwrite: true);
        return dest;
    }
}
