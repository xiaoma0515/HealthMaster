using System;
using System.IO;

namespace HealthMaster.Resources;

/// <summary>
/// 禅意提示音合成器：**零外部音频文件、零第三方库**，在内存里按物理模型合成 PCM，
/// 产出可直接喂给 <c>System.Media.SoundPlayer</c> 的标准 44.1kHz / 16bit / 单声道 WAV 字节流。
///
/// 与 <c>Strings</c> / <c>IconGeometries</c> 同属「代码即资源」：产物是纯数据，
/// 生成一次后由 <c>Services\SoundService</c> 缓存复用（红线 2：轻量低占用）。
///
/// 声学设计要点：
/// 1. 钟磬类乐器的泛音是**非谐波**的（不是基频整数倍），这是金属钟感的关键；
/// 2. 每个泛音有独立的指数衰减常数 τ，高频衰减更快（符合物理阻尼规律）；
/// 3. 每个泛音有独立的起振时间常数，避免全部同相起振产生爆点；
/// 4. 拍频（BeatHz）叠一个失谐孪生分量，产生颂钵的缓慢「呼吸感」；
/// 5. 首尾 raised-cosine 淡入淡出，杜绝 click 爆音。
///
/// ⚠️ **本文件的常量与运算顺序不可随意改动**：两条曲线（尤其是提醒音的 48 次固定种子相位试验）
/// 是用户逐条试听后拍板选定的音色，任何改动都会改变实际听到的声音。
/// 提醒音等价于原型导出的 <c>zen-1-loud-c.wav</c>，已用 SHA256 逐字节校验过。
/// </summary>
internal static class ZenTones
{
    public const int SampleRate = 44100;

    /// <summary>一个模态振动分量（泛音）。</summary>
    /// <param name="Ratio">相对基频的倍率（非整数即非谐波）。</param>
    /// <param name="Amp">初始振幅（相对值）。</param>
    /// <param name="DecaySec">指数衰减时间常数 τ，振幅按 e^(-t/τ) 衰减。</param>
    /// <param name="AttackSec">起振时间常数，振幅按 1-e^(-t/a) 上升。</param>
    /// <param name="BeatHz">拍频：叠一个失谐孪生分量产生缓慢音量起伏。0 表示不加。</param>
    /// <param name="Phase">初相位（弧度）。</param>
    private readonly record struct Partial(
        double Ratio,
        double Amp,
        double DecaySec,
        double AttackSec = 0.004,
        double BeatHz = 0,
        double Phase = 0);

    // ================================================================ 提醒音：颂钵
    //
    // 原型代号 zen-1「响亮」配方 C（c｜响亮：强中频补偿 + 长余韵 + 软限幅），用户选定。
    // 在原始颂钵之上叠了四把「响度刀」，按副作用从小到大：
    //  1. 相位优化（零音色代价）：6 个泛音在 t=0 同相叠加会产生远高于稳态的瞬态尖峰，
    //     波峰因数 crest 高达 ~18dB，峰值被这根针占满、平均响度上不去。随机搜索初相位、
    //     取 crest 最小的一组——对衰减音而言初相位听感上不可辨，纯赚的响度。
    //  2. 包络上行压缩（略改余韵长度，不改音色）：按解析宏包络施加增益，等效把衰减曲线压平。
    //     因为是解析曲线而非跟随器，不吃掉拍频「呼吸感」，也不产生泵浦感。
    //  3. 中频泛音补偿（音色略变亮）：174.6Hz 基频在笔记本小喇叭上被物理截掉大半，
    //     抬高 405/674/1041/1439Hz 这几档，让声音「穿得出来」。
    //  4. tanh 软限幅：把残余瞬态尖峰压圆，换取整体电平上抬（绝非硬削波）。

    private const double BowlF0 = 174.61;          // F3
    private const double BowlDurationSec = 2.90;
    private const double BowlFadeInSec = 0.006;
    private const double BowlFadeOutSec = 0.34;
    private const double BowlCompRatio = 0.55;     // 包络压缩指数：1=不压，越小余韵越长越响
    private const double BowlCompMaxGain = 9.0;    // 压缩最大增益（防止把尾巴无限抬起）
    private const double BowlLimitThreshold = 0.45;// 软限幅拐点（对归一到 1.0 的信号）
    private const float BowlTargetPeak = 0.89f;    // 最终峰值（线性）
    private const int BowlPhaseSeed = 915231;      // ⚠️ 固定种子，改了声音就变
    private const int BowlPhaseTrials = 48;        // ⚠️ 试验次数同上

