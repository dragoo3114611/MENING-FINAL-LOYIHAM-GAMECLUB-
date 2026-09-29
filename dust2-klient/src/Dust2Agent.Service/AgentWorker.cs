using System.Runtime.Versioning;
using System.Text.Json;
using Dust2Agent.Common;
using Microsoft.Extensions.Hosting;

namespace Dust2Agent.Service;

/// <summary>
/// Xizmatning asosiy tsikli: admin bilan aloqa, seans taymeri, qobiq bilan
/// almashinuv va Windows amallari (soat, o'chirish).
/// </summary>
public sealed class AgentWorker : BackgroundService
{
    private static readonly TimeSpan TimeSyncInterval = TimeSpan.FromMinutes(10);

    private readonly AgentLog _log;
    private readonly AdminConnection _admin;
    private readonly SessionManager _sessions;
    private readonly ShellPipeServer _pipe;
    private readonly ShellLauncher? _shell;
    private readonly WallpaperCache _wallpaper;

    private AgentConfig _config = new();
    private LockConfigPayload _lock = new();
    private BehaviourPayload _behaviour = new();
    private DateTime _lastTimeSync = DateTime.MinValue;
    private long _clockDrift;
    private string _connectError = "";
    private string _connectPhase = "idle";
    private string _loginError = "";
    private long _pendingDue;

    /// <summary>"Qulflash, keyin o'chirish" uchun belgilangan vaqt (UTC).</summary>
    private DateTime? _offAt;

    public AgentWorker(AgentLog log)
    {
        _log = log;
        _admin = new AdminConnection(log) { Version = Version() };
        _sessions = new SessionManager(log);
        _pipe = new ShellPipeServer(log);
        _wallpaper = new WallpaperCache(log);
        if (OperatingSystem.IsWindows()) _shell = new ShellLauncher(log);
    }

    private static string Version() =>
        typeof(AgentWorker).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        AgentPaths.EnsureCreated();
        _log.Info($"DUST2 klient agent ishga tushdi (versiya {Version()}, {Environment.MachineName})");

        _config = AgentConfig.Load();
        if (string.IsNullOrWhiteSpace(_config.PcName)) _config.PcName = Environment.MachineName;
        _admin.Config = _config;
        _admin.Paused = _config.Paused;
        if (_config.Paused) _log.Info("Klient dasturi to'xtatilgan holatda — qulf ekrani ko'rsatilmaydi");

        _lock = new LockConfigPayload();
        _sessions.WarnMinutes = _behaviour.WarnMin;
        _sessions.Restore();

        _admin.Received += OnAdminMessage;
        _admin.OnlineChanged += _ => PushState();
        _sessions.Changed += PushState;
        _sessions.Expired += OnSessionExpired;
        _sessions.Warning += OnWarning;
        _pipe.Received += OnShellMessage;
        _pipe.ShellConnected += connected =>
        {
            if (connected) PushState();
        };

        var adminTask = SuperviseAsync("Admin bilan aloqa", _admin.RunAsync, ct);
        var pipeTask = _pipe.RunAsync(ct);
        var loopTask = MainLoopAsync(ct);

