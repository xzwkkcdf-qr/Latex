using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using LatexVault.Services;

namespace LatexVault.ViewModels;

public partial class PreviewViewModel : ObservableObject
{
    private readonly PdfPreviewService _preview = new();

    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private string _hint = "Compile to preview PDF";

    public ObservableCollection<BitmapSource> Pages { get; } = new();

    public void Clear()
    {
        Pages.Clear();
        Hint = "Compile to preview PDF";
    }

    public void LoadPdf(string pdfPath)
    {
        Pages.Clear();
        if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
        {
            Hint = "No PDF";
            return;
        }

        try
        {
            foreach (var page in _preview.RenderPages(pdfPath))
                Pages.Add(page);
            Hint = Pages.Count == 0 ? "Empty PDF" : "";
        }
        catch (Exception ex)
        {
            Hint = "Preview failed: " + ex.Message;
        }
    }

    public void DisposePreview() => _preview.Dispose();
}