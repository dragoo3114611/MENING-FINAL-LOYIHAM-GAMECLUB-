namespace Dust2Agent.Common;

/// <summary>
/// Qobiqda qaysi ekran ko'rsatilishi. Sof mantiq — testlar bilan qoplangan.
///
/// Muhim qoida: seans pauzaga qo'yilganda ekran <c>locked</c> bo'ladi. Aks holda
/// mijoz pauza davomida o'ynayverardi va pauza bepul vaqtga aylanardi
/// ([qaror 14](../../../docs/QARORLAR.md)). Kiritishni bloklash ham shu holatga
/// bog'langan, shuning uchun qo'shimcha shart kerak emas.
/// </summary>
public static class ShellScreen
{
    public const string Setup = "setup";
    public const string Session = "session";
    public const string Locked = "locked";

    /// <param name="paired">Admin bilan juftlangan (token bor).</param>
    /// <param name="connecting">Foydalanuvchi hozir ulanish kodini kiritmoqda.</param>
    /// <param name="hasSession">Ochiq seans bormi.</param>
    /// <param name="paused">Seans pauzada.</param>
    public static string Pick(bool paired, bool connecting, bool hasSession, bool paused)
    {
        if (!paired && !connecting) return Setup;
        return hasSession && !paused ? Session : Locked;
    }

    /// <summary>
    /// Qulf ekranining pastki burchagida "Ulanish sozlamalari" tugmasi
    /// ko'rinsinmi.
    ///
    /// Juftlangan kompyuterda — yo'q. Aks holda mijoz qulf ekranida turib
    /// admin manzili va kompyuter nomini ochib ko'rardi; xizmat paroli
    /// o'rnatilmagan bo'lsa esa uni o'zgartira ham olardi. Juftlangandan
    /// keyin bu oynaga faqat maxfiy kombinatsiya orqali kiriladi
    /// ([qaror 18](../../../docs/QARORLAR.md)).
    ///
    /// Juftlanmagan kompyuterda tugma kerak: ulanish kodi kiritilayotganda
    /// ekran "locked" bo'ladi va orqaga qaytish yo'li shu.
    /// </summary>
    public static bool ShowSetupButton(bool paired) => !paired;

    /// <summary>
    /// Onlayn (admin bilan ulangan) holatni ham hisobga oladi: kompyuter admin
    /// nazoratida bo'lsa, token biror sababga ko'ra saqlanmagan bo'lsa ham
    /// (masalan DPAPI xatosi yoki %ProgramData% ga yozib bo'lmasa), ulanish
    /// sozlamalari tugmasi ko'rsatilmaydi. Aks holda mijoz admin manzilini ko'rar
    /// va parol o'rnatilmagan bo'lsa o'zgartira olardi ([qaror 18]).
    /// </summary>
    public static bool ShowSetupButton(bool paired, bool online) => !paired && !online;

    /// <summary>
    /// Bildirishnoma (admin xabari, ogohlantirish) alohida oynada
    /// ko'rsatilsinmi.
    ///
    /// Seans ochiq bo'lsa qulf oynasi yashiriladi va unga yozilgan xabarni
    /// mijoz ko'rmaydi — o'sha holatda alohida oyna kerak
    /// ([qaror 21](../../../docs/QARORLAR.md)). Qolgan hollarda qulf oynasining
    /// o'z qatlami ishlatiladi: u butun ekranni egallaydi va chiroyliroq.
    /// </summary>
    public static bool NoticeInOwnWindow(string? screen) => screen == Session;
}
