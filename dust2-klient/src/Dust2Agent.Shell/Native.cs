using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Dust2Agent.Shell;

/// <summary>Qobiq uchun kerakli Win32 chaqiruvlari.</summary>
internal static class Native
{
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    /// <summary>
    /// Oynani oldinga chiqaradi.
    ///
    /// Qobiqni Windows xizmati ishga tushiradi, shuning uchun jarayon "oldingi" emas va
    /// SetForegroundWindow o'zi ishlamasligi mumkin — bunday holda joriy oldingi oynaning
    /// kirish oqimiga vaqtincha ulanib olinadi. Aks holda oyna ko'rinadi, lekin klaviatura
    /// unga tushmaydi.
    /// </summary>
    public static void ForceForeground(Window w)
    {
        var hwnd = new WindowInteropHelper(w).Handle;
        if (hwnd == IntPtr.Zero) return;

        if (SetForegroundWindow(hwnd)) return;

        var fg = GetForegroundWindow();
        if (fg == IntPtr.Zero) return;

        var fgThread = GetWindowThreadProcessId(fg, IntPtr.Zero);
        var myThread = GetCurrentThreadId();
        if (fgThread == myThread) return;

        if (!AttachThreadInput(myThread, fgThread, true)) return;
        try
        {
            SetForegroundWindow(hwnd);
        }
        finally
        {
            AttachThreadInput(myThread, fgThread, false);
        }
    }
}
