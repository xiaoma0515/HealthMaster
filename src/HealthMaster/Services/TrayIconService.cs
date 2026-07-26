using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using HealthMaster.Resources;
using HealthMaster.Themes;

namespace HealthMaster.Services;

/// <summary>
/// 系统托盘图标与菜单（.NET 内置 WinForms NotifyIcon，零第三方依赖）。
/// 菜单：暂停 / 恢复全部、打开配置文件夹、退出。
/// 注意：不提供"隐藏悬浮窗"——提醒图标挂在悬浮窗旁，是取消弹窗后唯一的提醒通道，
/// 隐藏悬浮窗会让用户彻底且无感知地收不到提醒。
/// 图标在运行时绘制，无需二进制资源文件。
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly IReminderScheduler _scheduler;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly Action _exit;
    private readonly string _configDir;

    public TrayIconService(IReminderScheduler scheduler, Action exit, string configDir)
    {
        _scheduler = scheduler;
        _exit = exit;
        _configDir = configDir;

        var menu = new ContextMenuStrip();
        _pauseItem = new ToolStripMenuItem(Strings.TrayPauseAll, null, OnPauseToggle);
        var openConfigItem = new ToolStripMenuItem(Strings.TrayOpenConfig, null, (_, _) => OpenConfigFolder());
        var exitItem = new ToolStripMenuItem(Strings.TrayExit, null, (_, _) => _exit());

        menu.Items.Add(_pauseItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(openConfigItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        // 深色 HUD 观感（纯外观，不改任何菜单行为）——须在菜单项全部 Add 之后调用
        TrayMenuTheme.Apply(menu);

        _notifyIcon = new NotifyIcon
        {
            Icon = CreateIcon(),
            Visible = true,
            Text = Strings.AppName,
            ContextMenuStrip = menu
        };
    }

    private void OnPauseToggle(object? sender, EventArgs e)
    {
        if (_scheduler.IsAllPaused)
        {
            _scheduler.ResumeAll();
            _pauseItem.Text = Strings.TrayPauseAll;
        }
        else
        {
            _scheduler.PauseAll();
            _pauseItem.Text = Strings.TrayResumeAll;
        }
    }

    private void OpenConfigFolder()
    {
        try
        {
            Directory.CreateDirectory(_configDir);
            Process.Start(new ProcessStartInfo { FileName = _configDir, UseShellExecute = true });
        }
        catch { /* 忽略 */ }
    }

    /// <summary>运行时绘制一个绿色圆底 + 白色"十字"健康图标。</summary>
    private static Icon CreateIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            // 缩到 16px 显示时线条易发糊：留边距、收线宽、圆端点、高质量像素偏移
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            using var bg = new SolidBrush(Color.FromArgb(0x30, 0xD1, 0x58)); // Apple systemGreen(Dark)
            g.FillEllipse(bg, 2, 2, 28, 28);
            using var pen = new Pen(Color.White, 2.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(pen, 16, 10, 16, 22);
            g.DrawLine(pen, 10, 16, 22, 16);
        }
        // GetHicon 产生非托管句柄，交由 Icon 包装；生命周期与应用一致，退出时随进程释放
        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        // NotifyIcon.Dispose 不会释放挂上去的菜单，得自己来（先取引用，Dispose 后该属性不可再读）
        var menu = _notifyIcon.ContextMenuStrip;
        _notifyIcon.Dispose();
        menu?.Dispose();
    }
}