    /// <summary>刀 3：6 个泛音的幅度缩放（中频补偿）。</summary>
    private static readonly double[] BowlAmpScale = { 1.00, 1.70, 1.95, 2.00, 1.80, 1.50 };

    /// <summary>刀 3：6 个泛音的衰减 τ 缩放。</summary>
    private static readonly double[] BowlDecayScale = { 1.00, 1.22, 1.32, 1.35, 1.35, 1.25 };

    /// <summary>颂钵的原始非谐波泛音族。</summary>
    private static Partial[] BowlBasePartials() => new[]
    {
        //          倍率    振幅    衰减τ  起振   拍频  初相
        new Partial(1.000, 1.000, 0.78, 0.020, 0.55, 0.0),
        new Partial(2.320, 0.620, 0.62, 0.012, 0.85, 1.1),
        new Partial(3.860, 0.340, 0.46, 0.008, 1.30, 2.3),
        new Partial(5.960, 0.170, 0.32, 0.006, 1.90, 0.4),
        new Partial(8.240, 0.085, 0.22, 0.005, 2.60, 3.0),
        new Partial(11.30, 0.040, 0.14, 0.004, 0.00, 1.7),
    };

    /// <summary>
    /// 合成「到点提醒」音（颂钵，约 2.9s）。耗时百毫秒级（48 次全长渲染），
    /// **务必只调用一次并缓存结果**，见 <c>SoundService</c>。
    /// </summary>
    public static byte[] BuildReminderWav()
    {
        var partials = BowlBasePartials();
        for (int i = 0; i < partials.Length; i++)
            partials[i] = partials[i] with
            {
                Amp = partials[i].Amp * BowlAmpScale[i],
                DecaySec = partials[i].DecaySec * BowlDecayScale[i],
            };

        // 刀 1：相位优化，最小化波峰因数
        var buf = RenderBestPhase(partials, BowlDurationSec);

        // 刀 2：包络上行压缩
        ApplyEnvelopeCompression(buf, partials, BowlCompRatio, BowlCompMaxGain);

        // 淡入淡出（首尾样本必须为 0）
        ApplyFades(buf, BowlFadeInSec, BowlFadeOutSec);

        // 刀 4：归一到 1.0 → 软限幅 → 归一到目标峰值
        Normalize(buf, 1.0f);
        if (BowlLimitThreshold < 1.0)
            for (int i = 0; i < buf.Length; i++)
                buf[i] = SoftLimit(buf[i], (float)BowlLimitThreshold);
        Normalize(buf, BowlTargetPeak);

        return ToWav(buf);
    }

    // ================================================================ 完成音：檐下滴水
    //
    // 原型代号 zen-3。与提醒音形态完全不同，避免被误当成第二个提醒：
    // 主体是 40ms 内由 640Hz 上滑到 1450Hz 的短促水泡音（水滴入水后气泡收缩、共振腔变小
    // → 音高上行，这是「滴水」听感的物理来源）；前面垫一小撮低通噪声当「啵」的溅落瞬态，
    // 后面挂一段极轻的非谐波低频余韵。短、轻、有留白。

    private const double DropDurationSec = 1.10;
    private const double DropGlideFromHz = 640.0;
    private const double DropGlideToHz = 1450.0;
    private const int DropNoiseSeed = 20260726;    // ⚠️ 固定种子，保证每次合成结果一致
    private const double DropTailF0 = 329.63;      // E4

    /// <summary>
    /// 完成音的目标峰值。原型试听版为 0.22；按用户要求「再压低一档」，取
    /// **颂钵提醒音 RMS 的约 1/5.6（-15 dB）** 作为标定目标，落到 0.11。
    /// 它只是「点到了」的确认音，绝不能听成第二次提醒。
    /// </summary>
    private const float DropTargetPeak = 0.11f;

