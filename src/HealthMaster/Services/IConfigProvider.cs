using System.Collections.Generic;
using HealthMaster.Models;

namespace HealthMaster.Services;

/// <summary>
/// 提供四类提醒的静态定义。这是"可配置"的接缝：
/// v1 用 <see cref="DefaultConfigProvider"/>（默认值 + 可选 JSON 覆盖间隔），
/// 未来接入完整设置界面时替换实现即可，调度与 UI 无需改动。
/// </summary>
public interface IConfigProvider
{
    IReadOnlyList<ReminderDefinition> GetDefinitions();
}
