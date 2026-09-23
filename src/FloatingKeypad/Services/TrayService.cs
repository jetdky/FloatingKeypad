using System.Drawing;
using System.Windows.Forms;

namespace FloatingKeypad.Services;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly ToolStripMenuItem _exitItem;
    private readonly Icon _appIcon;
    private bool _windowsVisible = true;

    public event Action? SettingsRequested;

    public event Action? ToggleRequested;

    public event Action? ExitRequested;

    public TrayService()
    {
        var menu = new ContextMenuStrip();

        _settingsItem = new ToolStripMenuItem();
        _settingsItem.Click += (_, _) => SettingsRequested?.Invoke();

        _toggleItem = new ToolStripMenuItem();
        _toggleItem.Click += (_, _) => ToggleRequested?.Invoke();

        _exitItem = new ToolStripMenuItem();
        _exitItem.Click += (_, _) => ExitRequested?.Invoke();

        menu.Items.Add(_settingsItem);
        menu.Items.Add(_toggleItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_exitItem);

        using (var stream = System.Windows.Application.GetResourceStream(
                   new Uri("pack://application:,,,/Assets/logo.ico"))!.Stream)
        {
            _appIcon = new Icon(stream);
        }

        _icon = new NotifyIcon
        {
            Icon = _appIcon,
            Visible = true,
            ContextMenuStrip = menu
        };
        _icon.DoubleClick += (_, _) => SettingsRequested?.Invoke();

        Localization.Instance.PropertyChanged += (_, _) => UpdateTexts();
        UpdateTexts();
    }

    public void SetVisibleState(bool windowsVisible)
    {
        _windowsVisible = windowsVisible;
        UpdateTexts();
    }

    private void UpdateTexts()
    {
        _settingsItem.Text = Localization.T("TraySettings");
        _toggleItem.Text = Localization.T(_windowsVisible ? "TrayHide" : "TrayShow");
        _exitItem.Text = Localization.T("TrayExit");
        _icon.Text = Localization.T("TrayTip");
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _appIcon.Dispose();
    }
}
