using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Docnet.Core;
using Docnet.Core.Models;

namespace LatexVault.Services;

public sealed class PdfPreviewService : IDisposable
{
    private string? _stagedPath;

    public static string StageForPreview(string pdfPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "LatexVault");
        Directory.CreateDirectory(root);
        var dest = Path.Combine(root, Guid.NewGuid().ToString("N") + ".pdf");
        File.Copy(pdfPath, dest, overwrite: true);
        return dest;
    }

    public static int RenderDpi(double zoom, double pixelsPerDip, int baseDpi = 96) =>
        (int)Math.Clamp(Math.Round(baseDpi * zoom * pixelsPerDip), 72, 720);

    public static double BitmapDpi(double pixelsPerDip, int baseDpi = 96) =>
        baseDpi * pixelsPerDip;

    public static double GetPixelsPerDip()
    {
        try
        {
            if (Application.Current?.MainWindow is { } w)
            {
                var src = PresentationSource.FromVisual(w);
                var m = src?.CompositionTarget?.TransformToDevice.M11;
                if (m is > 0.1 and < 8)
                    return m.Value;
            }
        }
        catch { }

        return 1.0;
    }

    public IReadOnlyList<BitmapSource> RenderPages(
        string pdfPath,
        int renderDpi = 96,
        double bitmapDpi = 96)
    {
        ClearStaged();
        _stagedPath = StageForPreview(pdfPath);
        var list = new List<BitmapSource>();
        var scale = renderDpi / 72.0;

        using var docReader = DocLib.Instance.GetDocReader(_stagedPath, new PageDimensions(scale));
        for (var i = 0; i < docReader.GetPageCount(); i++)
        {
            using var page = docReader.GetPageReader(i);
            var w = page.GetPageWidth();
            var h = page.GetPageHeight();
            var raw = page.GetImage();
            var bmp = BitmapSource.Create(
                w, h, bitmapDpi, bitmapDpi,
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
            try { File.Delete(_stagedPath); } catch { }
        }
        _stagedPath = null;
    }

    public void Dispose() => ClearStaged();
}
