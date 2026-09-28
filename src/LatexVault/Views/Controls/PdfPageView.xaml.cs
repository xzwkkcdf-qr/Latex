using System.Windows;
using System.Windows.Controls;
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
        PageImage.Source = DataContext as BitmapSource;
    }
}