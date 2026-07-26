using System.Collections.Generic;
using System.Windows.Media;
using HealthMaster.Models;

namespace HealthMaster.ViewModels;

/// <summary>
/// 一枚"到点提醒图标"的可绑定数据（不可变，冒出后内容不变，故无需 INotifyPropertyChanged）。
/// 图标为内置矢量路径（<see cref="Resources.IconGeometries"/>），不引入任何外部图片或字体文件。
/// </summary>
public sealed class ReminderBadgeItem
{
    public ReminderBadgeItem(ReminderType type, string iconData, string tooltip)
    {
        Type = type;
        Icon = GetGeometry(iconData);
        Tooltip = tooltip;
    }

    /// <summary>所属提醒类别（单击后据此重置对应计时）。</summary>
    public ReminderType Type { get; }

    /// <summary>图标矢量几何（已 Freeze，可跨图标共享），绑定到 <c>Path.Data</c>。</summary>
    public Geometry Icon { get; }

    // 圆底配色不作为数据字段：Themes\Controls.xaml 按 Type 触发器映射 Brush.Badge.* 资源取色。

    /// <summary>鼠标悬停提示：类别 + 一句建议 + 操作说明。</summary>
    public string Tooltip { get; }

    // 四类路径数据固定，解析一次后冻结缓存复用：避免每次冒图标重复解析，也免去变更通知开销。
    // 仅在 UI 线程（图标增减）访问，无需加锁。
    private static readonly Dictionary<string, Geometry> GeometryCache = new();

    private static Geometry GetGeometry(string data)
    {
        if (GeometryCache.TryGetValue(data, out var cached)) return cached;

        var geometry = Geometry.Parse(data);
        geometry.Freeze();
        GeometryCache[data] = geometry;
        return geometry;
    }
}
