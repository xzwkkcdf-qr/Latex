using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LatexVault.Services;

public static class PdfExportService
{
    public static void ExportPagesSeparately(
        IReadOnlyList<BitmapSource> pages,
        string directory,
        string baseName)
    {
        Directory.CreateDirectory(directory);
        var safe = Sanitize(baseName);
        for (var i = 0; i < pages.Count; i++)
        {
            var path = Path.Combine(directory, $"{safe}-p{i + 1:00}.png");
            SavePng(pages[i], path);
        }
    }

    public static string ExportVerticalStrip(
        IReadOnlyList<BitmapSource> pages,
        string filePath)
    {
        if (pages.Count == 0)
            throw new InvalidOperationException("No pages to export.");

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        SavePng(StitchVertical(pages), filePath);
        return filePath;
    }

    public static BitmapSource StitchVertical(IReadOnlyList<BitmapSource> pages)
    {
        if (pages.Count == 0)
            throw new InvalidOperationException("No pages to stitch.");

        var prepared = pages.Select(EnsureBgra).ToList();
        var width = prepared.Max(p => p.PixelWidth);
        var height = prepared.Sum(p => p.PixelHeight);
        var dpiX = prepared[0].DpiX;
        var dpiY = prepared[0].DpiY;
        var stride = width * 4;
        var pixels = new byte[stride * height];
        var y = 0;
        foreach (var page in prepared)
        {
            var pageStride = page.PixelWidth * 4;
            var pagePixels = new byte[pageStride * page.PixelHeight];
            page.CopyPixels(pagePixels, pageStride, 0);
            for (var row = 0; row < page.PixelHeight; row++)
            {
                var destOffset = (y + row) * stride;
                var srcOffset = row * pageStride;
                var xOffset = (width - page.PixelWidth) / 2 * 4;
                for (var x = 0; x < width; x++)
                {
                    var o = destOffset + x * 4;
                    pixels[o] = 255;
                    pixels[o + 1] = 255;
                    pixels[o + 2] = 255;
                    pixels[o + 3] = 255;
                }
                Buffer.BlockCopy(pagePixels, srcOffset, pixels, destOffset + xOffset, pageStride);
            }
            y += page.PixelHeight;
        }

        var bmp = BitmapSource.Create(
            width, height, dpiX, dpiY,
            PixelFormats.Bgra32, null, pixels, stride);
        bmp.Freeze();
        return bmp;
    }

    private static BitmapSource EnsureBgra(BitmapSource source)
    {
        if (source.Format == PixelFormats.Bgra32)
            return source;
        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        converted.Freeze();
        return converted;
    }

    private static void SavePng(BitmapSource source, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(EnsureBgra(source)));
        using var fs = File.Create(path);
        encoder.Save(fs);
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "page" : name.Trim();
    }
}
