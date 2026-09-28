using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LatexVault.Models;
using LatexVault.Services;
using Microsoft.Win32;

namespace LatexVault.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly SettingsStore _settingsStore = SettingsStore.ForAppData();

    [ObservableProperty] private AppSettings _settings = new();
    [ObservableProperty] private string _statusPath = "";
    [ObservableProperty] private double _libraryPaneWidth = 280;
    [ObservableProperty] private double _previewPaneWidth = 360;

    public LibraryTreeViewModel Library { get; } = new();
    public EditorTabsViewModel Editor { get; } = new();
    public CompileViewModel Compile { get; } = new();
    public PreviewViewModel Preview { get; } = new();

    public ShellViewModel()
    {
        Settings = _settingsStore.Load();
        LibraryPaneWidth = Settings.LibraryPaneWidth > 0 ? Settings.LibraryPaneWidth : 280;
        PreviewPaneWidth = Settings.PreviewPaneWidth > 0 ? Settings.PreviewPaneWidth : 360;
        Preview.IsVisible = Settings.PreviewVisible;
        Compile.SelectedEngine = Settings.DefaultEngine;

        if (!string.IsNullOrWhiteSpace(Settings.LibraryRoot) &&
            Directory.Exists(Settings.LibraryRoot))
        {
            Library.Load(Settings.LibraryRoot);
            StatusPath = Settings.LibraryRoot;
        }
    }

    [RelayCommand]
    private void OpenLibrary()
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Open LaTeX library folder"
        };
        if (dlg.ShowDialog() != true)
            return;

        var root = dlg.FolderName;
        Library.Load(root);
        Settings.LibraryRoot = root;
        StatusPath = root;
        _settingsStore.Save(Settings);
    }

    [RelayCommand]
    private void Save()
    {
        Editor.SaveActive();
        PersistLayout();
    }

    [RelayCommand]
    private async Task CompileAsync()
    {
        var tab = Editor.SelectedTab;
        if (tab == null || string.IsNullOrWhiteSpace(tab.FilePath))
        {
            Compile.StatusText = "No file";
            return;
        }

        if (tab.IsDirty)
            Editor.SaveActive();

        Settings.DefaultEngine = Compile.SelectedEngine;
        var result = await Compile.CompileAsync(tab.FilePath, Settings);
        if (result?.Success == true && !string.IsNullOrWhiteSpace(result.PdfPath))
            Preview.LoadPdf(result.PdfPath!);
        PersistLayout();
    }

    [RelayCommand]
    private void TogglePreview()
    {
        Preview.IsVisible = !Preview.IsVisible;
        Settings.PreviewVisible = Preview.IsVisible;
        PersistLayout();
    }

    [RelayCommand]
    private void OpenSelectedNode()
    {
        var node = Library.SelectedNode;
        if (node == null || node.IsDirectory)
            return;
        Editor.OpenFile(node.FullPath);
        StatusPath = node.FullPath;
    }

    private void PersistLayout()
    {
        Settings.LibraryPaneWidth = LibraryPaneWidth;
        Settings.PreviewPaneWidth = PreviewPaneWidth;
        Settings.PreviewVisible = Preview.IsVisible;
        Settings.DefaultEngine = Compile.SelectedEngine;
        _settingsStore.Save(Settings);
    }
}