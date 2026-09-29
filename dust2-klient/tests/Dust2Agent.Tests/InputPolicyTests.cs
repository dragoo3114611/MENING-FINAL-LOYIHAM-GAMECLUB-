using Dust2Agent.Common;
using Xunit;

namespace Dust2Agent.Tests;

/// <summary>
/// Qulf ekranida kiritishni to'sish qoidalari.
///
/// Eng muhimi: oddiy yozuv (login/parol) va administratorning favqulodda
/// kombinatsiyasi hech qachon to'silmasligi kerak — aks holda kompyuterga
/// kirib bo'lmay qoladi.
/// </summary>
public class InputPolicyTests
{
    private const string Combo = "Ctrl+Alt+P";

    private static KeyEvent Key(int vk, bool alt = false, bool ctrl = false, bool shift = false, bool win = false) =>
        new(vk, alt, ctrl, shift, win);

    [Fact]
    public void Win_tugmasi_to_siladi()
    {
        Assert.True(InputPolicy.ShouldBlock(Key(InputPolicy.VK_LWIN, win: true), Combo));
        Assert.True(InputPolicy.ShouldBlock(Key(InputPolicy.VK_RWIN, win: true), Combo));
        // Win + boshqa tugma (Win+R, Win+E)
        Assert.True(InputPolicy.ShouldBlock(Key('R', win: true), Combo));
        Assert.True(InputPolicy.ShouldBlock(Key('E', win: true), Combo));
    }

    [Fact]
    public void Tizim_kombinatsiyalari_to_siladi()
    {
        Assert.True(InputPolicy.ShouldBlock(Key(InputPolicy.VK_TAB, alt: true), Combo));      // Alt+Tab
        Assert.True(InputPolicy.ShouldBlock(Key(InputPolicy.VK_ESCAPE, alt: true), Combo));   // Alt+Esc
        Assert.True(InputPolicy.ShouldBlock(Key(InputPolicy.VK_F4, alt: true), Combo));       // Alt+F4
        Assert.True(InputPolicy.ShouldBlock(Key(InputPolicy.VK_ESCAPE, ctrl: true), Combo));  // Ctrl+Esc
        Assert.True(InputPolicy.ShouldBlock(
            Key(InputPolicy.VK_ESCAPE, ctrl: true, shift: true), Combo));                     // Ctrl+Shift+Esc
        Assert.True(InputPolicy.ShouldBlock(Key(InputPolicy.VK_APPS), Combo));                // kontekst menyusi
    }

    [Fact]
    public void Oddiy_yozuv_to_silmaydi()
    {
        foreach (var ch in "ABZ0159")
        {
            Assert.False(InputPolicy.ShouldBlock(Key(ch), Combo), $"{ch} to'silmasligi kerak");
        }
        Assert.False(InputPolicy.ShouldBlock(Key(0x08), Combo));                  // Backspace
        Assert.False(InputPolicy.ShouldBlock(Key(0x0D), Combo));                  // Enter
        Assert.False(InputPolicy.ShouldBlock(Key(InputPolicy.VK_TAB), Combo));    // Tab (maydonlar orasida)
        Assert.False(InputPolicy.ShouldBlock(Key('V', ctrl: true), Combo));       // Ctrl+V
        Assert.False(InputPolicy.ShouldBlock(Key('A', shift: true), Combo));      // Shift+A
    }

    [Fact]
    public void Favqulodda_kombinatsiya_o_tadi()
    {
        Assert.False(InputPolicy.ShouldBlock(Key('P', ctrl: true, alt: true), Combo));
    }

    [Fact]
    public void Boshqa_kombinatsiya_tanlansa_ham_o_tadi()
    {
        // Administrator Ctrl+F9 ni tanlagan bo'lsa, u ham o'tishi kerak
        Assert.False(InputPolicy.ShouldBlock(Key(0x78, ctrl: true), "Ctrl+F9"));
    }

    [Theory]
    [InlineData("Ctrl+Alt+P", 0x50, true, true, false)]
    [InlineData("ctrl+alt+p", 0x50, true, true, false)]
    [InlineData("Ctrl+Shift+F1", 0x70, true, false, true)]
    [InlineData("Alt+F12", 0x7B, false, true, false)]
    [InlineData("K", 0x4B, false, false, false)]
    public void Kombinatsiya_o_qiladi(string combo, int vk, bool ctrl, bool alt, bool shift)
    {
        var p = InputPolicy.Parse(combo);
        Assert.NotNull(p);
        Assert.Equal(vk, p!.Value.Vk);
        Assert.Equal(ctrl, p.Value.Ctrl);
        Assert.Equal(alt, p.Value.Alt);
        Assert.Equal(shift, p.Value.Shift);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ctrl+Alt")]
    [InlineData("Ctrl+F13")]
    [InlineData("Ctrl+Nomalum")]
    public void Yaroqsiz_kombinatsiya_null(string combo) => Assert.Null(InputPolicy.Parse(combo));

    [Fact]
    public void Kombinatsiya_yo_q_bo_lsa_ham_bloklash_ishlaydi()
    {
        // Sozlama buzilgan bo'lsa ham tizim tugmalari to'siladi
        Assert.True(InputPolicy.ShouldBlock(Key(InputPolicy.VK_LWIN, win: true), ""));
        Assert.False(InputPolicy.ShouldBlock(Key('A'), ""));
    }
}
