using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dust2Agent.Common;

/// <summary>
/// Diskda saqlanadigan seans holati. Admin yoki kompyuter o'chsa ham seans
/// davom etishi uchun kerak ([qaror 1 va 10](../../docs/QARORLAR.md)).
/// </summary>
public sealed class StoredSession
{
    [JsonPropertyName("sessionId")] public long SessionId { get; set; }
    [JsonPropertyName("mode")] public string Mode { get; set; } = "pre";
    [JsonPropertyName("client")] public string? Client { get; set; }
    [JsonPropertyName("rate")] public long Rate { get; set; }
    [JsonPropertyName("paused")] public bool Paused { get; set; }
    [JsonPropertyName("due")] public long Due { get; set; }

    /// <summary>Mutlaq tugash vaqti (admin soati bo'yicha). Ochiq vaqtda null.</summary>
    [JsonPropertyName("endsAtUtc")] public long? EndsAtUtc { get; set; }

    /// <summary>Seans boshlangan vaqt (ko'rsatish uchun).</summary>
    [JsonPropertyName("startedAtUtc")] public long StartedAtUtc { get; set; }

    /// <summary>Holat diskka yozilgan payt — qayta yuklangandan keyin qolgan vaqtni hisoblash uchun.</summary>
    [JsonPropertyName("savedAtUtc")] public long SavedAtUtc { get; set; }

    /// <summary>Saqlangan paytdagi qolgan vaqt.</summary>
    [JsonPropertyName("remainingMs")] public long? RemainingMs { get; set; }
}

public static class SessionStore
{
    private static readonly object Lock = new();
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static StoredSession? Load()
    {
        try
        {
            if (!File.Exists(AgentPaths.StateFile)) return null;
            return JsonSerializer.Deserialize<StoredSession>(File.ReadAllText(AgentPaths.StateFile));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static void Save(StoredSession? s)
    {
        lock (Lock)
        {
            try
            {
                AgentPaths.EnsureCreated();
                if (s is null)
                {
                    if (File.Exists(AgentPaths.StateFile)) File.Delete(AgentPaths.StateFile);
                    return;
                }
                s.SavedAtUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var tmp = AgentPaths.StateFile + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(s, Options));
                File.Move(tmp, AgentPaths.StateFile, overwrite: true);
            }
            catch (IOException)
            {
                // Diskka yozib bo'lmasa ham agent ishlashda davom etadi
            }
        }
    }
}
