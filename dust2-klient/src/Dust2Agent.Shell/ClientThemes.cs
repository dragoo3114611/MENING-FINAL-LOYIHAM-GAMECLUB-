using System.Windows.Media;

namespace Dust2Agent.Shell;

/// <summary>Prototipdagi 5 ta klient mavzusi (docs/prototype/dust2-klient-prototip.html).</summary>
public sealed record ClientTheme(
    string Id,
    string Name,
    Color BackgroundFrom,
    Color BackgroundTo,
    Color Accent,
    Color AccentInk,
    Color Card,
    Color Text,
    Color Sub,
    Color Line,
    Color Input);

public static class ClientThemes
{
    private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex)!;

    public static readonly IReadOnlyList<ClientTheme> All = new[]
    {
        new ClientTheme("dust2", "Dust2 qum",
            C("#1A2233"), C("#2D241F"), C("#E0A84E"), C("#1B1407"),
            C("#B80E121A"), C("#FFFFFF"), C("#AAB4C4"), C("#24FFFFFF"), C("#14FFFFFF")),
        new ClientTheme("ct", "Kontr-terror",
            C("#08111D"), C("#0D1B2C"), C("#6EC3FF"), C("#04121F"),
            C("#BD081422"), C("#EEF6FF"), C("#9DB4CC"), C("#406EC3FF"), C("#146EC3FF")),
        new ClientTheme("terror", "Terror",
            C("#130B08"), C("#1F120C"), C("#FF7A3D"), C("#200A02"),
            C("#BD1A0C08"), C("#FFF4EC"), C("#CAA996"), C("#47FF7A3D"), C("#14FF7A3D")),
        new ClientTheme("neon", "Kiber neon",
            C("#0B0A18"), C("#150B24"), C("#FF4FD8"), C("#1A0418"),
            C("#B8140A22"), C("#F5ECFF"), C("#B7A6D6"), C("#59FF4FD8"), C("#14FF4FD8")),
        new ClientTheme("light", "Yorug' minimal",
            C("#D9E2EE"), C("#F3EFE8"), C("#2E5BD8"), C("#FFFFFF"),
            C("#CCFFFFFF"), C("#172033"), C("#5C6778"), C("#24141E32"), C("#0D141E32"))
    };

    public static ClientTheme Get(string? id) =>
        All.FirstOrDefault(t => t.Id == id) ?? All[0];
}
