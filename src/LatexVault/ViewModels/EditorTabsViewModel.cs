using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LatexVault.Models;

namespace LatexVault.ViewModels;

public partial class EditorTabsViewModel : ObservableObject
{
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

        var text = File.Exists(path) ? File.ReadAllText(path) : "";
        var tab = new DocumentTab
        {
            Title = Path.GetFileName(path),
            FilePath = path,
            Text = text,
            IsDirty = false,
            IsMissing = !File.Exists(path)
        };
        Tabs.Add(tab);
        SelectedTab = tab;
    }

    public void SaveSelected()
    {
        if (SelectedTab == null || string.IsNullOrWhiteSpace(SelectedTab.FilePath))
            return;
        File.WriteAllText(SelectedTab.FilePath, SelectedTab.Text ?? "");
        SelectedTab.IsDirty = false;
        SelectedTab.IsMissing = false;
    }
}