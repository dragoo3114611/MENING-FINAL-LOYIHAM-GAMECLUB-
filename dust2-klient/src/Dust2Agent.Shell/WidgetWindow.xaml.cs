using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Dust2Agent.Common;

namespace Dust2Agent.Shell;

/// <summary>
/// Runpad ustida turadigan taymer oynachasi: qolgan vaqt, hisob va
/// "Vaqt so'rash" tugmasi. Surib ko'chiriladi va kichraytiriladi.
/// </summary>
public partial class WidgetWindow : Window
{
    private readonly AgentClient _agent;
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromSeconds(1) };

    private ShellState _state = new();
    private bool _mini;
    private DateTime _syncedAt = DateTime.UtcNow;
    private long _remainingAtSync;
    private bool _placed;

    public WidgetWindow(AgentClient agent)
    {
        InitializeComponent();
        _agent = agent;
        _tick.Tick += (_, _) => Redraw();
        _tick.Start();
        Loaded += (_, _) => PlaceTopRight();
    }

    private void PlaceTopRight()
    {
        if (_placed) return;
        _placed = true;
        var area = SystemParameters.WorkArea;
        Left = area.Right - ActualWidth - 24;
        Top = area.Top + 24;
    }

    public void Apply(ShellState s)
    {
        var newSession = _state.Session?.SessionId != s.Session?.SessionId;
        _state = s;

        if (s.Session is null)
        {
            Hide();
            return;
        }

        _syncedAt = DateTime.UtcNow;
        _remainingAtSync = s.Session.RemainingMs ?? 0;

        PcNameText.Text = s.PcName;
        LogoutButton.Visibility = s.Session.Mode == "acc" ? Visibility.Visible : Visibility.Collapsed;

        var t = ClientThemes.Get(s.Lock.Theme);
        Card.Background = new SolidColorBrush(t.Card);
        Card.BorderBrush = new SolidColorBrush(t.Line);
        ProgressFill.Background = new SolidColorBrush(t.Accent);

        if (newSession) _mini = false;
        ApplyMini();
        Redraw();

        if (!IsVisible)
        {
            Show();
            PlaceTopRight();
        }
    }

    private void ApplyMini()
    {
        FullPanel.Visibility = _mini ? Visibility.Collapsed : Visibility.Visible;
        MiniText.Visibility = _mini ? Visibility.Visible : Visibility.Collapsed;
        MiniButton.Content = _mini ? "▢" : "–";
    }

    private void Redraw()
    {
        var s = _state.Session;
        if (s is null) return;

        var elapsedSinceSync = (long)(DateTime.UtcNow - _syncedAt).TotalMilliseconds;
        string text;
        double progress = 1;

        if (s.Mode == "open")
        {
            var played = Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - s.StartedAt);
            text = Hms(played);
        }
        else
        {
            var left = Math.Max(0, _remainingAtSync - elapsedSinceSync);
            text = Hms(left);
            var total = Math.Max(1, s.EndsAt.GetValueOrDefault() - s.StartedAt);
            progress = Math.Clamp((double)left / total, 0, 1);
            TimeText.Foreground = left <= 60_000
                ? new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x70))
                : left <= 5 * 60_000
                    ? new SolidColorBrush(Color.FromRgb(0xE0, 0xA8, 0x4E))
                    : Brushes.White;
        }

        TimeText.Text = text;
        MiniText.Text = text;
        StatusText.Text = s.Paused ? "Pauza" : _state.Online ? "Onlayn" : "Oflayn";
        ProgressFill.Width = Math.Max(0, 230 * progress);

        Rows.Children.Clear();
        if (s.Mode == "acc")
        {
            AddRow("Akkaunt", s.Client ?? "");
            AddRow("Tugaydi", s.Paused ? "to'xtatilgan" : EndsAtText());
        }
        else if (s.Mode == "open")
        {
            AddRow("Rejim", "Ochiq vaqt");
            AddRow("Ochildi", LocalTime(s.StartedAt));
            AddRow("Tarif", $"{s.Rate:N0} so'm/soat".Replace(",", " "));
        }
        else
        {
            AddRow("Ochildi", LocalTime(s.StartedAt));
            AddRow("Tugaydi", s.Paused ? "to'xtatilgan" : EndsAtText());
            AddRow("Tarif", $"{s.Rate:N0} so'm/soat".Replace(",", " "));
        }
    }

    private string EndsAtText()
    {
        var left = Math.Max(0, _remainingAtSync - (long)(DateTime.UtcNow - _syncedAt).TotalMilliseconds);
        return DateTime.Now.AddMilliseconds(left).ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private static string LocalTime(long utcMs) =>
        DateTimeOffset.FromUnixTimeMilliseconds(utcMs).LocalDateTime
            .ToString("HH:mm", CultureInfo.InvariantCulture);

    private static string Hms(long ms)
    {
        var t = TimeSpan.FromMilliseconds(Math.Max(0, ms));
        return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
    }

    private void AddRow(string key, string value)
    {
        var grid = new Grid { Margin = new Thickness(0, 2, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var k = new TextBlock
        {
            Text = key, FontSize = 12,
            Foreground = new SolidColorBrush(ClientThemes.Get(_state.Lock.Theme).Sub)
        };
        var v = new TextBlock
        {
            Text = value, FontSize = 12, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(ClientThemes.Get(_state.Lock.Theme).Text)
        };
        Grid.SetColumn(v, 1);
        grid.Children.Add(k);
        grid.Children.Add(v);
        Rows.Children.Add(grid);
    }

    private void Card_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button) return;
        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // sichqoncha tugmasi allaqachon qo'yib yuborilgan
        }
    }

    private void Mini_Click(object sender, RoutedEventArgs e)
    {
        _mini = !_mini;
        ApplyMini();
    }

    private void AskTime_Click(object sender, RoutedEventArgs e) => _agent.Send(PipeTypes.RequestTime);

    private void Logout_Click(object sender, RoutedEventArgs e) => _agent.Send(PipeTypes.Logout);
}
