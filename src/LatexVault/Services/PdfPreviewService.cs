using System.Windows.Media;
using System.Windows.Media.Imaging;
using Docnet.Core;
using Docnet.Core.Models;

namespace LatexVault.Services;

public sealed class PdfPreviewService : IDisposable
{
    private readonly string _tempRoot =
        Path.Combine(Path.GetTempPath(), "LatexVault");
    private string? _stagedPath;

    public static string StageForPreview(string pdfPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "LatexVault");
        Directory.CreateDirectory(root);
        var dest = Path.Combine(root, Guid.NewGuid().ToString("N") + ".pdf");
        File.Copy(pdfPath, dest, overwrite: true);
        return dest;
    }

    /// <summary>
    /// Render DPI for a zoom factor. Bitmap metadata stays 96 DPI so WPF DIP size
    /// grows with pixel count (= sharp zoom, not ScaleTransform stretch).
    /// </summary>
    public static int DpiForZoom(double zoom, int baseDpi = 96) =>
        (int)Math.Clamp(Math.Round(baseDpi * zoom), 72, 576);

    public IReadOnlyList<BitmapSource> RenderPages(string pdfPath, int dpi = 96)
    {
        ClearStaged();
        _stagedPath = StageForPreview(pdfPath);
        var list = new List<BitmapSource>();
        // PageDimensions(double) is a scaling factor; dpi/72 maps PDF points to pixels.
        using var docReader = DocLib.Instance.GetDocReader(
            _stagedPath,
            new PageDimensions(dpi / 72.0));
        for (var i = 0; i < docReader.GetPageCount(); i++)
        {
            using var page = docReader.GetPageReader(i);
            var w = page.GetPageWidth();
            var h = page.GetPageHeight();
            var raw = page.GetImage(); // BGRA
            // Stamp as 96 DPI: 1 device-independent pixel per bitmap pixel → zoom = more pixels.
            var bmp = BitmapSource.Create(
                w, h, 96, 96,
                PixelFormats.Bgra32, null, raw, w * 4);
            bmp.Freeze();
            list.Add(bmp);
        }
        return list;
    }

    private void ClearStaged()
    {
        if (_stagedPath != null && File.Exists(_stagedPath))
        {
            try { File.Delete(_stagedPath); } catch { /* ignore */ }
        }
        _stagedPath = null;
    }

    public void Dispose() => ClearStaged();
}
