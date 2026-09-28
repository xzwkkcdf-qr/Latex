using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LatexVault.Views.Controls;

public partial class PdfPageView : UserControl
{
    public PdfPageView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => ApplySource();
        Loaded += (_, _) => ApplySource();
    }

    private void ApplySource()
    {
        if (DataContext is not BitmapSource bmp)
        {
            PageImage.Source = null;
            return;
        }

        PageImage.Source = bmp;
        // Explicit DIP size from pixel count / bitmap DPI — avoid layout ambiguity.
        var dipW = bmp.PixelWidth * 96.0 / bmp.DpiX;
        var dipH = bmp.PixelHeight * 96.0 / bmp.DpiY;
        PageImage.Width = dipW;
        PageImage.Height = dipH;
        RenderOptions.SetBitmapScalingMode(PageImage, BitmapScalingMode.NearestNeighbor);
    }
}
