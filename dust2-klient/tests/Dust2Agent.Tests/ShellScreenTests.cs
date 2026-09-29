using Dust2Agent.Common;
using Xunit;

namespace Dust2Agent.Tests;

/// <summary>
/// Qaysi ekran ko'rsatiladi. Asosiysi: pauzada kompyuter qulflanadi — ilgari
/// taymer to'xtardi-yu, mijoz o'ynayverardi.
/// </summary>
public class ShellScreenTests
{
    [Fact]
    public void Juftlanmagan_kompyuterda_ulanish_oynasi()
    {
        Assert.Equal(ShellScreen.Setup,
            ShellScreen.Pick(paired: false, connecting: false, hasSession: false, paused: false));
    }

    [Fact]
    public void Ulanish_kodi_kiritilayotganda_setup_emas()
    {
        Assert.Equal(ShellScreen.Locked,
            ShellScreen.Pick(paired: false, connecting: true, hasSession: false, paused: false));
    }

    [Fact]
    public void Ochiq_seansda_qulf_yopiladi()
    {
        Assert.Equal(ShellScreen.Session,
            ShellScreen.Pick(paired: true, connecting: false, hasSession: true, paused: false));
    }

    [Fact]
    public void Pauzada_ekran_qulflanadi()
    {
        Assert.Equal(ShellScreen.Locked,
            ShellScreen.Pick(paired: true, connecting: false, hasSession: true, paused: true));
    }

    [Fact]
    public void Seanssiz_kompyuter_qulflangan()
    {
        Assert.Equal(ShellScreen.Locked,
            ShellScreen.Pick(paired: true, connecting: false, hasSession: false, paused: false));
    }

    [Fact]
    public void Juftlangan_kompyuterda_ulanish_sozlamalari_tugmasi_yashirinadi()
    {
        // Mijoz qulf ekranida turib admin manzilini ko'ra olmasin —
        // sozlamalarga faqat maxfiy kombinatsiya orqali kiriladi
        Assert.False(ShellScreen.ShowSetupButton(paired: true));
    }

    [Fact]
    public void Juftlanmagan_kompyuterda_tugma_kerak()
    {
        // Ulanish kodi kiritilayotganda ekran "locked" bo'ladi —
        // orqaga qaytish yo'li shu tugma
        Assert.True(ShellScreen.ShowSetupButton(paired: false));
    }

    [Fact]
    public void Onlayn_bolsa_token_saqlanmasa_ham_tugma_yashirinadi()
    {
        // Admin bilan ulangan bo'lsa (token DPAPI/%ProgramData% sababli saqlanmagan
        // bo'lsa ham) sozlamalar tugmasi ko'rsatilmaydi — xavfsizlik teshigi yopiladi.
        Assert.False(ShellScreen.ShowSetupButton(paired: false, online: true));
        // Oflayn va juftlanmagan bo'lsa — tugma kerak (kod kiritish uchun)
        Assert.True(ShellScreen.ShowSetupButton(paired: false, online: false));
    }

    [Fact]
    public void Seans_ochiq_bolsa_xabar_alohida_oynada()
    {
        // Qulf oynasi yashirin — unga yozilgan xabarni mijoz ko'rmaydi
        Assert.True(ShellScreen.NoticeInOwnWindow(ShellScreen.Session));
    }

    [Fact]
    public void Qulflangan_va_sozlash_ekranida_qulf_oynasi_ishlatiladi()
    {
        Assert.False(ShellScreen.NoticeInOwnWindow(ShellScreen.Locked));
        Assert.False(ShellScreen.NoticeInOwnWindow(ShellScreen.Setup));
        Assert.False(ShellScreen.NoticeInOwnWindow(null));
    }
}
