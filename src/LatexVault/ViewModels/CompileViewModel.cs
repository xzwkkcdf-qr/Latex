using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LatexVault.Models;
using LatexVault.Services;

namespace LatexVault.ViewModels;

public partial class CompileViewModel : ObservableObject
{
    private readonly CompileService _compile = new();
    private EditorTabsViewModel? _editor;
    private PreviewViewModel? _preview;
    private Func<AppSettings>? _getSettings;
    private Action? _onCompiled;

    [ObservableProperty] private CompileEngine _selectedEngine = CompileEngine.LatexMk;
    [ObservableProperty] private string _statusText = "Idle";
    [ObservableProperty] private bool _isCompiling;
    [ObservableProperty] private string _lastLog = "";
    [ObservableProperty] private bool _isLogOpen;
    [ObservableProperty] private int _logCaretIndex;

    public EngineOption[] EngineOptions { get; } =
    [
        new(CompileEngine.LatexMk, "latexmk"),
        new(CompileEngine.XeLatex, "xelatex"),
        new(CompileEngine.PdfLatex, "pdflatex")
    ];

    // Kept for compatibility with earlier bindings.
    public CompileEngine[] Engines { get; } =
        [CompileEngine.LatexMk, CompileEngine.XeLatex, CompileEngine.PdfLatex];

    public void Bind(
        EditorTabsViewModel editor,
        PreviewViewModel preview,
        Func<AppSettings> getSettings,
        Action? onCompiled = null)
    {
        if (_editor != null)
            _editor.PropertyChanged -= Editor_PropertyChanged;

        _editor = editor;
        _preview = preview;
        _getSettings = getSettings;
        _onCompiled = onCompiled;
        _editor.PropertyChanged += Editor_PropertyChanged;
        CompileCommand.NotifyCanExecuteChanged();
    }

    private void Editor_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorTabsViewModel.SelectedTab) or null)
            CompileCommand.NotifyCanExecuteChanged();
    }

    private int _compileGeneration;

    [RelayCommand(CanExecute = nameof(CanCompile))]
    private async Task CompileAsync()
    {
        if (_editor == null || _getSettings == null)
        {
            StatusText = "Not ready";
            return;
        }

        var tab = _editor.SelectedTab;
        if (tab == null || string.IsNullOrWhiteSpace(tab.FilePath))
        {
            StatusText = "No file";
            return;
        }

        if (tab.IsDirty)
            _editor.SaveActive();

        var settings = _getSettings();
        settings.DefaultEngine = SelectedEngine;

        // Re-click cancels the in-flight compile and starts a new one.
        if (IsCompiling)
            _compile.CancelRunning();

        var gen = ++_compileGeneration;
        IsCompiling = true;
        StatusText = "Compiling…";
        // Keep last good PDF pages; only hint updates until LoadPdf succeeds.
        if (_preview != null)
            _preview.Hint = "Compiling…";

        try
        {
            var result = await _compile.CompileAsync(SelectedEngine, tab.FilePath, settings);
            if (gen != _compileGeneration)
                return;

            LastLog = result.Log ?? "";
            LogCaretIndex = FirstErrorOffset(LastLog);

            if (result.Success)
            {
                StatusText = "OK";
                IsLogOpen = false;
                if (!string.IsNullOrWhiteSpace(result.PdfPath) && _preview != null)
                    await _preview.LoadPdf(result.PdfPath);
            }
            else
            {
                StatusText = result.ErrorHint ?? "Failed";
                IsLogOpen = true;
                if (_preview != null)
                    _preview.Hint = "Compile failed — see log";
            }

            _onCompiled?.Invoke();
        }
        finally
        {
            if (gen == _compileGeneration)
            {
                IsCompiling = false;
                CompileCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private bool CanCompile() =>
        _editor?.SelectedTab != null &&
        !string.IsNullOrWhiteSpace(_editor.SelectedTab.FilePath);

    [RelayCommand]
    private void ToggleLog() => IsLogOpen = !IsLogOpen;

    public void Cancel() => _compile.CancelRunning();

    public static int FirstErrorOffset(string log)
    {
        if (string.IsNullOrEmpty(log))
            return 0;

        var bang = log.IndexOf('!');
        var error = log.IndexOf("error", StringComparison.OrdinalIgnoreCase);
        var candidates = new List<int>();
        if (bang >= 0) candidates.Add(bang);
        if (error >= 0) candidates.Add(error);
        return candidates.Count == 0 ? 0 : candidates.Min();
    }
}

public sealed record EngineOption(CompileEngine Engine, string Display)
{
    public override string ToString() => Display;
}