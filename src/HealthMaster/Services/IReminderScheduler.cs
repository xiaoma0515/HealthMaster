using System;
using System.Collections.Generic;
using HealthMaster.Models;

namespace HealthMaster.Services;

/// <summary>四路提醒调度器：单一 1Hz 时钟、绝对墙钟到点判定、到点事件与倒计时刷新。</summary>
public interface IReminderScheduler
{
    /// <summary>每秒触发一次，携带当前 UTC 时间，供悬浮窗刷新倒计时。</summary>
    event Action<DateTime>? Tick;

    /// <summary>某类到点，需要在悬浮窗旁冒出该类提醒图标。</summary>
    event Action<ReminderType>? ReminderDue;

    /// <summary>
    /// 本次评估（同一调度批次）中至少有一类新到点并冒出了图标，无论一类还是四类**只触发一次**。
    /// 用于播放提醒音——四类同时到点时不叠加四声 2.9s 的长音。
    /// 刻意做成批次级事件而不是在 UI 侧用时间窗去猜：批次边界就是一次 <c>Evaluate</c>。
    /// 勿扰期间被抑制的到点、以及进入勿扰时清空残留图标，都**不会**触发本事件（自然静音）。
    /// </summary>
    event Action? RemindersDueBatch;

    /// <summary>
    /// 已冒出的提醒图标应全部清空：暂停 / 恢复全部，或进入夜间勿扰时段时触发。
    /// （进入勿扰时清空的那些类会被标记为"错过"，勿扰结束后补偿冒出一次。）
    /// </summary>
    event Action? RemindersReset;

    IReadOnlyList<ReminderState> States { get; }
    ReminderDefinition GetDefinition(ReminderType type);
    bool IsAllPaused { get; }

    /// <summary>该类是否已到点、图标正挂着等待用户单击确认（此期间无倒计时可显示）。</summary>
    bool IsHeld(ReminderType type);

    void Start();
    void Stop();

    /// <summary>用户单击提醒图标（视为"已完成"）：重置该类计时（now + Interval）。</summary>
    void Acknowledge(ReminderType type);

    void PauseAll();
    void ResumeAll();

    /// <summary>唤醒 / 解锁后立即重算一次，不必等下一秒。</summary>
    void RecalculateNow();

    TimeSpan Remaining(ReminderType type, DateTime nowUtc);
}
