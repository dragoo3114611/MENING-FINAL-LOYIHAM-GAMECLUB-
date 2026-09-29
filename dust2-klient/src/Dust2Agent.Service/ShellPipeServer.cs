using System.IO.Pipes;
using System.Threading.Channels;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Dust2Agent.Common;
using Microsoft.Win32.SafeHandles;

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

    /// <summary>Begona dastur haqidagi oxirgi yozuv — jurnal to'lib ketmasin.</summary>
    private DateTime _lastRejectLog = DateTime.MinValue;

    public ShellPipeServer(AgentLog log) => _log = log;

    /// <summary>
    /// Quvurga faqat haqiqiy qobiq (<see cref="ShellLauncher.ExePath"/>) ulana oladi.
    /// Windows xizmati sifatida ishlaganda yoqiladi; konsoldan (sinov uchun) ishga
    /// tushirilganda o'chiq — u yerda qobiq boshqa papkada turishi mumkin.
    /// </summary>
    public bool VerifyClient { get; init; }

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

        // Quvur hamma foydalanuvchilarga ochiq (qobiq oddiy huquq bilan ishlaydi), shuning
        // uchun ulangan dasturni tekshiramiz: aks holda istalgan skript qobiq nomidan
        // buyruq yubora olardi ([qaror 22](../../../docs/QARORLAR.md))
        if (VerifyClient && !IsTrustedClient(server))
        {
            try
            {
                server.Disconnect();
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            {
                // ulanish allaqachon uzilgan
            }
            await Task.Delay(250, ct).ConfigureAwait(false);
            return;
        }

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

    /// <summary>Ulangan dastur xizmat yonidagi qobiq faylimi.</summary>
    private bool IsTrustedClient(NamedPipeServerStream server)
    {
        var path = ClientImagePath(server, out var pid);
        if (ShellIdentity.IsTrustedShell(path, ShellLauncher.ExePath)) return true;

        var now = DateTime.UtcNow;
        if (now - _lastRejectLog > TimeSpan.FromSeconds(30))
        {
            _lastRejectLog = now;
            _log.Warn($"Quvurga begona dastur ulandi va rad etildi (pid {pid}, {path ?? "yo'li aniqlanmadi"})");
        }
        return false;
    }

    /// <summary>Quvurning narigi tomonidagi jarayonning to'liq yo'li (aniqlanmasa null).</summary>
    private static string? ClientImagePath(NamedPipeServerStream server, out uint pid)
    {
        if (!GetNamedPipeClientProcessId(server.SafePipeHandle, out pid)) return null;

        var process = OpenProcess(ProcessQueryLimitedInformation, false, pid);
        if (process == IntPtr.Zero) return null;
        try
        {
            var size = 1024u;
            var name = new StringBuilder((int)size);
            return QueryFullProcessImageName(process, 0, name, ref size) ? name.ToString() : null;
        }
        finally
        {
            CloseHandle(process);
        }
    }

    /* ------------------------------- P/Invoke ------------------------------- */

    private const uint ProcessQueryLimitedInformation = 0x1000;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint clientProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode,
        EntryPoint = "QueryFullProcessImageNameW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(IntPtr process, uint flags,
        StringBuilder exeName, ref uint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

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
