using System.Windows;
using System.Windows.Interop;
using Dust2Agent.Common;
using WinForms = System.Windows.Forms;

namespace Dust2Agent.Shell;

/// <summary>
/// Foydalanuvchi sessiyasidagi qobiq. Xizmat bilan named pipe orqali gaplashadi
/// va ekranda qulf oynasi yoki taymer oynachasini ko'rsatadi.
/// </summary>
public partial class App : Application
{
    private readonly AgentClient _agent = new();
    private readonly List<LockWindow> _locks = new();
    private readonly Dictionary<LockWindow, System.Drawing.Rectangle> _lockBounds = new();
    private WidgetWindow? _widget;
    private NoticeWindow? _notice;
    private AgentLog? _log;
    private Mutex? _single;
    private InputBlocker? _blocker;

    /// <summary>Foydalanuvchi dasturni to'xtatishni so'radi — holat xabarlariga javob bermaymiz.</summary>
    private bool _stopping;

    /// <summary>To'xtatilgan klient bir marta qayta ishga tushiriladi.</summary>
    private bool _resumeSent;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Local\ — sessiya ichida yagona nusxa. Global\ bo'lsa oddiy foydalanuvchida
        // SeCreateGlobalPrivilege yo'qligi sababli istisno tashlanardi va qobiq ishga
        // tushmay, xizmat uni qayta-qayta ochishga urinardi.
        try
        {
            _single = new Mutex(true, @"Local\Dust2AgentShell", out var isNew);
            if (!isNew)
            {
                Shutdown();
                return;
            }
        }
        catch (Exception)
        {
            // Mutex yaratib bo'lmasa ham qobiq ishlayversin
        }

        _log = new AgentLog("shell", AgentPaths.UserLogs);
        _log.Info("Qobiq ishga tushdi");

        if (OperatingSystem.IsWindows())
        {
            _blocker = new InputBlocker(_log);
            // Failsafe: o'tgan safar qobiq yiqilgan bo'lsa siyosat qolib ketgan bo'lishi mumkin
            TaskManagerPolicy.Apply(_log, disable: false);
        }

        DispatcherUnhandledException += (_, args) =>
        {
            _log?.Error("Qobiqda kutilmagan xato", args.Exception);
            args.Handled = true;
            ShowFault(args.Exception.Message);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            _log?.Error("Qobiqda kutilmagan xato (oqim)", args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            _log?.Error("Qobiqda kuzatilmagan vazifa xatosi", args.Exception);
            args.SetObserved();
        };

        CreateWindows();

        foreach (var w in _locks) w.StopRequested += StopClient;

        _agent.StateChanged += OnState;
        _agent.Toast += Notify;
        _agent.AdminMessage += NotifyMessage;
        _agent.UnlockResult += ok => Primary()?.OnPasswordChecked(ok, unlock: true);
        _agent.PasswordResult += ok => Primary()?.OnPasswordChecked(ok, unlock: false);
        _agent.ConnectionChanged += connected =>
        {
            if (!connected) _log?.Warn("Xizmat bilan aloqa yo'q");
            foreach (var w in _locks) w.SetServiceConnected(connected);
        };
        foreach (var w in _locks) w.SetServiceConnected(false);
        _agent.Start();
    }

    private LockWindow? Primary() => _locks.FirstOrDefault();

    /// <summary>Oxirgi holatdagi ekran — xabar qaysi oynaga borishini shu hal qiladi.</summary>
    private string _screen = "";

    /// <summary>
    /// Bildirishnomani ko'rinadigan oynaga yuboradi.
    ///
    /// Seans ochiq bo'lsa qulf oynasi yashirin — unga yozilgan xabarni mijoz
    /// ko'rmaydi. Shuning uchun o'sha holatda alohida bildirishnoma oynasi
    /// ishlatiladi ([qaror 21](../../../docs/QARORLAR.md)).
    /// </summary>
    private void Notify(ToastPayload t)
    {
        if (ShellScreen.NoticeInOwnWindow(_screen)) _notice?.ShowToast(t);
        else Primary()?.ShowToast(t);
    }

    private void NotifyMessage(string text)
    {
        if (ShellScreen.NoticeInOwnWindow(_screen)) _notice?.ShowMessage(text);
        else Primary()?.ShowMessage(text);
    }

    private DateTime _lastFault = DateTime.MinValue;

    /// <summary>
    /// Ichki xatoni ekranda ko'rsatadi. Ilgari ular faqat jurnalga tushardi va
    /// foydalanuvchi uchun "tugma ishlamayapti" ko'rinishida bo'lardi.
    /// </summary>
    private void ShowFault(string message)
    {
        if ((DateTime.UtcNow - _lastFault).TotalSeconds < 10) return;
        _lastFault = DateTime.UtcNow;
        Notify(new ToastPayload
        {
            Title = "Qobiqda xato",
            Text = message.Length > 160 ? message[..160] : message,
            Kind = "warn"
        });
    }

