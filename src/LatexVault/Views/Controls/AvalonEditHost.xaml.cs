using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;

namespace LatexVault.Views.Controls;

public partial class AvalonEditHost : UserControl
{
    private bool _internalChange;

    public AvalonEditHost()
    {
        InitializeComponent();
        LoadTexHighlighting();
        SearchPanel.Install(Editor);
        Editor.TextChanged += Editor_TextChanged;
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(AvalonEditHost),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnTextPropertyChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(
            nameof(IsReadOnly),
            typeof(bool),
            typeof(AvalonEditHost),
            new PropertyMetadata(false, OnIsReadOnlyChanged));

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var host = (AvalonEditHost)d;
        if (host._internalChange)
            return;
        host._internalChange = true;
        try
        {
            var next = e.NewValue as string ?? "";
            if (host.Editor.Text != next)
                host.Editor.Text = next;
        }
        finally
        {
            host._internalChange = false;
        }
    }

    private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((AvalonEditHost)d).Editor.IsReadOnly = e.NewValue is true;
    }

    private void Editor_TextChanged(object? sender, EventArgs e)
    {
        if (_internalChange)
            return;
        _internalChange = true;
        try
        {
            SetCurrentValue(TextProperty, Editor.Text);
        }
        finally
        {
            _internalChange = false;
        }
    }

    private void LoadTexHighlighting()
    {
        try
        {
            using var stream = typeof(AvalonEditHost).Assembly
                .GetManifestResourceStream("LatexVault.Assets.Tex-Mode.xshd");
            if (stream == null)
                return;
            using var reader = XmlReader.Create(stream);
            Editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
        catch
        {
            // Keep editor usable without highlighting.
        }
    }
}