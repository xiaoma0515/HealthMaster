using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HealthMaster.Models;
using HealthMaster.Resources;
using HealthMaster.Services;

namespace HealthMaster.ViewModels;

/// <summary>悬浮窗可绑定数据：四路倒计时文本 + 折叠态紧凑文本。</summary>
public sealed class FloatingViewModel : INotifyPropertyChanged
{
    private static readonly ReminderType[] AllTypes =
        { ReminderType.Eye, ReminderType.Sedentary, ReminderType.Water, ReminderType.Exercise };

    private readonly IReminderScheduler _scheduler;

    public FloatingViewModel(IReminderScheduler scheduler) => _scheduler = scheduler;

    private string _eye = "";
    private string _sedentary = "";
    private string _water = "";
    private string _exercise = "";
    private string _compact = Strings.FloatingCollapsedHint;

    public string Eye { get => _eye; private set => Set(ref _eye, value); }
    public string Sedentary { get => _sedentary; private set => Set(ref _sedentary, value); }
    public string Water { get => _water; private set => Set(ref _water, value); }
    public string Exercise { get => _exercise; private set => Set(ref _exercise, value); }
    public string Compact { get => _compact; private set => Set(ref _compact, value); }
    public string Header => Strings.FloatingExpandedHeader;

    /// <summary>每秒由 Scheduler.Tick 调用，刷新倒计时文本。</summary>
    public void Update(DateTime nowUtc)
    {
        bool paused = _scheduler.IsAllPaused;

        Eye = Row(ReminderType.Eye, nowUtc, paused);
        Sedentary = Row(ReminderType.Sedentary, nowUtc, paused);
        Water = Row(ReminderType.Water, nowUtc, paused);
        Exercise = Row(ReminderType.Exercise, nowUtc, paused);

        if (paused)
        {
            Compact = Strings.PausedLabel;
            return;
        }

        // 折叠态显示最近一项。已挂出图标（IsHeld）的类倒计时恒为 0，
        // 若参与比较会把折叠态永久锁死在 00:00，故先排除，只在其余类中挑最近的。
        ReminderType? nearest = null;
        var best = TimeSpan.MaxValue;
        foreach (var t in AllTypes)
        {
            if (_scheduler.IsHeld(t)) continue;
            var r = _scheduler.Remaining(t, nowUtc);
            if (r < best) { best = r; nearest = t; }
        }

        Compact = nearest is { } n
            ? $"{_scheduler.GetDefinition(n).ShortName} {Format(best)}"
            : Strings.HeldLabel;   // 四类图标全挂着，暂无倒计时可显示
    }

    private string Row(ReminderType type, DateTime nowUtc, bool paused)
    {
        var name = _scheduler.GetDefinition(type).ShortName;
        if (paused) return $"{name}   {Strings.PausedLabel}";
        // 图标挂着期间该类没有"下一次"，显示 00:00 会误导，改为"待完成"
        return _scheduler.IsHeld(type)
            ? $"{name}   {Strings.HeldLabel}"
            : $"{name}   {Format(_scheduler.Remaining(type, nowUtc))}";
    }

    private static string Format(TimeSpan r)
    {
        if (r < TimeSpan.Zero) r = TimeSpan.Zero;
        return r.TotalHours >= 1
            ? $"{(int)r.TotalHours}:{r.Minutes:00}:{r.Seconds:00}"
            : $"{r.Minutes:00}:{r.Seconds:00}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set(ref string field, string value, [CallerMemberName] string? name = null)
    {
        if (field == value) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
