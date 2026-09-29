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

/// <summary>Kompyuterni o'chirish va qayta yuklash.</summary>
[SupportedOSPlatform("windows")]
public static class PowerControl
{
    public static void Shutdown(AgentLog log) => Run("/s /t 5 /c \"DUST2: vaqt tugadi\"", log);

    public static void Reboot(AgentLog log) => Run("/r /t 5 /c \"DUST2: qayta yuklanmoqda\"", log);

    private static void Run(string args, AgentLog log)
    {
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
