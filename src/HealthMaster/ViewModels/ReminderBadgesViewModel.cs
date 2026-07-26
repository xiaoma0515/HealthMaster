using System;
using System.Collections.ObjectModel;
using HealthMaster.Models;
using HealthMaster.Resources;
using HealthMaster.Services;

namespace HealthMaster.ViewModels;

/// <summary>
/// 到点提醒图标集合：某类到点即冒出一枚图标，用户单击（表示已完成）后移除。
/// 始终按固定顺序（护眼 / 久坐 / 补水 / 运动）从上到下竖排，多类同时到点时顺序稳定。
/// </summary>
public sealed class ReminderBadgesViewModel
{
    /// <summary>竖排的固定顺序，与悬浮窗展开态的行序一致。</summary>
    private static readonly ReminderType[] Order =
        { ReminderType.Eye, ReminderType.Sedentary, ReminderType.Water, ReminderType.Exercise };

    // 四类圆底配色不在这里：由 Themes\Controls.xaml 按 ReminderBadgeItem.Type 静态映射到
    // Themes\Dark.xaml 的 Brush.Badge.* 资源。改色值请改那两处，别在 ViewModel 里加颜色字段。

    private readonly IReminderScheduler _scheduler;

    public ReminderBadgesViewModel(IReminderScheduler scheduler) => _scheduler = scheduler;

    public ObservableCollection<ReminderBadgeItem> Items { get; } = new();

    /// <summary>冒出某类图标（已存在则忽略）。</summary>
    public void Show(ReminderType type)
    {
        if (IndexOf(type) >= 0) return;

        var def = _scheduler.GetDefinition(type);
        var item = new ReminderBadgeItem(
            type,
            def.IconData,
            $"{def.DisplayName}：{def.Body}{Environment.NewLine}{Strings.BadgeClickHint}");

        Items.Insert(InsertIndexFor(type), item);
    }

    /// <summary>移除某类图标（用户已完成）。</summary>
    public void Remove(ReminderType type)
    {
        int i = IndexOf(type);
        if (i >= 0) Items.RemoveAt(i);
    }

    /// <summary>清空全部图标（暂停 / 恢复全部时）。</summary>
    public void Clear() => Items.Clear();

    private int IndexOf(ReminderType type)
    {
        for (int i = 0; i < Items.Count; i++)
            if (Items[i].Type == type) return i;
        return -1;
    }

    /// <summary>按 <see cref="Order"/> 求插入位置，保证竖排顺序恒定。</summary>
    private int InsertIndexFor(ReminderType type)
    {
        int rank = Array.IndexOf(Order, type);
        for (int i = 0; i < Items.Count; i++)
            if (Array.IndexOf(Order, Items[i].Type) > rank) return i;
        return Items.Count;
    }
}
