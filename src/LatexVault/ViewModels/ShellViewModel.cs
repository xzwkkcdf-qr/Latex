using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LatexVault.Models;
using LatexVault.Services;
using LatexVault.Views;
using Microsoft.Win32;

namespace LatexVault.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly SettingsStore _settingsStore = SettingsStore.ForAppData();
    private bool _firstRunChecked;

    [ObservableProperty] private AppSettings _settings = new();
    [ObservableProperty] private string _statusPath = "";
    [ObservableProperty] private string _statusMessage = "";
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
        Library.SetAfterRefresh(() => Editor.MarkMissingFiles());
        Compile.Bind(Editor, Preview, () => Settings, PersistLayout);

        if (!string.IsNullOrWhiteSpace(Settings.LibraryRoot) &&
            Directory.Exists(Settings.LibraryRoot))
        {
            Library.Load(Settings.LibraryRoot);
            StatusPath = Settings.LibraryRoot;
        }
    }

    public void EnsureLibraryOnStartup()
    {
        if (_firstRunChecked) return;
        _firstRunChecked = true;

        if (!string.IsNullOrWhiteSpace(Settings.LibraryRoot) &&
            Directory.Exists(Settings.LibraryRoot))
            return;

        OpenLibrary();
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
        StatusMessage = "";
        _settingsStore.Save(Settings);
    }

    [RelayCommand]
    private void Save()
    {
        Editor.SaveActive();
        PersistLayout();
        if (Settings.AutoCompileOnSave && Compile.CompileCommand.CanExecute(null))
            _ = Compile.CompileCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void TogglePreview()
    {
        Preview.IsVisible = !Preview.IsVisible;
        Settings.PreviewVisible = Preview.IsVisible;
        PersistLayout();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var updated = SettingsDialog.ShowAndGet(Settings);
        if (updated == null)
            return;

        var rootChanged = !string.Equals(
            Settings.LibraryRoot, updated.LibraryRoot, StringComparison.OrdinalIgnoreCase);

        Settings.LibraryRoot = updated.LibraryRoot;
        Settings.LatexMkPath = updated.LatexMkPath;
        Settings.XeLatexPath = updated.XeLatexPath;
        Settings.PdfLatexPath = updated.PdfLatexPath;
        Settings.AutoCompileOnSave = updated.AutoCompileOnSave;
        _settingsStore.Save(Settings);

        if (rootChanged &&
            !string.IsNullOrWhiteSpace(Settings.LibraryRoot) &&
            Directory.Exists(Settings.LibraryRoot))
        {
            Library.Load(Settings.LibraryRoot);
            StatusPath = Settings.LibraryRoot;
        }

        StatusMessage = "Settings saved.";
    }

    [RelayCommand]
    private void OpenSelectedNode()
    {
        var node = Library.SelectedNode;
        if (node == null || node.IsDirectory)
            return;
        Editor.OpenFile(node.FullPath);
        StatusPath = node.FullPath;
        StatusMessage = "";
    }

    [RelayCommand]
    private void NewTex()
    {
        var parent = Library.GetCreateParentDirectory();
        if (parent == null)
        {
            StatusMessage = "Open a library first.";
            return;
        }

        var name = NamePrompt.Ask("New TeX file", "untitled.tex");
        if (string.IsNullOrWhiteSpace(name))
            return;

        try
        {
            var path = Library.Service.CreateTexFile(parent, name);
            Library.Refresh();
            Editor.OpenFile(path);
            StatusPath = path;
            StatusMessage = "Created " + Path.GetFileName(path);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void NewFolder()
    {
        var parent = Library.GetCreateParentDirectory();
        if (parent == null)
        {
            StatusMessage = "Open a library first.";
            return;
        }

        var name = NamePrompt.Ask("New folder", "folder");
        if (string.IsNullOrWhiteSpace(name))
            return;

        try
        {
            var path = Library.Service.CreateFolder(parent, name);
            Library.Refresh();
            StatusPath = path;
            StatusMessage = "Created folder " + Path.GetFileName(path);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void RenameNode()
    {
        var node = Library.SelectedNode;
        if (node == null)
        {
            StatusMessage = "Select a file or folder.";
            return;
        }

        var name = NamePrompt.Ask("Rename", node.Name);
        if (string.IsNullOrWhiteSpace(name) || name == node.Name)
            return;

        var oldPath = node.FullPath;
        try
        {
            Library.Service.Rename(oldPath, name);
            var parent = Path.GetDirectoryName(oldPath) ?? "";
            var newPath = Path.Combine(parent, name);
            Editor.RemapPath(oldPath, newPath);
            Library.Refresh();
            StatusPath = newPath;
            StatusMessage = "Renamed to " + name;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void DeleteNode()
    {
        var node = Library.SelectedNode;
        if (node == null)
        {
            StatusMessage = "Select a file or folder.";
            return;
        }

        var confirm = ConfirmDialog.Show(
            "Confirm delete",
            $"Delete '{node.Name}'? This cannot be undone.",
            primaryText: "Delete",
            secondaryText: null,
            cancelText: "Cancel");
        if (confirm != ConfirmResult.Primary)
            return;

        try
        {
            Library.Service.Delete(node.FullPath);
            Library.Refresh();
            Editor.MarkMissingFiles();
            StatusMessage = "Deleted " + node.Name;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void RefreshLibrary()
    {
        Library.Refresh();
        StatusMessage = "Library refreshed.";
    }

    public void ReportIllegalMove()
    {
        StatusMessage = "Cannot move into its own subfolder.";
    }

    public void ReportStatus(string message) => StatusMessage = message;

    public bool ConfirmCloseSession() => Editor.ConfirmCloseAll();

    public void PersistSession(Window window)
    {
        Settings.LibraryPaneWidth = LibraryPaneWidth;
        Settings.PreviewPaneWidth = PreviewPaneWidth;
        Settings.PreviewVisible = Preview.IsVisible;
        Settings.DefaultEngine = Compile.SelectedEngine;
        Settings.WindowMaximized = window.WindowState == WindowState.Maximized;
        if (window.WindowState == WindowState.Normal)
        {
            Settings.WindowWidth = window.Width;
            Settings.WindowHeight = window.Height;
        }
        _settingsStore.Save(Settings);
    }

    public void ApplyWindowGeometry(Window window)
    {
        if (Settings.WindowWidth > 400)
            window.Width = Settings.WindowWidth;
        if (Settings.WindowHeight > 300)
            window.Height = Settings.WindowHeight;
        window.WindowState = Settings.WindowMaximized
            ? WindowState.Maximized
            : WindowState.Normal;
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