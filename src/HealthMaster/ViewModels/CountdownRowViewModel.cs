using System.ComponentModel;
using System.Runtime.CompilerServices;
using HealthMaster.Models;

namespace HealthMaster.ViewModels;

/// <summary>
/// 悬浮窗里「一行倒计时」的结构化数据（展开态四行各一个；折叠态复用一个实例表示最近一项）。
///
/// 之所以拆成结构化字段而非 v1.1 的 <c>"护眼   18:42"</c> 拼接串：
/// 拼接串没法让 XAML 把时间放进固定宽度的右对齐列，四行数字是歪的（UI-DESIGN-v2 §1.2 F5）。
/// 现在名称与时间是两个独立字段，XAML 可用 Auto / * / 68 三列做列对齐 + Tabular 数字。
///
/// 实例在 <see cref="FloatingViewModel"/> 构造时一次性建好、之后只改属性，
/// 不参与集合增删——绑定容器稳定，1Hz 刷新不产生容器重建。
/// （每秒仍有少量分配：格式化出的时间字符串 + 实际变化字段的 PropertyChangedEventArgs；
/// 属性 setter 都做了等值短路，值没变就不发通知。）
/// </summary>
public sealed class CountdownRowViewModel : INotifyPropertyChanged
{
    public CountdownRowViewModel(ReminderType? type, string name, string accentKey)
    {
        _type = type;
        _name = name;
        _accentKey = accentKey;
    }

    private ReminderType? _type;
    private string _name;
    private string _accentKey;
    private string _timeText = "";
    private string _statusText = "";
    private bool _isHeld;
    private bool _isPaused;

    /// <summary>所属提醒类别；折叠态在「已暂停 / 全部待完成」时为 null。</summary>
    public ReminderType? Type { get => _type; private set => SetField(ref _type, value); }

    /// <summary>类别短名（护眼 / 久坐 / 补水 / 运动）；无类别可指时为空串。</summary>
    public string Name { get => _name; private set => SetField(ref _name, value); }

    /// <summary>强调色资源 key（见 <see cref="Resources.ThemeKeys"/>），供色点 / 高亮取色。</summary>
    public string AccentKey { get => _accentKey; private set => SetField(ref _accentKey, value); }

    /// <summary>倒计时文本（<c>18:42</c> / <c>1:02:33</c>）；非计时态为空串。</summary>
    public string TimeText { get => _timeText; private set => SetField(ref _timeText, value); }

    /// <summary>状态文本（待完成 / 已暂停）；正常计时态为空串。</summary>
    public string StatusText { get => _statusText; private set => SetField(ref _statusText, value); }

    /// <summary>该类图标已挂出、等待用户单击确认（此期间没有「下一次」，故不显示倒计时）。</summary>
    public bool IsHeld { get => _isHeld; private set => SetField(ref _isHeld, value); }

    /// <summary>全局已暂停。</summary>
    public bool IsPaused { get => _isPaused; private set => SetField(ref _isPaused, value); }

    /// <summary>true = 该显示 <see cref="TimeText"/>；false = 该显示 <see cref="StatusText"/> 胶囊。</summary>
    public bool HasTime => !_isHeld && !_isPaused;

    /// <summary>切到正常计时态。</summary>
    internal void SetCountdown(ReminderType? type, string name, string accentKey, string timeText)
    {
        Type = type;
        Name = name;
        AccentKey = accentKey;
        TimeText = timeText;
        StatusText = "";
        SetFlags(held: false, paused: false);
    }

    /// <summary>切到状态态（待完成 / 已暂停）。</summary>
    internal void SetStatus(ReminderType? type, string name, string accentKey, string statusText,
        bool held, bool paused)
    {
        Type = type;
        Name = name;
        AccentKey = accentKey;
        TimeText = "";
        StatusText = statusText;
        SetFlags(held, paused);
    }

    private void SetFlags(bool held, bool paused)
    {
        bool before = HasTime;
        IsHeld = held;
        IsPaused = paused;
        if (before != HasTime) Raise(nameof(HasTime));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        Raise(name);
    }

    private void Raise(string? name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
