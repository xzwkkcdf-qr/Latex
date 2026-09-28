using System.Windows;
using LatexVault.Models;
using LatexVault.Services;

namespace LatexVault.Views;

public partial class SettingsDialog : Window
{
    public AppSettings Settings { get; private set; }

    public SettingsDialog(AppSettings settings)
    {
        InitializeComponent();
        Settings = Clone(settings);
        LibraryRootBox.Text = string.IsNullOrWhiteSpace(Settings.LibraryRoot)
            ? AppPaths.DefaultLibraryRoot
            : Settings.LibraryRoot;
        LatexMkPathBox.Text = Settings.LatexMkPath ?? "";
        XeLatexPathBox.Text = Settings.XeLatexPath ?? "";
        PdfLatexPathBox.Text = Settings.PdfLatexPath ?? "";
        AutoCompileBox.IsChecked = Settings.AutoCompileOnSave;
    }

    public static AppSettings? ShowAndGet(AppSettings settings, Window? owner = null)
    {
        var dlg = new SettingsDialog(settings)
        {
            Owner = owner ?? Application.Current?.MainWindow
        };
        return dlg.ShowDialog() == true ? dlg.Settings : null;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Settings.LibraryRoot = AppPaths.EnsureDefaultLibrary();
        Settings.LatexMkPath = NullIfEmpty(LatexMkPathBox.Text);
        Settings.XeLatexPath = NullIfEmpty(XeLatexPathBox.Text);
        Settings.PdfLatexPath = NullIfEmpty(PdfLatexPathBox.Text);
        Settings.AutoCompileOnSave = AutoCompileBox.IsChecked == true;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static string? NullIfEmpty(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static AppSettings Clone(AppSettings s) => new()
    {
        LibraryRoot = s.LibraryRoot,
        DefaultEngine = s.DefaultEngine,
        AutoCompileOnSave = s.AutoCompileOnSave,
        PreviewVisible = s.PreviewVisible,
        LatexMkPath = s.LatexMkPath,
        XeLatexPath = s.XeLatexPath,
        PdfLatexPath = s.PdfLatexPath,
        LibraryPaneWidth = s.LibraryPaneWidth,
        PreviewPaneWidth = s.PreviewPaneWidth,
        WindowWidth = s.WindowWidth,
        WindowHeight = s.WindowHeight,
        WindowMaximized = s.WindowMaximized
    };
}
