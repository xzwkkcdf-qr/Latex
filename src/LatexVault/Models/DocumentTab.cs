using CommunityToolkit.Mvvm.ComponentModel;

namespace LatexVault.Models;

public partial class DocumentTab : ObservableObject
{
    [ObservableProperty] private string _title = "untitled.tex";
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private string _text = "";
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private bool _isMissing;
}