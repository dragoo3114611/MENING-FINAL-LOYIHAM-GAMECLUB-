namespace Dust2Agent.Common;

/// <summary>
/// Agent fayllari joylashadigan papkalar. Xizmat LocalSystem huquqi bilan ishlagani uchun
/// hamma narsa %ProgramData%\DUST2 ostida saqlanadi — foydalanuvchi profiliga bog'liq emas.
/// </summary>
public static class AgentPaths
{
    public const string PipeName = "dust2agent";

    public static string Root
    {
        get
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (string.IsNullOrWhiteSpace(programData))
                programData = Path.GetTempPath();
            return Path.Combine(programData, "DUST2");
        }
    }

    public static string Logs => Path.Combine(Root, "logs");

    /// <summary>
    /// Qobiq loglari. Qobiq foydalanuvchi huquqi bilan ishlaydi, %ProgramData% ostidagi
    /// papka esa xizmat (LocalSystem) tomonidan yaratilgani uchun unga yozish mumkin emas —
    /// shuning uchun qobiq o'z profiliga yozadi.
    /// </summary>
    public static string UserLogs => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DUST2", "logs");

    /// <summary>Ulanish sozlamalari (admin IP, port, kompyuter nomi).</summary>
    public static string ConfigFile => Path.Combine(Root, "agent.json");

    /// <summary>DPAPI bilan shifrlangan juftlash tokeni.</summary>
    public static string TokenFile => Path.Combine(Root, "token.bin");

    /// <summary>Seans holati — admin yoki kompyuter o'chsa ham davom etishi uchun.</summary>
    public static string StateFile => Path.Combine(Root, "session.json");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Logs);
    }
}
