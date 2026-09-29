using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Channels;
using System.Windows;
using Dust2Agent.Common;

namespace Dust2Agent.Shell;

/// <summary>
/// Xizmat bilan named pipe orqali aloqa. Xizmat o'chib qolsa qayta ulanadi.
/// Barcha hodisalar UI oqimida chaqiriladi.
/// </summary>
public sealed class AgentClient
{
    private readonly object _lock = new();
    private Channel<string>? _outbox;
    private CancellationTokenSource? _cts;

    public event Action<ShellState>? StateChanged;
    public event Action<ToastPayload>? Toast;
    public event Action<string>? AdminMessage;
    /// <summary>Xizmat paroli tekshiruvi natijasi: to'g'rimi va (bo'lsa) xizmat xabari.</summary>
    public event Action<bool, string?>? UnlockResult;
    public event Action<bool, string?>? PasswordResult;
    public event Action<bool>? ConnectionChanged;

    public bool IsConnected
    {
        get { lock (_lock) return _outbox is not null; }
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => LoopAsync(_cts.Token));
    }

    public void Stop() => _cts?.Cancel();

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Yozish alohida vazifada — Send() interfeys oqimini hech qachon kutdirmaydi
            // (aks holda xizmat javob bermasa oyna muzlab qolardi).
            var outbox = Channel.CreateBounded<string>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true
            });
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Task pump = Task.CompletedTask;

            try
            {
                using var pipe = new NamedPipeClientStream(
                    ".", AgentPaths.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                await pipe.ConnectAsync(5000, ct).ConfigureAwait(false);

                var reader = new StreamReader(pipe, Encoding.UTF8);
                var writer = new StreamWriter(pipe, new UTF8Encoding(false)) { AutoFlush = true };

                lock (_lock) _outbox = outbox;
                pump = PumpAsync(writer, outbox, linked.Token);

                Post(() => ConnectionChanged?.Invoke(true));

                Send(PipeTypes.Hello);

                while (!ct.IsCancellationRequested && pipe.IsConnected)
                {
                    var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                    if (line is null) break;
                    var msg = PipeMessage.Parse(line);
                    if (msg is not null) Dispatch(msg);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception)
            {
                // Xizmat hali ishga tushmagan bo'lishi mumkin — qayta uriniladi
            }
            finally
            {
                lock (_lock) _outbox = null;
                outbox.Writer.TryComplete();
                linked.Cancel();
                try
                {
                    await pump.ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // yozuv vazifasi uzilgan aloqada tugaydi
                }
                Post(() => ConnectionChanged?.Invoke(false));
            }

            try
            {
                await Task.Delay(1500, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private void Dispatch(PipeMessage msg)
    {
        switch (msg.Type)
        {
            case PipeTypes.State:
            {
                var s = msg.As<ShellState>();
                if (s is not null) Post(() => StateChanged?.Invoke(s));
                return;
            }
            case PipeTypes.Toast:
            {
                var t = msg.As<ToastPayload>();
                if (t is not null) Post(() => Toast?.Invoke(t));
                return;
            }
            case PipeTypes.AdminMessage:
            {
                var text = msg.As<MessagePayload>()?.Text ?? "";
                if (!string.IsNullOrEmpty(text)) Post(() => AdminMessage?.Invoke(text));
                return;
            }
            case PipeTypes.Unlock:
            {
                var r = msg.As<OkPayload>();
                Post(() => UnlockResult?.Invoke(r?.Ok ?? false, r?.Message));
                return;
            }
            case PipeTypes.VerifyPassword:
            {
                var r = msg.As<OkPayload>();
                Post(() => PasswordResult?.Invoke(r?.Ok ?? false, r?.Message));
                return;
            }
        }
    }

    private static void Post(Action a)
    {
        var app = Application.Current;
        if (app is null) return;
        app.Dispatcher.BeginInvoke(a);
    }

    public void Send(string type, object? payload = null)
    {
        Channel<string>? ch;
        lock (_lock) ch = _outbox;
        if (ch is null) return;
        ch.Writer.TryWrite(PipeMessage.Create(type, payload).ToLine());
    }

    private static async Task PumpAsync(StreamWriter writer, Channel<string> outbox, CancellationToken ct)
    {
        try
        {
            await foreach (var line in outbox.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                await writer.WriteLineAsync(line.AsMemory(), ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // aloqa yopildi
        }
        catch (IOException)
        {
            // xizmat uzildi — o'qish tsikli ham tugaydi
        }
    }

    private sealed class MessagePayload
    {
        public string Text { get; set; } = "";
    }

    private sealed class OkPayload
    {
        public bool Ok { get; set; }

        /// <summary>Masalan: "Juda ko'p noto'g'ri urinish — 60 soniyadan keyin…"</summary>
        public string? Message { get; set; }
    }
}
