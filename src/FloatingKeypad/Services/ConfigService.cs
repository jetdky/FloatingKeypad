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
                return JsonSerializer.Deserialize<AppConfig>(json, Options) ?? Default();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载配置失败: {ex.Message}");
        }

        return Default();
    }

    private static AppConfig Default()
    {
        try
        {
            using var stream = typeof(ConfigService).Assembly
                .GetManifestResourceStream("FloatingKeypad.Assets.default-config.json");
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                return JsonSerializer.Deserialize<AppConfig>(reader.ReadToEnd(), Options) ?? new AppConfig();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载默认配置失败: {ex.Message}");
        }

        return new AppConfig();
    }

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(config, Options));
    }

    public static AppConfig LoadFrom(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppConfig>(json, Options) ?? new AppConfig();
    }

    public static void ExportTo(string path, AppConfig config)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(config, Options));
    }
}
