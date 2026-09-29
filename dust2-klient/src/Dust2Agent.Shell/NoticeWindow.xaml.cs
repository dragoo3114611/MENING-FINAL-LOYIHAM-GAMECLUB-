using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Dust2Agent.Common;

namespace Dust2Agent.Shell;

/// <summary>
/// Seans ochiq paytdagi bildirishnomalar.
///
/// Qulf oynasi seans davomida yashiriladi (<c>App.OnState</c>), shuning uchun
/// unga yozilgan xabarlar mijozga ko'rinmay qolardi: admin xabari ham,
/// "5 daqiqa qoldi" ogohlantirishi ham, "+30 daqiqa qo'shildi" ham
/// ([qaror 21](../../../docs/QARORLAR.md)).
///
/// Bu oyna o'yin ustida turadi, fokusni tortib olmaydi va o'zi yo'qoladi.
/// Admin xabari esa mijoz tugmani bosmaguncha turadi — muhim narsa e'tibordan
/// chetda qolmasin.
/// </summary>
public partial class NoticeWindow : Window
{
    /// <summary>Ogohlantirish shuncha vaqtdan keyin o'zi yo'qoladi.</summary>
    private static readonly TimeSpan ToastLife = TimeSpan.FromSeconds(8);

    private readonly DispatcherTimer _hide = new();

    public NoticeWindow()
    {
        InitializeComponent();
        _hide.Tick += (_, _) => Dismiss();
        SizeChanged += (_, _) => Place();
    }

    /// <summary>Ekranning tepa o'rtasida — o'yinning pastki qismini to'smaydi.</summary>
    private void Place()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Left + (area.Width - ActualWidth) / 2;
        Top = area.Top + 28;
    }

    /// <summary>Qisqa ogohlantirish — o'zi yo'qoladi.</summary>
    public void ShowToast(ToastPayload t)
    {
        TitleText.Text = t.Title;
        BodyText.Text = t.Text;
        BodyText.Visibility = string.IsNullOrWhiteSpace(t.Text) ? Visibility.Collapsed : Visibility.Visible;
        OkButton.Visibility = Visibility.Collapsed;
        Accent.Background = new SolidColorBrush(Kind(t.Kind));

        Pop();
        _hide.Interval = ToastLife;
        _hide.Stop();
        _hide.Start();
    }

    /// <summary>Admindan xabar — mijoz tugmani bosmaguncha turadi.</summary>
    public void ShowMessage(string text)
    {
        TitleText.Text = "Administrator xabari";
        BodyText.Text = text;
        BodyText.Visibility = Visibility.Visible;
        OkButton.Visibility = Visibility.Visible;
        Accent.Background = new SolidColorBrush(Color.FromRgb(0x6E, 0xC3, 0xFF));

        _hide.Stop();
        Pop();
    }

    public void Dismiss()
    {
        _hide.Stop();
        Hide();
    }

    private void Pop()
    {
        if (!IsVisible) Show();
        Place();
        // Fokusni tortib olmaydi, lekin boshqa "topmost" oynalar ostida qolmasin
        Topmost = false;
        Topmost = true;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => Dismiss();

    private static Color Kind(string? kind) => kind switch
    {
        "ok" => Color.FromRgb(0x3E, 0xCF, 0x8E),
        "warn" => Color.FromRgb(0xF0, 0xA4, 0x41),
        "bad" => Color.FromRgb(0xF0, 0x64, 0x5A),
        _ => Color.FromRgb(0xE0, 0xA8, 0x4E)
    };
}
