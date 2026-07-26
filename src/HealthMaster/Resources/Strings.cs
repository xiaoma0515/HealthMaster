namespace HealthMaster.Resources;

/// <summary>集中管理的中文文案（v1 仅简体中文，集中于此便于后续统一措辞 / 做设置界面）。</summary>
public static class Strings
{
    public const string AppName = "健康提醒小助手";

    // —— 提醒文案（一句可执行建议），用于提醒图标 tooltip 的第二行 ——
    // tooltip 首行用 ReminderDefinition.DisplayName（如"护眼提醒"），故不再单独维护标题文案。
    public const string EyeBody = "抬头看看 6 米外的远处，放松 20 秒再继续～";
    public const string SedentaryBody = "已经坐了一会儿，站起来走两步、伸展一下吧。";
    public const string WaterBody = "给身体补点水分，小口慢饮更健康。";
    public const string ExerciseBody = "做一组拉伸或原地活动，唤醒身体。";

    // —— 提醒图标（挂在悬浮窗旁，单击即"已完成"）——
    public const string BadgeClickHint = "单击图标表示已完成";

    // —— 悬浮窗 ——
    public const string FloatingCollapsedHint = "健康提醒";
    public const string FloatingExpandedHeader = "健康提醒 · 距下次";
    public const string PausedLabel = "已暂停";

    /// <summary>该类图标已挂出、等待用户单击确认期间的倒计时占位文本。</summary>
    public const string HeldLabel = "待完成";

    // —— 托盘菜单 ——
    public const string TrayPauseAll = "暂停全部";
    public const string TrayResumeAll = "恢复全部";
    // 提示音开关用「文字」而非 WinForms 的勾选框：托盘菜单主题（TrayMenuTheme）
    // 关掉了左侧图标 / 勾选边距槽，勾选标记无处可画。
    // 文案取**动作式**（描述"点下去会发生什么"），与紧邻的「暂停全部 / 恢复全部」语义方向一致：
    // 声音开着时显示「关闭提醒声音」，关着时显示「开启提醒声音」。
    public const string TraySoundTurnOff = "关闭提醒声音";
    public const string TraySoundTurnOn = "开启提醒声音";

    public const string TrayOpenConfig = "打开配置文件夹";
    public const string TrayExit = "退出";
}
