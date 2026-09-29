namespace Dust2Agent.Common;

/// <summary>
/// Qobiqdan keladigan qaysi buyruqlar xizmat parolini talab qiladi. Sof mantiq,
/// testlar bilan qoplangan.
///
/// Ilgari parol faqat qobiq oynasida so'ralardi, xizmat esa quvurdan kelgan
/// <c>pause</c>, <c>unpair</c> va <c>connect</c> ni tekshiruvsiz bajarardi: kompyuterdagi
/// istalgan dastur quvurga shu xabarni yozib qulfni o'chira olardi
/// ([qaror 22](../../../docs/QARORLAR.md)). Endi tekshiruv xizmatning o'zida.
/// </summary>
public static class ShellCommandPolicy
{
    /// <param name="type">Quvur xabari turi (<see cref="PipeTypes"/>).</param>
    /// <param name="hasPassword">Admin xizmat parolini o'rnatganmi. O'rnatilmagan bo'lsa
    /// (birinchi o'rnatish) hozirgidek hammasi ruxsat etiladi.</param>
    public static bool NeedsPassword(string? type, bool hasPassword)
    {
        if (!hasPassword) return false;
        return type is PipeTypes.Pause or PipeTypes.Unpair or PipeTypes.Connect;
    }
}

/// <summary>
/// Xizmat parolini tekshirish holati. To'g'ri parol himoyalangan buyruqlarga qisqa
/// muddatli ruxsat beradi; ketma-ket xatolardan keyin tekshiruv vaqtincha to'xtaydi —
/// to'rt xonali parolni dastur bilan terib chiqib bo'lmasin. Sof mantiq: vaqt
/// parametr sifatida beriladi.
/// </summary>
public sealed class ServicePasswordGate
{
    /// <summary>To'g'ri paroldan keyin himoyalangan buyruqlar shuncha vaqt ruxsat etiladi.</summary>
    public static readonly TimeSpan GrantFor = TimeSpan.FromMinutes(5);

    /// <summary>Shuncha xatodan keyin birinchi blok boshlanadi.</summary>
    public const int FreeAttempts = 5;

    public static readonly TimeSpan FirstBlock = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MaxBlock = TimeSpan.FromMinutes(15);

    private readonly object _lock = new();
    private DateTime _grantedUntil = DateTime.MinValue;
    private DateTime _blockedUntil = DateTime.MinValue;
    private int _failures;
    private TimeSpan _nextBlock = FirstBlock;

    /// <summary>Hozir himoyalangan buyruqlar ruxsat etilganmi.</summary>
    public bool IsGranted(DateTime now)
    {
        lock (_lock) return now < _grantedUntil;
    }

    /// <summary>Ruxsatni bekor qiladi (qobiq uzildi yoki parol o'zgardi).</summary>
    public void Revoke()
    {
        lock (_lock) _grantedUntil = DateTime.MinValue;
    }

    /// <summary>Tekshiruv vaqtincha to'xtatilganmi va yana qancha kutish kerak.</summary>
    public bool IsBlocked(DateTime now, out TimeSpan wait)
    {
        lock (_lock)
        {
            wait = _blockedUntil > now ? _blockedUntil - now : TimeSpan.Zero;
            return wait > TimeSpan.Zero;
        }
    }

    /// <summary>To'g'ri parol: ruxsat beriladi, xatolar hisobi nollanadi.</summary>
    public void RecordSuccess(DateTime now)
    {
        lock (_lock)
        {
            _failures = 0;
            _nextBlock = FirstBlock;
            _blockedUntil = DateTime.MinValue;
            _grantedUntil = now + GrantFor;
        }
    }

    /// <summary>
    /// Noto'g'ri parol. Dastlabki xatolar kutishsiz; <see cref="FreeAttempts"/>-xatodan
    /// boshlab har bir xato blok qo'yadi va keyingi blok ikki baravar uzayadi
    /// (<see cref="MaxBlock"/> gacha).
    /// </summary>
    /// <returns><c>true</c> — shu xato bilan blok boshlandi.</returns>
    public bool RecordFailure(DateTime now)
    {
        lock (_lock)
        {
            _failures++;
            if (_failures < FreeAttempts) return false;

            _blockedUntil = now + _nextBlock;
            var doubled = _nextBlock + _nextBlock;
            _nextBlock = doubled > MaxBlock ? MaxBlock : doubled;
            return true;
        }
    }
}

/// <summary>
/// Quvurga ulangan dastur haqiqiy qobiqmi. Xizmat faqat o'z yonidagi
/// <see cref="ShellExeName"/> bilan gaplashadi — boshqa dastur quvurni egallab,
/// qobiq nomidan buyruq bera olmasin.
/// </summary>
public static class ShellIdentity
{
    public const string ShellExeName = "Dust2Agent.Shell.exe";

    /// <param name="clientPath">Quvurga ulangan jarayonning to'liq yo'li.</param>
    /// <param name="expectedPath">Xizmat yonidagi qobiq fayli.</param>
    public static bool IsTrustedShell(string? clientPath, string? expectedPath)
    {
        if (string.IsNullOrWhiteSpace(clientPath) || string.IsNullOrWhiteSpace(expectedPath)) return false;
        try
        {
            return string.Equals(
                Path.GetFullPath(clientPath),
                Path.GetFullPath(expectedPath),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
