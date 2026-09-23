using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FloatingKeypad.Models;
using FloatingKeypad.Services;
using Localization = FloatingKeypad.Services.Localization;

namespace FloatingKeypad.Views;

public partial class SettingsWindow : Window
{
    private readonly List<ButtonConfig> _buttons;
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(600) };
    private bool _loading = true;

    public SettingsWindow(ButtonConfig? target)
    {
        InitializeComponent();

        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            App.Current.SaveConfig();
        };

        _buttons = App.Current.Config.Buttons;
        RefreshList();

        if (target != null && _buttons.Contains(target))
        {
            ButtonList.SelectedItem = target;
        }
        else if (_buttons.Count > 0)
        {
            ButtonList.SelectedIndex = 0;
        }
        else
        {
            LoadEmpty();
        }

        Closed += (_, _) => App.Current.SaveConfig();
    }

    private ButtonConfig? Current => ButtonList.SelectedItem as ButtonConfig;

    private void RefreshList()
    {
        var selected = Current;
        ButtonList.ItemsSource = null;
        ButtonList.ItemsSource = _buttons;
        if (selected != null && _buttons.Contains(selected))
        {
            ButtonList.SelectedItem = selected;
        }
    }

    public void SelectButton(ButtonConfig target)
    {
        if (_buttons.Contains(target))
        {
            ButtonList.SelectedItem = target;
        }
    }

    private void Config_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new GlobalConfigWindow { Owner = this };
        dialog.ShowDialog();
        ButtonList.Items.Refresh();
        LoadCurrent();
    }

    private void ButtonList_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadCurrent();

    private void LoadCurrent()
    {
        var b = Current;
        if (b == null)
        {
            LoadEmpty();
            return;
        }

        _loading = true;
        if (NameBox.Text != b.Label)
        {
            NameBox.Text = b.Label;
        }

        BindingText.Text = b.Describe();
        _loading = false;
    }

    private void LoadEmpty()
    {
        _loading = true;
        NameBox.Text = string.Empty;
        BindingText.Text = Localization.T("NotSet");
        _loading = false;
    }

    private void NameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        var b = Current;
        if (b == null)
        {
            return;
        }

        b.Label = NameBox.Text;
        ButtonList.Items.Refresh();
        App.Current.RefreshWindows();
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void Capture_Click(object sender, RoutedEventArgs e)
    {
        var b = Current;
        if (b == null)
        {
            return;
        }

        var previous = b.Describe();
        var dlg = new KeyCaptureDialog(b.Events) { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Result is { Count: > 0 })
        {
            b.Events = dlg.Result;
            var description = b.Describe();
            if (string.IsNullOrWhiteSpace(b.Label)
                || b.Label == Localization.T("NewButton")
                || b.Label == previous)
            {
                b.Label = description;
                NameBox.Text = b.Label;
            }

            BindingText.Text = description;
            RefreshList();
            App.Current.RefreshWindows();
            App.Current.SaveConfig();
        }
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var b = new ButtonConfig
        {
            Label = Localization.T("NewButton"),
            Left = 300 + _buttons.Count * 12,
            Top = 300 + _buttons.Count * 12
        };
        _buttons.Add(b);
        RefreshList();
        ButtonList.SelectedItem = b;
        App.Current.SaveConfig();
        App.Current.RebuildWindows();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var b = Current;
        if (b == null)
        {
            return;
        }

        _buttons.Remove(b);
        RefreshList();
        if (_buttons.Count > 0)
        {
            ButtonList.SelectedIndex = 0;
        }
        else
        {
            LoadEmpty();
        }

        App.Current.SaveConfig();
        App.Current.RebuildWindows();
    }
}