    private void CreateWindows()
    {
        // Har bir monitorda bittadan qulf oynasi; kirish maydonlari asosiysida
        var screens = WinForms.Screen.AllScreens;
        for (var i = 0; i < screens.Length; i++)
        {
            var s = screens[i];
            var w = new LockWindow(_agent, primary: s.Primary || (i == 0 && screens.All(x => !x.Primary)))
            {
                WindowStartupLocation = WindowStartupLocation.Manual
            };
            if (w.IsPrimaryScreen) _locks.Insert(0, w);
            else _locks.Add(w);
            w.Show();
            _lockBounds[w] = s.Bounds;
            PlaceOnScreen(w, s.Bounds);
        }

        // Xizmat ishga tushirgan jarayon o'z-o'zidan "oldingi" bo'lmaydi: oynani
        // ataylab oldinga chiqaramiz, aks holda klaviatura unga tushmaydi.
        var main = Primary();
        if (main is not null)
        {
            main.Activate();
            Native.ForceForeground(main);
        }

        _widget = new WidgetWindow(_agent);
        _notice = new NoticeWindow();
    }

    /// <summary>
    /// Oynani monitor chegarasiga aniq joylaydi. Screen.Bounds fizik pikselda, WPF esa
    /// DIP bilan ishlaydi — masshtab 100% dan farq qilsa (PerMonitorV2) ular teng emas,
    /// shuning uchun joylashtirishni Win32 bajaradi.
    /// </summary>
    private static void PlaceOnScreen(Window w, System.Drawing.Rectangle b)
    {
        var hwnd = new WindowInteropHelper(w).Handle;
        if (hwnd == IntPtr.Zero) return;
        Native.SetWindowPos(hwnd, IntPtr.Zero, b.Left, b.Top, b.Width, b.Height,
            Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
    }

    /// <summary>
    /// Kiritishni bloklash faqat qulf ekrani ko'rsatilayotganda ishlaydi: seans
    /// ochiq bo'lsa ham, ulanish oynasida ham kompyuter odatdagidek ishlaydi.
    /// </summary>
    private void ApplyInputBlock(ShellState s)
    {
        if (_blocker is null || !OperatingSystem.IsWindows()) return;

        var block = s.Behaviour.BlockInput && s.Screen == "locked" && !s.Paused;
        _blocker.UnlockCombo = string.IsNullOrWhiteSpace(s.UnlockCombo) ? "Ctrl+Alt+P" : s.UnlockCombo;
        if (block == _blocker.Active) return;

        _blocker.SetActive(block);
        TaskManagerPolicy.Apply(_log!, disable: block);
    }

    /// <summary>
    /// "Klient dasturini to'xtatish": xizmat qulf ekranini ko'rsatishni to'xtatadi va
    /// qobiqni qayta ochmaydi. Qobiq yorliq orqali ochilganda hammasi tiklanadi.
    /// </summary>
    private void StopClient()
    {
        _stopping = true;
        _log?.Info("Foydalanuvchi klient dasturini to'xtatdi");
        _agent.Send(PipeTypes.Pause);
        // Xabar quvurga yozilib ulgurishi uchun qisqa kechikish
        Task.Delay(400).ContinueWith(_ => Dispatcher.Invoke(() => Shutdown()));
    }

    private void OnState(ShellState s)
    {
        if (_stopping) return;

        ApplyInputBlock(s);

        // Dastur to'xtatilgan edi va qobiq qo'lda ochildi — demak uni tiklash kerak
        if (s.Paused && !_resumeSent)
        {
            _resumeSent = true;
            _log?.Info("Klient to'xtatilgan edi — qayta ishga tushirilmoqda");
            _agent.Send(PipeTypes.Resume);
            return;
        }

        _screen = s.Screen;
        // Qulf ekraniga o'tildi — bildirishnoma qulf oynasi ostida qolib ketmasin
        if (!ShellScreen.NoticeInOwnWindow(s.Screen)) _notice?.Dismiss();

        foreach (var w in _locks)
        {
            w.Apply(s);
            if (s.Screen == "session")
            {
                w.Hide();
            }
            else if (!w.IsVisible)
            {
                // Seansdan keyin qayta ko'rsatilganda WPF oynani standart o'lchamiga
                // qaytaradi — shuning uchun uni yana monitor chegarasiga joylaymiz,
                // aks holda qulf ekrani butun ekranni qoplamaydi.
                w.Show();
                w.Activate();
                if (_lockBounds.TryGetValue(w, out var b)) PlaceOnScreen(w, b);
            }
        }

        // Taymer oynachasi adminda o'chirilgan bo'lishi mumkin (Kompyuter xatti-harakati).
        // Seans ochiq paytda ekranga boshqa hech narsa qo'yilmaydi — kompyuter nomi
        // faqat qulf ekranida, soat bilan birga ko'rsatiladi.
        if (s.Screen == "session" && s.Behaviour.ShowWidget) _widget?.Apply(s);
        else _widget?.Hide();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (OperatingSystem.IsWindows())
        {
            _blocker?.Dispose();
            TaskManagerPolicy.Apply(_log!, disable: false);
        }
        _notice?.Dismiss();
        _agent.Stop();
        _log?.Info("Qobiq yopildi");
        _single?.Dispose();
        base.OnExit(e);
    }
}
