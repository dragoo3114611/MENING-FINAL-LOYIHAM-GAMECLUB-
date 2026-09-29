namespace Dust2Agent.Common;

/// <summary>
/// Vaqt tugaganda kompyuter nima qilishi — "Kompyuter xatti-harakati"
/// sozlamasining klient tomonidagi qismi. Sof mantiq, testlar bilan qoplangan.
///
/// Odatda o'chirishni admin boshqaradi: seans tugagach bazada "shu vaqtdan
/// keyin o'chsin" belgisi qo'yiladi va vaqti kelganda <c>power.off</c>
/// yuboriladi. Ammo admin dasturi yopiq yoki tarmoq uzilgan bo'lsa, hech kim
/// buyruq bera olmaydi — kompyuter qulflanib, cheksiz yonib turardi.
///
/// Shuning uchun agent ham o'zi hisoblaydi: bu **zaxira** yo'l. Ikkalasi
/// ishlasa ham zarari yo'q — kompyuter baribir bir marta o'chadi.
/// </summary>
public static class ExpirePolicy
{
    public const string Lock = "lock";
    public const string Off = "off";
    public const string Delay = "delay";

    /// <summary>Kechikish chegarasi: sozlamada buzuq qiymat kelsa ham aqlli qoladi.</summary>
    public const int MinDelayMinutes = 1;
    public const int MaxDelayMinutes = 120;

    /// <summary>Vaqt tugadi — darhol o'chirish kerakmi.</summary>
    public static bool OffNow(string? onExpire) => onExpire == Off;

    /// <summary>
    /// Vaqt tugadi — kechiktirilgan o'chirish vaqti. Sozlama "delay" bo'lmasa
    /// <c>null</c> (ya'ni hech qachon o'chmaydi, faqat qulflanadi).
    /// </summary>
    public static DateTime? OffAt(string? onExpire, int offDelayMinutes, DateTime now)
    {
        if (onExpire != Delay) return null;
        var minutes = Math.Clamp(offDelayMinutes, MinDelayMinutes, MaxDelayMinutes);
        return now.AddMinutes(minutes);
    }

    /// <summary>
    /// Belgilangan vaqt keldimi va hali ham o'chirish kerakmi.
    ///
    /// Yangi vaqt ochilgan bo'lsa (<paramref name="hasSession"/>) o'chirilmaydi —
    /// sozlamada ham "yangi vaqt ochilmasa" deb yozilgan. Klient dasturi
    /// to'xtatilgan bo'lsa (Ctrl+Alt+K) ham aralashmaymiz.
    /// </summary>
    public static bool ShouldPowerOff(DateTime? offAt, bool hasSession, bool clientPaused, DateTime now)
    {
        if (offAt is not { } t) return false;
        if (hasSession || clientPaused) return false;
        return now >= t;
    }
}
