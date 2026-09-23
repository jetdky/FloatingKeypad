using System.Windows;
using FloatingKeypad.Models;
using FloatingKeypad.Services;
using Localization = FloatingKeypad.Services.Localization;

namespace FloatingKeypad.Views;

public partial class KeyCaptureDialog : Window
{
    private readonly InputHook _hook = new();
    private readonly List<ushort> _captured = new();
    private readonly HashSet<ushort> _pressed = new();
    private readonly Dictionary<ushort, bool> _extMap = new();
    private MouseButton? _capturedMouseButton;
    private bool _mouseDown;
    private MouseAction? _wheel;
    private bool _done;
    private string _initialPreview = string.Empty;

    public List<InputEvent>? Result { get; private set; }

    public KeyCaptureDialog(IEnumerable<InputEvent>? existing = null)
    {
        InitializeComponent();

        var list = existing?.ToList();
        if (list is { Count: > 0 })
        {
            _initialPreview = new ButtonConfig { Events = list }.Describe();
            Preview.Text = _initialPreview;
        }

        _hook.KeyChanged += OnKeyChanged;
        _hook.MouseChanged += OnMouseChanged;
        Loaded += (_, _) => _hook.Start();
        Closed += (_, _) => _hook.Dispose();
    }

    private void OnKeyChanged(ushort vk, bool extended, bool down)
    {
        if (_done)
        {
            return;
        }

        if (down)
        {
            if (vk == 0x1B && _captured.Count == 0 && _pressed.Count == 0 && _capturedMouseButton == null)
            {
                DialogResult = false;
                Close();
                return;
            }

            if (!_pressed.Contains(vk))
            {
                _pressed.Add(vk);
                _captured.Add(vk);
                _extMap[vk] = extended;
            }

            UpdatePreview();
        }
        else
        {
            _pressed.Remove(vk);
            if (_pressed.Count == 0 && !_mouseDown && _captured.Count > 0)
            {
                Complete();
            }
            else
            {
                UpdatePreview();
            }
        }
    }

    private void OnMouseChanged(MouseButton button, MouseAction action)
    {
        if (_done)
        {
            return;
        }

        if (action is MouseAction.WheelUp or MouseAction.WheelDown
            or MouseAction.WheelLeft or MouseAction.WheelRight)
        {
            _wheel = action;
            Complete();
            return;
        }

        if (action == MouseAction.Down)
        {
            _mouseDown = true;
            _capturedMouseButton = button;
            UpdatePreview();
        }
        else
        {
            _mouseDown = false;
            if (_pressed.Count == 0 && (_captured.Count > 0 || _capturedMouseButton != null))
            {
                Complete();
            }
        }
    }

    private void Complete()
    {
        _done = true;
        var events = new List<InputEvent>();

        var mods = _captured.Where(KeyNames.IsModifier).ToList();
        var mains = _captured.Where(v => !KeyNames.IsModifier(v)).ToList();

        foreach (var m in mods)
        {
            events.Add(MakeKey(m, true));
        }

        foreach (var k in mains)
        {
            events.Add(MakeKey(k, true));
            events.Add(MakeKey(k, false));
        }

        if (_capturedMouseButton is { } btn)
        {
            events.Add(new MouseEvent { Button = btn, Action = MouseAction.Click });
        }

        if (_wheel is { } wheel)
        {
            events.Add(new MouseEvent { Action = wheel });
        }

        foreach (var m in Enumerable.Reverse(mods))
        {
            events.Add(MakeKey(m, false));
        }

        Result = events;
        DialogResult = true;
    }

    private KeyEvent MakeKey(ushort vk, bool down) =>
        new() { Vk = vk, Extended = _extMap.GetValueOrDefault(vk), Down = down };

    private void UpdatePreview()
    {
        if (_done)
        {
            return;
        }

        var temp = _captured
            .Select(vk => (InputEvent)MakeKey(vk, true))
            .ToList();

        if (_capturedMouseButton is { } btn)
        {
            temp.Add(new MouseEvent { Button = btn, Action = MouseAction.Click });
        }

        Preview.Text = temp.Count == 0
            ? _initialPreview
            : new ButtonConfig { Events = temp }.Describe();
    }
}
