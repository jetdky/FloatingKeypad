using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FloatingKeypad.Models;
using FloatingKeypad.Services;
using Localization = FloatingKeypad.Services.Localization;

namespace FloatingKeypad.Views;

public partial class GlobalConfigWindow : Window
{
    private bool _loading = true;

    public GlobalConfigWindow()
    {
        InitializeComponent();

        _loading = true;
        LanguageBox.SelectedIndex = Localization.Instance.Current == "en" ? 1 : 0;
        _loading = false;

        LoadAppearance();
    }

    private static AppearanceConfig Appearance => App.Current.Config.Appearance;

    private void LoadAppearance()
    {
        _loading = true;
        WidthSlider.Value = Appearance.Width;
        WidthBox.Text = Appearance.Width.ToString("0");
        HeightSlider.Value = Appearance.Height;
        HeightBox.Text = Appearance.Height.ToString("0");
        OpacitySlider.Value = Appearance.Opacity;
        OpacityBox.Text = Appearance.Opacity.ToString("0.00");
        BgBox.Text = Appearance.Background;
        FgBox.Text = Appearance.Foreground;
        _loading = false;

        UpdateSwatches();
        UpdatePreview();
    }

    private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading)
        {
            return;
        }

        _loading = true;
        WidthBox.Text = e.NewValue.ToString("0");
        _loading = false;
        Appearance.Width = e.NewValue;
        UpdatePreview();
    }

    private void WidthBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        if (!double.TryParse(WidthBox.Text, out var v) || v < 60 || v > 400)
        {
            return;
        }

        Appearance.Width = v;
        _loading = true;
        WidthSlider.Value = v;
        _loading = false;
        UpdatePreview();
    }

    private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading)
        {
            return;
        }

        _loading = true;
        HeightBox.Text = e.NewValue.ToString("0");
        _loading = false;
        Appearance.Height = e.NewValue;
        UpdatePreview();
    }

    private void HeightBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        if (!double.TryParse(HeightBox.Text, out var v) || v < 30 || v > 200)
        {
            return;
        }

        Appearance.Height = v;
        _loading = true;
        HeightSlider.Value = v;
        _loading = false;
        UpdatePreview();
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading)
        {
            return;
        }

        _loading = true;
        OpacityBox.Text = e.NewValue.ToString("0.00");
        _loading = false;
        Appearance.Opacity = e.NewValue;
        UpdatePreview();
    }

    private void OpacityBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        if (!double.TryParse(OpacityBox.Text, out var v) || v < 0.3 || v > 1.0)
        {
            return;
        }

        Appearance.Opacity = v;
        _loading = true;
        OpacitySlider.Value = v;
        _loading = false;
        UpdatePreview();
    }

    private void BgBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        Appearance.Background = BgBox.Text;
        UpdateSwatches();
        UpdatePreview();
    }

    private void FgBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        Appearance.Foreground = FgBox.Text;
        UpdateSwatches();
        UpdatePreview();
    }

    private void PickBg_Click(object sender, RoutedEventArgs e)
    {
        var picked = PickColor(Appearance.Background);
        if (picked != null)
        {
            BgBox.Text = picked;
        }
    }

    private void PickFg_Click(object sender, RoutedEventArgs e)
    {
        var picked = PickColor(Appearance.Foreground);
        if (picked != null)
        {
            FgBox.Text = picked;
        }
    }

    private static string? PickColor(string current)
    {
        using var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true };
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(current);
            dialog.Color = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        catch
        {
            // 无效颜色时用系统默认
        }

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return null;
        }

        var picked = dialog.Color;
        return $"#{picked.A:X2}{picked.R:X2}{picked.G:X2}{picked.B:X2}";
    }

    private void UpdateSwatches()
    {
        BgSwatch.Background = TryBrush(Appearance.Background, Colors.Transparent);
        FgSwatch.Background = TryBrush(Appearance.Foreground, Colors.Transparent);
    }

    private void UpdatePreview()
    {
        PreviewButton.Width = Appearance.Width;
        PreviewButton.Height = Appearance.Height;
        PreviewButton.Opacity = Appearance.Opacity;
        PreviewButton.Background = TryBrush(Appearance.Background, Colors.Transparent);
        PreviewText.Foreground = TryBrush(Appearance.Foreground, Colors.White);
    }

    private static Brush TryBrush(string hex, Color fallback)
    {
        try
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
        catch
        {
            return new SolidColorBrush(fallback);
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        Appearance.Reset();
        LoadAppearance();
    }

    private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        App.Current.SetLanguage(LanguageBox.SelectedIndex == 1 ? "en" : "zh-CN");
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => Close();

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = Localization.T("ExportTitle"),
            Filter = "JSON (*.json)|*.json",
            FileName = "FloatingKeypad-config.json"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            ConfigService.ExportTo(dialog.FileName, App.Current.Config);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"导出配置失败: {ex.Message}");
            MessageBox.Show(Localization.T("ExportFailed"), Localization.T("ExportTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = Localization.T("ImportTitle"),
            Filter = "JSON (*.json)|*.json",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        AppConfig imported;
        try
        {
            imported = ConfigService.LoadFrom(dialog.FileName);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"导入配置失败: {ex.Message}");
            MessageBox.Show(Localization.T("ImportFailed"), Localization.T("ImportTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(Localization.T("ImportConfirm"), Localization.T("ImportTitle"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        App.Current.ApplyConfig(imported);

        _loading = true;
        LanguageBox.SelectedIndex = Localization.Instance.Current == "en" ? 1 : 0;
        _loading = false;
        LoadAppearance();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        App.Current.SaveConfig();
        App.Current.RebuildWindows();
        base.OnClosing(e);
    }
}
