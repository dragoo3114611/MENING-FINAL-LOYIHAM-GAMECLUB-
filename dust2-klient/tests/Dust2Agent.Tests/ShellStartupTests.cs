using System.Xml.Linq;
using Dust2Agent.Common;
using Xunit;

namespace Dust2Agent.Tests;

/// <summary>
/// Qobiqning birinchi kadri.
///
/// Xizmatdan holat kelgunicha oyna XAML'dagi boshlang'ich ko'rinishni ko'rsatadi.
/// Ilgari u qulf ekrani edi: sozlanmagan kompyuterda foydalanuvchi IP va kompyuter
/// nomini kirita olmasdi (xizmat javob bermasa esa umuman kira olmasdi). Shuning uchun
/// boshlang'ich ko'rinish ShellState'ning standart qiymatiga mos bo'lishi shart.
/// </summary>
public class ShellStartupTests
{
    private static XElement LockWindowXaml()
    {
        // bin/... ichidan yuqoriga chiqib, manba faylni qidiramiz
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? path = null;
        for (var d = dir; d is not null && path is null; d = d.Parent)
        {
            var candidate = Path.Combine(d.FullName, "src", "Dust2Agent.Shell", "LockWindow.xaml");
            if (File.Exists(candidate)) path = candidate;
        }
        Assert.True(path is not null, $"LockWindow.xaml topilmadi ({AppContext.BaseDirectory} dan yuqoriga qarab)");
        return XElement.Parse(File.ReadAllText(path!));
    }

    private static XElement Named(XElement root, string name) =>
        root.Descendants().First(e =>
            (string?)e.Attribute(XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml")) == name);

    /// <summary>
    /// InvariantGlobalization yoqilgan bo'lsa, WPF matn maydoniga fokus berilganda
    /// "Only the invariant culture is supported" istisnosi tashlanadi va maydonlarga
    /// yozib bo'lmaydi. Shuning uchun u yoqilmasligi kerak.
    /// </summary>
    [Fact]
    public void Globalizatsiya_o_chirilmagan()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? props = null;
        for (var d = dir; d is not null && props is null; d = d.Parent)
        {
            var candidate = Path.Combine(d.FullName, "Directory.Build.props");
            if (File.Exists(candidate)) props = candidate;
        }
        Assert.True(props is not null, "Directory.Build.props topilmadi");

        var xml = XElement.Parse(File.ReadAllText(props!));
        var values = xml.Descendants()
            .Where(e => e.Name.LocalName == "InvariantGlobalization")
            .Select(e => e.Value.Trim())
            .ToList();
        Assert.All(values, v => Assert.Equal("false", v.ToLowerInvariant()));
    }

    [Fact]
    public void Standart_holat_ulanish_oynasi()
    {
        Assert.Equal("setup", new ShellState().Screen);
        Assert.Equal("Ctrl+Alt+P", new ShellState().UnlockCombo);
    }

    [Fact]
    public void Oyna_ochilganda_ulanish_paneli_ko_rinadi()
    {
        var xaml = LockWindowXaml();
        var setup = Named(xaml, "SetupPanel");
        var lockPanel = Named(xaml, "LockPanel");

        Assert.NotEqual("Collapsed", (string?)setup.Attribute("Visibility"));
        Assert.Equal("Collapsed", (string?)lockPanel.Attribute("Visibility"));
    }

    [Fact]
    public void Qulf_ekranida_statik_bloklangan_yozuvi_yo_q()
    {
        // "Klaviatura va sichqoncha bloklangan" faqat haqiqatan bloklangan holatda,
        // kod orqali qo'yiladi — XAML'da doimiy matn bo'lmasin.
        var xaml = LockWindowXaml();
        var foot = Named(xaml, "FootCenter");
        Assert.Null(foot.Attribute("Text"));
    }

    [Fact]
    public void Ulanish_sozlamalari_tugmasi_yozuvli()
    {
        var xaml = LockWindowXaml();
        var gear = Named(xaml, "GearButton");
        var content = (string?)gear.Attribute("Content") ?? "";
        Assert.Contains("sozlama", content, StringComparison.OrdinalIgnoreCase);
    }
}
