using System;
using System.Collections.Generic;
using HealthMaster.Models;
using HealthMaster.Resources;

namespace HealthMaster.Services;

/// <summary>
/// v1 默认定义：间隔与文案硬编码（依据通行健康指引）；
/// 若 <see cref="AppConfig.IntervalMinutes"/> 提供了覆盖值则采用之。
/// 默认间隔：护眼 20、久坐 45、补水 60、运动 120（分钟）。
/// </summary>
public sealed class DefaultConfigProvider : IConfigProvider
{
    private readonly AppConfig _config;

    public DefaultConfigProvider(AppConfig config) => _config = config;

    public IReadOnlyList<ReminderDefinition> GetDefinitions()
    {
        return new[]
        {
            Build(ReminderType.Eye,       "护眼", "护眼提醒",  20, IconGeometries.Eye,       Strings.EyeBody),
            Build(ReminderType.Sedentary, "久坐", "久坐提醒",  45, IconGeometries.Sedentary, Strings.SedentaryBody),
            Build(ReminderType.Water,     "补水", "补水提醒",  60, IconGeometries.Water,     Strings.WaterBody),
            Build(ReminderType.Exercise,  "运动", "运动提醒", 120, IconGeometries.Exercise,  Strings.ExerciseBody),
        };
    }

    private ReminderDefinition Build(ReminderType type, string shortName, string displayName,
        int defaultMinutes, string iconData, string body)
    {
        int minutes = defaultMinutes;
        if (_config.IntervalMinutes.TryGetValue(type.ToString(), out var m) && m > 0)
            minutes = m;

        return new ReminderDefinition(
            type, shortName, displayName,
            TimeSpan.FromMinutes(minutes), body, iconData);
    }
}
