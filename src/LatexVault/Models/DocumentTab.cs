using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexVault.Models;

public partial class DocumentTab : ObservableObject
{
    private bool _suppressDirty;

    [ObservableProperty] private string _title = "untitled.tex";
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private string _text = "";
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private bool _isMissing;

    partial void OnTextChanged(string value)
    {
        if (!_suppressDirty)
            IsDirty = true;
    }

    public void SetTextFromDisk(string text)
    {
        _suppressDirty = true;
        try
        {
            Text = text ?? "";
        }
        finally
        {
            _suppressDirty = false;
        }
        IsDirty = false;
    }
}