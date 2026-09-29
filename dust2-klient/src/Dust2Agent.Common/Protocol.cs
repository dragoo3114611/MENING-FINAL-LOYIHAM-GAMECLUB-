using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dust2Agent.Common;

/// <summary>
/// Admin bilan almashinadigan xabar konverti. To'liq tavsif: shared/protocol.md
/// </summary>
public sealed class Envelope
{
    public const int Version = 1;

    [JsonPropertyName("v")] public int V { get; set; } = Version;
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    // Diqqat: standart qiymat bo'sh — shunda "id" siz kelgan xabarni Parse() rad eta oladi.
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("ts")] public long Ts { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    [JsonPropertyName("payload")] public JsonElement Payload { get; set; }

    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true
    };

    public static Envelope Create(string type, object? payload = null)
    {
        var json = JsonSerializer.SerializeToElement(payload ?? new { }, Options);
        return new Envelope { Type = type, Id = Guid.NewGuid().ToString(), Payload = json };
    }

    public string ToJson() => JsonSerializer.Serialize(this, Options);

    /// <summary>Kiruvchi matnni konvertga aylantiradi; format buzilgan bo'lsa null.</summary>
    public static Envelope? Parse(string raw)
    {
        try
        {
            var e = JsonSerializer.Deserialize<Envelope>(raw, Options);
            if (e is null || e.V != Version || string.IsNullOrEmpty(e.Type) || string.IsNullOrEmpty(e.Id))
                return null;
            return e;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Payload'ni ko'rsatilgan turga o'giradi.</summary>
    public T? PayloadAs<T>() where T : class
    {
        try
        {
            return Payload.ValueKind == JsonValueKind.Undefined
                ? null
                : Payload.Deserialize<T>(Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string? Str(string name) =>
        Payload.ValueKind == JsonValueKind.Object && Payload.TryGetProperty(name, out var v) &&
        v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    public long? Num(string name) =>
        Payload.ValueKind == JsonValueKind.Object && Payload.TryGetProperty(name, out var v) &&
        v.ValueKind == JsonValueKind.Number
            ? v.GetInt64()
            : null;
}

/* ------------------------------ payload turlari ------------------------------ */

public sealed class SessionPayload
{
    [JsonPropertyName("sessionId")] public long SessionId { get; set; }
    /// <summary>pre | open | acc</summary>
    [JsonPropertyName("mode")] public string Mode { get; set; } = "pre";
    [JsonPropertyName("startedAt")] public long StartedAt { get; set; }
    [JsonPropertyName("endsAt")] public long? EndsAt { get; set; }
    [JsonPropertyName("remainingMs")] public long? RemainingMs { get; set; }
    [JsonPropertyName("rate")] public long Rate { get; set; }
    [JsonPropertyName("client")] public string? Client { get; set; }
    [JsonPropertyName("paused")] public bool Paused { get; set; }
    [JsonPropertyName("due")] public long Due { get; set; }

    public SessionMode ToMode() => Mode switch
    {
        "open" => SessionMode.Open,
        "acc" => SessionMode.Account,
        _ => SessionMode.Prepaid
    };
}

public sealed class LockConfigPayload
{
    [JsonPropertyName("theme")] public string Theme { get; set; } = "dust2";
    [JsonPropertyName("layout")] public string Layout { get; set; } = "center";
    [JsonPropertyName("valign")] public string Valign { get; set; } = "middle";
    [JsonPropertyName("clockSize")] public int ClockSize { get; set; } = 100;
    [JsonPropertyName("titleSize")] public int TitleSize { get; set; } = 100;
    [JsonPropertyName("textSize")] public int TextSize { get; set; } = 100;
    [JsonPropertyName("title")] public string Title { get; set; } = "DUST2 GAMEZONE";
    [JsonPropertyName("text")] public string Text { get; set; } = "";
    [JsonPropertyName("clock")] public bool Clock { get; set; } = true;

    /// <summary>Qulf ekranida kompyuter nomi (PC 01) ko'rsatilsinmi.</summary>
    [JsonPropertyName("pcName")] public bool PcName { get; set; } = true;

    [JsonPropertyName("login")] public bool Login { get; set; } = true;
    [JsonPropertyName("fit")] public string Fit { get; set; } = "cover";
    [JsonPropertyName("dim")] public int Dim { get; set; } = 35;
    [JsonPropertyName("wallpaperHash")] public string? WallpaperHash { get; set; }
}

public sealed class BehaviourPayload
{
    [JsonPropertyName("onExpire")] public string OnExpire { get; set; } = "lock";
    [JsonPropertyName("offDelay")] public int OffDelay { get; set; } = 5;
    [JsonPropertyName("blockInput")] public bool BlockInput { get; set; } = true;
    [JsonPropertyName("warnMin")] public int WarnMin { get; set; } = 5;

    /// <summary>Seans davomida taymer oynachasi ko'rsatilsinmi.</summary>
    [JsonPropertyName("showWidget")] public bool ShowWidget { get; set; } = true;
}

public sealed class AgentConfigPayload
{
    [JsonPropertyName("servicePassHash")] public string ServicePassHash { get; set; } = "";
    [JsonPropertyName("unlockCombo")] public string UnlockCombo { get; set; } = AgentConfig.DefaultUnlockCombo;
}

public sealed class HelloOkPayload
{
    [JsonPropertyName("pcId")] public long PcId { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("serverTime")] public long ServerTime { get; set; }
    [JsonPropertyName("session")] public SessionPayload? Session { get; set; }
    [JsonPropertyName("lock")] public LockConfigPayload? Lock { get; set; }
    [JsonPropertyName("behaviour")] public BehaviourPayload? Behaviour { get; set; }
    [JsonPropertyName("agent")] public AgentConfigPayload? Agent { get; set; }
}

public sealed class PairOkPayload
{
    [JsonPropertyName("token")] public string Token { get; set; } = "";
    [JsonPropertyName("pcId")] public long PcId { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("serverTime")] public long ServerTime { get; set; }
}

public sealed class PairDeniedPayload
{
    [JsonPropertyName("reason")] public string Reason { get; set; } = "";
    [JsonPropertyName("message")] public string Message { get; set; } = "";
}

public sealed class AuthOkPayload
{
    [JsonPropertyName("login")] public string Login { get; set; } = "";
    [JsonPropertyName("balance")] public long Balance { get; set; }
    [JsonPropertyName("remainingMs")] public long RemainingMs { get; set; }
    [JsonPropertyName("rate")] public long Rate { get; set; }
}

public sealed class AuthDeniedPayload
{
    [JsonPropertyName("reason")] public string Reason { get; set; } = "";
    [JsonPropertyName("message")] public string? Message { get; set; }
}
