using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Dust2Agent.Common;

namespace Dust2Agent.Service;

/// <summary>
/// Admin dasturi bilan WebSocket aloqasi: juftlash, qayta ulanish (eksponensial
/// kechikish), heartbeat va xabarlarni uzatish.
/// </summary>
public sealed class AdminConnection : IAsyncDisposable
{
    private static readonly TimeSpan Heartbeat = TimeSpan.FromSeconds(20);
    private static readonly int[] BackoffSeconds = { 1, 2, 4, 8, 16, 30 };

    private readonly AgentLog _log;
    /// <summary>Oxirgi marta jurnalga yozilgan MAC — har ulanishda takrorlanmasin.</summary>
    private string _lastNic = "";
    private readonly ConcurrentQueue<Envelope> _outbox = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private ClientWebSocket? _ws;
    private int _backoffIndex;

    public AdminConnection(AgentLog log) => _log = log;

    /// <summary>Admindan kelgan xabar.</summary>
    public event Action<Envelope>? Received;

    /// <summary>Aloqa holati o'zgardi.</summary>
    public event Action<bool>? OnlineChanged;

    public bool IsOnline { get; private set; }

    public AgentConfig Config { get; set; } = new();

    /// <summary>Klient dasturi vaqtincha to'xtatilgan (Ctrl+Alt+K → to'xtatish).</summary>
    public bool Paused { get; set; }

    /// <summary>Juftlash uchun kiritilgan kod (bir martalik).</summary>
    public string? PendingCode { get; set; }

    /// <summary>Kutishni bo'lish signali — sozlama o'zgarganda darhol qayta ulanamiz.</summary>
    private readonly SemaphoreSlim _wake = new(0, 1);

    /// <summary>Kutib turgan tsiklni uyg'otadi.</summary>
    public void Wake()
    {
        try
        {
            if (_wake.CurrentCount == 0) _wake.Release();
        }
        catch (SemaphoreFullException)
        {
            // allaqachon uyg'otilgan
        }
    }

    /// <summary>Belgilangan vaqt kutadi, lekin Wake() chaqirilsa darhol qaytadi.</summary>
    private async Task<bool> WaitOrWakeAsync(TimeSpan time, CancellationToken ct)
    {
        try
        {
            return await _wake.WaitAsync(time, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    public string Version { get; init; } = "0.1.0";

    /// <summary>Ulanishni shuncha kutamiz. Sinovda qisqartiriladi.</summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Asosiy tsikl: ulanadi, xabarlarni o'qiydi, uzilsa qayta uriniladi.</summary>
    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (Paused || !Config.IsConfigured
                || (string.IsNullOrEmpty(TokenStore.Load()) && PendingCode is null))
            {
                // Hali sozlanmagan yoki to'xtatilgan — qobiq ulanish oynasini ko'rsatadi
                await WaitOrWakeAsync(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);
                continue;
            }

            try
            {
                await ConnectOnceAsync(ct).ConfigureAwait(false);
                _backoffIndex = 0;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (OperationCanceledException)
            {
                // Bu bizning ulanish taymerimiz, xizmat to'xtayotgani emas.
                // Ilgari bu ham "to'xtash" deb qabul qilinardi va tsikl butunlay
                // chiqib ketardi: agent boshqa hech qachon ulanmasdi, jurnalda esa
                // birorta ham xabar qolmasdi ([qaror 16](../../../docs/QARORLAR.md)).
                _log.Warn($"Admin javob bermadi ({Config.Host}:{Config.Port}) — qayta urinamiz");
            }
            catch (Exception ex)
            {
                _log.Warn($"Admin bilan aloqa uzildi: {ex.Message}");
            }

            SetOnline(false);
            var wait = BackoffSeconds[Math.Min(_backoffIndex, BackoffSeconds.Length - 1)];
            _backoffIndex++;
            _log.Info($"{wait} soniyadan keyin qayta urinish…");
            try
            {
                // Kutish paytida yangi ulanish so'rovi kelsa, darhol uriniladi
                if (await WaitOrWakeAsync(TimeSpan.FromSeconds(wait), ct).ConfigureAwait(false))
                    _backoffIndex = 0;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
        }
    }

    /// <summary>Tez ishga tushirish yoqilgan bo'lsa Wake-on-LAN ishlamasligi mumkin.</summary>
    private static bool FastStartup() =>
        OperatingSystem.IsWindows() && PowerInfo.FastStartupEnabled();

    private async Task ConnectOnceAsync(CancellationToken ct)
    {
        var url = $"ws://{Config.Host}:{Config.Port}/agent";
        // IP/MAC aynan adminga boradigan kartadan olinsin (Wake-on-LAN shunga bog'liq)
        NetworkInfo.AdminHost = Config.Host;
        _log.Info($"Ulanmoqda: {url}");

        using var ws = new ClientWebSocket();
        ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connectCts.CancelAfter(ConnectTimeout);
        await ws.ConnectAsync(new Uri(url), connectCts.Token).ConfigureAwait(false);
        _ws = ws;

        var nic = NetworkInfo.Primary();
        if (nic.Mac != _lastNic)
        {
            _lastNic = nic.Mac;
            _log.Info(nic.Mac.Length > 0
                ? $"Tarmoq kartasi: {nic.Name} — {nic.Ip} / {nic.Mac}"
                : "Tarmoq kartasi aniqlanmadi — Wake-on-LAN uchun MAC adminda qo'lda kiritiladi");
        }

        var token = TokenStore.Load();
        if (!string.IsNullOrEmpty(token))
        {
            await SendNowAsync(Envelope.Create("hello", new
            {
                token,
                name = Config.PcName,
                ip = nic.Ip,
                mac = nic.Mac,
                version = Version,
                fastStartup = FastStartup()
            }), ct).ConfigureAwait(false);
        }
        else
        {
            await SendNowAsync(Envelope.Create("pair.request", new
            {
                name = Config.PcName,
                code = PendingCode ?? "",
                ip = nic.Ip,
                mac = nic.Mac,
                version = Version,
                fastStartup = FastStartup()
            }), ct).ConfigureAwait(false);
        }

        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var heartbeatTask = HeartbeatLoopAsync(heartbeatCts.Token);
        try
        {
            await ReceiveLoopAsync(ws, ct).ConfigureAwait(false);
        }
        finally
        {
            heartbeatCts.Cancel();
            try { await heartbeatTask.ConfigureAwait(false); } catch (OperationCanceledException) { }
            _ws = null;
            SetOnline(false);
        }
    }

    private async Task ReceiveLoopAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        var sb = new StringBuilder();

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                _log.Info("Admin ulanishni yopdi");
                return;
            }

            sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            if (!result.EndOfMessage) continue;

            var raw = sb.ToString();
            sb.Clear();

            var msg = Envelope.Parse(raw);
            if (msg is null)
            {
                _log.Warn("Tushunarsiz xabar keldi");
                continue;
            }

            if (msg.Type is "hello.ok" or "pair.ok")
            {
                SetOnline(true);
                await FlushOutboxAsync(ct).ConfigureAwait(false);
            }

            try
            {
                Received?.Invoke(msg);
            }
            catch (Exception ex)
            {
                _log.Error($"Xabarni qayta ishlashda xato ({msg.Type})", ex);
            }
        }
    }

