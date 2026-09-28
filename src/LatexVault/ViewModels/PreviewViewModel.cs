using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using LatexVault.Services;

namespace LatexVault.ViewModels;

public partial class PreviewViewModel : ObservableObject
{
    private readonly PdfPreviewService _preview = new();
    private int _loadGeneration;

    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private string _hint = "No preview — compile to refresh";

    public ObservableCollection<BitmapSource> Pages { get; } = new();

    public void Clear()
    {
        _loadGeneration++;
        Pages.Clear();
        IsEmpty = true;
        IsLoading = false;
        Hint = "No preview — compile to refresh";
    }

    public async Task LoadPdf(string pdfPath)
    {
        var gen = ++_loadGeneration;

        if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
        {
            if (gen != _loadGeneration) return;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Leave any previous good pages intact.
                IsLoading = false;
                if (Pages.Count == 0)
                {
                    IsEmpty = true;
                    Hint = "No preview — compile to refresh";
                }
            });
            return;
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            IsLoading = true;
            Hint = "Rendering…";
        });

        try
        {
            var pages = await Task.Run(() => _preview.RenderPages(pdfPath)).ConfigureAwait(false);
            if (gen != _loadGeneration) return;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (gen != _loadGeneration) return;
                // Replace pages only after a successful render.
                Pages.Clear();
                foreach (var page in pages)
                    Pages.Add(page);
                IsEmpty = Pages.Count == 0;
                Hint = IsEmpty ? "Empty PDF" : "";
                IsLoading = false;
            });
        }
        catch (Exception ex)
        {
            if (gen != _loadGeneration) return;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (gen != _loadGeneration) return;
                // Keep last good pages on render failure.
                IsEmpty = Pages.Count == 0;
                Hint = "Preview failed: " + ex.Message;
                IsLoading = false;
            });
        }
    }

    public void DisposePreview() => _preview.Dispose();
}