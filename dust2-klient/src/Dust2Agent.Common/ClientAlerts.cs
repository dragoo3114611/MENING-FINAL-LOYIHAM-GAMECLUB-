using System.Text.Json.Serialization;

namespace Dust2Agent.Common;

/// <summary>
/// Adminga yuboriladigan xavfli holatlar: <c>client.alert { kind, at }</c>.
/// Admin ularni qizil ogohlantirish qilib ko'rsatadi (resumed bundan mustasno).
/// </summary>
public static class AlertKinds
{
    /// <summary>Klient dasturi to'xtatildi — qulf ekrani o'chdi.</summary>
    public const string Paused = "paused";

    /// <summary>To'xtatilgan klient qayta ishga tushirildi (oddiy ma'lumot).</summary>
    public const string Resumed = "resumed";

    /// <summary>Qulf xizmat paroli bilan qo'lda ochildi.</summary>
    public const string ManualUnlock = "manual_unlock";

    /// <summary>Dust2Agent xizmati to'xtatildi (kompyuter o'chayotgani uchun emas).</summary>
    public const string ServiceStopped = "service_stopped";
}

/// <summary>Aloqa yo'q paytda navbatga qo'yilgan ogohlantirish.</summary>
public sealed class PendingAlert
{
    [JsonPropertyName("kind")] public string Kind { get; set; } = "";

    /// <summary>Yuz bergan vaqt, Unix ms — admin haqiqiy vaqtni ko'rsatadi.</summary>
    [JsonPropertyName("at")] public long At { get; set; }

    /// <summary>Navbat cheksiz o'smasin: eng eskilari tashlanadi.</summary>
    public const int Max = 20;

    public static void Add(List<PendingAlert> list, string kind, long at)
    {
        list.Add(new PendingAlert { Kind = kind, At = at });
        while (list.Count > Max) list.RemoveAt(0);
    }
}
