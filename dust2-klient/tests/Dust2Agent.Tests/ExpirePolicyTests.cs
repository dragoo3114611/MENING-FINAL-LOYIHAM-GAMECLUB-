using Dust2Agent.Common;
using Xunit;

namespace Dust2Agent.Tests;

/// <summary>
/// "Vaqt tugaganda" sozlamasining klient tomoni. Muhimi: "Qulflash, keyin
/// o'chirish" tanlanganda kompyuter haqiqatan o'chadi — ilgari agentda
/// <c>offDelay</c> umuman ishlatilmasdi va admin yopiq bo'lsa kompyuter
/// cheksiz qulflangan holda yonib turardi.
/// </summary>
public class ExpirePolicyTests
{
    private static readonly DateTime T0 = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Ochirish_tanlansa_darhol_ochadi()
    {
        Assert.True(ExpirePolicy.OffNow("off"));
        Assert.False(ExpirePolicy.OffNow("lock"));
        Assert.False(ExpirePolicy.OffNow("delay"));
        Assert.False(ExpirePolicy.OffNow(null));
    }

    [Fact]
    public void Faqat_qulflash_tanlansa_ochirish_rejalashtirilmaydi()
    {
        Assert.Null(ExpirePolicy.OffAt("lock", 5, T0));
        Assert.Null(ExpirePolicy.OffAt("off", 5, T0));
        Assert.Null(ExpirePolicy.OffAt(null, 5, T0));
    }

    [Fact]
    public void Kechiktirilgan_ochirish_vaqti_hisoblanadi()
    {
        Assert.Equal(T0.AddMinutes(1), ExpirePolicy.OffAt("delay", 1, T0));
        Assert.Equal(T0.AddMinutes(30), ExpirePolicy.OffAt("delay", 30, T0));
    }

    [Theory]
    [InlineData(0, ExpirePolicy.MinDelayMinutes)]
    [InlineData(-5, ExpirePolicy.MinDelayMinutes)]
    [InlineData(9999, ExpirePolicy.MaxDelayMinutes)]
    public void Buzuq_qiymat_chegaraga_tushadi(int given, int expected)
    {
        Assert.Equal(T0.AddMinutes(expected), ExpirePolicy.OffAt("delay", given, T0));
    }

    [Fact]
    public void Vaqti_kelmaguncha_ochirilmaydi()
    {
        var offAt = ExpirePolicy.OffAt("delay", 5, T0);
        Assert.False(ExpirePolicy.ShouldPowerOff(offAt, hasSession: false, clientPaused: false, T0.AddMinutes(4)));
        Assert.True(ExpirePolicy.ShouldPowerOff(offAt, hasSession: false, clientPaused: false, T0.AddMinutes(5)));
        Assert.True(ExpirePolicy.ShouldPowerOff(offAt, hasSession: false, clientPaused: false, T0.AddMinutes(9)));
    }

    [Fact]
    public void Yangi_vaqt_ochilsa_ochirilmaydi()
    {
        var offAt = ExpirePolicy.OffAt("delay", 1, T0);
        Assert.False(ExpirePolicy.ShouldPowerOff(offAt, hasSession: true, clientPaused: false, T0.AddMinutes(5)));
    }

    [Fact]
    public void Klient_toxtatilgan_bolsa_aralashmaymiz()
    {
        var offAt = ExpirePolicy.OffAt("delay", 1, T0);
        Assert.False(ExpirePolicy.ShouldPowerOff(offAt, hasSession: false, clientPaused: true, T0.AddMinutes(5)));
    }

    [Fact]
    public void Rejalashtirilmagan_bolsa_hech_qachon_ochmaydi()
    {
        Assert.False(ExpirePolicy.ShouldPowerOff(null, hasSession: false, clientPaused: false, T0.AddDays(1)));
    }
}
