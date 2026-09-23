using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using FloatingKeypad.Models;
using FloatingKeypad.Services;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace FloatingKeypad.Views;

public partial class FloatingButtonWindow : Window
{
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT p);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT r);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    private const double ShadowPad = 12;

    private readonly ButtonConfig _config;
    private IntPtr _hwnd;
    private bool _dragging;
    private bool _moved;
    private int _startCursorX;
    private int _startCursorY;
    private int _startWinX;
    private int _startWinY;
    private IntPtr _target;

    public ButtonConfig Config => _config;

    public FloatingButtonWindow(ButtonConfig config)
    {
        InitializeComponent();
        _config = config;
        Apply();
        Loaded += OnLoaded;
    }

    public void Apply()
    {
        var appearance = App.Current.Config.Appearance;
        Left = _config.Left;
        Top = _config.Top;
        Width = appearance.Width + ShadowPad * 2;
        Height = appearance.Height + ShadowPad * 2;
        Opacity = appearance.Opacity;
        LabelText.Text = string.IsNullOrWhiteSpace(_config.Label) ? _config.Describe() : _config.Label;

        try
        {
            Root.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(appearance.Background));
        }
        catch
        {
            Root.Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x2D, 0x7F, 0xF9));
        }

        try
        {
            LabelText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(appearance.Foreground));
        }
        catch
        {
            LabelText.Foreground = Brushes.White;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        WindowHelper.MakeNoActivate(this);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        _target = WindowHelper.Foreground;
        GetCursorPos(out var p);
        _startCursorX = p.X;
        _startCursorY = p.Y;
        if (GetWindowRect(_hwnd, out var r))
        {
            _startWinX = r.Left;
            _startWinY = r.Top;
        }

        _dragging = true;
        _moved = false;
        CaptureMouse();
        base.OnMouseLeftButtonDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging && e.LeftButton == MouseButtonState.Pressed)
        {
            GetCursorPos(out var p);
            var dx = p.X - _startCursorX;
            var dy = p.Y - _startCursorY;
            if (Math.Abs(dx) > 3 || Math.Abs(dy) > 3)
            {
                _moved = true;
            }

            SetWindowPos(_hwnd, IntPtr.Zero, _startWinX + dx, _startWinY + dy, 0, 0,
                SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (_dragging)
        {
            _dragging = false;
            ReleaseMouseCapture();

            if (_moved)
            {
                _config.Left = Left;
                _config.Top = Top;
                App.Current.SaveConfig();
            }
            else
            {
                Trigger();
            }
        }

        base.OnMouseLeftButtonUp(e);
    }

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        App.Current.OpenSettings(_config);
        base.OnMouseRightButtonUp(e);
    }

    private void Trigger()
    {
        if (_config.Events.Count == 0)
        {
            return;
        }

        var target = WindowHelper.IsAlive(_target) ? _target : WindowHelper.Foreground;
        InputSimulator.Execute(_config.Events, target);
    }
}
