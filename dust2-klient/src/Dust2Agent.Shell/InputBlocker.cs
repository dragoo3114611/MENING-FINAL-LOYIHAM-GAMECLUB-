using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Dust2Agent.Common;

namespace Dust2Agent.Shell;

/// <summary>
/// Qulf ekrani ochiq bo'lganda klaviatura va sichqonchani to'sadi
/// (past darajali ilgaklar — WH_KEYBOARD_LL / WH_MOUSE_LL).
///
/// Nimalar to'siladi: Win, Alt+Tab, Alt+Esc, Alt+F4, Ctrl+Esc, kontekst tugmasi
/// va qulf oynasidan tashqaridagi sichqoncha bosishlari. Oddiy yozuv — login va
/// parol kiritish — to'silmaydi. Administratorning favqulodda kombinatsiyasi
/// (Ctrl+Alt+P) ham o'tadi.
///
/// Xavfsizlik: ilgaklar jarayonga bog'langan — qobiq yiqilsa yoki yopilsa,
/// Windows ularni o'zi olib tashlaydi, ya'ni kiritish bloklangan holda qolib
/// ketmaydi. Ilgak javob bermay qolsa ham Windows uni o'chiradi
/// (LowLevelHooksTimeout).
///
/// Ctrl+Alt+Del ni bu yo'l bilan to'sib bo'lmaydi — [qaror 7](../../../docs/QARORLAR.md).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class InputBlocker : IDisposable
{
    private readonly AgentLog _log;
    private readonly int _pid = Environment.ProcessId;

    // Delegatlar maydonlarda saqlanadi: GC ularni yig'ib yuborsa ilgak yiqiladi
    private readonly HookProc _keyProc;
    private readonly HookProc _mouseProc;

    private IntPtr _keyHook;
    private IntPtr _mouseHook;

    public InputBlocker(AgentLog log)
    {
        _log = log;
        _keyProc = KeyCallback;
        _mouseProc = MouseCallback;
    }

    /// <summary>Hozir bloklash yoqilganmi.</summary>
    public bool Active { get; private set; }

    /// <summary>Favqulodda kombinatsiya — har doim o'tkaziladi.</summary>
    public string UnlockCombo { get; set; } = "Ctrl+Alt+P";

    public void SetActive(bool on)
    {
        if (on == Active) return;
        if (on) Install();
        else Remove();
    }

    private void Install()
    {
        var module = GetModuleHandle(Process.GetCurrentProcess().MainModule?.ModuleName);
        _keyHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyProc, module, 0);
        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, module, 0);

        if (_keyHook == IntPtr.Zero || _mouseHook == IntPtr.Zero)
        {
            _log.Warn($"Kiritishni to'sib bo'lmadi (kod {Marshal.GetLastWin32Error()})");
            Remove();
            return;
        }

        Active = true;
        _log.Info("Klaviatura va sichqoncha bloklandi");
    }

    private void Remove()
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
        if (Active) _log.Info("Kiritish bloki olib tashlandi");
        Active = false;
    }

    private IntPtr KeyCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code < 0 || !Active) return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);

        try
        {
            var data = Marshal.PtrToStructure<KeyboardLowLevel>(lParam);
            var e = new KeyEvent(
                (int)data.vkCode,
                Down(VK_MENU),
                Down(VK_CONTROL),
                Down(VK_SHIFT),
                Down(InputPolicy.VK_LWIN) || Down(InputPolicy.VK_RWIN));

            if (InputPolicy.ShouldBlock(e, UnlockCombo)) return 1;
        }
        catch (Exception)
        {
            // Ilgak hech qachon yiqilmasligi kerak — shubhali holatda o'tkazamiz
        }

        return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }

    private IntPtr MouseCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code < 0 || !Active) return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);

        try
        {
            var msg = (uint)wParam;
            // Sichqoncha harakatiga tegmaymiz — faqat bosish va g'ildirak
            if (msg != WM_MOUSEMOVE)
            {
                var data = Marshal.PtrToStructure<MouseLowLevel>(lParam);
                var hwnd = WindowFromPoint(data.pt);
                if (hwnd != IntPtr.Zero)
                {
                    GetWindowThreadProcessId(hwnd, out var owner);
                    // Boshqa dasturning oynasiga bosish — to'siladi
                    if (owner != 0 && owner != (uint)_pid) return 1;
                }
            }
        }
        catch (Exception)
        {
            // yuqoridagi izohga qarang
        }

        return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }

    private static bool Down(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    public void Dispose() => Remove();

    /* --------------------------------- Win32 --------------------------------- */

    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const uint WM_MOUSEMOVE = 0x0200;
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;

    private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardLowLevel
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseLowLevel
    {
        public Point pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc fn, IntPtr module, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vk);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(Point p);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? name);
}
