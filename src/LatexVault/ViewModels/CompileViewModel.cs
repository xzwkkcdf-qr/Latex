using CommunityToolkit.Mvvm.ComponentModel;
using LatexVault.Models;
using LatexVault.Services;

namespace LatexVault.ViewModels;

public partial class CompileViewModel : ObservableObject
{
    private readonly CompileService _compile = new();

    [ObservableProperty] private CompileEngine _selectedEngine = CompileEngine.LatexMk;
    [ObservableProperty] private string _statusText = "Idle";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _log = "";

    public CompileEngine[] Engines { get; } =
        [CompileEngine.LatexMk, CompileEngine.XeLatex, CompileEngine.PdfLatex];

    public async Task<CompileResult?> CompileAsync(string texPath, AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(texPath) || !File.Exists(texPath))
        {
            StatusText = "No file";
            return null;
        }

        IsBusy = true;
        StatusText = "Compiling…";
        try
        {
            var result = await _compile.CompileAsync(SelectedEngine, texPath, settings);
            Log = result.Log;
            StatusText = result.Success ? "OK" : (result.ErrorHint ?? "Failed");
            return result;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Cancel() => _compile.CancelRunning();
}