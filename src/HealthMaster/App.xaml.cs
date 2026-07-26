using System;
using System.IO;
using System.Threading;
using System.Windows;
using HealthMaster.Models;
using HealthMaster.Services;
using HealthMaster.ViewModels;
using HealthMaster.Views;

namespace HealthMaster;

/// <summary>
/// 应用入口与生命周期：单实例、托盘常驻、DI-lite 手工组装、全局异常兜底、资源释放。
/// 无主窗口常驻（ShutdownMode=OnExplicitShutdown），仅从托盘"退出"真正退出。
/// </summary>
public partial class App : Application
{
    private Mutex? _mutex;
    private ConfigStore _configStore = null!;
    private AppConfig _config = null!;
    private ReminderScheduler _scheduler = null!;
    private PowerEventMonitor _powerMonitor = null!;
    private TrayIconService _tray = null!;
    private FloatingWindow _floating = null!;
    private FloatingViewModel _floatingVm = null!;
    private ReminderBadgesViewModel _badgesVm = null!;
    private ReminderBadgeWindow _badges = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // —— 单实例 ——
        _mutex = new Mutex(true, "HealthMaster.SingleInstance", out bool isNew);
        if (!isNew)
        {
            Shutdown();
            return;
        }

        // —— 全局异常兜底（本地日志，不联网）——
        DispatcherUnhandledException += (_, ex) => { Log(ex.Exception); ex.Handled = true; };
        AppDomain.CurrentDomain.UnhandledException += (_, ex) => Log(ex.ExceptionObject as Exception);

        // —— 组装（OnStartup 顺序）——
        _configStore = new ConfigStore();
        _config = _configStore.Load();

        IConfigProvider configProvider = new DefaultConfigProvider(_config);
        var dnd = new DndEvaluator(_config.DoNotDisturb);
        _scheduler = new ReminderScheduler(configProvider, dnd);

        _floatingVm = new FloatingViewModel(_scheduler);
        _floating = new FloatingWindow { DataContext = _floatingVm };
        PlaceFloatingWindow(_floating);
        _floating.PositionChanged += OnFloatingMoved;

        // 到点提醒图标：非打断式，挂在悬浮窗旁竖排冒出，单击即"已完成"
        _badgesVm = new ReminderBadgesViewModel(_scheduler);
        _badges = new ReminderBadgeWindow(_badgesVm.Items);
        _badges.BadgeClicked += OnBadgeCompleted;

        // 悬浮窗不可见时不刷新 VM（无意义的每秒计算）。
        // 悬浮窗常驻不可隐藏，此处仅覆盖启动前 / 退出中等窗体尚未（或不再）可见的时段。
        _scheduler.Tick += now =>
        {
            if (_floating.IsVisible) _floatingVm.Update(now);
        };
        _scheduler.ReminderDue += type => _badgesVm.Show(type);
        _scheduler.RemindersReset += () => _badgesVm.Clear();

        _powerMonitor = new PowerEventMonitor(_scheduler, Dispatcher);
        _tray = new TrayIconService(_scheduler, ExitApp, _configStore.ConfigDirectory);

        _floating.Show();
        _badges.AttachTo(_floating);   // 图标窗跟随悬浮窗移动 / 显隐
        _floatingVm.Update(DateTime.UtcNow);
        _scheduler.Start();
    }

    /// <summary>用户单击提醒图标：该类视为已完成，图标消失并重置计时。</summary>
    private void OnBadgeCompleted(ReminderType type)
    {
        _scheduler.Acknowledge(type);
        _badgesVm.Remove(type);
    }

    private void PlaceFloatingWindow(FloatingWindow w)
    {
        w.WindowStartupLocation = WindowStartupLocation.Manual;
        var area = SystemParameters.WorkArea;

        if (_config.FloatingX is double x && _config.FloatingY is double y)
        {
            w.Left = x;
            w.Top = y;
        }
        else
        {
            // 首启放屏幕右下角安全区
            w.Left = area.Right - 170;
            w.Top = area.Bottom - 70;
        }

        // 尺寸已知后夹紧到工作区内，避免记忆位置落到屏幕外（如分辨率变化）
        w.Loaded += (_, _) => ClampToWorkArea(w);
    }

    private static void ClampToWorkArea(Window w)
    {
        if (w.ActualWidth <= 0 || w.ActualHeight <= 0) return;
        // 用窗口所在那块屏幕的工作区：记忆位置在副屏时，按主屏工作区夹紧会把悬浮窗拽回主屏
        var area = WorkAreaHelper.For(w);
        if (w.Left + w.ActualWidth > area.Right) w.Left = area.Right - w.ActualWidth;
        if (w.Top + w.ActualHeight > area.Bottom) w.Top = area.Bottom - w.ActualHeight;
        if (w.Left < area.Left) w.Left = area.Left;
        if (w.Top < area.Top) w.Top = area.Top;
    }

    private void OnFloatingMoved(double left, double top)
    {
        _config.FloatingX = left;
        _config.FloatingY = top;
        // 每次拖动完成仅定向保存位置（不整份回写，避免覆盖用户手改的勿扰 / 间隔）
        _configStore.SaveFloatingPosition(left, top);
    }

    private void ExitApp() => Shutdown();

    /// <summary>仅定向保存悬浮窗位置，绝不整份回写内存配置。</summary>
    private void SaveFloatingPositionSafe()
    {
        try
        {
            if (_floating != null)
                _configStore.SaveFloatingPosition(_floating.Left, _floating.Top);
        }
        catch { /* 忽略 */ }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _scheduler?.Stop();
            _badges?.Detach();
            _powerMonitor?.Dispose();
            _tray?.Dispose();
            SaveFloatingPositionSafe();
        }
        finally
        {
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }

    private void Log(Exception? ex)
    {
        if (ex == null) return;
        try
        {
            var dir = _configStore?.LogDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HealthMaster", "logs");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"error-{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(file, $"[{DateTime.Now:HH:mm:ss}] {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { /* 日志失败不致命 */ }
    }
}
