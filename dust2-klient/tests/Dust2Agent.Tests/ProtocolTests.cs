using System.Text.Json;
using Dust2Agent.Common;
using Xunit;

namespace Dust2Agent.Tests;

public class ProtocolTests
{
    [Fact]
    public void Envelope_round_trips()
    {
        var msg = Envelope.Create("session.start", new { sessionId = 7, mode = "pre", remainingMs = 5400000 });
        var json = msg.ToJson();

        var back = Envelope.Parse(json);

        Assert.NotNull(back);
        Assert.Equal("session.start", back!.Type);
        Assert.Equal(1, back.V);
        Assert.Equal(7, back.Num("sessionId"));
        Assert.Equal("pre", back.Str("mode"));
    }

    [Fact]
    public void Envelope_rejects_broken_messages()
    {
        Assert.Null(Envelope.Parse("buzuq"));
        Assert.Null(Envelope.Parse("""{"v":2,"type":"hello","id":"a","ts":1}"""));
        Assert.Null(Envelope.Parse("""{"v":1,"id":"a","ts":1}"""));
        Assert.Null(Envelope.Parse("""{"v":1,"type":"hello","ts":1}"""));
    }

    [Fact]
    public void Session_payload_is_read_from_admin_json()
    {
        // Admin yuboradigan haqiqiy shakl (camelCase)
        const string raw = """
        {"v":1,"type":"session.start","id":"x","ts":1700000000000,
         "payload":{"sessionId":12,"mode":"acc","startedAt":1700000000000,"endsAt":1700003600000,
                    "remainingMs":3600000,"rate":10000,"client":"sanjar_07","paused":false,"due":0}}
        """;

        var msg = Envelope.Parse(raw);
        var p = msg!.PayloadAs<SessionPayload>();

        Assert.NotNull(p);
        Assert.Equal(12, p!.SessionId);
        Assert.Equal(SessionMode.Account, p.ToMode());
        Assert.Equal("sanjar_07", p.Client);
        Assert.Equal(3_600_000, p.RemainingMs);
    }

    [Fact]
    public void Hello_ok_carries_lock_behaviour_and_agent_config()
    {
        const string raw = """
        {"v":1,"type":"hello.ok","id":"x","ts":1,
         "payload":{"pcId":3,"name":"PC 03","serverTime":1700000000000,
                    "session":null,
                    "lock":{"theme":"ct","title":"DUST2","clock":true,"login":true,"dim":40},
                    "behaviour":{"onExpire":"delay","offDelay":10,"blockInput":true,"warnMin":3},
                    "agent":{"servicePassHash":"pbkdf2$1$a$b","unlockCombo":"Ctrl+Alt+P"}}}
        """;

        var p = Envelope.Parse(raw)!.PayloadAs<HelloOkPayload>();

        Assert.NotNull(p);
        Assert.Equal("PC 03", p!.Name);
        Assert.Null(p.Session);
        Assert.Equal("ct", p.Lock!.Theme);
        Assert.Equal(40, p.Lock.Dim);
        Assert.Equal("delay", p.Behaviour!.OnExpire);
        Assert.Equal(3, p.Behaviour.WarnMin);
        Assert.Equal("Ctrl+Alt+P", p.Agent!.UnlockCombo);
    }
}

public class PipeContractTests
{
    [Fact]
    public void Pipe_message_round_trips()
    {
        var state = new ShellState { Screen = "locked", PcName = "PC 01", Online = true, Due = 13000 };
        var line = PipeMessage.Create(PipeTypes.State, state).ToLine();

        var back = PipeMessage.Parse(line)!.As<ShellState>();

        Assert.NotNull(back);
        Assert.Equal("locked", back!.Screen);
        Assert.Equal("PC 01", back.PcName);
        Assert.True(back.Online);
        Assert.Equal(13000, back.Due);
    }

    [Fact]
    public void Anonymous_payload_is_read_case_insensitively()
    {
        // Xizmat anonim obyekt yuboradi: new { ok = true }
        var line = PipeMessage.Create(PipeTypes.Unlock, new { ok = true }).ToLine();
        var json = PipeMessage.Parse(line)!.Json;

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());

        var connect = PipeMessage.Create(PipeTypes.Connect,
            new ConnectRequest { Host = "192.168.1.10", Port = 7777, Code = "482913", Name = "PC 01" });
        var r = PipeMessage.Parse(connect.ToLine())!.As<ConnectRequest>();
        Assert.Equal("192.168.1.10", r!.Host);
        Assert.Equal("482913", r.Code);
    }

    [Fact]
    public void Broken_line_is_ignored()
    {
        Assert.Null(PipeMessage.Parse("{"));
        Assert.Null(PipeMessage.Parse("""{"json":"{}"}"""));
    }
}

public class PasswordHashTests
{
    /// <summary>Admin (Node) yaratgan hash — agent uni oflayn tekshira olishi kerak.</summary>
    private const string FromAdmin =
        "pbkdf2$120000$RFVTVDItdGVzdC1zYWx0IQ==$sNMnWhom+8VfoysoGMiM9Syi0mgnFHjV2u8v27XN+XM=";

    [Fact]
    public void Verifies_hash_created_by_admin()
    {
        Assert.True(PasswordHash.Verify(FromAdmin, "maxfiy123"));
        Assert.False(PasswordHash.Verify(FromAdmin, "boshqa"));
        Assert.False(PasswordHash.Verify(FromAdmin, ""));
    }

    [Fact]
    public void Rejects_broken_hashes()
    {
        Assert.False(PasswordHash.Verify("", "x"));
        Assert.False(PasswordHash.Verify("nimadir", "x"));
        Assert.False(PasswordHash.Verify("pbkdf2$abc$a$b", "x"));
        Assert.False(PasswordHash.Verify("pbkdf2$1000$!!!$!!!", "x"));
    }
}

public class SessionStoreTests
{
    [Fact]
    public void Stored_session_round_trips_through_json()
    {
        var s = new StoredSession
        {
            SessionId = 5,
            Mode = "pre",
            Rate = 10000,
            EndsAtUtc = 1_700_003_600_000,
            StartedAtUtc = 1_700_000_000_000,
            RemainingMs = 3_600_000,
            Due = 13000
        };

        var json = JsonSerializer.Serialize(s);
        var back = JsonSerializer.Deserialize<StoredSession>(json);

        Assert.NotNull(back);
        Assert.Equal(5, back!.SessionId);
        Assert.Equal("pre", back.Mode);
        Assert.Equal(1_700_003_600_000, back.EndsAtUtc);
        Assert.Equal(13000, back.Due);
    }
}
