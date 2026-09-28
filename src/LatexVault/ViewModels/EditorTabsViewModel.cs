using System.Collections.ObjectModel;
using System.Text;
using LatexVault.Models;
using LatexVault.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexVault.ViewModels;

public partial class EditorTabsViewModel : ObservableObject
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    [ObservableProperty] private DocumentTab? _selectedTab;

    public ObservableCollection<DocumentTab> Tabs { get; } = new();

    public bool AnyDirty => Tabs.Any(t => t.IsDirty);

    public void OpenFile(string path)
    {
        path = Path.GetFullPath(path);
        var existing = Tabs.FirstOrDefault(t =>
            string.Equals(t.FilePath, path, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            SelectedTab = existing;
            return;
        }

        var exists = File.Exists(path);
        var text = exists ? File.ReadAllText(path, Encoding.UTF8) : "";
        var tab = new DocumentTab
        {
            Title = Path.GetFileName(path),
            FilePath = path,
            IsMissing = !exists
        };
        tab.SetTextFromDisk(text);
        Tabs.Add(tab);
        SelectedTab = tab;
    }

    public void SaveActive()
    {
        if (SelectedTab == null || string.IsNullOrWhiteSpace(SelectedTab.FilePath))
            return;
        File.WriteAllText(SelectedTab.FilePath, SelectedTab.Text ?? "", Utf8NoBom);
        SelectedTab.IsDirty = false;
        SelectedTab.IsMissing = false;
    }

    public void SaveTab(DocumentTab tab)
    {
        if (string.IsNullOrWhiteSpace(tab.FilePath))
            return;
        File.WriteAllText(tab.FilePath, tab.Text ?? "", Utf8NoBom);
        tab.IsDirty = false;
        tab.IsMissing = false;
    }

    public void CloseTab(DocumentTab? tab = null)
    {
        tab ??= SelectedTab;
        if (tab == null)
            return;

        if (tab.IsDirty)
        {
            var result = ConfirmDialog.Show(
                "Unsaved changes",
                $"Save changes to {tab.Title} before closing?",
                primaryText: "Save",
                secondaryText: "Don't Save",
                cancelText: "Cancel");
            if (result == ConfirmResult.Cancel)
                return;
            if (result == ConfirmResult.Primary)
                SaveTab(tab);
        }

        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        if (SelectedTab == tab)
            SelectedTab = Tabs.Count == 0 ? null : Tabs[Math.Clamp(index, 0, Tabs.Count - 1)];
    }

    public bool ConfirmCloseAll()
    {
        foreach (var tab in Tabs.Where(t => t.IsDirty).ToList())
        {
            var result = ConfirmDialog.Show(
                "Unsaved changes",
                $"Save changes to {tab.Title} before closing?",
                primaryText: "Save",
                secondaryText: "Don't Save",
                cancelText: "Cancel");
            if (result == ConfirmResult.Cancel)
                return false;
            if (result == ConfirmResult.Primary)
                SaveTab(tab);
        }
        return true;
    }

    public void MarkMissingFiles()
    {
        foreach (var tab in Tabs)
        {
            if (string.IsNullOrWhiteSpace(tab.FilePath))
                continue;
            tab.IsMissing = !File.Exists(tab.FilePath);
        }
    }

    public void RemapPath(string oldPath, string newPath)
    {
        oldPath = Path.GetFullPath(oldPath);
        newPath = Path.GetFullPath(newPath);
        var oldPrefix = oldPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        + Path.DirectorySeparatorChar;

        foreach (var tab in Tabs)
        {
            if (string.IsNullOrWhiteSpace(tab.FilePath))
                continue;
            var full = Path.GetFullPath(tab.FilePath);
            if (string.Equals(full, oldPath, StringComparison.OrdinalIgnoreCase))
            {
                tab.FilePath = newPath;
                tab.Title = Path.GetFileName(newPath);
                tab.IsMissing = !File.Exists(newPath);
            }
            else if (full.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var remapped = Path.Combine(newPath, full.Substring(oldPrefix.Length));
                tab.FilePath = remapped;
                tab.Title = Path.GetFileName(remapped);
                tab.IsMissing = !File.Exists(remapped);
            }
        }
    }
}