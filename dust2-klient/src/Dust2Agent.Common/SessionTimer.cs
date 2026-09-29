namespace Dust2Agent.Common;

public enum SessionMode
{
    /// <summary>Oldindan to'langan — qolgan vaqt sanaladi.</summary>
    Prepaid,

    /// <summary>Ochiq vaqt — o'ynalgan vaqt yuqoriga sanaladi.</summary>
    Open,

    /// <summary>Akkaunt — balansdan yechiladi, qolgan vaqt admin tomonidan beriladi.</summary>
    Account
}

/// <summary>
/// Seans taymeri. Windows soatiga bog'liq emas: hisob <see cref="Environment.TickCount64"/>
/// (monotonik soat) bo'yicha yuritiladi, shuning uchun soat o'zgarsa ham vaqt buzilmaydi.
/// Admin mutlaq tugash vaqtini yuboradi, agent esa qolgan millisekundlarni monotonik sanaydi.
/// </summary>
public sealed class SessionTimer
{
    private readonly Func<long> _now;

    private long _startedMono;
    private long _pausedAtMono;     // 0 — pauza emas
    private long _pausedForMs;
    private long _totalMs;          // oldindan to'langan / akkaunt uchun berilgan vaqt

    public SessionTimer(Func<long>? monotonicNow = null) =>
        _now = monotonicNow ?? (() => Environment.TickCount64);

    public bool IsRunning { get; private set; }
    public SessionMode Mode { get; private set; } = SessionMode.Prepaid;
    public bool IsPaused => _pausedAtMono != 0;

    /// <summary>Seansni boshlaydi. <paramref name="totalMs"/> — ochiq vaqtda 0.</summary>
    public void Start(SessionMode mode, long totalMs)
    {
        Mode = mode;
        _totalMs = mode == SessionMode.Open ? 0 : Math.Max(0, totalMs);
        _startedMono = _now();
        _pausedAtMono = 0;
        _pausedForMs = 0;
        IsRunning = true;
    }

    /// <summary>Vaqt qo'shadi. Ochiq vaqtda taymer paydo bo'lmaydi — summa adminda hisoblanadi.</summary>
    public void AddTime(long ms)
    {
        if (!IsRunning || Mode == SessionMode.Open) return;
        _totalMs = Math.Max(0, _totalMs + ms);
    }

    /// <summary>Admindan kelgan qolgan vaqt bilan sinxronlash (kompyuter yonganda / aloqa tiklanganda).</summary>
    public void SyncRemaining(long remainingMs)
    {
        if (!IsRunning || Mode == SessionMode.Open) return;
        _totalMs = Math.Max(0, ElapsedMs + remainingMs);
    }

    public void Pause()
    {
        if (!IsRunning || IsPaused) return;
        _pausedAtMono = _now();
    }

    public void Resume()
    {
        if (!IsRunning || !IsPaused) return;
        _pausedForMs += _now() - _pausedAtMono;
        _pausedAtMono = 0;
    }

    public void Stop()
    {
        IsRunning = false;
        _pausedAtMono = 0;
    }

    /// <summary>Pauzalarni hisobga olgan holda o'ynalgan vaqt.</summary>
    public long ElapsedMs
    {
        get
        {
            if (!IsRunning) return 0;
            var paused = IsPaused ? _now() - _pausedAtMono : 0;
            return Math.Max(0, _now() - _startedMono - _pausedForMs - paused);
        }
    }

    /// <summary>Qolgan vaqt. Ochiq vaqtda <c>null</c>.</summary>
    public long? RemainingMs => Mode == SessionMode.Open || !IsRunning ? null : _totalMs - ElapsedMs;

    public bool IsExpired => RemainingMs is <= 0;
}
