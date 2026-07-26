using System;
using HealthMaster.Models;

namespace HealthMaster.Services;

/// <summary>夜间勿扰时段判定（本地时间，支持跨零点，如 22:00 → 07:00）。</summary>
public sealed class DndEvaluator
{
    private readonly DndConfig _cfg;

    public DndEvaluator(DndConfig cfg) => _cfg = cfg;

    /// <summary>给定本地时间，是否处于勿扰时段内。</summary>
    public bool IsInWindow(DateTime localNow)
    {
        if (!_cfg.Enabled) return false;
        if (!TimeOnly.TryParse(_cfg.Start, out var start)) return false;
        if (!TimeOnly.TryParse(_cfg.End, out var end)) return false;
        if (start == end) return false; // 起止相同视为未设置

        var t = TimeOnly.FromDateTime(localNow);
        return start < end
            ? t >= start && t < end     // 同日区间
            : t >= start || t < end;    // 跨零点区间
    }
}
