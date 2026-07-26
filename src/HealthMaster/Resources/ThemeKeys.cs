namespace HealthMaster.Resources;

/// <summary>
/// 主题资源 key（供 ViewModel 输出「强调色键」，由 XAML 侧的 Themes/*.xaml 定义同名画刷）。
/// ViewModel 只产出 key 字符串，不引用任何 Brush 实例，逻辑层与视觉层解耦：
/// 换肤 / 改色只需改资源字典，无需动 C#。
/// </summary>
public static class ThemeKeys
{
    public const string AccentEye = "Brush.Accent.Eye";
    public const string AccentSedentary = "Brush.Accent.Sedentary";
    public const string AccentWater = "Brush.Accent.Water";
    public const string AccentExercise = "Brush.Accent.Exercise";

    /// <summary>中性态（已暂停 / 全部待完成）用的弱化色。</summary>
    public const string AccentNeutral = "Brush.Text.Tertiary";
}
