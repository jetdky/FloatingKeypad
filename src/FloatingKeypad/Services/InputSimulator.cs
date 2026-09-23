using System.Runtime.InteropServices;
using FloatingKeypad.Models;

namespace FloatingKeypad.Services;

public static class InputSimulator
{
    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_XDOWN = 0x0080;
    private const uint MOUSEEVENTF_XUP = 0x0100;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint MOUSEEVENTF_HWHEEL = 0x1000;

    private const int XBUTTON1 = 0x0001;
    private const int XBUTTON2 = 0x0002;
    private const int WHEEL_DELTA = 120;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    public static void Execute(IReadOnlyList<InputEvent> events)
    {
        foreach (var e in events)
        {
            switch (e)
            {
                case KeyEvent k:
                    SendKey(k.Vk, k.Extended, k.Down);
                    break;
                case MouseEvent m:
                    SendMouse(m);
                    break;
            }
        }
    }

    private static void SendKey(ushort vk, bool extended, bool down)
    {
        var flags = 0u;
        if (extended)
        {
            flags |= KEYEVENTF_EXTENDEDKEY;
        }

        if (!down)
        {
            flags |= KEYEVENTF_KEYUP;
        }

        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, dwFlags = flags } }
        };

        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    private static void SendMouse(MouseEvent m)
    {
        if (m.Action is MouseAction.WheelUp or MouseAction.WheelDown
            or MouseAction.WheelLeft or MouseAction.WheelRight)
        {
            SendWheel(m.Action);
            return;
        }

        var (downFlag, upFlag) = ButtonFlags(m.Button);
        var data = ButtonData(m.Button);
        switch (m.Action)
        {
            case MouseAction.Down:
                SendMouseInput(downFlag, data);
                break;
            case MouseAction.Up:
                SendMouseInput(upFlag, data);
                break;
            default:
                SendMouseInput(downFlag, data);
                SendMouseInput(upFlag, data);
                break;
        }
    }

    private static (uint Down, uint Up) ButtonFlags(MouseButton button) => button switch
    {
        MouseButton.Left => (MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP),
        MouseButton.Right => (MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP),
        MouseButton.Middle => (MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP),
        _ => (MOUSEEVENTF_XDOWN, MOUSEEVENTF_XUP)
    };

    private static uint ButtonData(MouseButton button) => button switch
    {
        MouseButton.X1 => (uint)XBUTTON1 << 16,
        MouseButton.X2 => (uint)XBUTTON2 << 16,
        _ => 0
    };

    private static void SendWheel(MouseAction action)
    {
        var horizontal = action is MouseAction.WheelLeft or MouseAction.WheelRight;
        var delta = action switch
        {
            MouseAction.WheelUp => WHEEL_DELTA,
            MouseAction.WheelDown => -WHEEL_DELTA,
            MouseAction.WheelLeft => -WHEEL_DELTA,
            _ => WHEEL_DELTA
        };

        SendMouseInput(horizontal ? MOUSEEVENTF_HWHEEL : MOUSEEVENTF_WHEEL, unchecked((uint)delta));
    }

    private static void SendMouseInput(uint flags, uint mouseData)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion { mi = new MOUSEINPUT { mouseData = mouseData, dwFlags = flags } }
        };

        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }
}
