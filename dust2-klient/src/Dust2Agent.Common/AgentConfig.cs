using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dust2Agent.Common;

/// <summary>
/// Klient kompyuterning ulanish sozlamalari — %ProgramData%\DUST2\agent.json.
/// Token alohida faylda, DPAPI bilan shifrlangan holda saqlanadi.
/// </summary>
public sealed class AgentConfig
{
    [JsonPropertyName("host")] public string Host { get; set; } = "";
    [JsonPropertyName("port")] public int Port { get; set; } = 7777;
    [JsonPropertyName("pcName")] public string PcName { get; set; } = Environment.MachineName;
    [JsonPropertyName("pcId")] public long PcId { get; set; }

    /// <summary>Admindan kelgan sozlamalar — aloqa yo'q bo'lsa ham kerak.</summary>
    [JsonPropertyName("servicePassHash")] public string ServicePassHash { get; set; } = "";
    [JsonPropertyName("unlockCombo")] public string UnlockCombo { get; set; } = DefaultUnlockCombo;

    /// <summary>
    /// Standart kombinatsiya — Klub Pult admin ham shuni ko'rsatadi va yuboradi.
    /// Admin boshqasini tanlasa, hello.ok / sync bilan keladi va shu faylga yoziladi.
    /// </summary>
    public const string DefaultUnlockCombo = "Ctrl+Alt+K";

    /// <summary>
    /// Admin bilan aloqa yo'q paytda yuz bergan xavfli holatlar (client.alert).
    /// Diskda turadi, shuning uchun kompyuter qayta yoqilsa ham ulanganda yetib boradi.
    /// </summary>
    [JsonPropertyName("pendingAlerts")] public List<PendingAlert> PendingAlerts { get; set; } = new();

    /// <summary>
    /// Klient dasturi vaqtincha to'xtatilgan (Ctrl+Alt+K → "Dasturni to'xtatish").
    /// Bunda qulf ekrani ko'rsatilmaydi, admin bilan aloqa uzilgan holda turadi.
    /// Qobiq qo'lda ishga tushirilganda avtomatik tiklanadi.
    /// </summary>
    [JsonPropertyName("paused")] public bool Paused { get; set; }

    [JsonIgnore] public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && Port > 0;

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private static readonly object Lock = new();

    public static AgentConfig Load()
    {
        try
        {
            if (File.Exists(AgentPaths.ConfigFile))
            {
                var c = JsonSerializer.Deserialize<AgentConfig>(File.ReadAllText(AgentPaths.ConfigFile));
                if (c is not null) return c;
            }
        }
        catch (Exception)
        {
            // Buzilgan sozlama fayli — standart qiymatlar bilan davom etamiz
        }
        return new AgentConfig();
    }

    public void Save()
    {
        lock (Lock)
        {
            AgentPaths.EnsureCreated();
            var tmp = AgentPaths.ConfigFile + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(this, Options));
            File.Move(tmp, AgentPaths.ConfigFile, overwrite: true);
        }
    }
}

/// <summary>Juftlash tokeni: DPAPI (LocalMachine) bilan shifrlab saqlanadi.</summary>
public static class TokenStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("DUST2-agent-token-v1");

    public static void Save(string token)
    {
        AgentPaths.EnsureCreated();
        var raw = Encoding.UTF8.GetBytes(token);
        var data = OperatingSystem.IsWindows()
            ? ProtectedData.Protect(raw, Entropy, DataProtectionScope.LocalMachine)
            : raw;
        File.WriteAllBytes(AgentPaths.TokenFile, data);
    }

    public static string Load()
    {
        try
        {
            if (!File.Exists(AgentPaths.TokenFile)) return "";
            var data = File.ReadAllBytes(AgentPaths.TokenFile);
            if (data.Length == 0) return "";
            var raw = OperatingSystem.IsWindows()
                ? ProtectedData.Unprotect(data, Entropy, DataProtectionScope.LocalMachine)
                : data;
            return Encoding.UTF8.GetString(raw);
        }
        catch (Exception)
        {
            // Kalit o'qilmadi (masalan, Windows qayta o'rnatilgan) — qayta juftlash kerak
            return "";
        }
    }

    public static void Clear()
    {
        try
        {
            if (File.Exists(AgentPaths.TokenFile)) File.Delete(AgentPaths.TokenFile);
        }
        catch (IOException)
        {
            // keyingi safar o'chiriladi
        }
    }
}

/// <summary>
/// Admindan kelgan xizmat parolini oflayn tekshirish.
/// Format: pbkdf2$&lt;takrorlar&gt;$&lt;salt base64&gt;$&lt;hash base64&gt; (SHA-256).
/// </summary>
public static class PasswordHash
{
    public static bool Verify(string stored, string plain)
    {
        if (string.IsNullOrEmpty(stored)) return false;
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2") return false;
        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0) return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(plain), salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
