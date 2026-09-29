using Dust2Agent.Common;
using Xunit;

namespace Dust2Agent.Tests;

/// <summary>
/// Xizmat paroli endi xizmatning o'zida tekshiriladi ([qaror 22]). Ilgari quvurga
/// <c>pause</c> yozgan istalgan dastur qulf ekranini parolsiz o'chira olardi.
/// </summary>
public class ServiceAuthTests
{
    private static readonly DateTime T0 = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    /* ---------------------------- qaysi buyruqlar ---------------------------- */

    [Theory]
    [InlineData(PipeTypes.Pause)]
    [InlineData(PipeTypes.Unpair)]
    [InlineData(PipeTypes.Connect)]
    public void Xavfli_buyruqlar_parol_talab_qiladi(string type)
    {
        Assert.True(ShellCommandPolicy.NeedsPassword(type, hasPassword: true));
    }

    [Theory]
    [InlineData(PipeTypes.Hello)]
    [InlineData(PipeTypes.Login)]
    [InlineData(PipeTypes.Logout)]
    [InlineData(PipeTypes.RequestTime)]
    [InlineData(PipeTypes.CallAdmin)]
    [InlineData(PipeTypes.Resume)]
    [InlineData(PipeTypes.VerifyPassword)]
    [InlineData(PipeTypes.Unlock)]
    [InlineData("noma'lum")]
    [InlineData(null)]
    public void Oddiy_buyruqlar_parolsiz_ishlaydi(string? type)
    {
        Assert.False(ShellCommandPolicy.NeedsPassword(type, hasPassword: true));
    }

    [Theory]
    [InlineData(PipeTypes.Pause)]
    [InlineData(PipeTypes.Unpair)]
    [InlineData(PipeTypes.Connect)]
    public void Parol_ornatilmagan_bolsa_hammasi_ruxsat(string type)
    {
        // Birinchi o'rnatishda admin hali parol yubormagan — ulanish oynasi ishlashi kerak
        Assert.False(ShellCommandPolicy.NeedsPassword(type, hasPassword: false));
    }

    /* -------------------------------- ruxsat -------------------------------- */

    [Fact]
    public void Togri_paroldan_keyin_besh_daqiqa_ruxsat()
    {
        var gate = new ServicePasswordGate();
        Assert.False(gate.IsGranted(T0));

        gate.RecordSuccess(T0);
        Assert.True(gate.IsGranted(T0));
        Assert.True(gate.IsGranted(T0 + ServicePasswordGate.GrantFor - TimeSpan.FromSeconds(1)));
        Assert.False(gate.IsGranted(T0 + ServicePasswordGate.GrantFor));
    }

    [Fact]
    public void Ruxsat_bekor_qilinadi()
    {
        var gate = new ServicePasswordGate();
        gate.RecordSuccess(T0);
        gate.Revoke();
        Assert.False(gate.IsGranted(T0));
    }

    /* -------------------------------- xatolar -------------------------------- */

    [Fact]
    public void Dastlabki_xatolar_kutishsiz()
    {
        var gate = new ServicePasswordGate();
        for (var i = 1; i < ServicePasswordGate.FreeAttempts; i++)
        {
            Assert.False(gate.RecordFailure(T0));
            Assert.False(gate.IsBlocked(T0, out _));
        }
    }

    [Fact]
    public void Beshinchi_xatodan_keyin_bir_daqiqa_blok()
    {
        var gate = new ServicePasswordGate();
        for (var i = 1; i < ServicePasswordGate.FreeAttempts; i++) gate.RecordFailure(T0);

        Assert.True(gate.RecordFailure(T0));
        Assert.True(gate.IsBlocked(T0, out var wait));
        Assert.Equal(TimeSpan.FromMinutes(1), wait);
        Assert.False(gate.IsBlocked(T0.AddMinutes(1), out _));
    }

    [Fact]
    public void Blok_har_safar_ikki_baravar_uzayadi_lekin_15_daqiqadan_oshmaydi()
    {
        var gate = new ServicePasswordGate();
        var now = T0;
        for (var i = 1; i < ServicePasswordGate.FreeAttempts; i++) gate.RecordFailure(now);

        var expected = new[] { 1, 2, 4, 8, 15, 15 };
        foreach (var minutes in expected)
        {
            Assert.True(gate.RecordFailure(now));
            Assert.True(gate.IsBlocked(now, out var wait));
            Assert.Equal(TimeSpan.FromMinutes(minutes), wait);
            now += wait;
        }
    }

    [Fact]
    public void Togri_parol_xatolar_hisobini_nollaydi()
    {
        var gate = new ServicePasswordGate();
        for (var i = 0; i < ServicePasswordGate.FreeAttempts; i++) gate.RecordFailure(T0);
        var after = T0.AddMinutes(1);

        gate.RecordSuccess(after);
        Assert.False(gate.IsBlocked(after, out _));

        // Hisob boshidan boshlanadi: yana to'rtta xato kutishsiz
        for (var i = 1; i < ServicePasswordGate.FreeAttempts; i++) Assert.False(gate.RecordFailure(after));
        // Blok ham yana bir daqiqadan boshlanadi
        Assert.True(gate.RecordFailure(after));
        Assert.True(gate.IsBlocked(after, out var wait));
        Assert.Equal(TimeSpan.FromMinutes(1), wait);
    }

    /* ---------------------------- qobiq fayli ---------------------------- */

    [Fact]
    public void Aynan_qobiq_fayli_ishonchli()
    {
        const string expected = @"C:\Program Files\DUST2 Klient\Dust2Agent.Shell.exe";
        Assert.True(ShellIdentity.IsTrustedShell(expected, expected));
        // Windows yo'llarida katta-kichik harf farqi yo'q
        Assert.True(ShellIdentity.IsTrustedShell(@"c:\program files\dust2 klient\DUST2AGENT.SHELL.EXE", expected));
        // ".." bilan yozilgan yo'l ham shu faylga olib keladi
        Assert.True(ShellIdentity.IsTrustedShell(@"C:\Program Files\DUST2 Klient\logs\..\Dust2Agent.Shell.exe", expected));
    }

    [Theory]
    [InlineData(@"C:\Users\mijoz\Desktop\Dust2Agent.Shell.exe")]
    [InlineData(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe")]
    [InlineData(@"C:\Program Files\DUST2 Klient\Dust2Agent.Service.exe")]
    [InlineData("")]
    [InlineData(null)]
    public void Boshqa_dastur_ishonchli_emas(string? clientPath)
    {
        Assert.False(ShellIdentity.IsTrustedShell(clientPath, @"C:\Program Files\DUST2 Klient\Dust2Agent.Shell.exe"));
    }
}