    /// <summary>合成「单击完成」的轻确认音（檐下滴水，约 1.1s，实际可闻约 0.4s）。</summary>
    public static byte[] BuildAcknowledgeWav()
    {
        int n = (int)(SampleRate * DropDurationSec);
        var buf = new float[n];

        // —— 主体：上滑水泡音 ——
        const double glideTau = 0.028;   // 音高上滑的时间常数
        const double bodyTau = 0.075;    // 振幅衰减
        const double bodyAtk = 0.0025;
        double phase = 0;
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / SampleRate;
            double f = DropGlideToHz + (DropGlideFromHz - DropGlideToHz) * Math.Exp(-t / glideTau);
            phase += 2 * Math.PI * f / SampleRate;
            double env = (1 - Math.Exp(-t / bodyAtk)) * Math.Exp(-t / bodyTau);
            buf[i] += (float)(Math.Sin(phase) * env);
        }

        // —— 溅落瞬态：一小撮低通噪声（固定种子，保证可复现） ——
        var rng = new Random(DropNoiseSeed);
        double lp = 0;                      // 一阶低通，截止约 1.4kHz
        double a = 1 - Math.Exp(-2 * Math.PI * 1400.0 / SampleRate);
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / SampleRate;
            double white = rng.NextDouble() * 2 - 1;
            lp += a * (white - lp);
            double env = Math.Exp(-t / 0.010) * (1 - Math.Exp(-t / 0.0008));
            buf[i] += (float)(lp * env * 0.14);
        }

        // —— 余韵：两个非谐波低频分量，模拟水面 / 陶罐的空腔共鸣 ——
        Partial[] tail =
        {
            new(1.000, 0.16, 0.34, 0.030, 0.40, 0.0),
            new(2.410, 0.07, 0.22, 0.024, 0.70, 2.1),
        };
        var tailBuf = Render(DropTailF0, tail, DropDurationSec);
        for (int i = 0; i < n; i++) buf[i] += tailBuf[i];

        ApplyFades(buf, fadeInSec: 0.003, fadeOutSec: 0.180);
        Normalize(buf, DropTargetPeak);
        return ToWav(buf);
    }

    // ================================================================ 合成引擎

    /// <summary>
    /// 随机搜索初相位，返回波峰因数最小的渲染结果。
    /// ⚠️ 种子与试验次数固定，结果可复现；改动会改变最终音色。
    /// </summary>
    private static float[] RenderBestPhase(Partial[] partials, double dur)
    {
        var best = Render(BowlF0, partials, dur);
        double bestCrest = Crest(best);

        var rng = new Random(BowlPhaseSeed);
        for (int trial = 0; trial < BowlPhaseTrials; trial++)
        {
            var cand = new Partial[partials.Length];
            for (int i = 0; i < partials.Length; i++)
                cand[i] = partials[i] with { Phase = rng.NextDouble() * 2 * Math.PI };

            var buf = Render(BowlF0, cand, dur);
            double c = Crest(buf);
            if (c < bestCrest) { bestCrest = c; best = buf; }
        }

        return best;
    }

    /// <summary>波峰因数（峰值 / RMS，dB）。</summary>
    private static double Crest(float[] buf)
    {
        double peak = 0, sumSq = 0;
        foreach (var s in buf) { peak = Math.Max(peak, Math.Abs(s)); sumSq += (double)s * s; }
        double rms = Math.Sqrt(sumSq / buf.Length);
        return 20 * Math.Log10(peak / Math.Max(rms, 1e-12));
    }

    private static float[] Render(double f0, Partial[] partials, double durationSec)
    {
        int n = (int)(SampleRate * durationSec);
        var buf = new float[n];

        foreach (var p in partials)
        {
            double f = f0 * p.Ratio;
            if (f >= SampleRate / 2.0) continue;             // 防混叠

            double w = 2 * Math.PI * f / SampleRate;
            // 拍频：叠一个失谐孪生分量，两者相加即产生 BeatHz 的缓慢音量起伏
            double wBeat = 2 * Math.PI * (f + p.BeatHz) / SampleRate;
            bool hasBeat = p.BeatHz > 0;

            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SampleRate;
                double env = (1 - Math.Exp(-t / p.AttackSec)) * Math.Exp(-t / p.DecaySec);
                double s = Math.Sin(i * w + p.Phase);
                if (hasBeat) s = (s + Math.Sin(i * wBeat + p.Phase + 0.7)) * 0.5;
                buf[i] += (float)(s * p.Amp * env);
            }
        }

        return buf;
    }

    /// <summary>
    /// 按解析宏包络做上行压缩：E(t)=Σa_i·e^(-t/τ_i)，gain(t)=min(maxGain, (E(t)/E(0))^(ratio-1))。
    /// 等效于衰减曲线变成 E(t)^ratio —— 余韵更长更饱满、RMS 抬起来，且完全平滑无泵浦。
    /// </summary>
    private static void ApplyEnvelopeCompression(float[] buf, Partial[] partials, double ratio, double maxGain)
    {
        if (ratio >= 0.999) return;

        double e0 = 0;
        foreach (var p in partials) e0 += p.Amp;

        for (int i = 0; i < buf.Length; i++)
        {
            double t = (double)i / SampleRate;
            double e = 0;
            foreach (var p in partials) e += p.Amp * Math.Exp(-t / p.DecaySec);
            double g = Math.Pow(Math.Max(e / e0, 1e-9), ratio - 1.0);
            buf[i] *= (float)Math.Min(g, maxGain);
        }
    }

    /// <summary>tanh 软拐点限幅：在 thr 处 C1 连续，输出严格小于 1，绝不硬削。</summary>
    private static float SoftLimit(float x, float thr)
    {
        float a = Math.Abs(x);
        if (a <= thr) return x;
        float room = 1f - thr;
        float y = thr + room * (float)Math.Tanh((a - thr) / room);
        return x < 0 ? -y : y;
    }

    /// <summary>首尾做 raised-cosine 淡入淡出，杜绝起始 click 与截断爆音。</summary>
    private static void ApplyFades(float[] buf, double fadeInSec, double fadeOutSec)
    {
        int fin = Math.Min((int)(SampleRate * fadeInSec), buf.Length);
        for (int i = 0; i < fin; i++)
            buf[i] *= (float)(0.5 - 0.5 * Math.Cos(Math.PI * i / fin));

        int fout = Math.Min((int)(SampleRate * fadeOutSec), buf.Length);
        for (int i = 0; i < fout; i++)
        {
            int idx = buf.Length - fout + i;
            buf[idx] *= (float)(0.5 + 0.5 * Math.Cos(Math.PI * i / fout));
        }
    }

    /// <summary>峰值归一到指定电平（留足余量，避免削波，也控制响度）。</summary>
    private static void Normalize(float[] buf, float peak)
    {
        float max = 0;
        foreach (var s in buf) max = Math.Max(max, Math.Abs(s));
        if (max <= 1e-9f) return;
        float g = peak / max;
        for (int i = 0; i < buf.Length; i++) buf[i] *= g;
    }

    // ================================================================ WAV 封装

    /// <summary>把 float[-1,1] 打包成 44.1kHz / 16bit / 单声道 WAV 字节流（含 44 字节标准头）。</summary>
    private static byte[] ToWav(float[] samples)
    {
        int dataBytes = samples.Length * 2;
        using var ms = new MemoryStream(44 + dataBytes);
        using var w = new BinaryWriter(ms);

        w.Write(new[] { 'R', 'I', 'F', 'F' });
        w.Write(36 + dataBytes);
        w.Write(new[] { 'W', 'A', 'V', 'E' });
        w.Write(new[] { 'f', 'm', 't', ' ' });
        w.Write(16);                       // PCM fmt chunk size
        w.Write((short)1);                 // PCM
        w.Write((short)1);                 // mono
        w.Write(SampleRate);
        w.Write(SampleRate * 2);           // byte rate
        w.Write((short)2);                 // block align
        w.Write((short)16);                // bits per sample
        w.Write(new[] { 'd', 'a', 't', 'a' });
        w.Write(dataBytes);

        foreach (var s in samples)
        {
            int v = (int)Math.Round(Math.Clamp(s, -1f, 1f) * 32767.0);
            w.Write((short)v);
        }

        w.Flush();
        return ms.ToArray();
    }
}
