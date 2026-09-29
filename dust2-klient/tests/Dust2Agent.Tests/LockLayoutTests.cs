using Dust2Agent.Common;
using Xunit;

namespace Dust2Agent.Tests;

/// <summary>
/// Qulf ekranidagi joylashuv. Ilgari "Markazda" va "Hammasi chapda" tanlanganda ham
/// akkaunt bilan kirish kartasi o'ng tomonda qolib ketardi — faqat yozuvlar ko'chardi.
/// </summary>
public class LockLayoutTests
{
    [Fact]
    public void Split_da_kirish_kartasi_yozuvlar_yonida()
    {
        var texts = LockLayout.Texts("split");
        var login = LockLayout.Login("split");

        Assert.Equal(0, texts.Row);
        Assert.Equal(0, texts.Column);
        Assert.Equal("left", texts.Align);

        // bir qatorda, o'ng ustunda
        Assert.Equal(0, login.Row);
        Assert.Equal(1, login.Column);
        Assert.Equal("right", login.Align);
    }

    [Theory]
    [InlineData("center", "center")]
    [InlineData("left", "left")]
    public void Markazda_va_chapda_kirish_kartasi_yozuvlar_ostida(string layout, string align)
    {
        var texts = LockLayout.Texts(layout);
        var login = LockLayout.Login(layout);

        Assert.Equal(0, texts.Row);
        Assert.Equal(align, texts.Align);

        Assert.Equal(1, login.Row);          // yozuvlarning ostida
        Assert.Equal(0, login.Column);
        Assert.Equal(2, login.ColumnSpan);
        Assert.Equal(align, login.Align);    // yozuvlar bilan bir tomonda
    }

    [Fact]
    public void Matn_tekislash_faqat_markazda_markazlashadi()
    {
        Assert.Equal("center", LockLayout.TextAlign("center"));
        Assert.Equal("left", LockLayout.TextAlign("left"));
        Assert.Equal("left", LockLayout.TextAlign("split"));
    }

    [Theory]
    [InlineData("top", "top")]
    [InlineData("bottom", "bottom")]
    [InlineData("middle", "center")]
    [InlineData("", "center")]
    public void Vertikal_joylashuv(string input, string expected) =>
        Assert.Equal(expected, LockLayout.Valign(input));

    [Fact]
    public void Notanish_joylashuv_markazga_tushadi()
    {
        Assert.Equal(LockLayout.Login("center"), LockLayout.Login("nomalum"));
        Assert.Equal(LockLayout.Texts("center"), LockLayout.Texts("nomalum"));
    }

    [Fact]
    public void Taymer_oynachasi_sozlamasi_standart_yoniq() =>
        Assert.True(new BehaviourPayload().ShowWidget);
}
