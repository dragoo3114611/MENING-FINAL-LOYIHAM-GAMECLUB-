namespace Dust2Agent.Common;

/// <summary>Oddiy fayl jurnali: %ProgramData%\DUST2\logs\&lt;nom&gt;-YYYY-MM-DD.log</summary>
public sealed class AgentLog
{
    private readonly string _name;
    private readonly string _dir;
    private readonly object _lock = new();

    /// <summary>Jurnal fayllari shuncha kundan ortiq saqlanmaydi.</summary>
    public const int KeepDays = 14;

    /// <param name="dir">Log papkasi; ko'rsatilmasa %ProgramData%\DUST2\logs.</param>
    public AgentLog(string name, string? dir = null)
    {
        _name = name;
        _dir = dir ?? AgentPaths.Logs;
        CleanOld(_dir);
    }

    /// <summary>
    /// Eski jurnallarni o'chiradi: klient kompyuterda agent oylab ishlaydi,
    /// papka cheksiz o'smasligi kerak.
    /// </summary>
    public static int CleanOld(string dir, int keepDays = KeepDays)
    {
        var limit = DateTime.UtcNow.AddDays(-keepDays);
        var removed = 0;
        try
        {
            foreach (var file in System.IO.Directory.GetFiles(dir, "*.log"))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(file) >= limit) continue;
                    File.Delete(file);
                    removed++;
                }
                catch (Exception)
                {
                    // fayl band — keyingi safar
                }
            }
        }
        catch (Exception)
        {
            // papka hali yo'q
        }
        return removed;
    }

    /// <summary>Loglar yozilayotgan papka (interfeysda ko'rsatish uchun).</summary>
    public string Directory => _dir;

    private string FileFor(DateTime now) =>
        Path.Combine(_dir, $"{_name}-{now:yyyy-MM-dd}.log");

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);

    public void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : $"{message}: {ex.Message}\n{ex.StackTrace}");

    private void Write(string level, string message)
    {
        var now = DateTime.Now;
        var line = $"{now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        Console.WriteLine(line);
        try
        {
            lock (_lock)
            {
                System.IO.Directory.CreateDirectory(_dir);
                File.AppendAllText(FileFor(now), line + Environment.NewLine);
            }
        }
        catch
        {
            // Jurnalga yozib bo'lmasa ham agent ishlashda davom etadi
        }
    }
}
