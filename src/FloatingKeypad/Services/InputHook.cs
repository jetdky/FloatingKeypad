using System.Runtime.InteropServices;
using FloatingKeypad.Models;

namespace FloatingKeypad.Services;

public sealed class InputHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;

    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

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

    private const uint LLMHF_INJECTED = 0x00000001;

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public int x;
        public int y;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private readonly HookProc _keyProc;
    private readonly HookProc _mouseProc;
    private IntPtr _keyHook;
    private IntPtr _mouseHook;

    public event Action<ushort, bool, bool>? KeyChanged;

    public event Action<MouseButton, MouseAction>? MouseChanged;

    public InputHook()
    {
        _keyProc = KeyCallback;
        _mouseProc = MouseCallback;
    }

    public void Start()
    {
        var module = GetModuleHandle(null);
        _keyHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyProc, module, 0);
        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, module, 0);
    }

    public void Dispose()
    {
        if (_keyHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyHook);
            _keyHook = IntPtr.Zero;
        }

        if (_mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
    }

    private IntPtr KeyCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var down = wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN;
            var up = wParam == WM_KEYUP || wParam == WM_SYSKEYUP;
            if (down || up)
            {
                var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                var extended = (data.flags & 0x01) != 0;
                KeyChanged?.Invoke((ushort)data.vkCode, extended, down);
            }
        }

        return CallNextHookEx(_keyHook, nCode, wParam, lParam);
    }

    private IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            if ((data.flags & LLMHF_INJECTED) == 0)
            {
                var xButton = (data.mouseData >> 16) == 1 ? MouseButton.X1 : MouseButton.X2;
                var wheelDelta = (short)(data.mouseData >> 16);

                switch ((int)wParam)
                {
                    case WM_LBUTTONDOWN:
                        MouseChanged?.Invoke(MouseButton.Left, MouseAction.Down);
                        break;
                    case WM_LBUTTONUP:
                        MouseChanged?.Invoke(MouseButton.Left, MouseAction.Up);
                        break;
                    case WM_RBUTTONDOWN:
                        MouseChanged?.Invoke(MouseButton.Right, MouseAction.Down);
                        break;
                    case WM_RBUTTONUP:
                        MouseChanged?.Invoke(MouseButton.Right, MouseAction.Up);
                        break;
                    case WM_MBUTTONDOWN:
                        MouseChanged?.Invoke(MouseButton.Middle, MouseAction.Down);
                        break;
                    case WM_MBUTTONUP:
                        MouseChanged?.Invoke(MouseButton.Middle, MouseAction.Up);
                        break;
                    case WM_XBUTTONDOWN:
                        MouseChanged?.Invoke(xButton, MouseAction.Down);
                        break;
                    case WM_XBUTTONUP:
                        MouseChanged?.Invoke(xButton, MouseAction.Up);
                        break;
                    case WM_MOUSEWHEEL:
                        MouseChanged?.Invoke(MouseButton.Middle, wheelDelta > 0 ? MouseAction.WheelUp : MouseAction.WheelDown);
                        break;
                    case WM_MOUSEHWHEEL:
                        MouseChanged?.Invoke(MouseButton.Middle, wheelDelta > 0 ? MouseAction.WheelRight : MouseAction.WheelLeft);
                        break;
                }
            }
        }

        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }
}
