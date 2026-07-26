using System;
using System.Windows;
using System.Windows.Input;

namespace HealthMaster.Views;

/// <summary>
/// 常驻小悬浮窗：always-on-top、无边框可拖动、鼠标悬停展开四路倒计时、位置跨重启记忆。
/// </summary>
public partial class FloatingWindow : Window
{
    /// <summary>拖动结束后回调新位置（Left, Top），供上层持久化。</summary>
    public event Action<double, double>? PositionChanged;

    public FloatingWindow()
    {
        InitializeComponent();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;
        try
        {
            DragMove(); // 阻塞直到松开鼠标
            PositionChanged?.Invoke(Left, Top);
        }
        catch (InvalidOperationException)
        {
            // 极少数情况下鼠标捕获丢失，忽略
        }
    }
}
