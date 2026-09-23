using System.Windows.Forms;
using FloatingKeypad.Services;

namespace FloatingKeypad.Models;

public static class KeyNames
{
    private static readonly Dictionary<ushort, string> Special = new()
    {
        [0x10] = "Shift",
        [0x11] = "Ctrl",
        [0x12] = "Alt",
        [0x08] = "Backspace",
        [0x09] = "Tab",
        [0x0D] = "Enter",
        [0x13] = "Pause",
        [0x14] = "CapsLock",
        [0x1B] = "Esc",
        [0x20] = "Space",
        [0x21] = "PageUp",
        [0x22] = "PageDown",
        [0x23] = "End",
        [0x24] = "Home",
        [0x25] = "←",
        [0x26] = "↑",
        [0x27] = "→",
        [0x28] = "↓",
        [0x2C] = "PrintScreen",
        [0x2D] = "Insert",
        [0x2E] = "Delete",
        [0x5B] = "Win",
        [0x5C] = "Win",
        [0x5D] = "Menu",
        [0x90] = "NumLock",
        [0x91] = "ScrollLock",
        [0xA0] = "Shift",
        [0xA1] = "Shift",
        [0xA2] = "Ctrl",
        [0xA3] = "Ctrl",
        [0xA4] = "Alt",
        [0xA5] = "Alt"
    };

    private static readonly HashSet<ushort> Modifiers = new()
    {
        0x10, 0x11, 0x12, 0x5B, 0x5C,
        0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5
    };

    public static bool IsModifier(ushort vk) => Modifiers.Contains(vk);

    public static string Get(ushort vk)
    {
        if (Special.TryGetValue(vk, out var s))
        {
            return s;
        }

        if (vk is >= 0x30 and <= 0x39)
        {
            return ((char)vk).ToString();
        }

        if (vk is >= 0x41 and <= 0x5A)
        {
            return ((char)vk).ToString();
        }

        if (vk is >= 0x60 and <= 0x69)
        {
            return "Num" + (vk - 0x60);
        }

        if (vk is >= 0x6A and <= 0x6F)
        {
            return vk switch
            {
                0x6A => "Num*",
                0x6B => "Num+",
                0x6D => "Num-",
                0x6E => "Num.",
                0x6F => "Num/",
                _ => "Num"
            };
        }

        if (vk is >= 0x70 and <= 0x87)
        {
            return "F" + (vk - 0x6F);
        }

        return ((Keys)vk).ToString();
    }

    public static string Describe(MouseEvent m)
    {
        return m.Action switch
        {
            MouseAction.WheelUp => Localization.T("WheelUp"),
            MouseAction.WheelDown => Localization.T("WheelDown"),
            MouseAction.WheelLeft => Localization.T("WheelLeft"),
            MouseAction.WheelRight => Localization.T("WheelRight"),
            MouseAction.Down => Localization.T(ButtonKey(m.Button, "Down")),
            MouseAction.Up => Localization.T(ButtonKey(m.Button, "Up")),
            _ => Localization.T(ButtonKey(m.Button, "Click"))
        };
    }

    private static string ButtonKey(MouseButton button, string suffix) => button switch
    {
        MouseButton.Left => "MouseLeft" + suffix,
        MouseButton.Right => "MouseRight" + suffix,
        MouseButton.Middle => "MouseMiddle" + suffix,
        MouseButton.X1 => "MouseX1" + suffix,
        _ => "MouseX2" + suffix
    };
}
