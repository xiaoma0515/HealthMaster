using System;

namespace HealthMaster.Models;

/// <summary>提醒的运行时状态（每类一份，可变）。</summary>
public sealed class ReminderState
{
    public ReminderType Type { get; init; }

    /// <summary>
    /// 绝对到点时间（UTC 墙钟）。用绝对时间判定到点，而非累计 tick——
    /// 这是正确处理系统休眠 / 唤醒的核心。
    /// </summary>
    public DateTime NextDueUtc { get; set; }

    /// <summary>
    /// 是否已被"挂起"：到点后等待用户确认期间为 true，避免同一类重复触发 / 重复入队。
    /// </summary>
    public bool IsHeld { get; set; }

    /// <summary>
    /// 在勿扰时段内到点被抑制，标记为"错过"，勿扰结束后补偿提醒一次（只一次，不连环）。
    /// </summary>
    public bool MissedDuringDnd { get; set; }

    /// <summary>距离下次到点的剩余时间（不为负）。</summary>
    public TimeSpan Remaining(DateTime nowUtc) =>
        NextDueUtc > nowUtc ? NextDueUtc - nowUtc : TimeSpan.Zero;
}
