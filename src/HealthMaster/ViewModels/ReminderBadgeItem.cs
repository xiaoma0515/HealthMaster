using HealthMaster.Models;

namespace HealthMaster.ViewModels;

/// <summary>
/// 一枚"到点提醒图标"的可绑定数据（不可变，冒出后内容不变，故无需 INotifyPropertyChanged）。
/// 图标字形来自系统自带 Segoe UI Emoji，不引入任何外部图片文件。
/// </summary>
public sealed class ReminderBadgeItem
{
    public ReminderBadgeItem(ReminderType type, string glyph, string accent, string tooltip)
    {
        Type = type;
        Glyph = glyph;
        Accent = accent;
        Tooltip = tooltip;
    }

    /// <summary>所属提醒类别（单击后据此重置对应计时）。</summary>
    public ReminderType Type { get; }

    /// <summary>图标字形（emoji）。</summary>
    public string Glyph { get; }

    /// <summary>圆底主题色（#AARRGGBB），用于区分四类。</summary>
    public string Accent { get; }

    /// <summary>鼠标悬停提示：类别 + 一句建议 + 操作说明。</summary>
    public string Tooltip { get; }
}
