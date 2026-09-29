using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dust2Agent.Common;

/// <summary>
/// Xizmat (LocalSystem) va qobiq (foydalanuvchi sessiyasi) o'rtasidagi named pipe
/// xabarlari. Faqat lokal: \\.\pipe\dust2agent
/// </summary>
public sealed class PipeMessage
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("json")] public string Json { get; set; } = "{}";

    /// <summary>
    /// Nomlar katta-kichikligiga qaramaydi: xizmat anonim obyektlarni
    /// (`new { ok = true }`) yuboradi, qobiq esa Pascal nomli sinflarga o'qiydi.
    /// </summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static PipeMessage Create(string type, object? payload = null) => new()
    {
        Type = type,
        Json = JsonSerializer.Serialize(payload ?? new { })
    };

    public T? As<T>() where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(Json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string ToLine() => JsonSerializer.Serialize(this);

    public static PipeMessage? Parse(string line)
    {
        try
        {
            var m = JsonSerializer.Deserialize<PipeMessage>(line, Options);
            return string.IsNullOrEmpty(m?.Type) ? null : m;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>Xizmat → qobiq: ekranda nima ko'rsatilishi.</summary>
public sealed class ShellState
{
    /// <summary>setup | locked | session</summary>
    [JsonPropertyName("screen")] public string Screen { get; set; } = "setup";

    [JsonPropertyName("pcName")] public string PcName { get; set; } = "";
    [JsonPropertyName("host")] public string Host { get; set; } = "";
    [JsonPropertyName("port")] public int Port { get; set; } = 7777;
    [JsonPropertyName("online")] public bool Online { get; set; }
    [JsonPropertyName("paired")] public bool Paired { get; set; }

    /// <summary>Ulanish urinishi holati: idle | connecting | error</summary>
    [JsonPropertyName("connectPhase")] public string ConnectPhase { get; set; } = "idle";
    [JsonPropertyName("connectError")] public string ConnectError { get; set; } = "";

    [JsonPropertyName("session")] public SessionPayload? Session { get; set; }
    [JsonPropertyName("lock")] public LockConfigPayload Lock { get; set; } = new();
    [JsonPropertyName("behaviour")] public BehaviourPayload Behaviour { get; set; } = new();
    [JsonPropertyName("unlockCombo")] public string UnlockCombo { get; set; } = AgentConfig.DefaultUnlockCombo;
    [JsonPropertyName("hasServicePassword")] public bool HasServicePassword { get; set; }

    /// <summary>Qulf ekranida ko'rsatiladigan to'lanmagan summa.</summary>
    [JsonPropertyName("due")] public long Due { get; set; }

    /// <summary>Qulf ekranida akkaunt bilan kirish xatosi.</summary>
    [JsonPropertyName("loginError")] public string LoginError { get; set; } = "";

    [JsonPropertyName("wallpaperPath")] public string WallpaperPath { get; set; } = "";
    [JsonPropertyName("clockDriftMs")] public long ClockDriftMs { get; set; }

    /// <summary>Klient dasturi to'xtatilganmi.</summary>
    [JsonPropertyName("paused")] public bool Paused { get; set; }
}

public static class PipeTypes
{
    // xizmat → qobiq
    public const string State = "state";
    public const string Toast = "toast";
    public const string AdminMessage = "admin_message";

    // qobiq → xizmat
    public const string Connect = "connect";
    public const string Unpair = "unpair";
    public const string Login = "login";
    public const string RequestTime = "request_time";
    public const string CallAdmin = "call_admin";
    public const string Logout = "logout";
    public const string Unlock = "unlock";
    public const string VerifyPassword = "verify_password";
    public const string Hello = "hello";

    /// <summary>Klient dasturini to'xtatish (qulf ekrani ko'rsatilmaydi).</summary>
    public const string Pause = "pause";

    /// <summary>To'xtatilgan klientni qayta ishga tushirish.</summary>
    public const string Resume = "resume";
}

public sealed class ConnectRequest
{
    [JsonPropertyName("host")] public string Host { get; set; } = "";
    [JsonPropertyName("port")] public int Port { get; set; } = 7777;
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

public sealed class LoginRequest
{
    [JsonPropertyName("login")] public string Login { get; set; } = "";
    [JsonPropertyName("password")] public string Password { get; set; } = "";
}

public sealed class PasswordRequest
{
    [JsonPropertyName("password")] public string Password { get; set; } = "";
}

public sealed class ToastPayload
{
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("text")] public string Text { get; set; } = "";
    [JsonPropertyName("kind")] public string Kind { get; set; } = "info";
}
