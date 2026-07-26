using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HealthMaster.Themes;

/// <summary>
/// 托盘右键菜单的视觉主题（纯外观，不含任何菜单行为逻辑）。
///
/// 托盘菜单是 WinForms 的 <see cref="ContextMenuStrip"/>，无法用 XAML 定制，
/// 只能通过内置的 <see cref="ToolStripProfessionalRenderer"/> 接管绘制
/// （<c>System.Windows.Forms</c> 自带，零第三方依赖）。
/// 目标：与悬浮窗一致的深色 HUD 观感 —— 去掉左侧空图标槽、深色底、圆角选中态、内缩分隔线、Win11 圆角。
/// </summary>
internal static class TrayMenuTheme
{
    private static readonly Color Bg = Color.FromArgb(0x2C, 0x2C, 0x2E);
    private static readonly Color TextColor = Color.FromArgb(0xF2, 0xF2, 0xF2);
    private static readonly Color HoverBg = Color.FromArgb(0x3A, 0x3A, 0x3C);
    private static readonly Color SepColor = Color.FromArgb(0x48, 0x48, 0x4A);

    /// <summary>把 HUD 观感应用到已建好的菜单（须在菜单项全部 Add 之后调用）。</summary>
    public static void Apply(ContextMenuStrip menu)
    {
        menu.RenderMode = ToolStripRenderMode.Professional;
        menu.Renderer = new HudRenderer();
        // 干掉左侧那条永远空着的图标边距槽 —— WinForms 默认菜单最土的一点
        menu.ShowImageMargin = false;
        menu.ShowCheckMargin = false;
        menu.BackColor = Bg;
        menu.ForeColor = TextColor;
        menu.Font = PickFont();
        menu.Padding = new Padding(0, 4, 0, 4);

        foreach (ToolStripItem item in menu.Items)
        {
            item.ForeColor = TextColor;
            item.BackColor = Bg;
            if (item is ToolStripMenuItem mi)
                mi.Padding = new Padding(8, 5, 8, 5);   // 项高 ≈ 28
        }

        menu.HandleCreated += (_, _) => TryRoundCorners(menu.Handle);
    }

    /// <summary>
    /// 按可用性挑字体：Segoe UI Variable（Win11）→ Segoe UI → 系统默认。
    ///
    /// 刻意用「先查名字再按名字建 Font」而非「new FontFamily 再喂给 Font」：
    /// 后者若把 family 包进 using，方法返回时 family 已 Dispose，而 Font 内部仍引用它，
    /// 后续读 <c>Font.FontFamily</c> 的 WinForms 路径（如 DPI 变化重建字体）会拿到已释放对象。
    /// 按名字的重载让 GDI+ 自己持有 family，调用方无需管其生命周期。
    /// </summary>
    private static Font PickFont()
    {
        foreach (var name in new[] { "Segoe UI Variable Text", "Segoe UI" })
        {
            if (!IsInstalled(name)) continue;
            try
            {
                return new Font(name, 9.5f, FontStyle.Regular, GraphicsUnit.Point);
            }
            catch (ArgumentException)
            {
                // 名字在册但 GDI+ 拒绝（缺常规字重等），试下一个
            }
        }
        return new Font(SystemFonts.MenuFont?.FontFamily ?? FontFamily.GenericSansSerif, 9.5f);
    }

    private static bool IsInstalled(string name)
    {
        foreach (var f in FontFamily.Families)
            if (string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    // —— Win11 原生圆角（DWM，系统 API，不算第三方依赖）；失败自动降级为方角，不影响功能 ——
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private static void TryRoundCorners(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;
        try
        {
            int pref = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
        }
        catch (DllNotFoundException) { /* 老系统无 dwmapi，忽略 */ }
        catch (EntryPointNotFoundException) { /* 老版本无该属性，忽略 */ }
    }

    /// <summary>只接管背景 / 选中态 / 分隔线 / 文字色的绘制，不接管布局与命中测试。</summary>
    private sealed class HudRenderer : ToolStripProfessionalRenderer
    {
        public HudRenderer() => RoundedEdges = false;

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var b = new SolidBrush(Bg);
            e.Graphics.FillRectangle(b, e.AffectedBounds);
        }

        // 外框交给 DWM 画（圆角 + 系统阴影），自己不描边，避免直角描边把圆角切出来
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled) return;

            var r = new Rectangle(4, 0, e.Item.Width - 8, e.Item.Height);
            if (r.Width <= 0 || r.Height <= 0) return;

            var old = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = Rounded(r, 6))
            using (var b = new SolidBrush(HoverBg))
                e.Graphics.FillPath(b, path);
            e.Graphics.SmoothingMode = old;
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using var p = new Pen(SepColor);
            e.Graphics.DrawLine(p, 12, y, e.Item.Width - 12, y);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? TextColor : Color.FromArgb(0x80, 0x80, 0x84);
            e.TextFormat |= TextFormatFlags.NoPadding;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            base.OnRenderItemText(e);
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
