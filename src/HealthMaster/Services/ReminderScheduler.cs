using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using HealthMaster.Models;

namespace HealthMaster.Services;

/// <summary>
/// 核心调度：单一 <see cref="DispatcherTimer"/>（1Hz）驱动全部倒计时；
/// 用绝对墙钟时间（<see cref="DateTime.UtcNow"/>）判定到点，天然正确处理休眠 / 唤醒；
/// 勿扰时段内抑制并在结束后补偿一次；用户单击提醒图标（已完成）后重置计时。
/// </summary>
public sealed class ReminderScheduler : IReminderScheduler
{
    private readonly Dictionary<ReminderType, ReminderDefinition> _defs;
    private readonly Dictionary<ReminderType, ReminderState> _states;
    private readonly DndEvaluator _dnd;
    private readonly DispatcherTimer _timer;

    private bool _globalPaused;
    private bool _wasInDnd;

    public event Action<DateTime>? Tick;
    public event Action<ReminderType>? ReminderDue;
    public event Action? RemindersDueBatch;
    public event Action? RemindersReset;

    public ReminderScheduler(IConfigProvider config, DndEvaluator dnd)
    {
        _dnd = dnd;
        _defs = config.GetDefinitions().ToDictionary(d => d.Type);

        var now = DateTime.UtcNow;
        _states = _defs.Values.ToDictionary(
            d => d.Type,
            d => new ReminderState { Type = d.Type, NextDueUtc = now + d.Interval });

        _timer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (_, _) => Evaluate(DateTime.UtcNow, DateTime.Now);
    }

    public IReadOnlyList<ReminderState> States => _states.Values.ToList();
    public bool IsAllPaused => _globalPaused;
    public ReminderDefinition GetDefinition(ReminderType type) => _defs[type];
    public bool IsHeld(ReminderType type) => _states[type].IsHeld;
    public TimeSpan Remaining(ReminderType type, DateTime nowUtc) => _states[type].Remaining(nowUtc);

    public void Start()
    {
        _wasInDnd = _dnd.IsInWindow(DateTime.Now);
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    /// <summary>核心：单时钟的一次评估。到点检查 + 勿扰补偿 + 通知 UI 刷新。</summary>
    private void Evaluate(DateTime nowUtc, DateTime nowLocal)
    {
        if (!_globalPaused)
        {
            bool inDnd = _dnd.IsInWindow(nowLocal);
            bool dndJustStarted = !_wasInDnd && inDnd;
            bool dndJustEnded = _wasInDnd && !inDnd;
            _wasInDnd = inDnd;

            if (dndJustStarted)
            {
                // 进入勿扰：清掉残留在屏幕上的图标（勿扰期间不该有任何提醒可见）。
                // 这些类仍保持 IsHeld，并统一标记为"错过"，由勿扰结束时的补偿逻辑重新冒出。
                foreach (var st in _states.Values)
                {
                    if (st.IsHeld) st.MissedDuringDnd = true;
                }
                RemindersReset?.Invoke();
            }

            // 本次评估是否有新图标冒出：多类同时到点也只算一批，批次末尾统一发一次
            // RemindersDueBatch（提醒音只响一声，见 IReminderScheduler 的说明）。
            bool anyDue = false;

            foreach (var st in _states.Values)
            {
                if (st.IsHeld) continue;              // 图标已挂出、等待用户单击期间不重复触发
                if (nowUtc >= st.NextDueUtc)
                {
                    st.IsHeld = true;                 // 挂起
                    if (inDnd)
                        st.MissedDuringDnd = true;    // 勿扰内抑制，不冒图标，稍后补偿一次
                    else
                    {
                        ReminderDue?.Invoke(st.Type); // 正常到点，冒出图标
                        anyDue = true;
                    }
                }
            }

            if (dndJustEnded)
            {
                // 勿扰结束：错过的每类只补偿提醒一次（不按错过周期数连环冒图标）
                foreach (var st in _states.Values)
                {
                    if (st.MissedDuringDnd)
                    {
                        st.MissedDuringDnd = false;
                        ReminderDue?.Invoke(st.Type);
                        anyDue = true;                // 补偿也是正常提醒，该出声
                    }
                }
            }

            if (anyDue) RemindersDueBatch?.Invoke();
        }

        Tick?.Invoke(nowUtc);
    }

    public void Acknowledge(ReminderType type)
    {
        var st = _states[type];
        st.NextDueUtc = DateTime.UtcNow + _defs[type].Interval;
        st.IsHeld = false;
        st.MissedDuringDnd = false;
    }

    public void PauseAll()
    {
        _globalPaused = true;
        // 图标被清空，模型侧的"挂起 / 错过"标记必须同步清掉，否则恢复前 UI 与模型不一致
        foreach (var st in _states.Values)
        {
            st.IsHeld = false;
            st.MissedDuringDnd = false;
        }
        RemindersReset?.Invoke();   // 已冒出的图标随之清空
    }

    public void ResumeAll()
    {
        _globalPaused = false;
        var now = DateTime.UtcNow;
        foreach (var st in _states.Values)
        {
            st.NextDueUtc = now + _defs[st.Type].Interval;
            st.IsHeld = false;
            st.MissedDuringDnd = false;
        }
        _wasInDnd = _dnd.IsInWindow(DateTime.Now);
        RemindersReset?.Invoke();   // 已冒出的图标随之清空
    }

    public void RecalculateNow() => Evaluate(DateTime.UtcNow, DateTime.Now);
}
