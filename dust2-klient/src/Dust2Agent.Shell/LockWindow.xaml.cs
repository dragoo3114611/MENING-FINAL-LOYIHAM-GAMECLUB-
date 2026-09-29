using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Dust2Agent.Common;

namespace Dust2Agent.Shell;

/// <summary>
/// Qulf ekrani va ulanish oynasi. Har bir monitorda bittadan ochiladi;
/// kirish maydonlari faqat asosiy monitordagi oynada ko'rsatiladi.
/// </summary>
public partial class LockWindow : Window
{
    private readonly AgentClient _agent;
    private readonly bool _primary;
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(5) };

    /// <summary>Ulanish javobi kelmasa tugma qotib qolmasligi uchun.</summary>
    private readonly DispatcherTimer _connectTimeout = new() { Interval = TimeSpan.FromSeconds(25) };

    private ShellState _state = new();
    /// <summary>Parol so'ralgach nima qilish: setup | unlock</summary>
    private string _passwordPurpose = "setup";

    private static readonly string[] WeekDays =
        { "yakshanba", "dushanba", "seshanba", "chorshanba", "payshanba", "juma", "shanba" };

    private static readonly string[] Months =
        { "yanvar", "fevral", "mart", "aprel", "may", "iyun", "iyul", "avgust",
          "sentabr", "oktabr", "noyabr", "dekabr" };

    /// <summary>Asosiy monitordagi oyna — kirish maydonlari shu yerda.</summary>
    public bool IsPrimaryScreen => _primary;

    /// <summary>Foydalanuvchi "Klient dasturini to'xtatish" ni tanladi.</summary>
    public event Action? StopRequested;

    public LockWindow(AgentClient agent, bool primary)
    {
        InitializeComponent();
        _agent = agent;
        _primary = primary;

        if (!primary)
        {
            // Qo'shimcha monitorlarda faqat fon ko'rsatiladi
            LockPanel.Visibility = Visibility.Collapsed;
            SetupPanel.Visibility = Visibility.Collapsed;
            Footer.Visibility = Visibility.Collapsed;
        }

        _clock.Tick += (_, _) => UpdateClock();
        _clock.Start();
        UpdateClock();

        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            ToastBox.Visibility = Visibility.Collapsed;
        };

        _connectTimeout.Tick += (_, _) =>
        {
            _connectTimeout.Stop();
            if (SetupPanel.Visibility != Visibility.Visible) return;
            ConnectButton.IsEnabled = true;
            ConnectButton.Content = _state.Paired ? "Saqlash va ulanish" : "Ulanish";
            if (string.IsNullOrEmpty(SetupStatus.Text))
                SetupStatus.Text = "Javob kelmadi. IP manzil va portni tekshiring, admin dasturi ochiqligiga ishonch hosil qiling.";
        };

        NameBox.Text = Environment.MachineName;
        PcBadgeText.Text = Environment.MachineName;
        PreviewKeyDown += OnPreviewKeyDown;

        // Xizmatdan holat kelgunicha ham oyna to'g'ri ko'rinsin: sozlanmagan kompyuterda
        // birinchi kadrdanoq ulanish oynasi chiqadi (ilgari qulf ekrani ko'rinardi va
        // foydalanuvchi IP hamda kompyuter nomini kirita olmasdi).
        Apply(_state);
        Loaded += (_, _) =>
        {
            if (_primary && SetupPanel.Visibility == Visibility.Visible) FocusSetup();
        };
    }

    /// <summary>Ulanish oynasi ochilganda oynani oldinga chiqarib, birinchi maydonga fokus beradi.</summary>
    private void FocusSetup()
    {
        Activate();
        Native.ForceForeground(this);
        HostBox.Focus();
        HostBox.CaretIndex = HostBox.Text.Length;
    }

    /// <summary>Xizmat (named pipe) bilan aloqa bor-yo'qligini ko'rsatadi.</summary>
    public void SetServiceConnected(bool connected)
    {
        SvcWarn.Visibility = connected ? Visibility.Collapsed : Visibility.Visible;
    }

    /* --------------------------------- holat --------------------------------- */

    public void Apply(ShellState s)
    {
        _state = s;
        ApplyTheme(s.Lock, s.WallpaperPath);

        // Kompyuter nomi har bir monitorda ko'rinib turadi — mijoz ham, operator ham
        // "PC 05 da muammo" deb aniq ayta olsin
        var pcName = string.IsNullOrWhiteSpace(s.PcName) ? Environment.MachineName : s.PcName;
        PcBadgeText.Text = pcName;
        PcNameText.Text = pcName;

        if (!_primary) return;

        var setup = s.Screen == "setup";
        SetupPanel.Visibility = setup ? Visibility.Visible : Visibility.Collapsed;
        LockPanel.Visibility = setup ? Visibility.Collapsed : Visibility.Visible;
        // Juftlangan kompyuterda ulanish sozlamalari faqat kombinatsiya orqali
        GearButton.Visibility = ShellScreen.ShowSetupButton(s.Paired, s.Online)
            ? Visibility.Visible
            : Visibility.Collapsed;
        // Ulanish oynasida nom burchakda, qulf ekranida esa soat yonida katta
        PcBadge.Visibility = setup ? Visibility.Visible : Visibility.Collapsed;

        if (setup)
        {
            if (!HostBox.IsFocused && !string.IsNullOrEmpty(s.Host)) HostBox.Text = s.Host;
            if (!PortBox.IsFocused && s.Port > 0) PortBox.Text = s.Port.ToString(CultureInfo.InvariantCulture);
            if (!NameBox.IsFocused && !string.IsNullOrEmpty(s.PcName)) NameBox.Text = s.PcName;

            CodeRow.Visibility = s.Paired ? Visibility.Collapsed : Visibility.Visible;
            UnpairButton.Visibility = s.Paired ? Visibility.Visible : Visibility.Collapsed;
            SetupTitle.Text = s.Paired ? "Ulanish sozlamalari" : "Adminga ulanish";
            ConnectButton.Content = s.ConnectPhase == "connecting"
                ? "Ulanmoqda…"
                : s.Paired ? "Saqlash va ulanish" : "Ulanish";
            ConnectButton.IsEnabled = s.ConnectPhase != "connecting";
            if (s.ConnectPhase != "connecting") _connectTimeout.Stop();
            SetupStatus.Text = s.ConnectPhase == "connecting"
                ? $"{s.Host}:{s.Port} ga ulanmoqda…"
                : s.ConnectError;
            SetupFoot.Text = $"Bu kompyuter: {Environment.MachineName} · Klient versiyasi " +
                             (typeof(LockWindow).Assembly.GetName().Version?.ToString(3) ?? "0.1.0") +
                             $" · Loglar: {AgentPaths.UserLogs}";
            if (!HostBox.IsKeyboardFocusWithin && !PortBox.IsKeyboardFocusWithin &&
                !CodeBox.IsKeyboardFocusWithin && !NameBox.IsKeyboardFocusWithin)
            {
                FocusSetup();
            }
        }
        else
        {
            TitleText.Text = s.Lock.Title;
            TitleText.Visibility = string.IsNullOrEmpty(s.Lock.Title)
                ? Visibility.Collapsed : Visibility.Visible;
            SubText.Text = s.Lock.Text;
            ClockText.Visibility = s.Lock.Clock ? Visibility.Visible : Visibility.Collapsed;
            DateText.Visibility = s.Lock.Clock ? Visibility.Visible : Visibility.Collapsed;
            // Adminda "Kompyuter nomini ko'rsatish" olib tashlangan bo'lishi mumkin
            PcNameText.Visibility = s.Lock.PcName ? Visibility.Visible : Visibility.Collapsed;

            var ended = s.Due > 0;
            // Seans ochiq, lekin admin vaqtni to'xtatgan — mijoz o'ynay olmaydi
            var paused = s.Session is { Paused: true };

            CardTitle.Text = paused
                ? "Vaqt to'xtatilgan"
                : ended ? "Vaqtingiz tugadi" : "Akkaunt bilan kirish";
            CardTitle.Foreground = ended ? Brush("#E5484D") : Brushes.White;
            CardSub.Text = paused
                ? "Administrator davom ettirguncha kuting. Qolgan vaqtingiz saqlanib turibdi."
                : ended
                    ? "To'lanmagan summa — iltimos, administratorga to'lang."
                    : "Balansingizdan vaqt yechiladi";

            DueText.Visibility = ended && !paused ? Visibility.Visible : Visibility.Collapsed;
            DueText.Text = ended ? $"{s.Due:N0} so'm".Replace(",", " ") : "";

            // Pauzada login kerak emas — seans allaqachon ochiq
            var showLogin = s.Lock.Login && !ended && !paused;
            LoginFields.Visibility = showLogin ? Visibility.Visible : Visibility.Collapsed;
            LoginCard.Visibility = showLogin || ended || paused ? Visibility.Visible : Visibility.Collapsed;
            LoginError.Text = s.LoginError;

            FootCenter.Text = s.Behaviour.BlockInput
                ? "Klaviatura va sichqoncha bloklangan — faqat kirish maydoni ishlaydi"
                : "";
            FootLeft.Text = paused ? $"{s.PcName} · vaqt to'xtatilgan" : $"{s.PcName} · qulflangan";
            FootRight.Text = s.Online
                ? $"Admin: {s.Host}"
                : s.Paired ? "Admin bilan aloqa yo'q" : "Adminga ulanmagan";
        }

        UpdateClock();
    }

    private void ApplyTheme(LockConfigPayload lockCfg, string wallpaper)
    {
        var t = ClientThemes.Get(lockCfg.Theme);
        BgFrom.Color = t.BackgroundFrom;
        BgTo.Color = t.BackgroundTo;
        Dim.Opacity = Math.Clamp(lockCfg.Dim / 100.0, 0, 0.9);

        TitleText.Foreground = new SolidColorBrush(t.Accent);
        ClockText.Foreground = new SolidColorBrush(t.Text);
        PcNameText.Foreground = new SolidColorBrush(t.Text);
        SubText.Foreground = new SolidColorBrush(t.Text);
        DateText.Foreground = new SolidColorBrush(t.Sub);
        LoginCard.Background = new SolidColorBrush(t.Card);
        LoginCard.BorderBrush = new SolidColorBrush(t.Line);
        SetupPanel.Background = new SolidColorBrush(t.Card);
        SetupPanel.BorderBrush = new SolidColorBrush(t.Line);
        PcBadge.Background = new SolidColorBrush(t.Card);
        PcBadge.BorderBrush = new SolidColorBrush(t.Line);
        PcBadgeText.Foreground = new SolidColorBrush(t.Text);
        LoginButton.Background = new SolidColorBrush(t.Accent);
        LoginButton.Foreground = new SolidColorBrush(t.AccentInk);
        ConnectButton.Background = new SolidColorBrush(t.Accent);
        ConnectButton.Foreground = new SolidColorBrush(t.AccentInk);

        ClockText.FontSize = 130 * (lockCfg.ClockSize / 100.0);
        // Nom soat bilan bir xil o'lchov sozlamasiga bo'ysunadi
        PcNameText.FontSize = 90 * (lockCfg.ClockSize / 100.0);
        PcNameText.LineHeight = 96 * (lockCfg.ClockSize / 100.0);
        TitleText.FontSize = 36 * (lockCfg.TitleSize / 100.0);
        SubText.FontSize = 17 * (lockCfg.TextSize / 100.0);

        ApplyLayout(lockCfg.Layout, lockCfg.Valign);

        if (!string.IsNullOrEmpty(wallpaper) && File.Exists(wallpaper))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(wallpaper);
                bmp.EndInit();
                Wallpaper.Source = bmp;
                Wallpaper.Stretch = lockCfg.Fit switch
                {
                    "contain" => Stretch.Uniform,
                    "stretch" => Stretch.Fill,
                    "center" => Stretch.None,
                    _ => Stretch.UniformToFill
                };
                Wallpaper.Visibility = Visibility.Visible;
            }
            catch (Exception)
            {
                Wallpaper.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            Wallpaper.Visibility = Visibility.Collapsed;
        }
    }


    /// <summary>
    /// Yozuvlar va kirish kartasini prototipdagi joylashuvga keltiradi
    /// (docs/prototype dagi .lay-center / .lay-left / .lay-split).
    ///
    /// Ilgari faqat yozuvlar ko'chardi, kirish kartasi esa har doim o'ngda qolardi.
    /// </summary>
    private void ApplyLayout(string layout, string valign)
    {
        var texts = LockLayout.Texts(layout);
        var login = LockLayout.Login(layout);
        var split = layout == "split";

        LockInner.VerticalAlignment = LockLayout.Valign(valign) switch
        {
            "top" => VerticalAlignment.Top,
            "bottom" => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Center
        };
        LockInner.Margin = LockLayout.Valign(valign) switch
        {
            "top" => new Thickness(0, 60, 0, 0),
            "bottom" => new Thickness(0, 0, 0, 90),
            _ => new Thickness(0)
        };
        LockInner.HorizontalAlignment = split
            ? HorizontalAlignment.Stretch
            : Align(LockLayout.TextAlign(layout));

        var textAlign = LockLayout.TextAlign(layout) == "center"
            ? TextAlignment.Center : TextAlignment.Left;
        PcNameText.TextAlignment = textAlign;
        ClockText.TextAlignment = textAlign;
        DateText.TextAlignment = textAlign;
        TitleText.TextAlignment = textAlign;
        SubText.TextAlignment = textAlign;

        ColTexts.Width = split ? new GridLength(1.25, GridUnitType.Star) : GridLength.Auto;
        ColLogin.Width = split ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

        Place(LockTexts, texts);
        Place(LoginCard, login);
        LoginCard.Margin = split ? new Thickness(40, 0, 0, 0) : new Thickness(0, 34, 0, 0);
    }

    private static void Place(FrameworkElement el, Placement p)
    {
        Grid.SetRow(el, p.Row);
        Grid.SetColumn(el, p.Column);
        Grid.SetColumnSpan(el, p.ColumnSpan);
        el.HorizontalAlignment = Align(p.Align);
    }

    private static HorizontalAlignment Align(string a) => a switch
    {
        "left" => HorizontalAlignment.Left,
        "right" => HorizontalAlignment.Right,
        "stretch" => HorizontalAlignment.Stretch,
        _ => HorizontalAlignment.Center
    };

    private static SolidColorBrush Brush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex)!);

    private void UpdateClock()
    {
        var d = DateTime.Now;
        ClockText.Text = d.ToString("HH:mm", CultureInfo.InvariantCulture);
        DateText.Text = $"{d.Day}-{Months[d.Month - 1]}, {WeekDays[(int)d.DayOfWeek]}";
    }

    /* --------------------------------- amallar --------------------------------- */

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        var login = LoginBox.Text.Trim();
        var pass = PassBox.Password;
        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(pass))
        {
            LoginError.Text = "Login va parolni kiriting";
            return;
        }
        LoginError.Text = "";
        _agent.Send(PipeTypes.Login, new LoginRequest { Login = login, Password = pass });
        PassBox.Clear();
    }

    private void Connect_Click(object sender, RoutedEventArgs e)
    {
        if (!_agent.IsConnected)
        {
            SetupStatus.Text = "DUST2 klient agent xizmati ishlamayapti — sozlama saqlanmaydi. " +
                               "services.msc dan \"DUST2 klient agent\" xizmatini ishga tushiring.";
            SetServiceConnected(false);
            return;
        }

        var host = HostBox.Text.Trim();
        if (!System.Net.IPAddress.TryParse(host, out _) && !Uri.CheckHostName(host).Equals(UriHostNameType.Dns))
        {
            SetupStatus.Text = "IP manzil noto'g'ri. Masalan: 192.168.1.10";
            HostBox.Focus();
            return;
        }
        if (!int.TryParse(PortBox.Text.Trim(), out var port) || port < 1 || port > 65535)
        {
            SetupStatus.Text = "Port 1 dan 65535 gacha son bo'lishi kerak";
            PortBox.Focus();
            return;
        }
        var name = NameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            SetupStatus.Text = "Kompyuter nomini kiriting";
            NameBox.Focus();
            return;
        }
        var code = CodeBox.Text.Trim();
        if (!_state.Paired && code.Length != 6)
        {
            SetupStatus.Text = "Ulanish kodi 6 ta raqamdan iborat";
            CodeBox.Focus();
            return;
        }

        SetupStatus.Text = "";
        ConnectButton.IsEnabled = false;
        ConnectButton.Content = "Ulanmoqda…";
        _connectTimeout.Stop();
        _connectTimeout.Start();
        _agent.Send(PipeTypes.Connect,
            new ConnectRequest { Host = host, Port = port, Code = code, Name = name });
        CodeBox.Clear();
    }

    private void Unpair_Click(object sender, RoutedEventArgs e) => _agent.Send(PipeTypes.Unpair);

    private void Gear_Click(object sender, RoutedEventArgs e)
    {
        if (!_state.HasServicePassword)
        {
            ShowSetup();
            return;
        }
        _passwordPurpose = "setup";
        PwTitle.Text = "Ulanish sozlamalari";
        PwSub.Text = "Faqat administrator o'zgartira oladi.";
        PwError.Text = "";
        ServicePassBox.Clear();
        PasswordOverlay.Visibility = Visibility.Visible;
        ServicePassBox.Focus();
    }

    private void ShowSetup()
    {
        SetupPanel.Visibility = Visibility.Visible;
        LockPanel.Visibility = Visibility.Collapsed;
        CodeRow.Visibility = _state.Paired ? Visibility.Collapsed : Visibility.Visible;
        UnpairButton.Visibility = _state.Paired ? Visibility.Visible : Visibility.Collapsed;
        SetupTitle.Text = _state.Paired ? "Ulanish sozlamalari" : "Adminga ulanish";
        FocusSetup();
    }

    private void MessageOk_Click(object sender, RoutedEventArgs e) =>
        MessageOverlay.Visibility = Visibility.Collapsed;

    private void PwCancel_Click(object sender, RoutedEventArgs e)
    {
        PasswordOverlay.Visibility = Visibility.Collapsed;
        ServicePassBox.Clear();
    }

    private void PwOk_Click(object sender, RoutedEventArgs e)
    {
        if (!_agent.IsConnected)
        {
            PwError.Text = "Xizmat bilan aloqa yo'q — parolni tekshirib bo'lmaydi.";
            return;
        }
        var type = _passwordPurpose == "unlock" ? PipeTypes.Unlock : PipeTypes.VerifyPassword;
        _agent.Send(type, new PasswordRequest { Password = ServicePassBox.Password });
    }

    /// <summary>Xizmatdan kelgan parol tekshiruvi natijasi.</summary>
    public void OnPasswordChecked(bool ok, bool unlock)
    {
        if (!ok)
        {
            PwError.Text = "Parol noto'g'ri";
            ServicePassBox.Clear();
            ServicePassBox.Focus();
            return;
        }
        PasswordOverlay.Visibility = Visibility.Collapsed;
        ServicePassBox.Clear();
        switch (_passwordPurpose)
        {
            case "unlock":
                Hide();
                ShowToast(new ToastPayload
                {
                    Title = "Qulf ochildi",
                    Text = "Xizmat paroli bilan qo'lda ochildi",
                    Kind = "warn"
                });
                return;
            case "actions":
                ShowActions();
                return;
            default:
                ShowSetup();
                return;
        }
    }

    /* ------------------------------ klient boshqaruvi ------------------------------ */

    private void ShowActions()
    {
        ActionsOverlay.Visibility = Visibility.Visible;
    }

    private void ActSetup_Click(object sender, RoutedEventArgs e)
    {
        ActionsOverlay.Visibility = Visibility.Collapsed;
        ShowSetup();
    }

    private void ActStop_Click(object sender, RoutedEventArgs e)
    {
        ActionsOverlay.Visibility = Visibility.Collapsed;
        StopRequested?.Invoke();
    }

    private void ActCancel_Click(object sender, RoutedEventArgs e)
    {
        ActionsOverlay.Visibility = Visibility.Collapsed;
    }

    public void ShowMessage(string text)
    {
        MessageText.Text = text;
        MessageOverlay.Visibility = Visibility.Visible;
    }

    public void ShowToast(ToastPayload t)
    {
        ToastTitle.Text = t.Title;
        ToastText.Text = t.Text;
        ToastText.Visibility = string.IsNullOrEmpty(t.Text) ? Visibility.Collapsed : Visibility.Visible;
        ToastBox.Visibility = Visibility.Visible;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    /* ------------------------ favqulodda qulfni ochish ------------------------ */

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (SetupPanel.Visibility == Visibility.Visible) Connect_Click(sender, e);
            else if (PasswordOverlay.Visibility == Visibility.Visible) PwOk_Click(sender, e);
            else if (LoginFields.Visibility == Visibility.Visible) Login_Click(sender, e);
            return;
        }

        if (!MatchesCombo(e, _state.UnlockCombo)) return;

        e.Handled = true;

        // Xizmat paroli hali o'rnatilmagan bo'lsa tekshiradigan narsa yo'q —
        // boshqaruv oynasi darhol ochiladi.
        if (!_state.HasServicePassword)
        {
            ShowActions();
            return;
        }

        _passwordPurpose = "actions";
        PwTitle.Text = "Klient boshqaruvi";
        PwSub.Text = "Xizmat parolini kiriting.";
        PwError.Text = "";
        ServicePassBox.Clear();
        PasswordOverlay.Visibility = Visibility.Visible;
        ServicePassBox.Focus();
    }

    /// <summary>"Ctrl+Alt+P" ko'rinishidagi kombinatsiyani tekshiradi.</summary>
    internal static bool MatchesCombo(KeyEventArgs e, string combo)
    {
        if (string.IsNullOrWhiteSpace(combo)) return false;

        var parts = combo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;

        var needCtrl = parts.Any(p => p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase));
        var needAlt = parts.Any(p => p.Equals("Alt", StringComparison.OrdinalIgnoreCase));
        var needShift = parts.Any(p => p.Equals("Shift", StringComparison.OrdinalIgnoreCase));
        var keyName = parts.LastOrDefault(p =>
            !p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) &&
            !p.Equals("Alt", StringComparison.OrdinalIgnoreCase) &&
            !p.Equals("Shift", StringComparison.OrdinalIgnoreCase));
        if (keyName is null) return false;

        if (!Enum.TryParse<Key>(keyName, true, out var key)) return false;

        var actual = e.Key == Key.System ? e.SystemKey : e.Key;
        if (actual != key) return false;

        var mods = Keyboard.Modifiers;
        return needCtrl == mods.HasFlag(ModifierKeys.Control)
               && needAlt == mods.HasFlag(ModifierKeys.Alt)
               && needShift == mods.HasFlag(ModifierKeys.Shift);
    }
}
