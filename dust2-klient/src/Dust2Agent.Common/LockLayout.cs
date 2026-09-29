namespace Dust2Agent.Common;

/// <summary>Elementning qulf ekrani tarmog'idagi o'rni.</summary>
/// <param name="Row">Qator (0 — yozuvlar, 1 — kirish kartasi pastda bo'lsa).</param>
/// <param name="Column">Ustun (0 — chap, 1 — o'ng).</param>
/// <param name="ColumnSpan">Nechta ustunni egallaydi.</param>
/// <param name="Align">Gorizontal joylashuv: left | center | right | stretch.</param>
public readonly record struct Placement(int Row, int Column, int ColumnSpan, string Align);

/// <summary>
/// Qulf ekrani joylashuvi — prototipdagi .lay-center / .lay-left / .lay-split bilan bir xil
/// (docs/prototype/dust2-boshqaruv-prototip.html).
///
/// Bu yerda WPF turlari ishlatilmaydi, shuning uchun mantiqni sinovdan o'tkazish mumkin;
/// qobiq qaytgan qiymatlarni WPF joylashuviga o'giradi.
/// </summary>
public static class LockLayout
{
    /// <summary>Yozuvlar (soat, sarlavha, matn) qayerda turadi.</summary>
    public static Placement Texts(string layout) => layout switch
    {
        "split" => new Placement(0, 0, 1, "left"),
        "left" => new Placement(0, 0, 2, "left"),
        _ => new Placement(0, 0, 2, "center")
    };

    /// <summary>
    /// Akkaunt bilan kirish kartasi qayerda turadi. "split" da yozuvlarning o'ng yonida,
    /// qolgan joylashuvlarda esa ularning ostida — ilgari u har doim o'ngda qolar edi.
    /// </summary>
    public static Placement Login(string layout) => layout switch
    {
        "split" => new Placement(0, 1, 1, "right"),
        "left" => new Placement(1, 0, 2, "left"),
        _ => new Placement(1, 0, 2, "center")
    };

    /// <summary>Butun blokning vertikal o'rni: top | center | bottom.</summary>
    public static string Valign(string valign) => valign switch
    {
        "top" => "top",
        "bottom" => "bottom",
        _ => "center"
    };

    /// <summary>Yozuvlar matni qaysi tomonga tekislanadi.</summary>
    public static string TextAlign(string layout) => layout == "center" ? "center" : "left";
}
