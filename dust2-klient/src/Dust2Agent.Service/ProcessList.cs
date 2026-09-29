using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Dust2Agent.Common;

namespace Dust2Agent.Service;

/// <summary>
/// Klient kompyuterdagi jarayonlar ro'yxati va ularni yopish.
///
/// Bu kod o'yin kompyuterida ishlaydi, shuning uchun imkon qadar yengil:
///
///  • protsessor foizi oldingi o'lchov bilan farqdan olinadi — kutish (sleep) yo'q;
///  • jarayonning fayl yo'li, tavsifi va turi PID bo'yicha keshlanadi va faqat
///    yangi jarayon uchun o'qiladi (MainModule/FileVersionInfo har safar chaqirilsa
///    juda qimmat tushadi);
///  • "javob bermayapti" holati bitta EnumWindows o'tishida aniqlanadi va
///    SendMessageTimeout 100 ms bilan cheklanadi (Process.Responding 5 soniyagacha
///    kutib turadi).
/// </summary>
[SupportedOSPlatform("windows")]
public static class ProcessList
{
    /// <summary>Ro'yxat shuncha jarayondan oshmaydi (interfeysda ham ortig'i kerak emas).</summary>
    private const int MaxItems = 250;

    /// <summary>Oldingi o'lchov shundan eski bo'lsa, protsessor foizi ishonchsiz.</summary>
    private static readonly TimeSpan SampleMaxAge = TimeSpan.FromSeconds(30);

    /// <summary>Juda qisqa oraliqda o'lchash shovqin beradi.</summary>
    private static readonly TimeSpan SampleMinAge = TimeSpan.FromMilliseconds(250);

    /// <summary>Birinchi so'rovda foizni ko'rsatish uchun qisqa o'lchov.</summary>
    private static readonly TimeSpan FirstSample = TimeSpan.FromMilliseconds(250);

    /// <summary>Bitta oynaning javobini shuncha ms kutamiz (osilgan oyna aniqlanadi).</summary>
    private const uint HangTimeoutMs = 60;

    private static readonly string[] AlwaysProtected =
    {
        "system", "idle", "registry", "memory compression", "csrss", "wininit", "winlogon",
        "services", "lsass", "smss", "svchost", "fontdrvhost", "dwm", "sihost", "explorer",
        "runpad", "dust2agent.service", "dust2agent.shell"
    };

    /// <summary>
    /// PID bo'yicha o'zgarmaydigan ma'lumot (bir marta o'qiladi).
    ///
    /// Kesh jarayon nomi bilan tekshiriladi: `Process.StartTime` har chaqirilganda
    /// tizim so'rovi bo'ladi va himoyalangan jarayonlarda istisno tashlaydi, nom esa
    /// ro'yxat olinganda allaqachon tayyor.
    /// </summary>
    private sealed record Meta(string ProcName, string Exe, string Name, string Kind, bool Protected);

    private static readonly ConcurrentDictionary<int, Meta> MetaCache = new();

    private static Dictionary<int, TimeSpan> _lastCpu = new();
    private static DateTime _lastSampleAt = DateTime.MinValue;
    private static readonly object CpuLock = new();

    public sealed class Item
    {
        public int Pid { get; init; }
        public string Exe { get; init; } = "";
        public string Name { get; init; } = "";
        public double Cpu { get; set; }
        public double RamMb { get; init; }
        public bool Protected { get; init; }
        /// <summary>sys | shell | app</summary>
        public string Kind { get; init; } = "app";
        public bool Hang { get; init; }
    }

    public sealed class Result
    {
        public IReadOnlyList<Item> Items { get; init; } = Array.Empty<Item>();
        public double Cpu { get; init; }
        public double RamUsedMb { get; init; }
        public double RamTotalMb { get; init; }
    }

