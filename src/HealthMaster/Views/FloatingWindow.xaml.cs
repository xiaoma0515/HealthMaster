using System;
using System.Windows;
using System.Windows.Input;
using HealthMaster.Services;

namespace HealthMaster.Views;

/// <summary>
/// 常驻小悬浮窗：always-on-top、无边框可拖动、鼠标悬停展开四路倒计时、位置跨重启记忆。
///
/// —— 位置语义（改动前务必先读）——
/// 本窗是 <c>SizeToContent="WidthAndHeight"</c>，悬停展开时宽高会同时变大（实测宽 123→182、
/// 差约 59 DIP）。窗口坐标 <see cref="Window.Left"/>/<see cref="Window.Top"/> 指的是左上角，
/// 因此「原地变大」= 向右下方生长，贴着屏幕右缘/下缘时展开态会被裁到屏幕外。
///
/// 解法：把「用户认定的位置」与「窗口当前实际坐标」拆成两个概念——
///   • <see cref="AnchorLeft"/>/<see cref="AnchorTop"/>：锚点，只在首次放置和**拖动结束**时变，
///     也是唯一被持久化到配置的值；
///   • 实际 Left/Top：每次尺寸变化都由 <see cref="ApplyPosition"/> 从锚点重新算，
///     算法是「先回到锚点，再按当前尺寸夹进工作区」。
/// 这样展开若越界就自动向左/向上生长恰好够用的量，收起时又必然精确回到锚点，
/// **反复悬停不会产生位置漂移**（每次都是从同一个锚点重算，而非在上次结果上累加）。
///
/// 另注：夹紧后的新矩形必定完整包含夹紧前的旧矩形（位移量 ≤ 尺寸增量），
/// 故鼠标不会因窗口移动而脱离窗体，不存在展开/收起的抖动循环。
/// </summary>
public partial class FloatingWindow : Window
{
    /// <summary>拖动结束后回调新位置（Left, Top），供上层持久化。</summary>
    public event Action<double, double>? PositionChanged;

    private double _anchorLeft;
    private double _anchorTop;
    private bool _hasAnchor;

    /// <summary>锚点位置：收起态的稳定坐标，持久化用它（而不是展开态的临时坐标）。</summary>
    public double AnchorLeft => _hasAnchor ? _anchorLeft : Left;
    public double AnchorTop => _hasAnchor ? _anchorTop : Top;

    public FloatingWindow()
    {
        InitializeComponent();
        // 展开/收起、内容宽度变化都会走这里，重新按当前尺寸把窗口夹进工作区
        SizeChanged += (_, _) => ApplyPosition();
    }

    /// <summary>设置锚点并立即应用（尺寸未就绪时，首次 SizeChanged 会补算）。</summary>
    public void SetPosition(double left, double top)
    {
        _anchorLeft = left;
        _anchorTop = top;
        _hasAnchor = true;
        Left = left;
        Top = top;
        ApplyPosition();
    }

    /// <summary>从锚点出发，按当前实际尺寸夹进窗口所在那块屏幕的工作区。</summary>
    private void ApplyPosition()
    {
        if (!_hasAnchor || ActualWidth <= 0 || ActualHeight <= 0) return;

        // 按「锚点 + 当前尺寸」的中心点判屏：多屏下必须是窗口所在屏的工作区，不是主屏（v1.1 B1）
        var area = WorkAreaHelper.For(this, new Point(
            _anchorLeft + ActualWidth / 2, _anchorTop + ActualHeight / 2));

        double left = _anchorLeft;
        double top = _anchorTop;

        // 先推右/下缘，再推左/上缘：窗口大于工作区时以左上角对齐（保证标题侧可见）
        if (left + ActualWidth > area.Right) left = area.Right - ActualWidth;
        if (top + ActualHeight > area.Bottom) top = area.Bottom - ActualHeight;
        if (left < area.Left) left = area.Left;
        if (top < area.Top) top = area.Top;

        if (Left != left) Left = left;
        if (Top != top) Top = top;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;
        try
        {
            DragMove(); // 阻塞直到松开鼠标

            // 拖动过程中不夹（每帧夹会让拖动不跟手），只在结束时夹一次
            _anchorLeft = Left;
            _anchorTop = Top;
            _hasAnchor = true;
            ApplyPosition();
            _anchorLeft = Left;   // 以夹紧后的结果为准，避免锚点停在屏幕外
            _anchorTop = Top;

            PositionChanged?.Invoke(_anchorLeft, _anchorTop);
        }
        catch (InvalidOperationException)
        {
            // 极少数情况下鼠标捕获丢失，忽略
        }
    }
}
