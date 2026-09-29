using System.Net;
using System.Net.Sockets;
using Dust2Agent.Common;
using Dust2Agent.Service;
using Xunit;

namespace Dust2Agent.Tests;

/// <summary>
/// Ulanish tsikli hech qachon jimgina to'xtab qolmasligi kerak.
///
/// Haqiqiy nosozlik: admin dasturi yopilgan paytda ulanish taymeri (10 soniya)
/// ishga tushdi. U ham, xizmatning to'xtashi ham OperationCanceledException
/// tashlaydi — ular ajratilmagani uchun tsikl butunlay chiqib ketdi. Natijada
/// kompyuter admin qayta ochilganda ham ulanmadi va jurnalda birorta ham xabar
/// qolmadi (oxirgi qator "Ulanmoqda: ws://…").
/// </summary>
public class AdminReconnectTests
{
    /// <summary>TCP ulanishni qabul qiladi, lekin hech qanday javob bermaydi.</summary>
    private static TcpListener StartBlackHole(out int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var held = new List<TcpClient>();
        _ = Task.Run(async () =>
        {
            try
            {
                while (true) held.Add(await listener.AcceptTcpClientAsync().ConfigureAwait(false));
            }
            catch (Exception)
            {
                foreach (var c in held) c.Dispose();
            }
        });
        return listener;
    }

    [Fact]
    public async Task Ulanish_taymeri_ishlaganda_tsikl_toxtamaydi()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dust2-test-" + Guid.NewGuid().ToString("N"));
        var log = new AgentLog("service", dir);
        var listener = StartBlackHole(out var port);

        var admin = new AdminConnection(log)
        {
            Config = new AgentConfig { Host = "127.0.0.1", Port = port, PcName = "PC 01" },
            PendingCode = "123456",
            ConnectTimeout = TimeSpan.FromMilliseconds(300)
        };

        using var cts = new CancellationTokenSource();
        var run = admin.RunAsync(cts.Token);
        await Task.Delay(2500);
        cts.Cancel();
        try { await run; } catch (OperationCanceledException) { }
        listener.Stop();

        var file = Directory.GetFiles(dir, "service-*.log").Single();
        var text = await File.ReadAllTextAsync(file);
        var attempts = text.Split("Ulanmoqda:").Length - 1;

        // Asosiysi: tsikl tirik qoldi va qayta urindi
        Assert.True(attempts >= 2, $"tsikl to'xtab qoldi — faqat {attempts} ta urinish:\n{text}");
        Assert.Contains("qayta urinish", text);

        try { Directory.Delete(dir, recursive: true); } catch (IOException) { }
    }
}
