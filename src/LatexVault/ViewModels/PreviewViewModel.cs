using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LatexVault.Services;
using Microsoft.Win32;

namespace LatexVault.ViewModels;

/// <summary>Live preview is WebView2 / Edge PDF only.</summary>
public partial class PreviewViewModel : ObservableObject
{
    private int _loadGeneration;
    private string? _lastPdfPath;
    private string? _stagedViewerPath;
    private int _navEpoch;

    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private string _hint = "No preview — compile to refresh";
    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private string _zoomText = "100%";
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string? _viewerUri;

    public bool HasDocument => !string.IsNullOrWhiteSpace(_lastPdfPath);

    partial void OnZoomChanged(double value)
    {
        ZoomText = $"{Math.Round(value * 100)}%";
        if (!string.IsNullOrWhiteSpace(_stagedViewerPath) && File.Exists(_stagedViewerPath))
            ViewerUri = BuildViewerUri(_stagedViewerPath, value);
    }

    public void Clear()
    {
        _loadGeneration++;
        _lastPdfPath = null;
        ClearStagedViewer();
        ViewerUri = null;
        IsEmpty = true;
        IsLoading = false;
        Hint = "No preview — compile to refresh";
        StatusMessage = "";
        OnPropertyChanged(nameof(HasDocument));
    }

    public async Task LoadPdf(string pdfPath)
    {
        var gen = ++_loadGeneration;
        if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
        {
            if (gen != _loadGeneration) return;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsLoading = false;
                if (IsEmpty)
                    Hint = "No preview — compile to refresh";
            });
            return;
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            IsLoading = true;
            Hint = "Loading PDF…";
        });

        try
        {
            var staged = await Task.Run(() => PdfPreviewService.StageForPreview(pdfPath)).ConfigureAwait(false);
            if (gen != _loadGeneration)
            {
                try { File.Delete(staged); } catch { }
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (gen != _loadGeneration)
                {
                    try { File.Delete(staged); } catch { }
                    return;
                }

                ClearStagedViewer();
                _stagedViewerPath = staged;
                _lastPdfPath = pdfPath;
                // Setting ViewerUri is the single signal for the view to Navigate (once).
                ViewerUri = BuildViewerUri(staged, Zoom);
                IsEmpty = false;
                Hint = "";
                IsLoading = false;
                StatusMessage = "WebView2";
                OnPropertyChanged(nameof(HasDocument));
            });
        }
        catch (Exception ex)
        {
            if (gen != _loadGeneration) return;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (gen != _loadGeneration) return;
                Hint = "Preview failed: " + ex.Message;
                IsLoading = false;
            });
        }
    }

    private string BuildViewerUri(string path, double zoom)
    {
        _navEpoch++;
        // Cache-bust so recompile reloads; Edge PDF uses view=FitH; app zoom via WebView2.ZoomFactor.
        return ToFileUri(path) + $"#view=FitH&nav={_navEpoch}&zoom={Math.Round(zoom * 100)}";
    }

    public static string ToFileUri(string path)
    {
        var full = Path.GetFullPath(path).Replace('\\', '/');
        if (full.Length >= 2 && full[1] == ':')
            return "file:///" + full.Replace(" ", "%20");
        return "file://" + full.Replace(" ", "%20");
    }

    [RelayCommand] private void ZoomIn() => SetZoom(Zoom * 1.25);
    [RelayCommand] private void ZoomOut() => SetZoom(Zoom / 1.25);
    [RelayCommand] private void ZoomReset() => SetZoom(1.0);
    public void SetZoom(double value) => Zoom = Math.Clamp(value, 0.25, 4.0);
    public void ZoomByWheel(int delta) => SetZoom(Zoom * (delta > 0 ? 1.1 : 1.0 / 1.1));

    [RelayCommand]
    private async Task ExportPagesAsync()
    {
        if (string.IsNullOrWhiteSpace(_lastPdfPath) || !File.Exists(_lastPdfPath)) return;
        var dlg = new OpenFolderDialog { Title = "Export one PNG per page" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            StatusMessage = "Exporting pages…";
            var pages = await Task.Run(() => PdfRasterService.RenderPages(_lastPdfPath!, dpi: 200));
            var baseName = Path.GetFileNameWithoutExtension(_lastPdfPath);
            PdfExportService.ExportPagesSeparately(pages, dlg.FolderName, baseName);
            StatusMessage = $"Exported {pages.Count} page image(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = "Export failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task ExportVerticalAsync()
    {
        if (string.IsNullOrWhiteSpace(_lastPdfPath) || !File.Exists(_lastPdfPath)) return;
        var baseName = Path.GetFileNameWithoutExtension(_lastPdfPath) + "-strip";
        var dlg = new SaveFileDialog
        {
            Title = "Export vertical stitch PNG",
            Filter = "PNG image (*.png)|*.png",
            FileName = baseName + ".png",
            AddExtension = true,
            DefaultExt = ".png"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            StatusMessage = "Stitching pages…";
            var pages = await Task.Run(() => PdfRasterService.RenderPages(_lastPdfPath!, dpi: 200));
            await Task.Run(() => PdfExportService.ExportVerticalStrip(pages, dlg.FileName));
            StatusMessage = "Exported vertical strip.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Export failed: " + ex.Message;
        }
    }

    private void ClearStagedViewer()
    {
        if (_stagedViewerPath != null)
        {
            try { File.Delete(_stagedViewerPath); } catch { }
            _stagedViewerPath = null;
        }
    }

    public void DisposePreview() => ClearStagedViewer();
}
