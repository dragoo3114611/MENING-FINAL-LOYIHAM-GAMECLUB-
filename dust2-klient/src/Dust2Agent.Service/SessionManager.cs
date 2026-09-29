using Dust2Agent.Common;

namespace Dust2Agent.Service;

/// <summary>
/// Seans holati va taymer. Vaqt monotonik soat bilan sanaladi, shuning uchun
/// Windows soati o'zgarsa ham buzilmaydi. Holat diskka yoziladi — admin yoki
/// kompyuter o'chsa ham seans davom etadi.
/// </summary>
public sealed class SessionManager
{
    private readonly AgentLog _log;
    private readonly SessionTimer _timer = new();
    private readonly object _lock = new();

    private SessionPayload? _session;
    private bool _warned;

    public SessionManager(AgentLog log) => _log = log;

    /// <summary>Seans tugadi (vaqt bitdi) — qulf ekrani ko'rsatiladi.</summary>
    public event Action<long>? Expired;

    /// <summary>Tugashiga oz qoldi.</summary>
    public event Action<int>? Warning;

    /// <summary>Holat o'zgardi — qobiqqa yuborish kerak.</summary>
    public event Action? Changed;

    public SessionPayload? Current
    {
        get { lock (_lock) return _session; }
    }

    public bool HasSession => Current is not null;

    /// <summary>Ogohlantirish chegarasi (admindan keladi).</summary>
    public int WarnMinutes { get; set; } = 5;

    /// <summary>Kompyuter yonganda saqlangan seansni tiklaydi.</summary>
    public void Restore()
    {
        var stored = SessionStore.Load();
        if (stored is null) return;

        long? remaining = null;
        if (stored.EndsAtUtc is { } endsAt)
        {
            // Vaqt kompyuter o'chiq bo'lganda ham sanaladi (qaror 1)
            remaining = endsAt - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (remaining <= 0)
            {
                _log.Info("Saqlangan seansning vaqti kompyuter o'chiq paytda tugagan");
                SessionStore.Save(null);
                return;
            }
        }

        lock (_lock)
        {
            _session = new SessionPayload
            {
                SessionId = stored.SessionId,
                Mode = stored.Mode,
                Client = stored.Client,
                Rate = stored.Rate,
                Paused = stored.Paused,
                Due = stored.Due,
                StartedAt = stored.StartedAtUtc,
                EndsAt = stored.EndsAtUtc,
                RemainingMs = remaining
            };
            _timer.Start(_session.ToMode(), remaining ?? 0);
            if (stored.Paused) _timer.Pause();
        }

        _log.Info(
            remaining is null
                ? "Saqlangan seans tiklandi (ochiq vaqt)"
                : $"Saqlangan seans tiklandi — qoldi {remaining / 60000} daqiqa");
        Changed?.Invoke();
    }

    /// <summary>Admindan kelgan seans holati.</summary>
    public void Apply(SessionPayload? s)
    {
        lock (_lock)
        {
            if (s is null)
            {
                // Admin har sinxronizatsiyada seans holatini yuboradi. Seans allaqachon
                // yo'q bo'lsa hech narsa o'zgarmaydi — jurnalni to'ldirmaymiz va
                // qobiqqa ortiqcha yangilanish yubormaymiz.
                if (_session is null && !_timer.IsRunning) return;

                _session = null;
                _timer.Stop();
                _warned = false;
                SessionStore.Save(null);
                _log.Info("Seans yopildi — ekran qulflandi");
                Changed?.Invoke();
                return;
            }

            var isNew = _session is null || _session.SessionId != s.SessionId;
            _session = s;
            if (isNew) _warned = false;

            if (s.Mode == "open")
            {
                _timer.Start(SessionMode.Open, 0);
            }
            else
            {
                var remaining = s.RemainingMs ?? 0;
                if (isNew || !_timer.IsRunning) _timer.Start(s.ToMode(), remaining);
                else _timer.SyncRemaining(remaining);
            }

            if (s.Paused) _timer.Pause();
            else _timer.Resume();

            Persist();
        }
        Changed?.Invoke();
    }

    public void Pause()
    {
        lock (_lock)
        {
            if (_session is null) return;
            _timer.Pause();
            _session.Paused = true;
            Persist();
        }
        Changed?.Invoke();
    }

    public void Resume(long? remainingMs)
    {
        lock (_lock)
        {
            if (_session is null) return;
            if (remainingMs is { } r) _timer.SyncRemaining(r);
            _timer.Resume();
            _session.Paused = false;
            Persist();
        }
        Changed?.Invoke();
    }

    public void AddTime(long? remainingMs, int addedMin)
    {
        lock (_lock)
        {
            if (_session is null) return;
            if (_session.Mode != "open" && remainingMs is { } r)
            {
                _timer.SyncRemaining(r);
                _session.RemainingMs = r;
                _session.EndsAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + r;
                _warned = false;
            }
            Persist();
        }
        _log.Info($"Vaqt qo'shildi: +{addedMin} daqiqa");
        Changed?.Invoke();
    }

    /// <summary>Qolgan vaqt (ochiq vaqtda null).</summary>
    public long? RemainingMs
    {
        get { lock (_lock) return _timer.RemainingMs; }
    }

    public long ElapsedMs
    {
        get { lock (_lock) return _timer.ElapsedMs; }
    }

    /// <summary>Har soniyada chaqiriladi.</summary>
    public void Tick()
    {
        SessionPayload? expired = null;
        int? warn = null;

        lock (_lock)
        {
            if (_session is null || _session.Paused) return;

            var left = _timer.RemainingMs;
            if (left is null) return;

            if (!_warned && WarnMinutes > 0 && left > 0 && left <= WarnMinutes * 60_000L)
            {
                _warned = true;
                warn = WarnMinutes;
            }

            if (left <= 0)
            {
                expired = _session;
                _session = null;
                _timer.Stop();
                _warned = false;
                SessionStore.Save(null);
            }
        }

        if (warn is { } w) Warning?.Invoke(w);
        if (expired is not null)
        {
            _log.Info("Vaqt tugadi — ekran qulflandi (mahalliy taymer)");
            Expired?.Invoke(expired.Due);
            Changed?.Invoke();
        }
    }

    /// <summary>Holatni diskka yozadi (lock ichida chaqiriladi).</summary>
    private void Persist()
    {
        if (_session is null)
        {
            SessionStore.Save(null);
            return;
        }
        var remaining = _timer.RemainingMs;
        SessionStore.Save(new StoredSession
        {
            SessionId = _session.SessionId,
            Mode = _session.Mode,
            Client = _session.Client,
            Rate = _session.Rate,
            Paused = _session.Paused,
            Due = _session.Due,
            StartedAtUtc = _session.StartedAt,
            EndsAtUtc = remaining is null
                ? null
                : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + remaining,
            RemainingMs = remaining
        });
    }

    /// <summary>Holatni diskka yozishni majburlaydi (davriy saqlash uchun).</summary>
    public void Flush()
    {
        lock (_lock) Persist();
    }
}
