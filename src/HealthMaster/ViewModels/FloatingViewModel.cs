using System;
using System.Collections.Generic;
using HealthMaster.Models;
using HealthMaster.Resources;
using HealthMaster.Services;

namespace HealthMaster.ViewModels;

/// <summary>
/// 悬浮窗可绑定数据：展开态四行结构化倒计时 + 折叠态最近一项。
/// 每行是一个 <see cref="CountdownRowViewModel"/>（名称 / 时间 / 状态 / 强调色键分开），
/// XAML 据此做固定宽右对齐时间列；本类自身无可变属性，故不需要 INotifyPropertyChanged。
/// </summary>
public sealed class FloatingViewModel
{
    private static readonly ReminderType[] AllTypes =
        { ReminderType.Eye, ReminderType.Sedentary, ReminderType.Water, ReminderType.Exercise };

    private readonly IReminderScheduler _scheduler;

    public FloatingViewModel(IReminderScheduler scheduler)
    {
        _scheduler = scheduler;

        Eye = NewRow(ReminderType.Eye);
        Sedentary = NewRow(ReminderType.Sedentary);
        Water = NewRow(ReminderType.Water);
        Exercise = NewRow(ReminderType.Exercise);
        Rows = new[] { Eye, Sedentary, Water, Exercise };

        Compact = new CountdownRowViewModel(null, Strings.FloatingCollapsedHint, ThemeKeys.AccentNeutral);
    }

    /// <summary>展开态第 1 行。</summary>
    public CountdownRowViewModel Eye { get; }

    /// <summary>展开态第 2 行。</summary>
    public CountdownRowViewModel Sedentary { get; }

    /// <summary>展开态第 3 行。</summary>
    public CountdownRowViewModel Water { get; }

    /// <summary>展开态第 4 行。</summary>
    public CountdownRowViewModel Exercise { get; }

    /// <summary>展开态四行，顺序恒为 护眼 → 久坐 → 补水 → 运动（与提醒图标竖排顺序一致）。</summary>
    public IReadOnlyList<CountdownRowViewModel> Rows { get; }

    /// <summary>折叠态显示的「最近一项」（同一实例复用，随时间在四类之间切换）。</summary>
    public CountdownRowViewModel Compact { get; }

    /// <summary>展开态小标题。</summary>
    public string Header => Strings.FloatingExpandedHeader;

    /// <summary>每秒由 Scheduler.Tick 调用（悬浮窗不可见时上层不调用），刷新倒计时。</summary>
    public void Update(DateTime nowUtc)
    {
        bool paused = _scheduler.IsAllPaused;

        foreach (var row in Rows) UpdateRow(row, nowUtc, paused);

        if (paused)
        {
            Compact.SetStatus(null, "", ThemeKeys.AccentNeutral, Strings.PausedLabel,
                held: false, paused: true);
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

        if (nearest is { } n)
            Compact.SetCountdown(n, _scheduler.GetDefinition(n).ShortName, AccentKeyFor(n), Format(best));
        else
            // 四类图标全挂着，暂无倒计时可显示
            Compact.SetStatus(null, "", ThemeKeys.AccentNeutral, Strings.HeldLabel,
                held: true, paused: false);
    }

    private void UpdateRow(CountdownRowViewModel row, DateTime nowUtc, bool paused)
    {
        var type = row.Type!.Value;   // 四行的 Type 恒定不为 null
        var accent = AccentKeyFor(type);

        if (paused)
        {
            row.SetStatus(type, row.Name, accent, Strings.PausedLabel, held: false, paused: true);
            return;
        }

        // 图标挂着期间该类没有"下一次"，显示 00:00 会误导，改为"待完成"
        if (_scheduler.IsHeld(type))
            row.SetStatus(type, row.Name, accent, Strings.HeldLabel, held: true, paused: false);
        else
            row.SetCountdown(type, row.Name, accent, Format(_scheduler.Remaining(type, nowUtc)));
    }

    private CountdownRowViewModel NewRow(ReminderType type) =>
        new(type, _scheduler.GetDefinition(type).ShortName, AccentKeyFor(type));

    private static string AccentKeyFor(ReminderType type) => type switch
    {
        ReminderType.Eye => ThemeKeys.AccentEye,
        ReminderType.Sedentary => ThemeKeys.AccentSedentary,
        ReminderType.Water => ThemeKeys.AccentWater,
        ReminderType.Exercise => ThemeKeys.AccentExercise,
        _ => ThemeKeys.AccentNeutral,
    };

    private static string Format(TimeSpan r)
    {
        if (r < TimeSpan.Zero) r = TimeSpan.Zero;
        return r.TotalHours >= 1
            ? $"{(int)r.TotalHours}:{r.Minutes:00}:{r.Seconds:00}"
            : $"{r.Minutes:00}:{r.Seconds:00}";
    }
}
