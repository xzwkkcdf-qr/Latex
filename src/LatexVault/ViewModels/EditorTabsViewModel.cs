using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using LatexVault.Models;

namespace LatexVault.ViewModels;

public partial class EditorTabsViewModel : ObservableObject
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    [ObservableProperty] private DocumentTab? _selectedTab;

    public ObservableCollection<DocumentTab> Tabs { get; } = new();

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

    public void CloseTab(DocumentTab? tab = null)
    {
        tab ??= SelectedTab;
        if (tab == null)
            return;

        if (tab.IsDirty)
        {
            var result = MessageBox.Show(
                $"Save changes to {tab.Title} before closing?",
                "Unsaved changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel)
                return;
            if (result == MessageBoxResult.Yes)
            {
                if (string.IsNullOrWhiteSpace(tab.FilePath))
                    return;
                File.WriteAllText(tab.FilePath, tab.Text ?? "", Utf8NoBom);
                tab.IsDirty = false;
            }
        }

        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        if (SelectedTab == tab)
            SelectedTab = Tabs.Count == 0 ? null : Tabs[Math.Clamp(index, 0, Tabs.Count - 1)];
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
}