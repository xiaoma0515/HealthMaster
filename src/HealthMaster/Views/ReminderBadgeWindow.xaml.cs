using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using HealthMaster.Models;
using HealthMaster.Services;
using HealthMaster.ViewModels;

namespace HealthMaster.Views;

/// <summary>
/// 到点提醒图标窗：非打断式提醒——不弹窗、不抢焦点，只在悬浮窗旁竖排冒出图标，
/// 左键单击某图标即表示该类"已完成"，图标消失并重置该类计时。
/// 独立窗体是为了完全不影响悬浮窗自身的尺寸与位置持久化逻辑；
/// 本窗跟随悬浮窗移动 / 显隐，置顶行为与悬浮窗一致。
/// 窗体加 <c>WS_EX_NOACTIVATE</c>：单击图标不会激活本窗、不会让用户正在输入的窗口失焦。
/// </summary>
public partial class ReminderBadgeWindow : Window
{
    /// <summary>图标与悬浮窗之间的间距（DIP）。</summary>
    private const double Gap = 6;

    private readonly ObservableCollection<ReminderBadgeItem> _items;
    private Window? _anchor;
    private bool _placementQueued;

    /// <summary>用户单击了某类图标（视为已完成）。</summary>
    public event Action<ReminderType>? BadgeClicked;

    public ReminderBadgeWindow(ObservableCollection<ReminderBadgeItem> items)
    {
        InitializeComponent();

        _items = items;
        DataContext = items;

        _items.CollectionChanged += OnItemsChanged;
        SizeChanged += OnSelfSizeChanged;   // 尺寸随图标数变化，需重新定位
    }

    // —— 不抢焦点（WS_EX_NOACTIVATE）——
    // ShowActivated=False 只保证"显示时"不激活；用户点击图标仍会激活本窗、打断正在进行的输入。
    // 加 WS_EX_NOACTIVATE 后窗体永不接受激活，但照常收到鼠标消息，Button.Click 正常触发。
    // 用的是系统 API（user32），不引入任何第三方依赖。
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        // 32 位 user32 未导出 *LongPtrW，按位宽分流
        if (IntPtr.Size == 8)
        {
            long ex = GetWindowLongPtr64(hwnd, GWL_EXSTYLE).ToInt64();
            SetWindowLongPtr64(hwnd, GWL_EXSTYLE, new IntPtr(ex | WS_EX_NOACTIVATE));
        }
        else
        {
            int ex = GetWindowLong32(hwnd, GWL_EXSTYLE);
            SetWindowLong32(hwnd, GWL_EXSTYLE, ex | WS_EX_NOACTIVATE);
        }
    }

    /// <summary>挂到悬浮窗上：跟随其移动、尺寸变化与显隐。重复调用会先解绑旧锚点。</summary>
    public void AttachTo(Window anchor)
    {
        Detach();

        _anchor = anchor;
        anchor.LocationChanged += OnAnchorMoved;
        anchor.SizeChanged += OnAnchorSizeChanged;
        anchor.IsVisibleChanged += OnAnchorVisibilityChanged;
        UpdatePlacement();
    }

    /// <summary>解绑锚点，撤销全部订阅（避免重复订阅与悬挂引用）。</summary>
    public void Detach()
    {
        if (_anchor == null) return;

        _anchor.LocationChanged -= OnAnchorMoved;
        _anchor.SizeChanged -= OnAnchorSizeChanged;
        _anchor.IsVisibleChanged -= OnAnchorVisibilityChanged;
        _anchor = null;
    }

    /// <summary>
    /// 图标增减后重新定位。**必须延后到本次集合变更通知处理完毕**：
    /// 本窗的 CollectionChanged 订阅早于 ItemsControl 的内部订阅，若在此同步调用
    /// <see cref="UpdatePlacement"/>（内含 Show / 布局），会在 ItemsControl 尚未处理完这条
    /// Add 通知时重入其容器生成器，导致同一项被生成两次（屏幕上多出一枚重复图标）。
    /// </summary>
    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_placementQueued) return;   // 同一轮多次增减只排一次
        _placementQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            _placementQueued = false;
            UpdatePlacement();
        }));
    }

    private void OnSelfSizeChanged(object? sender, SizeChangedEventArgs e) => UpdatePlacement();
    private void OnAnchorMoved(object? sender, EventArgs e) => UpdatePlacement();
    private void OnAnchorSizeChanged(object? sender, SizeChangedEventArgs e) => UpdatePlacement();
    private void OnAnchorVisibilityChanged(object? sender, DependencyPropertyChangedEventArgs e) => UpdatePlacement();

    /// <summary>无图标（或悬浮窗隐藏）时隐藏自身；否则贴在悬浮窗左侧，空间不足时改贴右侧。</summary>
    private void UpdatePlacement()
    {
        if (_anchor == null) return;

        if (_items.Count == 0 || !_anchor.IsVisible)
        {
            if (IsVisible)
            {
                Opacity = 0;   // 下次显示重新走首帧门控
                Hide();
            }
            return;
        }

        if (!IsVisible)
        {
            Opacity = 0;   // 定位算完前保持不可见，避免在屏幕左上角闪一帧
            Show();        // ShowActivated=False：不抢焦点
        }

        if (ActualWidth <= 0 || ActualHeight <= 0) return;   // 布局未完成，SizeChanged 后会再来一次

        // 用悬浮窗所在那块屏幕的工作区，而不是主屏工作区，否则副屏上图标会被夹回主屏
        var area = WorkAreaHelper.For(_anchor);

        double left = _anchor.Left - ActualWidth - Gap;
        if (left < area.Left)
            left = _anchor.Left + _anchor.ActualWidth + Gap;   // 左侧放不下改放右侧
        if (left + ActualWidth > area.Right)
            left = area.Right - ActualWidth;
        if (left < area.Left) left = area.Left;

        double top = _anchor.Top;
        if (top + ActualHeight > area.Bottom) top = area.Bottom - ActualHeight;
        if (top < area.Top) top = area.Top;

        Left = left;
        Top = top;
        Opacity = 1;   // 定位完成，放行显示
    }

    private void Badge_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ReminderBadgeItem item })
            BadgeClicked?.Invoke(item.Type);
    }
}
