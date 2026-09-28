using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LatexVault.Services;

namespace LatexVault.ViewModels;

/// <summary>Live PDF preview: WebView2 only. No Docnet / bitmap path.</summary>
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
            Hint = "Loading PDF (WebView2)…";
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
                ViewerUri = BuildViewerUri(staged, Zoom);
                IsEmpty = false;
                Hint = "";
                IsLoading = false;
                StatusMessage = "WebView2 Edge PDF";
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
        return ToFileUri(path) + $"#view=FitH&nav={_navEpoch}";
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
