using System.IO;
using System.Text.Json;
using FloatingKeypad.Models;

namespace FloatingKeypad.Services;

public sealed class AppConfig
{
    public string Language { get; set; } = string.Empty;

    public AppearanceConfig Appearance { get; set; } = new();

    public List<ButtonConfig> Buttons { get; set; } = new();
}

public static class ConfigService
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FloatingKeypad");

    public static string FilePath { get; } = Path.Combine(Dir, "config.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppConfig>(json, Options) ?? new AppConfig();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载配置失败: {ex.Message}");
        }

        return new AppConfig();
    }

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(config, Options));
    }
}