        await Task.WhenAll(adminTask, pipeTask, loopTask).ConfigureAwait(false);
        _log.Info("DUST2 klient agent to'xtatildi");
    }

    /// <summary>
    /// Tsikl xizmat to'xtamagan holda tugab qolsa, uni qayta ishga tushiradi.
    ///
    /// Bir marta shunday bo'lgan: ulanish taymeri ishga tushganda admin tsikli
    /// jimgina chiqib ketdi va kompyuter admin bilan boshqa ulanmadi. Sabab
    /// tuzatildi, lekin himoya qatlami qolsin — agent hech qachon "jim"
    /// to'xtab qolmasligi kerak.
    /// </summary>
    private async Task SuperviseAsync(string name, Func<CancellationToken, Task> run, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await run(ct).ConfigureAwait(false);
                if (ct.IsCancellationRequested) return;
                _log.Warn($"{name} tsikli to'xtab qoldi — qayta ishga tushirilmoqda");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _log.Error($"{name} tsiklida kutilmagan xato — qayta ishga tushirilmoqda", ex);
            }

            try
            {
                await Task.Delay(1000, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>Har soniyalik tsikl: taymer, qobiq nazorati, rejali soat tekshiruvi.</summary>
    private async Task MainLoopAsync(CancellationToken ct)
    {
        var lastFlush = DateTime.UtcNow;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _sessions.Tick();
                // To'xtatilgan bo'lsa qobiqni o'zimiz ochmaymiz — foydalanuvchi
                // yorliq orqali ishga tushirsa, u qayta tiklanadi
                if (!_config.Paused && OperatingSystem.IsWindows()) _shell?.EnsureRunning();

                // Yangi vaqt ochilgan bo'lsa kechiktirilgan o'chirish bekor bo'ladi
                if (_sessions.HasSession) _offAt = null;
                else if (OperatingSystem.IsWindows()
                         && ExpirePolicy.ShouldPowerOff(_offAt, hasSession: false, _config.Paused, DateTime.UtcNow))
                {
                    _offAt = null;
                    _log.Info("Belgilangan vaqt tugadi — kompyuter o'chirilmoqda");
                    PowerControl.Shutdown(_log);
                }

                if (_admin.IsOnline && DateTime.UtcNow - _lastTimeSync > TimeSyncInterval)
                {
                    _lastTimeSync = DateTime.UtcNow;
                    _admin.Send("time.request");
                }

                // Seans holatini vaqti-vaqti bilan diskka yozamiz (tok o'chib qolsa ham qolsin)
                if (_sessions.HasSession && DateTime.UtcNow - lastFlush > TimeSpan.FromSeconds(30))
                {
                    lastFlush = DateTime.UtcNow;
                    _sessions.Flush();
                }

                PushStateIfSession();
            }
            catch (Exception ex)
            {
                _log.Error("Agent tsiklida xato", ex);
            }

            try
            {
                await Task.Delay(1000, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>Seans ochiq bo'lsa qobiqdagi taymer har soniyada yangilanadi.</summary>
    private void PushStateIfSession()
    {
        if (_sessions.HasSession) PushState();
    }

    /* ------------------------------ admin xabarlari ------------------------------ */

    private void OnAdminMessage(Envelope msg)
    {
        switch (msg.Type)
        {
            case "pair.ok":
            {
                var p = msg.PayloadAs<PairOkPayload>();
                if (p is null) return;
                TokenStore.Save(p.Token);
                _config.PcId = p.PcId;
                _config.PcName = p.Name;
                _config.Save();
                _admin.PendingCode = null;
                _connectPhase = "idle";
                _connectError = "";
                _log.Info($"Juftlandi: {p.Name} (pcId {p.PcId})");
                SyncClock(p.ServerTime);
                Toast("Adminga ulandi", $"{_config.Host}:{_config.Port} · {p.Name}", "ok");
                PushState();
                return;
            }

            case "pair.denied":
            {
                var p = msg.PayloadAs<PairDeniedPayload>();
                _admin.PendingCode = null;
                _connectPhase = "error";
                _connectError = p?.Message ?? "Juftlash rad etildi";
                _log.Warn($"Juftlash rad etildi: {_connectError}");
                _admin.Drop();
                PushState();
                return;
            }

            case "hello.ok":
            {
                var p = msg.PayloadAs<HelloOkPayload>();
                if (p is null) return;
                _config.PcId = p.PcId;
                _config.PcName = p.Name;
                ApplyAgentConfig(p.Agent);
                if (p.Lock is not null) ApplyLock(p.Lock);
                if (p.Behaviour is not null) ApplyBehaviour(p.Behaviour);
                _sessions.Apply(p.Session);
                _connectPhase = "idle";
                _connectError = "";
                SyncClock(p.ServerTime);
                PushState();
                return;
            }

            case "sync":
            {
                var p = msg.PayloadAs<HelloOkPayload>();
                if (p is null) return;
                ApplyAgentConfig(p.Agent);
                if (p.Lock is not null) ApplyLock(p.Lock);
                if (p.Behaviour is not null) ApplyBehaviour(p.Behaviour);
                _sessions.Apply(p.Session);
                PushState();
                return;
            }

            case "session.start":
            {
                var p = msg.PayloadAs<SessionPayload>();
                if (p is null) return;
                _pendingDue = 0;
                _loginError = "";
                _sessions.Apply(p);
                _admin.Send("session.started", new { sessionId = p.SessionId });
                return;
            }

            case "session.add_time":
            {
                var remaining = msg.Num("remainingMs");
                var added = (int)(msg.Num("addedMin") ?? 0);
                _sessions.AddTime(remaining, added);
                Toast($"+{added} daqiqa qo'shildi", "", "ok");
                _admin.Send("session.updated", new
                {
                    sessionId = _sessions.Current?.SessionId ?? 0,
                    remainingMs = _sessions.RemainingMs ?? 0
                });
                return;
            }

            case "session.pause":
                _sessions.Pause();
                Toast("Pauza", "Administrator vaqtni to'xtatdi", "info");
                return;

            case "session.resume":
                _sessions.Resume(msg.Num("remainingMs"));
                Toast("Davom etmoqda", "Vaqt yana sanalmoqda", "ok");
                return;

            case "session.end":
            case "session.lock":
                _pendingDue = msg.Num("due") ?? 0;
                _sessions.Apply(null);
                _admin.Send("session.ended", new { reason = "admin" });
                PushState();
                return;

            case "message":
            {
                var text = msg.Str("text") ?? "";
                _pipe.Send(PipeTypes.AdminMessage, new { text });
                _admin.Send("message.shown", new { re = msg.Id });
                return;
            }

            case "lock.config":
            {
                var p = msg.PayloadAs<LockConfigPayload>();
                if (p is not null) ApplyLock(p);
                PushState();
                return;
            }

            case "lock.wallpaper":
            {
                var hash = msg.Str("hash");
                var mime = msg.Str("mime") ?? "image/jpeg";
                var data = msg.Str("dataBase64") ?? "";
                _wallpaper.Save(hash, mime, data);
                PushState();
                return;
            }

            case "theme.set":
            {
                var id = msg.Str("id");
                if (!string.IsNullOrEmpty(id)) _lock.Theme = id;
                PushState();
                return;
            }

            case "time.sync":
            {
                SyncClock(msg.Num("serverTime") ?? 0);
                return;
            }

            case "auth.ok":
            {
                _loginError = "";
                var p = msg.PayloadAs<AuthOkPayload>();
                if (p is not null) Toast($"Xush kelibsiz, {p.Login}", $"Balans: {p.Balance} so'm", "ok");
                return;
            }

            case "auth.denied":
            {
                var p = msg.PayloadAs<AuthDeniedPayload>();
                _loginError = p?.Message ?? p?.Reason switch
                {
                    "low_balance" => "Balans yetarli emas",
                    "busy" => "Bu akkaunt boshqa kompyuterda ochiq",
                    _ => "Login yoki parol noto'g'ri"
                };
                PushState();
                return;
            }

            case "power.off":
            {
                var mode = msg.Str("mode") ?? "now";
                if (mode == "after_session")
                {
                    _log.Info("Vaqt tugagach o'chirish rejalashtirildi");
                    return;
                }
                _log.Info("Admin buyrug'i bilan o'chirilmoqda");
                if (OperatingSystem.IsWindows()) PowerControl.Shutdown(_log);
                return;
            }

            case "power.reboot":
                _log.Info("Admin buyrug'i bilan qayta yuklanmoqda");
                if (OperatingSystem.IsWindows()) PowerControl.Reboot(_log);
                return;

            case "process.list":
                SendProcessListAsync();
                return;

            case "process.kill":
            {
                var pids = msg.Payload.TryGetProperty("pids", out var arr) && arr.ValueKind == JsonValueKind.Array
                    ? arr.EnumerateArray().Where(x => x.TryGetInt32(out _)).Select(x => x.GetInt32()).ToArray()
                    : Array.Empty<int>();
                if (pids.Length == 0) return;
                var killed = OperatingSystem.IsWindows() ? ProcessList.Kill(_log, pids) : 0;
                _log.Info($"Admin buyrug'i bilan {killed} ta jarayon yopildi");
                SendProcessListAsync();
                return;
            }

            case "ack":
                return;

            case "error":
            {
                var code = msg.Str("code") ?? "";
                _log.Warn($"Admin xatosi ({code}): {msg.Str("message")}");
                if (code == "unauthorized")
                {
                    TokenStore.Clear();
                    _config.PcId = 0;
                    _config.Save();
                    _connectPhase = "error";
                    _connectError = "Maxfiy kalit yaroqsiz — ulanish kodini qayta kiriting";
                    PushState();
                }
                return;
            }

            default:
                _log.Warn($"Noma'lum xabar: {msg.Type}");
                return;
        }
    }

    private void ApplyAgentConfig(AgentConfigPayload? a)
    {
        if (a is null) return;
        var changed = _config.ServicePassHash != a.ServicePassHash || _config.UnlockCombo != a.UnlockCombo;
        _config.ServicePassHash = a.ServicePassHash;
        _config.UnlockCombo = string.IsNullOrWhiteSpace(a.UnlockCombo) ? "Ctrl+Alt+P" : a.UnlockCombo;
        if (changed) _config.Save();
    }

    private void ApplyLock(LockConfigPayload p)
    {
        _lock = p;
        if (!string.IsNullOrEmpty(p.WallpaperHash) && !_wallpaper.Has(p.WallpaperHash))
        {
            _admin.Send("lock.wallpaper.request");
        }
        else if (string.IsNullOrEmpty(p.WallpaperHash))
        {
            _wallpaper.Clear();
        }
    }

    private void ApplyBehaviour(BehaviourPayload p)
    {
        _behaviour = p;
        _sessions.WarnMinutes = p.WarnMin;
    }

    private void SyncClock(long serverTime)
    {
        if (serverTime <= 0) return;
        _lastTimeSync = DateTime.UtcNow;
        if (!OperatingSystem.IsWindows())
        {
            _clockDrift = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - serverTime;
            _admin.Send("time.synced", new { driftMs = _clockDrift });
            return;
        }
        _clockDrift = ClockSync.SyncTo(serverTime, _log);
        _admin.Send("time.synced", new { driftMs = _clockDrift });
        if (Math.Abs(_clockDrift) >= 1000)
        {
            Toast("Soat sinxronlandi", "Kompyuter soati admin bilan moslandi", "ok");
        }
    }

    /// <summary>
    /// Jarayonlar ro'yxatini yig'ib adminga yuboradi. Protsessor foizini o'lchash
    /// uchun ikki o'lchov kerak, shuning uchun alohida vazifada bajariladi.
    /// </summary>
    private void SendProcessListAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            _admin.Send("process.list.result", new { items = Array.Empty<object>() });
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var r = await ProcessList.CollectAsync(_log, CancellationToken.None).ConfigureAwait(false);
                _admin.Send("process.list.result", new
                {
                    items = r.Items.Select(x => new
                    {
                        pid = x.Pid,
                        exe = x.Exe,
                        name = x.Name,
                        cpu = x.Cpu,
                        ramMb = x.RamMb,
                        @protected = x.Protected,
                        kind = x.Kind,
                        hang = x.Hang
                    }),
                    cpu = r.Cpu,
                    ramUsedMb = r.RamUsedMb,
                    ramTotalMb = r.RamTotalMb
                });
            }
            catch (Exception ex)
            {
                _log.Error("Jarayonlar ro'yxatini yig'ib bo'lmadi", ex);
                _admin.Send("process.list.result", new { items = Array.Empty<object>() });
            }
        });
    }

    private void OnSessionExpired(long due)
    {
        _pendingDue = due;
        _admin.Send("session.ended", new { reason = "expired" });
        PushState();

        // To'xtatilgan klientda kompyuterni o'chirmaymiz — dastur umuman ishlamayapti
        _offAt = null;
        if (_config.Paused || !OperatingSystem.IsWindows()) return;

        if (ExpirePolicy.OffNow(_behaviour.OnExpire))
        {
            _log.Info("Sozlamaga ko'ra kompyuter o'chirilmoqda");
            PowerControl.Shutdown(_log);
            return;
        }

        // "Qulflash, keyin o'chirish": odatda buyruqni admin yuboradi, lekin
        // admin yopiq yoki tarmoq uzilgan bo'lsa hech kim yubormaydi — o'zimiz
        // ham sanab turamiz (zaxira yo'l)
        var now = DateTime.UtcNow;
        _offAt = ExpirePolicy.OffAt(_behaviour.OnExpire, _behaviour.OffDelay, now);
        if (_offAt is { } t)
        {
            var minutes = (int)Math.Round((t - now).TotalMinutes);
            _log.Info($"Sozlamaga ko'ra {minutes} daqiqadan keyin o'chadi (yangi vaqt ochilmasa)");
        }
    }

    private void OnWarning(int minutes)
    {
        Toast($"{minutes} daqiqa qoldi", "Vaqt qo'shish uchun administratorga murojaat qiling", "warn");
        _admin.Send("client.warning_shown", new { minutes });
    }

    /* ------------------------------ qobiq xabarlari ------------------------------ */

    private void OnShellMessage(PipeMessage msg)
    {
        switch (msg.Type)
        {
            case PipeTypes.Hello:
                PushState();
                return;

            case PipeTypes.Connect:
            {
                var r = msg.As<ConnectRequest>();
                if (r is null) return;
                _config.Host = r.Host.Trim();
                _config.Port = r.Port;
                if (!string.IsNullOrWhiteSpace(r.Name)) _config.PcName = r.Name.Trim();
                _config.Save();
                _admin.Config = _config;
                _admin.PendingCode = string.IsNullOrWhiteSpace(r.Code) ? null : r.Code.Trim();
                _connectPhase = "connecting";
                _connectError = "";
                _log.Info($"Ulanish so'raldi: {_config.Host}:{_config.Port} ({_config.PcName})");
                _admin.Drop();
                PushState();
                return;
            }

            case PipeTypes.Pause:
                if (!_config.Paused)
                {
                    _config.Paused = true;
                    _config.Save();
                    _admin.Paused = true;
                    _admin.Drop();
                    _log.Info("Klient dasturi to'xtatildi (qulf ekrani o'chirildi)");
                }
                PushState();
                return;

            case PipeTypes.Resume:
                if (_config.Paused)
                {
                    _config.Paused = false;
                    _config.Save();
                    _admin.Paused = false;
                    _admin.Wake();
                    _log.Info("Klient dasturi qayta ishga tushirildi");
                }
                PushState();
                return;

            case PipeTypes.Unpair:
                // Adminga ham aytamiz: u tokenni o'chiradi va kompyuterni
                // "juftlanmagan" deb ko'rsatadi (aks holda ikki tomon kelishmaydi)
                if (_admin.IsOnline) _admin.Send("client.unpair");
                TokenStore.Clear();
                _config.PcId = 0;
                _config.Save();
                _admin.Drop();
                _sessions.Apply(null);
                _connectPhase = "idle";
                _log.Info("Klient admindan uzildi");
                PushState();
                return;

            case PipeTypes.Login:
            {
                var r = msg.As<LoginRequest>();
                if (r is null) return;
                if (!_admin.IsOnline)
                {
                    _loginError = "Admin bilan aloqa yo'q — akkaunt bilan kirib bo'lmaydi";
                    PushState();
                    return;
                }
                _loginError = "";
                _admin.Send("auth.login", new { login = r.Login, password = r.Password });
                return;
            }

            case PipeTypes.RequestTime:
                _admin.Send("client.request_time");
                Toast("So'rov yuborildi", "Administrator ekranida xabar chiqdi", "info");
                return;

            case PipeTypes.CallAdmin:
                _admin.Send("client.call_admin");
                Toast("Administrator chaqirildi", "", "info");
                return;

            case PipeTypes.Logout:
                _admin.Send("session.ended", new { reason = "logout" });
                _sessions.Apply(null);
                PushState();
                return;

            case PipeTypes.VerifyPassword:
            {
                var r = msg.As<PasswordRequest>();
                var ok = string.IsNullOrEmpty(_config.ServicePassHash) ||
                         PasswordHash.Verify(_config.ServicePassHash, r?.Password ?? "");
                _pipe.Send(PipeTypes.VerifyPassword, new { ok });
                return;
            }

            case PipeTypes.Unlock:
            {
                var r = msg.As<PasswordRequest>();
                var ok = string.IsNullOrEmpty(_config.ServicePassHash) ||
                         PasswordHash.Verify(_config.ServicePassHash, r?.Password ?? "");
                _pipe.Send(PipeTypes.Unlock, new { ok });
                if (ok) _log.Warn("Qulf xizmat paroli bilan qo'lda ochildi");
                return;
            }

            default:
                return;
        }
    }

    /* --------------------------------- holat --------------------------------- */

    private void Toast(string title, string text, string kind) =>
        _pipe.Send(PipeTypes.Toast, new ToastPayload { Title = title, Text = text, Kind = kind });

    private void PushState()
    {
        var s = _sessions.Current;
        if (s is not null)
        {
            s.RemainingMs = _sessions.RemainingMs;
            s.Due = _pendingDue;
        }

        var paired = !string.IsNullOrEmpty(TokenStore.Load());
        var state = new ShellState
        {
            Screen = ShellScreen.Pick(
                paired,
                connecting: _admin.PendingCode is not null,
                hasSession: s is not null,
                paused: s?.Paused ?? false),
            PcName = _config.PcName,
            Host = _config.Host,
            Port = _config.Port,
            Online = _admin.IsOnline,
            Paired = paired,
            ConnectPhase = _connectPhase,
            ConnectError = _connectError,
            Session = s,
            Lock = _lock,
            Behaviour = _behaviour,
            UnlockCombo = _config.UnlockCombo,
            HasServicePassword = !string.IsNullOrEmpty(_config.ServicePassHash),
            Due = _pendingDue,
            LoginError = _loginError,
            WallpaperPath = _wallpaper.Path,
            ClockDriftMs = _clockDrift,
            Paused = _config.Paused
        };
        _pipe.Send(PipeTypes.State, state);
    }
}