    /// <summary>
    /// Jarayonlarni to'playdi. Odatda darhol qaytadi; faqat birinchi so'rovda
    /// (yoki uzoq tanaffusdan keyin) 250 ms o'lchov oladi.
    /// </summary>
    public static async Task<Result> CollectAsync(AgentLog log, CancellationToken ct)
    {
        var needSample = DateTime.UtcNow - _lastSampleAt > SampleMaxAge;
        if (needSample)
        {
            Sample();
            try
            {
                await Task.Delay(FirstSample, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return new Result();
            }
        }

        return Collect(log);
    }

    /// <summary>Protsessor vaqtini o'lchab qo'yadi (foiz keyingi so'rovda hisoblanadi).</summary>
    private static void Sample()
    {
        var map = new Dictionary<int, TimeSpan>(256);
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                map[p.Id] = p.TotalProcessorTime;
            }
            catch (Exception)
            {
                // tizim jarayoniga kirish taqiqlangan
            }
            finally
            {
                p.Dispose();
            }
        }
        lock (CpuLock)
        {
            _lastCpu = map;
            _lastSampleAt = DateTime.UtcNow;
        }
    }

    private static Result Collect(AgentLog log)
    {
        Dictionary<int, TimeSpan> prev;
        DateTime prevAt;
        lock (CpuLock)
        {
            prev = _lastCpu;
            prevAt = _lastSampleAt;
        }

        var now = DateTime.UtcNow;
        var elapsed = now - prevAt;
        var canCpu = elapsed >= SampleMinAge && elapsed <= SampleMaxAge;
        var maxMs = elapsed.TotalMilliseconds * Environment.ProcessorCount;

        var hangs = HungWindowProcesses();
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var next = new Dictionary<int, TimeSpan>(256);
        var items = new List<Item>(256);

        foreach (var p in Process.GetProcesses())
        {
            try
            {
                var pid = p.Id;
                TimeSpan cpuTime;
                try
                {
                    cpuTime = p.TotalProcessorTime;
                    next[pid] = cpuTime;
                }
                catch (Exception)
                {
                    cpuTime = TimeSpan.Zero;
                }

                double cpu = 0;
                if (canCpu && maxMs > 0 && cpuTime > TimeSpan.Zero && prev.TryGetValue(pid, out var before))
                {
                    cpu = Math.Clamp((cpuTime - before).TotalMilliseconds / maxMs * 100.0, 0, 100);
                }

                var meta = MetaFor(p, windows);
                items.Add(new Item
                {
                    Pid = pid,
                    Exe = meta.Exe,
                    Name = meta.Name,
                    Cpu = Math.Round(cpu, 1),
                    RamMb = Math.Round(p.WorkingSet64 / 1024d / 1024d),
                    Protected = meta.Protected,
                    Kind = meta.Kind,
                    Hang = hangs.Contains(pid)
                });
            }
            catch (Exception ex)
            {
                log.Warn($"Jarayon o'qilmadi: {ex.Message}");
            }
            finally
            {
                p.Dispose();
            }
        }

        lock (CpuLock)
        {
            _lastCpu = next;
            _lastSampleAt = now;
        }
        PruneMeta(next.Keys);

        var (usedMb, totalMb) = Memory();
        var top = items
            .OrderByDescending(x => x.Cpu)
            .ThenByDescending(x => x.RamMb)
            .Take(MaxItems)
            .ToList();

        return new Result
        {
            Items = top,
            Cpu = Math.Round(Math.Min(100, items.Sum(x => x.Cpu)), 0),
            RamUsedMb = usedMb,
            RamTotalMb = totalMb
        };
    }

    /// <summary>PID bo'yicha keshlangan ma'lumot; yangi jarayon bo'lsa bir marta o'qiladi.</summary>
    private static Meta MetaFor(Process p, string windowsDir)
    {
        var procName = p.ProcessName;
        if (MetaCache.TryGetValue(p.Id, out var cached) && cached.ProcName == procName) return cached;

        var lower = procName.ToLowerInvariant();
        var exe = procName + ".exe";
        var isShell = lower is "runpad" or "dust2agent.shell";
        var isSystem = AlwaysProtected.Contains(lower) || p.Id <= 4;

        // Fayl yo'li faqat tanish bo'lmagan jarayonlar uchun o'qiladi
        var path = "";
        if (!isSystem && !isShell)
        {
            path = ImagePath(p.Id);
            if (path.Length > 0 && windowsDir.Length > 0
                && path.StartsWith(windowsDir, StringComparison.OrdinalIgnoreCase))
            {
                isSystem = true;
            }
        }

        var name = procName;
        if (!isSystem && path.Length > 0)
        {
            try
            {
                var d = FileVersionInfo.GetVersionInfo(path).FileDescription;
                if (!string.IsNullOrWhiteSpace(d)) name = d.Trim();
            }
            catch (Exception)
            {
                // tavsif yo'q — jarayon nomi qoladi
            }
        }

        var meta = new Meta(
            procName,
            exe,
            name,
            isShell ? "shell" : isSystem ? "sys" : "app",
            isSystem || isShell);
        MetaCache[p.Id] = meta;
        return meta;
    }

    /// <summary>Yopilgan jarayonlarning keshini tozalaydi.</summary>
    private static void PruneMeta(IEnumerable<int> alive)
    {
        if (MetaCache.Count < 400) return;
        var live = new HashSet<int>(alive);
        foreach (var pid in MetaCache.Keys)
        {
            if (!live.Contains(pid)) MetaCache.TryRemove(pid, out _);
        }
    }

    /// <summary>Tanlangan jarayonlarni yopadi. Himoyalanganlari tegilmaydi.</summary>
    public static int Kill(AgentLog log, IEnumerable<int> pids)
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var killed = 0;
        foreach (var pid in pids)
        {
            try
            {
                using var p = Process.GetProcessById(pid);
                var meta = MetaFor(p, windows);
                if (meta.Protected)
                {
                    log.Warn($"Himoyalangan jarayon yopilmadi: {p.ProcessName} ({pid})");
                    continue;
                }
                p.Kill(entireProcessTree: true);
                killed++;
                log.Info($"Jarayon yopildi: {p.ProcessName} ({pid})");
            }
            catch (ArgumentException)
            {
                // Jarayon allaqachon yopilgan
            }
            catch (Exception ex)
            {
                log.Warn($"Jarayonni yopib bo'lmadi ({pid}): {ex.Message}");
            }
        }
        return killed;
    }

    /* --------------------------------- Win32 --------------------------------- */

    /// <summary>
    /// Javob bermayotgan oynali jarayonlar. Barcha yuqori darajali oynalar bir marta
    /// aylanib chiqiladi; har bir oynaga 100 ms li so'rov yuboriladi.
    /// </summary>
    private static HashSet<int> HungWindowProcesses()
    {
        var hung = new HashSet<int>();
        try
        {
            EnumWindows((hwnd, _) =>
            {
                if (!IsWindowVisible(hwnd)) return true;
                if (GetWindow(hwnd, GW_OWNER) != IntPtr.Zero) return true;
                if (SendMessageTimeout(hwnd, WM_NULL, IntPtr.Zero, IntPtr.Zero,
                        SMTO_ABORTIFHUNG | SMTO_BLOCK, HangTimeoutMs, out _) == IntPtr.Zero)
                {
                    GetWindowThreadProcessId(hwnd, out var pid);
                    if (pid != 0) hung.Add((int)pid);
                }
                return true;
            }, IntPtr.Zero);
        }
        catch (Exception)
        {
            // oynalarni aylanib bo'lmadi — "javob bermayapti" belgisi ko'rsatilmaydi
        }
        return hung;
    }

    private static string ImagePath(int pid)
    {
        var h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (h == IntPtr.Zero) return "";
        try
        {
            var sb = new StringBuilder(1024);
            var size = sb.Capacity;
            return QueryFullProcessImageName(h, 0, sb, ref size) ? sb.ToString() : "";
        }
        finally
        {
            CloseHandle(h);
        }
    }

    private static (double used, double total) Memory()
    {
        var s = new MemoryStatusEx { dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref s)) return (0, 0);
        var total = s.ullTotalPhys / 1024d / 1024d;
        var used = (s.ullTotalPhys - s.ullAvailPhys) / 1024d / 1024d;
        return (Math.Round(used), Math.Round(total));
    }

    private const uint WM_NULL = 0x0000;
    private const uint SMTO_ABORTIFHUNG = 0x0002;
    private const uint SMTO_BLOCK = 0x0001;
    private const uint GW_OWNER = 4;
    private const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr param);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr param);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hwnd, uint cmd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeoutMs,
        out IntPtr result);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int access, [MarshalAs(UnmanagedType.Bool)] bool inherit, int pid);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(IntPtr handle, int flags, StringBuilder name, ref int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
}
