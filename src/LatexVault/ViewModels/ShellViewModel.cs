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
        EngineLocator.ApplyDetectedPaths(Settings);
        if (Settings.DefaultEngine == CompileEngine.LatexMk &&
            string.IsNullOrWhiteSpace(Settings.LatexMkPath) &&
            !string.IsNullOrWhiteSpace(Settings.XeLatexPath))
        {
            Settings.DefaultEngine = CompileEngine.XeLatex;
        }

        _settingsStore.Save(Settings);
        LibraryPaneWidth = Settings.LibraryPaneWidth > 0 ? Settings.LibraryPaneWidth : 280;
        PreviewPaneWidth = Settings.PreviewPaneWidth > 0 ? Settings.PreviewPaneWidth : 360;
        Preview.IsVisible = Settings.PreviewVisible;
        Compile.SelectedEngine = Settings.DefaultEngine;
        Library.SetAfterRefresh(() => Editor.MarkMissingFiles());
        Compile.Bind(Editor, Preview, () => Settings, PersistLayout);
        Preview.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PreviewViewModel.StatusMessage) &&
                !string.IsNullOrWhiteSpace(Preview.StatusMessage))
                StatusMessage = Preview.StatusMessage;
        };
        OpenDefaultLibrary(persistIfChanged: true);
    }

    public void EnsureLibraryOnStartup()
    {
        if (_firstRunChecked) return;
        _firstRunChecked = true;
        OpenDefaultLibrary(persistIfChanged: true);
    }

    private void OpenDefaultLibrary(bool persistIfChanged)
    {
        var root = AppPaths.EnsureDefaultLibrary();
        var changed = !string.Equals(Settings.LibraryRoot, root, StringComparison.OrdinalIgnoreCase);
        Library.Load(root);
        Settings.LibraryRoot = root;
        StatusPath = root;
        StatusMessage = "";
        if (persistIfChanged && changed)
            _settingsStore.Save(Settings);
    }

    [RelayCommand]
    private void ImportTex()
    {
        var parent = Library.GetCreateParentDirectory()
                     ?? (Directory.Exists(Settings.LibraryRoot) ? Settings.LibraryRoot : null)
                     ?? AppPaths.EnsureDefaultLibrary();

        var dlg = new OpenFileDialog
        {
            Title = "Import TeX files",
            Filter = "TeX files (*.tex)|*.tex|All files (*.*)|*.*",
            Multiselect = true
        };
        if (dlg.ShowDialog() != true || dlg.FileNames.Length == 0)
            return;

        string? firstImported = null;
        var count = 0;
        try
        {
            foreach (var source in dlg.FileNames)
            {
                if (!LibraryPathRules.IsTexFile(source))
                    continue;

                var destName = Path.GetFileName(source);
                var dest = Path.Combine(parent, destName);
                if (File.Exists(dest))
                {
                    var stem = Path.GetFileNameWithoutExtension(destName);
                    var ext = Path.GetExtension(destName);
                    var i = 1;
                    do
                    {
                        dest = Path.Combine(parent, $"{stem}-{i}{ext}");
                        i++;
                    } while (File.Exists(dest));
                }

                File.Copy(source, dest);
                firstImported ??= dest;
                count++;
            }

            Library.Refresh();
            if (firstImported != null)
            {
                Editor.OpenFile(firstImported);
                StatusPath = firstImported;
            }

            StatusMessage = count == 0
                ? "No .tex files imported."
                : $"Imported {count} file(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
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

        Settings.LatexMkPath = updated.LatexMkPath;
        Settings.XeLatexPath = updated.XeLatexPath;
        Settings.PdfLatexPath = updated.PdfLatexPath;
        Settings.AutoCompileOnSave = updated.AutoCompileOnSave;
        Settings.LibraryRoot = AppPaths.EnsureDefaultLibrary();
        _settingsStore.Save(Settings);
        OpenDefaultLibrary(persistIfChanged: false);
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

    public void ReportIllegalMove() => StatusMessage = "Cannot move into its own subfolder.";
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
