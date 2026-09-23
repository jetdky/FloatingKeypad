using System.Runtime.InteropServices;
using FloatingKeypad.Models;

namespace FloatingKeypad.Services;

public static class InputSimulator
{
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;

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

    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_XBUTTONDOWN = 0x020B;
    private const int WM_XBUTTONUP = 0x020C;
    private const int WM_MOUSEHWHEEL = 0x020E;

    private const int MK_LBUTTON = 0x0001;
    private const int MK_RBUTTON = 0x0002;
    private const int MK_MBUTTON = 0x0010;
    private const int XBUTTON1 = 0x0001;
    private const int XBUTTON2 = 0x0002;
    private const int WHEEL_DELTA = 120;

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
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT p);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT p);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT r);

    public static void Execute(IReadOnlyList<InputEvent> events, IntPtr target)
    {
        foreach (var e in events)
        {
            switch (e)
            {
                case KeyEvent k:
                    SendKey(k.Vk, k.Extended, k.Down);
                    break;
                case MouseEvent m:
                    SendMouse(m, target);
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

    private static void SendMouse(MouseEvent m, IntPtr target)
    {
        if (m.Action is MouseAction.WheelUp or MouseAction.WheelDown
            or MouseAction.WheelLeft or MouseAction.WheelRight)
        {
            SendWheel(m.Action, target);
            return;
        }

        if (!WindowHelper.IsAlive(target))
        {
            return;
        }

        var (msgDown, msgUp, downFlag) = m.Button switch
        {
            MouseButton.Left => (WM_LBUTTONDOWN, WM_LBUTTONUP, MK_LBUTTON),
            MouseButton.Right => (WM_RBUTTONDOWN, WM_RBUTTONUP, MK_RBUTTON),
            MouseButton.Middle => (WM_MBUTTONDOWN, WM_MBUTTONUP, MK_MBUTTON),
            _ => (WM_XBUTTONDOWN, WM_XBUTTONUP, 0)
        };

        var isX = m.Button is MouseButton.X1 or MouseButton.X2;
        var xButton = m.Button == MouseButton.X1 ? XBUTTON1 : XBUTTON2;
        var lParam = MakeClientLParam(target);

        void Post(bool down)
        {
            var wParam = isX
                ? (IntPtr)((xButton << 16) | (down ? downFlag : 0))
                : (IntPtr)(down ? downFlag : 0);
            PostMessage(target, down ? msgDown : msgUp, wParam, lParam);
        }

        switch (m.Action)
        {
            case MouseAction.Down:
                Post(true);
                break;
            case MouseAction.Up:
                Post(false);
                break;
            default:
                Post(true);
                Post(false);
                break;
        }
    }

    private static void SendWheel(MouseAction action, IntPtr target)
    {
        if (!WindowHelper.IsAlive(target))
        {
            return;
        }

        var horizontal = action is MouseAction.WheelLeft or MouseAction.WheelRight;
        var delta = action switch
        {
            MouseAction.WheelUp => WHEEL_DELTA,
            MouseAction.WheelDown => -WHEEL_DELTA,
            MouseAction.WheelLeft => -WHEEL_DELTA,
            _ => WHEEL_DELTA
        };

        GetCursorPos(out var screen);
        var lParam = (IntPtr)((screen.Y << 16) | (screen.X & 0xFFFF));
        var wParam = (IntPtr)(delta << 16);

        PostMessage(target, horizontal ? WM_MOUSEHWHEEL : WM_MOUSEWHEEL, wParam, lParam);
    }

    private static IntPtr MakeClientLParam(IntPtr target)
    {
        GetCursorPos(out var pt);
        ScreenToClient(target, ref pt);

        if (GetClientRect(target, out var rect))
        {
            var w = rect.Right - rect.Left;
            var h = rect.Bottom - rect.Top;
            if (pt.X < 0 || pt.Y < 0 || pt.X > w || pt.Y > h)
            {
                pt.X = w / 2;
                pt.Y = h / 2;
            }
        }

        return (IntPtr)((pt.Y << 16) | (pt.X & 0xFFFF));
    }
}
