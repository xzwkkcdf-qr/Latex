using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LatexVault.Services;
using Microsoft.Win32;

namespace LatexVault.ViewModels;

public partial class PreviewViewModel : ObservableObject
{
    private readonly PdfPreviewService _preview = new();
    private int _loadGeneration;
    private string? _lastPdfPath;
    private readonly DispatcherTimer _zoomRenderTimer;

    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private string _hint = "No preview — compile to refresh";
    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private string _zoomText = "100%";
    [ObservableProperty] private string _statusMessage = "";

    public ObservableCollection<BitmapSource> Pages { get; } = new();
    public bool HasPages => Pages.Count > 0;

    /// <summary>PDF points → pixels at 100% zoom (96 DIP/inch screen).</summary>
    public const int BaseRenderDpi = 96;

    public PreviewViewModel()
    {
        Pages.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasPages));
        _zoomRenderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        _zoomRenderTimer.Tick += async (_, _) =>
        {
            _zoomRenderTimer.Stop();
            if (!string.IsNullOrWhiteSpace(_lastPdfPath))
                await LoadPdf(_lastPdfPath, keepZoom: true).ConfigureAwait(true);
        };
    }

    partial void OnZoomChanged(double value)
    {
        ZoomText = $"{Math.Round(value * 100)}%";
        // Re-render at higher DPI after wheel/button settle — do not bitmap-stretch.
        if (!string.IsNullOrWhiteSpace(_lastPdfPath))
        {
            _zoomRenderTimer.Stop();
            _zoomRenderTimer.Start();
        }
    }

    public void Clear()
    {
        _zoomRenderTimer.Stop();
        _loadGeneration++;
        _lastPdfPath = null;
        Pages.Clear();
        IsEmpty = true;
        IsLoading = false;
        Hint = "No preview — compile to refresh";
    }

    public Task LoadPdf(string pdfPath) => LoadPdf(pdfPath, keepZoom: false);

    public async Task LoadPdf(string pdfPath, bool keepZoom)
    {
        var gen = ++_loadGeneration;
        if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
        {
            if (gen != _loadGeneration) return;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsLoading = false;
                if (Pages.Count == 0)
                {
                    IsEmpty = true;
                    Hint = "No preview — compile to refresh";
                }
            });
            return;
        }

        var zoom = Zoom;
        var dpi = PdfPreviewService.DpiForZoom(zoom, BaseRenderDpi);

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            IsLoading = true;
            if (Pages.Count == 0)
                Hint = "Rendering…";
        });

        try
        {
            var pages = await Task.Run(() => _preview.RenderPages(pdfPath, dpi)).ConfigureAwait(false);
            if (gen != _loadGeneration) return;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (gen != _loadGeneration) return;
                Pages.Clear();
                foreach (var page in pages)
                    Pages.Add(page);
                _lastPdfPath = pdfPath;
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
                IsEmpty = Pages.Count == 0;
                Hint = "Preview failed: " + ex.Message;
                IsLoading = false;
            });
        }
    }

    [RelayCommand] private void ZoomIn() => SetZoom(Zoom * 1.25);
    [RelayCommand] private void ZoomOut() => SetZoom(Zoom / 1.25);
    [RelayCommand] private void ZoomReset() => SetZoom(1.0);
    public void SetZoom(double value) => Zoom = Math.Clamp(value, 0.25, 4.0);
    public void ZoomByWheel(int delta) => SetZoom(Zoom * (delta > 0 ? 1.1 : 1.0 / 1.1));

    [RelayCommand]
    private async Task ExportPagesAsync()
    {
        if (Pages.Count == 0) return;
        var dlg = new OpenFolderDialog { Title = "Export one PNG per page" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            StatusMessage = "Exporting pages…";
            var pages = await GetExportPagesAsync().ConfigureAwait(true);
            var baseName = string.IsNullOrWhiteSpace(_lastPdfPath)
                ? "page"
                : Path.GetFileNameWithoutExtension(_lastPdfPath);
            PdfExportService.ExportPagesSeparately(pages, dlg.FolderName, baseName);
            StatusMessage = $"Exported {pages.Count} page(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = "Export failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task ExportVerticalAsync()
    {
        if (Pages.Count == 0) return;
        var dlg = new SaveFileDialog
        {
            Title = "Export vertical strip",
            Filter = "PNG image|*.png",
            FileName = "preview-strip.png"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            StatusMessage = "Exporting strip…";
            var pages = await GetExportPagesAsync().ConfigureAwait(true);
            PdfExportService.ExportVerticalStrip(pages, dlg.FileName);
            StatusMessage = "Exported vertical strip";
        }
        catch (Exception ex)
        {
            StatusMessage = "Export failed: " + ex.Message;
        }
    }

    private async Task<IReadOnlyList<BitmapSource>> GetExportPagesAsync()
    {
        if (!string.IsNullOrWhiteSpace(_lastPdfPath) && File.Exists(_lastPdfPath))
        {
            try
            {
                return await Task.Run(() => _preview.RenderPages(_lastPdfPath!, dpi: 200));
            }
            catch
            {
                // fall through to on-screen pages
            }
        }

        return Pages.ToList();
    }

    public void DisposePreview() => _preview.Dispose();
}
