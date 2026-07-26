using System;
using System.Windows.Threading;
using Microsoft.Win32;

namespace HealthMaster.Services;

/// <summary>
/// 订阅电源 / 会话事件，休眠唤醒或解锁后立即重算一次（不必等下一秒），响应更跟手。
/// 事件在系统专用线程触发，通过 Dispatcher 编组回 UI 线程。
/// </summary>
public sealed class PowerEventMonitor : IDisposable
{
    private readonly IReminderScheduler _scheduler;
    private readonly Dispatcher _dispatcher;

    public PowerEventMonitor(IReminderScheduler scheduler, Dispatcher dispatcher)
    {
        _scheduler = scheduler;
        _dispatcher = dispatcher;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
            _dispatcher.BeginInvoke(new Action(_scheduler.RecalculateNow));
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock)
            _dispatcher.BeginInvoke(new Action(_scheduler.RecalculateNow));
    }

    public void Dispose()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
    }
}