    private async Task HeartbeatLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(Heartbeat, ct).ConfigureAwait(false);
            if (IsOnline) Send("heartbeat");
        }
    }

    /// <summary>Xabar yuboradi; aloqa yo'q bo'lsa navbatga qo'yadi.</summary>
    public void Send(string type, object? payload = null)
    {
        var msg = Envelope.Create(type, payload);
        if (!IsOnline || _ws is null || _ws.State != WebSocketState.Open)
        {
            _outbox.Enqueue(msg);
            while (_outbox.Count > 100) _outbox.TryDequeue(out _);
            return;
        }
        _ = SendNowAsync(msg, CancellationToken.None);
    }

    /// <summary>
    /// Darhol yuboradi va yozilishini kutadi (keyin ulanish uziladigan hollar uchun).
    /// Aloqa yo'q bo'lsa hech narsa qilmaydi va false qaytaradi.
    /// </summary>
    public async Task<bool> SendNowOrFailAsync(string type, object? payload, TimeSpan timeout)
    {
        if (!IsOnline || _ws is null || _ws.State != WebSocketState.Open) return false;
        var send = SendNowAsync(Envelope.Create(type, payload), CancellationToken.None);
        return await Task.WhenAny(send, Task.Delay(timeout)).ConfigureAwait(false) == send;
    }

    private async Task FlushOutboxAsync(CancellationToken ct)
    {
        while (_outbox.TryDequeue(out var msg))
        {
            await SendNowAsync(msg, ct).ConfigureAwait(false);
        }
    }

    private async Task SendNowAsync(Envelope msg, CancellationToken ct)
    {
        var ws = _ws;
        if (ws is null || ws.State != WebSocketState.Open) return;

        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var bytes = Encoding.UTF8.GetBytes(msg.ToJson());
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log.Warn($"Xabar yuborilmadi ({msg.Type}): {ex.Message}");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private void SetOnline(bool value)
    {
        if (IsOnline == value) return;
        IsOnline = value;
        _log.Info(value ? "Admin bilan aloqa o'rnatildi" : "Admin bilan aloqa yo'q");
        OnlineChanged?.Invoke(value);
    }

    /// <summary>Ulanishni majburan uzadi (sozlama o'zgarganda).</summary>
    public void Drop()
    {
        _backoffIndex = 0;
        Wake();
        try
        {
            _ws?.Abort();
        }
        catch (Exception)
        {
            // ulanish allaqachon yopilgan
        }
    }

    public ValueTask DisposeAsync()
    {
        Drop();
        _sendLock.Dispose();
        _wake.Dispose();
        return ValueTask.CompletedTask;
    }
}
