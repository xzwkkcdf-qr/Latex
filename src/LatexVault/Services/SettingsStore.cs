using System.Text;
using System.Text.Json;
using LatexVault.Models;

namespace LatexVault.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly string _filePath;

    public SettingsStore(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "settings.json");
    }

    public static SettingsStore ForAppData() =>
        new(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LatexVault"));

    public AppSettings Load()
    {
        if (!File.Exists(_filePath))
            return new AppSettings();
        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_filePath, json, Utf8NoBom);
    }
}