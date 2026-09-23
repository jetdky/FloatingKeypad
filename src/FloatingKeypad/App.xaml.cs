using System.Globalization;
using System.Threading;
using System.Windows;
using FloatingKeypad.Models;
using FloatingKeypad.Services;
using FloatingKeypad.Views;
using Application = System.Windows.Application;
using Localization = FloatingKeypad.Services.Localization;

namespace FloatingKeypad;

public partial class App : Application
{
    private Mutex? _mutex;
    private TrayService? _tray;
    private SettingsWindow? _settings;
    private readonly List<FloatingButtonWindow> _windows = new();
    private bool _windowsVisible = true;

    public static new App Current => (App)Application.Current;

    public AppConfig Config { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "FloatingKeypad.SingleInstance", out var created);
        if (!created)
        {
            MessageBox.Show("悬浮键鼠助手已在运行。", "悬浮键鼠助手", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        Config = ConfigService.Load();
        ApplyLanguage();
        EnsureDefaults();

        _tray = new TrayService();
        _tray.SettingsRequested += () => OpenSettings(null);
        _tray.ToggleRequested += ToggleWindows;
        _tray.ExitRequested += ExitApp;

        RebuildWindows();
    }

    private void ApplyLanguage()
    {
        var lang = Config.Language;
        if (string.IsNullOrEmpty(lang))
        {
            lang = CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? "zh-CN"
                : "en";
        }

        Localization.Instance.SetLanguage(lang);
    }

    public void SetLanguage(string lang)
    {
        Config.Language = lang;
        Localization.Instance.SetLanguage(lang);
        SaveConfig();
        RebuildWindows();
    }

    public void ApplyConfig(AppConfig config)
    {
        Config = config;
        ApplyLanguage();
        SaveConfig();
        RebuildWindows();
    }

    private void EnsureDefaults()
    {
        if (Config.Buttons.Count > 0)
        {
            return;
        }

        Config.Buttons.Add(new ButtonConfig
        {
            Label = "复制",
            Left = 320,
            Top = 320,
            Events = new List<InputEvent>
            {
                new KeyEvent { Vk = 0x11, Down = true },
                new KeyEvent { Vk = 0x43, Down = true },
                new KeyEvent { Vk = 0x43, Down = false },
                new KeyEvent { Vk = 0x11, Down = false }
            }
        });
        ConfigService.Save(Config);
    }

    public void RebuildWindows()
    {
        foreach (var w in _windows)
        {
            w.Close();
        }

        _windows.Clear();

        foreach (var b in Config.Buttons)
        {
            var w = new FloatingButtonWindow(b);
            _windows.Add(w);
            w.Show();
        }
    }

    public void ToggleWindows()
    {
        _windowsVisible = !_windowsVisible;
        foreach (var w in _windows)
        {
            w.Visibility = _windowsVisible ? Visibility.Visible : Visibility.Hidden;
        }

        _tray?.SetVisibleState(_windowsVisible);
    }

    public void SaveConfig() => ConfigService.Save(Config);

    public void RefreshWindows()
    {
        foreach (var w in _windows)
        {
            w.Apply();
        }
    }

    public void SetOverlaysClickThrough(bool enabled)
    {
        foreach (var w in _windows)
        {
            w.SetClickThrough(enabled);
        }
    }

    public void OpenSettings(ButtonConfig? target)
    {
        if (_settings == null)
        {
            _settings = new SettingsWindow(target);
            _settings.Closed += (_, _) => _settings = null;
        }
        else if (target != null)
        {
            _settings.SelectButton(target);
        }

        BringToFront(_settings);
    }

    private static void BringToFront(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        if (!window.IsVisible)
        {
            window.Show();
        }

        var wasTopmost = window.Topmost;
        window.Topmost = true;
        window.Topmost = wasTopmost;
        window.Activate();
    }

    private void ExitApp()
    {
        SaveConfig();
        _tray?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
