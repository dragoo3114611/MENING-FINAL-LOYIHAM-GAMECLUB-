using System.Text.Json;
using Dust2Agent.Common;
using Xunit;

namespace Dust2Agent.Tests;

/// <summary>
/// Klub Pult admin Ctrl+Alt+K ni ko'rsatadi — agent ham xuddi shuni kutishi kerak.
/// Aloqa yo'q paytdagi xavfli holatlar esa yo'qolmasligi kerak.
/// </summary>
public class ClientAlertTests
{
    [Fact]
    public void Default_unlock_combo_matches_admin()
    {
        Assert.Equal("Ctrl+Alt+K", AgentConfig.DefaultUnlockCombo);
        Assert.Equal("Ctrl+Alt+K", new AgentConfig().UnlockCombo);
        Assert.Equal("Ctrl+Alt+K", new AgentConfigPayload().UnlockCombo);
        Assert.Equal((0x4B, true, true, false), InputPolicy.Parse(AgentConfig.DefaultUnlockCombo));
    }

    [Fact]
    public void Pending_alerts_keep_newest_only()
    {
        var list = new List<PendingAlert>();
        for (var i = 0; i < PendingAlert.Max + 5; i++) PendingAlert.Add(list, AlertKinds.ManualUnlock, i);

        Assert.Equal(PendingAlert.Max, list.Count);
        Assert.Equal(5, list[0].At);
        Assert.Equal(PendingAlert.Max + 4, list[^1].At);
    }

    [Fact]
    public void Pending_alerts_survive_config_round_trip()
    {
        var c = new AgentConfig();
        PendingAlert.Add(c.PendingAlerts, AlertKinds.Paused, 1_700_000_000_000);

        var back = JsonSerializer.Deserialize<AgentConfig>(JsonSerializer.Serialize(c))!;

        var a = Assert.Single(back.PendingAlerts);
        Assert.Equal("paused", a.Kind);
        Assert.Equal(1_700_000_000_000, a.At);
    }

    [Fact]
    public void Old_config_without_alerts_loads()
    {
        var c = JsonSerializer.Deserialize<AgentConfig>("""{"host":"10.0.0.1","unlockCombo":"Ctrl+Alt+P"}""")!;
        Assert.Empty(c.PendingAlerts);
        Assert.Equal("Ctrl+Alt+P", c.UnlockCombo);
    }
}
