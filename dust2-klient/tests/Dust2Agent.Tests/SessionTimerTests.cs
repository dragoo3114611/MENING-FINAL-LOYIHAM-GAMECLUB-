using Dust2Agent.Common;
using Xunit;

namespace Dust2Agent.Tests;

public class SessionTimerTests
{
    /// <summary>Sinov uchun qo'lda boshqariladigan monotonik soat.</summary>
    private sealed class FakeClock
    {
        public long Now { get; private set; } = 1_000_000;
        public void Advance(long ms) => Now += ms;
    }

    private const long Minute = 60_000;

    [Fact]
    public void Prepaid_session_counts_down()
    {
        var clock = new FakeClock();
        var t = new SessionTimer(() => clock.Now);
        t.Start(SessionMode.Prepaid, 60 * Minute);

        clock.Advance(50 * Minute);

        Assert.Equal(50 * Minute, t.ElapsedMs);
        Assert.Equal(10 * Minute, t.RemainingMs);
        Assert.False(t.IsExpired);
    }

    [Fact]
    public void Prepaid_session_expires_when_time_runs_out()
    {
        var clock = new FakeClock();
        var t = new SessionTimer(() => clock.Now);
        t.Start(SessionMode.Prepaid, 30 * Minute);

        clock.Advance(30 * Minute + 1);

        Assert.True(t.IsExpired);
    }

    [Fact]
    public void Added_time_extends_the_session()
    {
        var clock = new FakeClock();
        var t = new SessionTimer(() => clock.Now);
        t.Start(SessionMode.Prepaid, 60 * Minute);
        clock.Advance(55 * Minute);

        t.AddTime(30 * Minute);

        Assert.Equal(35 * Minute, t.RemainingMs);
    }

    [Fact]
    public void Pause_freezes_the_countdown()
    {
        var clock = new FakeClock();
        var t = new SessionTimer(() => clock.Now);
        t.Start(SessionMode.Prepaid, 60 * Minute);

        clock.Advance(10 * Minute);
        t.Pause();
        clock.Advance(25 * Minute);

        Assert.Equal(10 * Minute, t.ElapsedMs);
        Assert.Equal(50 * Minute, t.RemainingMs);

        t.Resume();
        clock.Advance(5 * Minute);

        Assert.Equal(15 * Minute, t.ElapsedMs);
        Assert.Equal(45 * Minute, t.RemainingMs);
    }

    [Fact]
    public void Open_session_has_no_remaining_time()
    {
        var clock = new FakeClock();
        var t = new SessionTimer(() => clock.Now);
        t.Start(SessionMode.Open, 0);

        clock.Advance(95 * Minute);
        t.AddTime(30 * Minute); // ochiq vaqtda taymer paydo bo'lmasligi kerak

        Assert.Equal(95 * Minute, t.ElapsedMs);
        Assert.Null(t.RemainingMs);
        Assert.False(t.IsExpired);
    }

    [Fact]
    public void Sync_from_admin_replaces_remaining_time()
    {
        var clock = new FakeClock();
        var t = new SessionTimer(() => clock.Now);
        t.Start(SessionMode.Prepaid, 60 * Minute);
        clock.Advance(20 * Minute);

        // Kompyuter o'chiq turganda admin vaqtni sanashda davom etgan
        t.SyncRemaining(5 * Minute);

        Assert.Equal(5 * Minute, t.RemainingMs);
        Assert.Equal(20 * Minute, t.ElapsedMs);
    }
}
