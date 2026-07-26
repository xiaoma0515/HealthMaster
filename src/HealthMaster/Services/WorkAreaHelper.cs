using System.Windows;
using Screen = System.Windows.Forms.Screen;

namespace HealthMaster.Services;

/// <summary>
/// 多屏工作区换算。<see cref="SystemParameters.WorkArea"/> 只返回主屏，
/// 窗口位于副屏时用它夹紧会把窗口拽回主屏，故统一改用 WinForms <see cref="Screen"/>
/// （.NET 内置，非第三方）按窗口中心点定位所在屏幕。
/// </summary>
public static class WorkAreaHelper
{
    /// <summary>
    /// 取窗口所在那块屏幕的工作区（WPF 的 DIP 坐标）。
    /// <see cref="Screen.WorkingArea"/> 是物理像素，需按该窗口的 DPI 变换回 DIP；
    /// 窗口尚未取得句柄（拿不到 DPI 信息）时退回主屏工作区。
    /// </summary>
    public static Rect For(Window window)
    {
        var ct = PresentationSource.FromVisual(window)?.CompositionTarget;
        if (ct == null) return SystemParameters.WorkArea;

        var centerDip = new Point(
            window.Left + window.ActualWidth / 2,
            window.Top + window.ActualHeight / 2);
        var centerPx = ct.TransformToDevice.Transform(centerDip);

        var wa = Screen.FromPoint(new System.Drawing.Point((int)centerPx.X, (int)centerPx.Y)).WorkingArea;

        var topLeft = ct.TransformFromDevice.Transform(new Point(wa.Left, wa.Top));
        var bottomRight = ct.TransformFromDevice.Transform(new Point(wa.Right, wa.Bottom));
        return new Rect(topLeft, bottomRight);
    }
}
