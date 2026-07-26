using System;

namespace HealthMaster.Models;

/// <summary>
/// 提醒的静态定义（名称 / 间隔 / 文案）。来自配置，v1 由 <c>DefaultConfigProvider</c> 提供。
/// </summary>
public sealed record ReminderDefinition(
    ReminderType Type,
    string ShortName,     // 悬浮窗紧凑标签，如 "护眼"
    string DisplayName,   // 完整名称，如 "护眼提醒"
    TimeSpan Interval,    // 提醒间隔
    string Body,          // 一句可执行建议（用于提醒图标 tooltip 的第二行）
    string IconData);     // 提醒图标的矢量路径数据（24×24 视框，见 Resources.IconGeometries，无外部图片文件）
