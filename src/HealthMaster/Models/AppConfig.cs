using System.Collections.Generic;

namespace HealthMaster.Models;

/// <summary>
/// 本地持久化配置（<c>%APPDATA%\HealthMaster\config.json</c>）。
/// 红线：纯本地，不联网、不上传。
/// </summary>
public sealed class AppConfig
{
    public int SchedulerVersion { get; set; } = 1;

    /// <summary>
    /// 可选的间隔覆盖（分钟），键为 <see cref="ReminderType"/> 名称（如 "Eye"）。
    /// 为空则用内置默认值——这是"间隔可配置"的预留接缝，v1 无设置界面，用户可直接改 JSON。
    /// </summary>
    public Dictionary<string, int> IntervalMinutes { get; set; } = new();

    /// <summary>悬浮窗上次位置（跨重启记忆）。</summary>
    public double? FloatingX { get; set; }
    public double? FloatingY { get; set; }

    /// <summary>夜间勿扰时段。</summary>
    public DndConfig DoNotDisturb { get; set; } = new();

    /// <summary>
    /// 提醒提示音总开关（v2.1，**默认开启**）。托盘菜单可即时切换并持久化。
    /// 老配置文件里没有这个键时，反序列化不会赋值，属性保持这里的初始值 true——即「默认开启」。
    /// </summary>
    public bool SoundEnabled { get; set; } = true;
}

/// <summary>夜间勿扰时段配置。</summary>
public sealed class DndConfig
{
    /// <summary>是否启用勿扰时段。</summary>
    public bool Enabled { get; set; }

    /// <summary>起始时刻，格式 "HH:mm"（本地时间）。</summary>
    public string Start { get; set; } = "22:00";

    /// <summary>结束时刻，格式 "HH:mm"（本地时间，可跨零点，如 22:00→07:00）。</summary>
    public string End { get; set; } = "07:00";
}
