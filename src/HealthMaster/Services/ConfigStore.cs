using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using HealthMaster.Models;

namespace HealthMaster.Services;

/// <summary>
/// 本地 JSON 配置读写（<c>%APPDATA%\HealthMaster\config.json</c>）。
/// 红线：纯本地、不联网、不上传。损坏或缺失时回退默认值。
/// </summary>
public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        // 让中文按原样写出而非 \uXXXX 转义，便于用户手动编辑
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>配置目录 <c>%APPDATA%\HealthMaster</c>。</summary>
    public string ConfigDirectory { get; }
    public string FilePath { get; }
    public string LogDirectory { get; }

    public ConfigStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        ConfigDirectory = Path.Combine(appData, "HealthMaster");
        FilePath = Path.Combine(ConfigDirectory, "config.json");
        LogDirectory = Path.Combine(ConfigDirectory, "logs");
    }

    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AppConfig();

            var json = File.ReadAllText(FilePath);
            AppConfig? cfg;
            try
            {
                cfg = JsonSerializer.Deserialize<AppConfig>(json, Options);
            }
            catch (JsonException)
            {
                // 损坏但可能可人工修复：先备份坏文件再回退默认，避免被后续写入永久覆盖
                BackupCorrupt();
                return new AppConfig();
            }

            if (cfg == null)
            {
                BackupCorrupt();
                return new AppConfig();
            }

            cfg.IntervalMinutes ??= new();
            cfg.DoNotDisturb ??= new();
            return cfg;
        }
        catch
        {
            // 其他读盘异常：回退默认，不影响启动
            return new AppConfig();
        }
    }

    /// <summary>
    /// 只保存悬浮窗位置：先读磁盘最新配置，仅覆盖位置字段再写回。
    /// 这样用户在程序运行期间手改的勿扰 / 间隔配置不会被本进程的内存快照覆盖。
    /// </summary>
    public void SaveFloatingPosition(double x, double y)
    {
        var latest = Load();
        latest.FloatingX = x;
        latest.FloatingY = y;
        Save(latest);
    }

    /// <summary>
    /// 只保存提示音开关：同 <see cref="SaveFloatingPosition"/> 的定向保存约定——
    /// 先读磁盘最新配置，仅覆盖该字段再写回，绝不用内存快照整份覆盖用户手改的其他项。
    /// </summary>
    public void SaveSoundEnabled(bool enabled)
    {
        var latest = Load();
        latest.SoundEnabled = enabled;
        Save(latest);
    }

    public void Save(AppConfig config)
    {
        try
        {
            Directory.CreateDirectory(ConfigDirectory);
            var json = JsonSerializer.Serialize(config, Options);

            // 原子写入：先写临时文件，再替换，避免写一半崩溃导致文件损坏
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(FilePath))
                File.Replace(tmp, FilePath, null);
            else
                File.Move(tmp, FilePath);
        }
        catch
        {
            // 写盘失败不致命，忽略
        }
    }

    private void BackupCorrupt()
    {
        try
        {
            var backup = Path.Combine(ConfigDirectory, "config.corrupt.json");
            if (File.Exists(backup)) File.Delete(backup);
            File.Move(FilePath, backup);
        }
        catch
        {
            // 备份失败也不阻断启动
        }
    }
}
