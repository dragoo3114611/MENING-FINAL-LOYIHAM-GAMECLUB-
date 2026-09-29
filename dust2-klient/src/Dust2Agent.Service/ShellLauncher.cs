using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Dust2Agent.Common;

namespace Dust2Agent.Service;

/// <summary>
/// Qobiqni (Dust2Agent.Shell) foydalanuvchi sessiyasida ishga tushiradi va
/// nazorat qiladi.
///
/// Windows xizmati Session 0 da ishlaydi va foydalanuvchi ekraniga oyna
/// chiqara olmaydi, shuning uchun qobiq WTSQueryUserToken + CreateProcessAsUser
/// orqali faol konsol sessiyasida ochiladi. Yopilsa — qayta ishga tushiriladi.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ShellLauncher
{
    private readonly AgentLog _log;
    private readonly string _exePath;
    private int _lastPid;
    private DateTime _lastAttempt = DateTime.MinValue;

    /// <summary>Qayta ishga tushirishlar soni — tez-tez yiqilishni ko'rish uchun.</summary>
    private int _restarts;
    private DateTime _restartWindow = DateTime.MinValue;

    /// <summary>Oxirgi token xatosi — bir xil xato har 3 soniyada takrorlanmasin.</summary>
    private int _lastTokenError;

    public ShellLauncher(AgentLog log)
    {
        _log = log;
        var dir = AppContext.BaseDirectory;
        _exePath = Path.Combine(dir, "Dust2Agent.Shell.exe");
    }

    /// <summary>Qobiq ishlayaptimi (faol konsol sessiyasida).</summary>
    public bool IsRunning()
    {
        if (_lastPid == 0) return false;
        try
        {
            using var p = Process.GetProcessById(_lastPid);
            return !p.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>Kerak bo'lsa qobiqni ishga tushiradi. Har soniyada chaqirilishi mumkin.</summary>
    public void EnsureRunning()
    {
        if (IsRunning()) return;
        if ((DateTime.UtcNow - _lastAttempt).TotalSeconds < RetrySeconds) return;
        _lastAttempt = DateTime.UtcNow;

        if (!File.Exists(_exePath))
        {
            _log.Warn($"Qobiq fayli topilmadi: {_exePath}");
            return;
        }

        var sessionId = WTSGetActiveConsoleSessionId();
        if (sessionId == 0xFFFFFFFF)
        {
            // Hech kim tizimga kirmagan — keyingi urinishda
            return;
        }

        // Qobiq allaqachon ochiq bo'lishi mumkin. Diqqat: u boshqa (endi faol
        // bo'lmagan) sessiyada qolgan bo'lsa hisobga olinmaydi — aks holda
        // foydalanuvchi almashgandan keyin yangi ekranda qulf ochilmay qoladi.
        foreach (var p in Process.GetProcessesByName("Dust2Agent.Shell"))
        {
            using (p)
            {
                try
                {
                    if ((uint)p.SessionId != sessionId) continue;
                    _lastPid = p.Id;
                    return;
                }
                catch (InvalidOperationException)
                {
                    // Jarayon shu orada yopildi
                }
            }
        }

        try
        {
            _lastPid = LaunchInSession(sessionId);
            if (_lastPid != 0)
            {
                _log.Info($"Qobiq ishga tushirildi (sessiya {sessionId}, pid {_lastPid})");
                NoteRestart();
            }
        }
        catch (Exception ex)
        {
            _log.Error("Qobiqni ishga tushirib bo'lmadi", ex);
        }
    }

    /// <summary>Qobiq tez-tez yiqilayotgan bo'lsa jurnalda ko'rinsin.</summary>
    private void NoteRestart()
    {
        var now = DateTime.UtcNow;
        if (now - _restartWindow > TimeSpan.FromMinutes(5))
        {
            _restartWindow = now;
            _restarts = 0;
        }
        _restarts++;
        if (_restarts >= 5)
        {
            _log.Warn($"Qobiq 5 daqiqada {_restarts} marta qayta ishga tushirildi — " +
                      $"jurnalda xato bo'lishi mumkin: {AgentPaths.UserLogs}");
            _restartWindow = now;
            _restarts = 0;
        }
    }

    private int LaunchInSession(uint sessionId)
    {
        if (!WTSQueryUserToken(sessionId, out var userToken))
        {
            // Kirish ekrani, sessiya almashinuvi yoki kompyuter o'chayotgan paytda
            // token bo'lmaydi. Bu nosozlik emas — keyingi urinishda ochiladi,
            // shuning uchun jurnalga stack bilan ERROR yozilmaydi.
            var code = Marshal.GetLastWin32Error();
            if (code != _lastTokenError)
            {
                _lastTokenError = code;
                _log.Warn($"Foydalanuvchi sessiyasi hali tayyor emas (kod {code}) — keyinroq urinamiz");
            }
            return 0;
        }
        _lastTokenError = 0;

        var envBlock = IntPtr.Zero;
        var dupToken = IntPtr.Zero;
        try
        {
            var sa = new SecurityAttributes { Length = Marshal.SizeOf<SecurityAttributes>() };
            // SecurityImpersonation — CreateProcessAsUser uchun talab qilinadigan daraja.
            // Identification darajasida jarayon yaratilsa ham, u ish stoliga to'liq kira olmaydi.
            if (!DuplicateTokenEx(userToken, MaximumAllowed, ref sa, SecurityImpersonation,
                    TokenPrimary, out dupToken))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "DuplicateTokenEx");
            }

            if (!CreateEnvironmentBlock(out envBlock, dupToken, false))
            {
                envBlock = IntPtr.Zero;
            }

            var si = new StartupInfo { cb = Marshal.SizeOf<StartupInfo>(), lpDesktop = @"winsta0\default" };

            if (!CreateProcessAsUser(dupToken, _exePath, null, IntPtr.Zero, IntPtr.Zero, false,
                    CreateUnicodeEnvironment, envBlock,
                    Path.GetDirectoryName(_exePath), ref si, out var pi))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateProcessAsUser");
            }

            CloseHandle(pi.hThread);
            CloseHandle(pi.hProcess);
            return pi.dwProcessId;
        }
        finally
        {
            if (envBlock != IntPtr.Zero) DestroyEnvironmentBlock(envBlock);
            if (dupToken != IntPtr.Zero) CloseHandle(dupToken);
            CloseHandle(userToken);
        }
    }

    /* ------------------------------- P/Invoke ------------------------------- */

    /// <summary>Yiqilgan qobiq shuncha soniyadan keyin qayta ochiladi.</summary>
    private const int RetrySeconds = 3;

    private const int MaximumAllowed = 0x2000000;
    private const int SecurityImpersonation = 2;
    private const int TokenPrimary = 1;
    private const uint CreateUnicodeEnvironment = 0x00000400;

    [StructLayout(LayoutKind.Sequential)]
    private struct SecurityAttributes
    {
        public int Length;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr hProcess, hThread;
        public int dwProcessId, dwThreadId;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("wtsapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WTSQueryUserToken(uint sessionId, out IntPtr token);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DuplicateTokenEx(IntPtr existingToken, int desiredAccess,
        ref SecurityAttributes attributes, int impersonationLevel, int tokenType, out IntPtr newToken);

    [DllImport("userenv.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateEnvironmentBlock(out IntPtr env, IntPtr token, bool inherit);

    [DllImport("userenv.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyEnvironmentBlock(IntPtr env);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcessAsUser(IntPtr token, string? applicationName,
        string? commandLine, IntPtr processAttributes, IntPtr threadAttributes, bool inheritHandles,
        uint creationFlags, IntPtr environment, string? currentDirectory,
        ref StartupInfo startupInfo, out ProcessInformation processInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}
