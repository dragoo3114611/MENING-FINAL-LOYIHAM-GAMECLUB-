using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Dust2Agent.Common;

namespace Dust2Agent.Service;

/// <summary>Windows soatini o'rnatish (SetSystemTime) — LocalSystem huquqi kerak.</summary>
[SupportedOSPlatform("windows")]
public static class ClockSync
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SystemTime
    {
        public ushort Year, Month, DayOfWeek, Day, Hour, Minute, Second, Milliseconds;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetSystemTime(ref SystemTime st);

    /// <summary>Farq 1 soniyadan katta bo'lsa soatni to'g'rilaydi. Farqni (ms) qaytaradi.</summary>
    public static long SyncTo(long serverTimeMs, AgentLog log)
    {
        var drift = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - serverTimeMs;
        if (Math.Abs(drift) < 1000) return drift;

        var utc = DateTimeOffset.FromUnixTimeMilliseconds(serverTimeMs).UtcDateTime;
        var st = new SystemTime
        {
            Year = (ushort)utc.Year,
            Month = (ushort)utc.Month,
            DayOfWeek = (ushort)utc.DayOfWeek,
            Day = (ushort)utc.Day,
            Hour = (ushort)utc.Hour,
            Minute = (ushort)utc.Minute,
            Second = (ushort)utc.Second,
            Milliseconds = (ushort)utc.Millisecond
        };

        if (SetSystemTime(ref st))
        {
            log.Info($"Windows soati to'g'rilandi ({drift / 1000} soniya farq bor edi)");
        }
        else
        {
            log.Warn($"Soatni o'rnatib bo'lmadi (xato {Marshal.GetLastWin32Error()})");
        }
        return drift;
    }
}

/// <summary>
/// Windows o'chayotganini aniqlash: xizmat to'xtashi — oddiy o'chirishmi yoki
/// kimdir xizmatni ataylab to'xtatdimi.
/// </summary>
public static class SystemShutdown
{
    private static volatile bool _marked;

    /// <summary>Windows xizmatga SERVICE_CONTROL_SHUTDOWN yubordi.</summary>
    public static void Mark() => _marked = true;

    private const int SM_SHUTTINGDOWN = 0x2000;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    public static bool InProgress
    {
        get
        {
            if (_marked) return true;
            if (!OperatingSystem.IsWindows()) return false;
            try
            {
                return GetSystemMetrics(SM_SHUTTINGDOWN) != 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

/// <summary>Kompyuterni o'chirish va qayta yuklash.</summary>
[SupportedOSPlatform("windows")]
public static class PowerControl
{
    /// <summary>
    /// Kompyuterni o'chirish/qayta yuklashni agentning o'zi boshladi — shunda
    /// xizmat to'xtashi "kimdir xizmatni to'xtatdi" deb hisoblanmaydi.
    /// </summary>
    public static volatile bool Requested;

    public static void Shutdown(AgentLog log) => Run("/s /t 5 /c \"DUST2: vaqt tugadi\"", log);

    public static void Reboot(AgentLog log) => Run("/r /t 5 /c \"DUST2: qayta yuklanmoqda\"", log);

    private static void Run(string args, AgentLog log)
    {
        Requested = true;
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("shutdown", args)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            System.Diagnostics.Process.Start(psi);
            log.Info($"shutdown {args}");
        }
        catch (Exception ex)
        {
            log.Error("Kompyuterni o'chirib bo'lmadi", ex);
        }
    }
}
