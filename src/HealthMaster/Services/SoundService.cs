using System;
using System.IO;
using System.Media;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using HealthMaster.Resources;

namespace HealthMaster.Services;

/// <summary>
/// 提示音播放（v2.1）。两种声音，均为**代码合成、零外部音频文件、零第三方依赖**：
/// <list type="bullet">
///   <item>提醒音：颂钵，约 2.9s。到点冒出图标时播放，同一调度批次只播一次。</item>
///   <item>完成音：檐下滴水，很轻，时长 1.1s、实际可闻约 0.4s。用户单击图标完成时播放。</item>
/// </list>
///
/// 红线相关：
/// - 纯本地：声音在内存里算出来，不读盘、不联网。
/// - 非打断式：<see cref="SoundPlayer"/> 底层是 winmm <c>PlaySound</c>，不创建窗口、不抢焦点。
/// - 轻量低占用（关键）：
///   1. 提醒音的合成要跑 49 次 2.9s 全长渲染（48 次固定种子相位试验 + 基准）再加包络压缩，
///      本机 **Release 实测首次约 4.4–4.5 秒**单核 CPU（含分层 JIT 预热；**Debug 下约 6.6 秒**
///      属正常，不是回归），完成音约 15ms。故两条音轨都**只合成一次**，
///      WAV 字节缓存在 <see cref="SoundPlayer"/> 内部，之后每次播放只是一次
///      <c>PlaySound(SND_MEMORY|SND_ASYNC)</c>，开销可忽略。绝不能每次提醒都重算。
///   2. 合成跑在**一条专用后台线程**上，且优先级压到 <see cref="ThreadPriority.BelowNormal"/>：
///      - 不进线程池——数秒的纯 CPU 活会占死一个池线程；
///      - 降优先级保证它只吃空闲算力，不与 UI 渲染 / 用户前台程序抢核（红线 2）。
///   3. 选「后台预热」而非「首次播放时现算」：首次播放正是用户该听到提醒的那一刻，
///      现算会让提醒晚到数秒（若在 UI 线程上更会直接卡住悬浮窗）。而第一次提醒最快也在
///      20 分钟之后，预热必然早已完成。
///   4. **预热是惰性的**：只在提示音开启时才起线程。用户明确关掉声音后，进程不应再为
///      两条永不播放的音轨烧掉数秒 CPU 与约 24MB 分配（笔记本电池 / 发热场景尤甚）。
///      托盘把开关从关拨到开时，用 <see cref="Interlocked"/> 保证预热**至多触发一次**。
///   5. 不新增任何定时器。
///
/// 已知限制：winmm 的 <c>PlaySound</c> 在**进程内只有一个播放槽**，后一次播放会打断前一次。
/// 因此在提醒音（2.9s）还没放完时点击图标，提醒音会被完成音截断——这是可接受甚至符合直觉的行为
/// （用户已经处理完了）。两个音轨仍各持一个 <see cref="SoundPlayer"/> 实例，避免同一实例
/// 并发 <c>Play</c> 时互相踩内部缓冲。
/// </summary>
public sealed class SoundService : IDisposable
{
    /// <summary>预热完成信号（合成结束即完成，无论成败）。用于极早期播放请求的补播。</summary>
    private readonly TaskCompletionSource _warmed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    // 由后台预热线程写入、UI 线程读取，故 volatile。预热失败时保持 null（静音，不崩）。
    private volatile SoundPlayer? _reminder;
    private volatile SoundPlayer? _acknowledge;
    private volatile bool _enabled;
    private volatile bool _disposed;

    /// <summary>预热线程是否已启动（0/1）。用 <see cref="Interlocked"/> 保证全生命周期只起一次。</summary>
    private int _warmupStarted;

    // —— 冷路径（预热未完成时收到播放请求）状态，见 PlayWhenReady ——
    private int _pendingKind;            // 0=无, 1=提醒音, 2=完成音；只保留最后一次请求
    private int _pendingHooked;          // 0/1：是否已挂过续体，避免重复挂

    public SoundService(bool enabled)
    {
        _enabled = enabled;
        if (enabled) EnsureWarmupStarted();
    }

