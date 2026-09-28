using System.Windows.Media;
using System.Windows.Media.Imaging;
using Docnet.Core;
using Docnet.Core.Models;

namespace LatexVault.Services;

/// <summary>Docnet rasterization for PNG export only (not used by live preview).</summary>
public static class PdfRasterService
{
    public static IReadOnlyList<BitmapSource> RenderPages(string pdfPath, int dpi = 200)
    {
        var staged = PdfPreviewService.StageForPreview(pdfPath);
        try
        {
            var list = new List<BitmapSource>();
            var scale = dpi / 72.0;
            using var docReader = DocLib.Instance.GetDocReader(staged, new PageDimensions(scale));
            for (var i = 0; i < docReader.GetPageCount(); i++)
            {
                using var page = docReader.GetPageReader(i);
                var w = page.GetPageWidth();
                var h = page.GetPageHeight();
                var raw = page.GetImage();
                var bmp = BitmapSource.Create(
                    w, h, 96, 96,
                    PixelFormats.Bgra32, null, raw, w * 4);
                bmp.Freeze();
                list.Add(bmp);
            }
            return list;
        }
        finally
        {
            try { File.Delete(staged); } catch { /* ignore */ }
        }
    }
}
