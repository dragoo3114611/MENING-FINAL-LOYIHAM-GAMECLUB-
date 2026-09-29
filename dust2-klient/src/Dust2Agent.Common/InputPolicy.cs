namespace Dust2Agent.Common;

/// <summary>Klaviaturadan kelgan bitta hodisa (past darajali ilgak uchun).</summary>
/// <param name="VirtualKey">Windows virtual-key kodi.</param>
public readonly record struct KeyEvent(int VirtualKey, bool Alt, bool Ctrl, bool Shift, bool Win);

/// <summary>
/// Qulf ekrani yoqilganda qaysi tugmalar to'siladi.
///
/// Bu yerda Windows API yo'q — qaror sof mantiq, shuning uchun sinovdan o'tkaziladi.
/// Ilgakning o'zi qobiqda (Dust2Agent.Shell/InputBlocker.cs).
///
/// Qoida: oddiy yozuv (login/parol kiritish) hech qachon to'silmaydi, faqat tizim
/// kombinatsiyalari — Win, Alt+Tab, Ctrl+Esc va hokazo. Favqulodda kombinatsiya
/// (masalan Ctrl+Alt+K) har doim o'tkaziladi, aks holda administrator qulfni
/// ocha olmay qoladi.
///
/// Ctrl+Alt+Del ni bu yo'l bilan to'sib bo'lmaydi — u uchun Task Manager
/// siyosati ishlatiladi ([qaror 7](../../../docs/QARORLAR.md)).
/// </summary>
public static class InputPolicy
{
    public const int VK_TAB = 0x09;
    public const int VK_ESCAPE = 0x1B;
    public const int VK_LWIN = 0x5B;
    public const int VK_RWIN = 0x5C;
    public const int VK_APPS = 0x5D;
    public const int VK_F4 = 0x73;

    /// <summary>Tugma to'silishi kerakmi.</summary>
    public static bool ShouldBlock(KeyEvent e, string unlockCombo)
    {
        // Administratorning favqulodda kombinatsiyasi hamisha o'tadi
        if (Matches(e, unlockCombo)) return false;

        // Win tugmasi va u bilan har qanday kombinatsiya (Win+R, Win+E, Win+D…)
        if (e.VirtualKey is VK_LWIN or VK_RWIN) return true;
        if (e.Win) return true;

        // Kontekst menyusi tugmasi
        if (e.VirtualKey == VK_APPS) return true;

        // Alt+Tab, Alt+Esc, Alt+F4
        if (e.Alt && e.VirtualKey is VK_TAB or VK_ESCAPE or VK_F4) return true;

        // Ctrl+Esc (Boshlash menyusi) va Ctrl+Shift+Esc (Task Manager)
        if (e.Ctrl && e.VirtualKey == VK_ESCAPE) return true;

        return false;
    }

    /// <summary>"Ctrl+Alt+K" ko'rinishidagi kombinatsiyaga mos keladimi.</summary>
    public static bool Matches(KeyEvent e, string combo)
    {
        var parsed = Parse(combo);
        if (parsed is null) return false;
        var (vk, ctrl, alt, shift) = parsed.Value;
        return e.VirtualKey == vk && e.Ctrl == ctrl && e.Alt == alt && e.Shift == shift;
    }

    /// <summary>
    /// Kombinatsiya matnini virtual-key kodiga o'giradi.
    /// Harflar va raqamlar uchun kod ASCII bilan bir xil, F1–F12 — 0x70…0x7B.
    /// </summary>
    public static (int Vk, bool Ctrl, bool Alt, bool Shift)? Parse(string? combo)
    {
        if (string.IsNullOrWhiteSpace(combo)) return null;

        var parts = combo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return null;

        var ctrl = false;
        var alt = false;
        var shift = false;
        string? key = null;

        foreach (var raw in parts)
        {
            var p = raw.Trim();
            if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)
                || p.Equals("Control", StringComparison.OrdinalIgnoreCase)) ctrl = true;
            else if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase)) alt = true;
            else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase)) shift = true;
            else key = p;
        }

        if (key is null) return null;

        int vk;
        if (key.Length == 1 && (char.IsLetterOrDigit(key[0])))
        {
            vk = char.ToUpperInvariant(key[0]);
        }
        else if (key.Length is 2 or 3 && (key[0] == 'F' || key[0] == 'f')
                 && int.TryParse(key[1..], out var n) && n is >= 1 and <= 12)
        {
            vk = 0x70 + n - 1;
        }
        else
        {
            return null;
        }

        return (vk, ctrl, alt, shift);
    }
}