    /// <summary>
    /// 提示音总开关（托盘菜单可切换，配置持久化）。关闭后彻底静音。
    /// 关→开时惰性触发一次预热；开→关时顺手打断正在响的那一声
    /// （用户此刻按下开关，多半正是被那一声吵到）。
    /// setter 只在 UI 线程（托盘菜单回调）调用。
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;
            if (value) EnsureWarmupStarted();
            else StopPlayback();
        }
    }

    /// <summary>起后台合成线程；重复调用无副作用（CAS 保证至多一次）。</summary>
    private void EnsureWarmupStarted()
    {
        if (_disposed) return;
        if (Interlocked.CompareExchange(ref _warmupStarted, 1, 0) != 0) return;

        new Thread(Warmup)
        {
            IsBackground = true,                       // 不阻止进程退出
            Priority = ThreadPriority.BelowNormal,     // 只吃空闲算力
            Name = "HealthMaster.ToneSynth"
        }.Start();
    }

    /// <summary>立即停止正在播放的声音（winmm 进程内单播放槽，两个实例都停一遍即可）。</summary>
    private void StopPlayback()
    {
        try { _reminder?.Stop(); } catch { /* 忽略 */ }
        try { _acknowledge?.Stop(); } catch { /* 忽略 */ }
    }

    /// <summary>到点提醒音。同一调度批次多类同时到点时只应调用一次（见 <c>ReminderScheduler.RemindersDueBatch</c>）。</summary>
    public void PlayReminder() => PlayWhenReady(reminder: true);

    /// <summary>单击图标完成时的轻确认音。</summary>
    public void PlayAcknowledge() => PlayWhenReady(reminder: false);

    private void PlayWhenReady(bool reminder)
    {
        if (!_enabled || _disposed) return;

        // 声音在启动时是关的、用户中途打开：此时才第一次需要音轨，惰性起预热。
        EnsureWarmupStarted();

        if (_warmed.Task.IsCompleted)
        {
            Pick(reminder);
            return;
        }

        // —— 冷路径：预热还没跑完就要出声（如启动后立刻唤醒即到点，或刚打开开关）——
        // 挂在预热信号后面补播，不丢提醒，也不阻塞 UI 线程。
        // 只挂**一个**续体、只记**最后一次**请求：winmm 进程内单播放槽，预热完成瞬间连续
        // Play 会互相截断，听感是"咔"一下；而且这些请求本就发生在同一个几秒的窗口里，
        // 用户真正想听到的就是最近那一次。（不选"冷路径直接丢弃"是因为启动即到点这种
        // 场景下仍应出声。）
        Volatile.Write(ref _pendingKind, reminder ? 1 : 2);

        if (Interlocked.CompareExchange(ref _pendingHooked, 1, 0) == 0)
            _warmed.Task.ContinueWith(_ => DrainPending(), TaskScheduler.Default);

        // 兜底：若刚才写 _pendingKind 时预热恰好完成、续体已取走过挂起项，这里补一次，
        // 否则这次请求会落进那个极窄的窗口里被丢掉。
        if (_warmed.Task.IsCompleted) DrainPending();
    }

    /// <summary>取出并播放冷路径上挂起的最后一次请求（取走即清空，故不会重复播）。</summary>
    private void DrainPending()
    {
        var kind = Interlocked.Exchange(ref _pendingKind, 0);
        if (kind != 0) Pick(reminder: kind == 1);
    }

    private void Pick(bool reminder)
    {
        if (!_enabled || _disposed) return;
        var player = reminder ? _reminder : _acknowledge;
        try
        {
            player?.Play();
        }
        catch
        {
            // 无声卡 / 设备被独占等情形：静默失败，绝不影响提醒本身（图标才是主通道）
        }
    }

    /// <summary>后台合成两条音轨并预加载进 SoundPlayer（其内部会把流拷成字节缓冲）。</summary>
    private void Warmup()
    {
        try
        {
            _acknowledge = Create(ZenTones.BuildAcknowledgeWav);   // 便宜（~15ms），先备好
            _reminder = Create(ZenTones.BuildReminderWav);         // 贵（Release 实测 4.4–4.5s，含分层 JIT 预热）
        }
        finally
        {
            _warmed.TrySetResult();

            // 合成期间累计分配约 24.5MB：49 次 2.9s 渲染各要一个 127890 个 float 的中间缓冲
            // （512KB > 85KB 门限 → 直接进大对象堆 LOH），而 LOH 默认既不压缩也不主动归还。
            // 实测结束时仍有约 5.4MB LOH 残留 / 私有内存多占约 5MB，做一次带 LOH 压缩的显式回收
            // 即可全部还给系统，耗时实测 1.1ms（红线 2：轻量低占用）。
            // 这是整个进程生命周期里**唯一一次**大批量分配，故也只在这里回收一次，不是周期性 GC。
            try
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            }
            catch { /* 忽略 */ }

            // 竞态兜底：预热期间若已 Dispose，这里补一次释放
            if (_disposed) DisposePlayers();
        }
    }

    private static SoundPlayer? Create(Func<byte[]> build)
    {
        try
        {
            var player = new SoundPlayer(new MemoryStream(build()));
            player.Load();   // 同步读进内部缓冲，之后每次 Play 都不再碰流
            return player;
        }
        catch
        {
            return null;     // 合成 / 加载失败就静音，不影响图标提醒
        }
    }

    public void Dispose()
    {
        _disposed = true;
        DisposePlayers();
    }

    private void DisposePlayers()
    {
        var r = _reminder;
        var a = _acknowledge;
        _reminder = null;
        _acknowledge = null;
        try { r?.Stop(); r?.Dispose(); } catch { /* 忽略 */ }
        try { a?.Stop(); a?.Dispose(); } catch { /* 忽略 */ }
    }
}
