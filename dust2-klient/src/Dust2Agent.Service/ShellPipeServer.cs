using System.IO.Pipes;
using System.Threading.Channels;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Dust2Agent.Common;

namespace Dust2Agent.Service;

/// <summary>
/// Xizmat va qobiq o'rtasidagi named pipe serveri (\\.\pipe\dust2agent).
/// Faqat lokal: tarmoqqa chiqmaydi.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ShellPipeServer : IAsyncDisposable
{
    private readonly AgentLog _log;
    private readonly object _lock = new();
    private Channel<string>? _outbox;

    public ShellPipeServer(AgentLog log) => _log = log;

    /// <summary>Qobiqdan kelgan xabar.</summary>
    public event Action<PipeMessage>? Received;

    /// <summary>Qobiq ulandi yoki uzildi.</summary>
    public event Action<bool>? ShellConnected;

    public bool IsConnected
    {
        get { lock (_lock) return _outbox is not null; }
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ServeOneAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _log.Warn($"Qobiq bilan aloqa xatosi: {ex.Message}");
                await Task.Delay(1000, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task ServeOneAsync(CancellationToken ct)
    {
        using var server = CreatePipe();
        await server.WaitForConnectionAsync(ct).ConfigureAwait(false);

        var reader = new StreamReader(server, Encoding.UTF8);
        var writer = new StreamWriter(server, new UTF8Encoding(false)) { AutoFlush = true };

        // Yozish alohida vazifada: Send() hech qachon chaqiruvchini kutdirmaydi.
        // Ilgari xabarlar to'g'ridan-to'g'ri, bir nechta oqimdan yozilardi — qobiq
        // o'qishni to'xtatsa, yozuv qotib qolar va admin bilan ulanish tsikli ham
        // muzlab qolardi (shu sabab uzilgandan keyin qayta ulanib bo'lmasdi).
        var outbox = Channel.CreateBounded<string>(new BoundedChannelOptions(200)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
        lock (_lock) _outbox = outbox;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var pump = PumpAsync(writer, outbox, linked.Token);

        _log.Info("Qobiq ulandi");
        ShellConnected?.Invoke(true);

        try
        {
            while (!ct.IsCancellationRequested && server.IsConnected)
            {
                var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is null) break;

                var msg = PipeMessage.Parse(line);
                if (msg is null) continue;

                try
                {
                    Received?.Invoke(msg);
                }
                catch (Exception ex)
                {
                    _log.Error($"Qobiq xabarida xato ({msg.Type})", ex);
                }
            }
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
                // yozuv vazifasi uzilgan aloqada tugaydi — bu normal
            }
            _log.Info("Qobiq uzildi");
            ShellConnected?.Invoke(false);
        }
    }

    /// <summary>Navbatdagi xabarlarni qobiqqa yozadi.</summary>
    private async Task PumpAsync(StreamWriter writer, Channel<string> outbox, CancellationToken ct)
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
            // qobiq uzildi — o'qish tsikli ham tugaydi
        }
    }

    private NamedPipeServerStream CreatePipe()
    {
        // Qobiq oddiy foydalanuvchi huquqi bilan ishlaydi, shuning uchun unga ruxsat beramiz
        var security = new PipeSecurity();
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
            PipeAccessRights.ReadWrite, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            PipeAccessRights.FullControl, AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            AgentPaths.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous, 64 * 1024, 64 * 1024, security);
    }

    /// <summary>
    /// Qobiqqa xabar qo'yadi. Hech qachon bloklanmaydi: navbat to'lsa eng eski xabar
    /// tashlanadi (holat xabarlari baribir to'liq yangilanadi).
    /// </summary>
    public void Send(string type, object? payload = null)
    {
        Channel<string>? ch;
        lock (_lock) ch = _outbox;
        if (ch is null) return;
        ch.Writer.TryWrite(PipeMessage.Create(type, payload).ToLine());
    }

    public ValueTask DisposeAsync()
    {
        lock (_lock) _outbox = null;
        return ValueTask.CompletedTask;
    }
}
