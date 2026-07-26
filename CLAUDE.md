# Health Master — 健康提醒小助手

> Windows 桌面端健康提醒工具：常驻悬浮窗 + 到点冒出图标（单击即完成），提醒用户不要久坐、放松眼睛、补水、运动。

---

## 角色与协作模式（红线，务必遵守）

- **PM（我，Fable 5 模型）**：只负责理解需求、拆解任务、分发给 subagent、维护本文件与红线，不亲自写代码、不做 code review。
- **专家（subagents，opus 最新模型）**：承担所有具体工作（架构、编码、review、测试）。
- **只使用项目级 subagent**（`.claude/agents/`），严禁调用/修改 user/系统级 agent。
- 需求有任何不明确处，PM 必须向用户询问，禁止自行揣测。
- **专家调用约定**：项目级专家（architect/developer/reviewer）已可**按名字原生调用**（`subagent_type: architect` / `developer` / `reviewer`）。任务开头仍让其先读对应 `.claude/agents/<role>.md`，确保角色职责与红线到位。

## 产品红线（约束，未经用户同意不得突破）

1. **纯本地运行**：不联网、不上传任何用户数据、不接入第三方服务。
2. **轻量低占用**：悬浮窗常驻，CPU/内存占用必须尽可能低。
3. **中文界面**。
4. **平台**：Windows 11 桌面应用。
5. 交互形态：**常驻小悬浮窗（悬停显示各项倒计时）** + **到点在悬浮窗旁冒出提醒图标（单击即完成）**。
6. **非打断式**：不得使用弹窗、任务栏闪烁、抢焦点等任何打断用户当前工作的强提醒手段。图标出现与点击均不得夺取焦点。（v1.1 用户明确要求，替代了 v1 的弹窗方案）

## 需求快照（v1.1 现行，已确认）

- 技术栈：**C# / .NET 8 + WPF**，零第三方依赖，纯本地。
- 四类提醒：久坐 / 护眼 / 补水 / 运动，各自独立计时。默认间隔：护眼 20min、久坐 45min、补水 60min、运动 120min。
- 交互：常驻小悬浮窗（always-on-top，可拖动，悬停显示四类倒计时，**位置跨重启记忆**）+ 到点在悬浮窗旁**冒出提醒图标**。
- **图标交互**：左键单击 = 该类完成，图标消失并重置计时；**无贪睡**；不点则一直挂着，不自动消失、不升级为弹窗。
- 多类同时到点：图标**从上到下竖排一列**，顺序恒定为 护眼→久坐→补水→运动，各自独立点击。
- 图标以彩色圆底 + 系统字体字形区分四类，**不引入外部图片文件**；带中文 tooltip 与悬停放大反馈；无动画循环、无额外定时器。
- **夜间勿扰时段**：期间不冒新图标；**进入勿扰时自动清空已挂出的残留图标**；勿扰结束后只补偿一次。
- **托盘无「隐藏悬浮窗」选项**（图标是唯一提醒通道，隐藏会导致彻底收不到提醒且无感知）。
- 单一 1Hz 时钟驱动，用绝对墙钟时间判定到点，处理休眠/唤醒。
- 已确认默认策略：暂不开机自启。
- **提示音（v2.1 新增）**：提醒冒图标时播禅意颂钵音，点击完成播一声很轻的滴水音；**默认开启**，托盘可切换、配置可关；多类同时到点**只播一次**；勿扰期间与清残留图标**绝不出声**。纯代码合成，无外部音频文件。
- 配置：支持「勿扰时段」与悬浮窗位置的本地持久化（本地 JSON，不上传）；间隔等其余项预留可配置接缝，界面可后续做。

## 专家团队（项目级 subagents）

| Agent | 职责 |
|-------|------|
| `architect` | 选型技术栈、设计架构与文件结构、产出实施方案 |
| `developer` | 按方案编码实现 |
| `reviewer` | 代码审查与质量把关 |
| `ui-designer` | UI/视觉设计（**苹果设计语言方向**）与 XAML 实现、视觉验收 |

- **分工边界**：`ui-designer` 管视觉与 XAML，`developer` 管逻辑与架构落地。二者可能碰同一批 `Views/*.xaml`，**PM 不得让它们并发改同一文件**，须串行派发。

## Hooks（项目钩子）

- 待技术栈确定后补充（如：保存时 lint/格式化、提交前测试）。当前无。

## 进行中 / 待办

- **v1.1 代码已完成**（B1/B3/O-a~O-g + 去「隐藏悬浮窗」+ 勿扰清图标，全部实测通过，0 警告 0 错误）。
- ✅ **v1.1 已上线**（2026-07-26）：旧进程已终止，新版 exe 已覆盖到 `publish\selfcontained\HealthMaster.exe` 并重新启动，当前运行的就是图标版。
  旧弹窗版备份 `HealthMaster.v1-popup.exe.bak` 已按用户要求删除（v1 弹窗方案不再保留可执行副本，需要时从源码回退重编）。`publish\selfcontained_new\` 与 .pdb 仍在，可删。
- 🚧 **v2 UI 改造进行中**（2026-07-26 启动）：用户反馈「前端整体太丑」，要求向苹果产品观感靠近。
  - ✅ `ui-designer` 已产出设计方案 `docs/UI-DESIGN-v2.md`（恒深色 macOS HUD 材质、Apple System Colors、Segoe UI Variable 优先 + Tabular 数字、三层 Border 伪阴影替代 `Effect`）。
  - ✅ **用户已定的三项决策**：① 悬浮窗**恒深色 HUD**，不做浅色主题；② 批准图标字形 emoji → **自绘矢量 Path**（仍无外部文件）；③ `FloatingViewModel` 结构化改造归 **developer**，`ui-designer` 只管 XAML。
  - 派发顺序（串行，不得并发碰同一批文件）：developer 改 ViewModel/Glyph 数据层 → ui-designer 套 XAML 视觉 → reviewer 审查。
  - ✅ developer 完成：新增 `CountdownRowViewModel` / `IconGeometries.cs` / `ThemeKeys.cs`；`Glyph`(emoji string) → `Icon`(Freeze 的 `Geometry`) + 3 个引用点同步。0 警告 0 错误。
  - ✅ ui-designer 完成 P0：新增 `Themes\Tokens.xaml` / `Dark.xaml` / `Controls.xaml` / `TrayMenuTheme.cs`，`App.xaml` 合并；悬浮窗全量重写（三层 Border 伪阴影、**全窗零 `Effect`**、展开态固定 176 宽避免逐秒重排）；图标 34+Margin6（实测总宽 46.4 DIP）。0 警告 0 错误。
  - 🔎 **ui-designer 抓到的坑（勿重犯）**：`Width="{StaticResource Sz.TimeCol}"` 把 `Double` 喂给需要 `GridLength` 的 `ColumnDefinition` → 展开态首次渲染抛 `XamlParseException`，且发生在 `Measure` 内变成**布局崩溃循环**（几分钟写 30MB 日志）。折叠态完全看不出，只有真展开才炸。已改列 `Auto` + `ContentControl Width=68`。
  - 📐 **用户已定**：伪阴影使卡片视觉边缘内缩 3px，图标间距实际为 9px——**保持 9px 不改**（此尺度下更贴 Apple 观感）。
  - PM 判定：`TrayIconService.cs` 被 ui-designer 动过（using + 一行 `TrayMenuTheme.Apply` + 托盘图标绘制），属纯外观、不涉逻辑，**不算越界**。
  - ✅ **v2 已上线**（2026-07-26 11:10）：旧进程 PID 57396 已终止，新 exe（139.4MB，11:09:50）已覆盖 `publish\selfcontained\HealthMaster.exe` 并启动（PID 57596）。位置记忆核对无误（config `1309.6/832` = 实际显示位置）。用户 `config.json` 全程 SHA256 未变。
    - 旧 emoji 版备份**保留**：`publish\selfcontained\HealthMaster.v1.1-emoji.exe.bak`（139.4MB）。已删构建垃圾：`selfcontained\HealthMaster.pdb`、`selfcontained_new\` 整个目录。
    - ✅ v2 全部改动已 commit + push：`f42e11f`（22 文件，+1793/-189），已推到 `origin/main`。用户验收「看着没有问题」。
  - ⚠️ **仍待用户真鼠标复验**：hover 展开、单击消失、pressed 态、tooltip、托盘右键菜单渲染——本机合成鼠标输入到不了该应用（拿 v1.1 原版对照过，是环境限制非回归）；且只在 125% DPI 目视过，150%/200% 未实机验证。
  - 🔧 **验证手法（重要，省后人时间）**：本机 `SendInput` 合成鼠标**到不了**本应用（多个 agent 卡在这里，误以为无法验证交互），但 **`SetCursorPos` 可以**——已实证能真实驱动 hover 展开。此外 UIA `Invoke` 可覆盖图标点击逻辑（但绕过真实命中测试），`VisualTreeHelper.HitTest` 可验命中链路。
  - 📎 **强杀进程不丢位置记忆**：位置在拖动完成时即落盘，不依赖退出保存。另：WPF 无边框工具窗收不到 `CloseMainWindow()` 的 WM_CLOSE 生效路径，脚本化终止需 `Stop-Process -Force`。
  - ✅ reviewer 审查：**放行、无阻断项**。另跑 4 轮 WPF 运行时探针补上「合成鼠标到不了应用」的空白——点击链路 4 枚图标全部命中且 `Button.Tag` 类别一一对应、方框四角正确穿透；items=4/buttons=4 无重复；悬浮窗全树 `Effect` 计数=0；12s 内 Gen0 GC=0、CPU 与 v1.1 同机对照无回归；`git diff` 确认 code-behind / `ReminderScheduler` / `ConfigStore` / `WorkAreaHelper` 完全未改，v1.1 已修 bug 无一回退。
  - ✅ developer 收尾 S1/S2/S3/S6：删死属性 `ValueText`（每秒白发 5 次通知）、清 Material 旧色死代码、`PickFont` 不再持有 `FontFamily` 实例 + `Dispose` 补释放 `ContextMenuStrip`、`App.xaml` 加合并字典顺序防呆注释。0 警告 0 错误，UIA 冒烟通过。
    - 📎 developer 用对照探针纠正了 reviewer 对 S3 的定性：.NET 8 下旧写法**复现不出崩溃**（GDI+ 的 `Font` 从 family 创建时会拷贝），属「依赖未文档化实现细节的脆弱写法」而非现实 bug。
  - ✅ **S4 已修**（2026-07-26，用户实测反馈「展开框很容易扩展出边界」）：根因是 `FloatingWindow` 为 `SizeToContent="WidthAndHeight"` 而 `Left/Top` 指左上角，**悬停变大 = 向右下方生长**（宽 +59 / 高 +106 DIP），而夹紧只在 `Loaded` 跑一次；**且拖动路径完全无夹紧，越界坐标会被写进 config**。
    - 方案：**锚点位置模型**（非简单的「展开时 `Left -= Δ`」）。`_anchorLeft/_anchorTop` 只在首次放置与**拖动结束**时变、也是唯一被持久化的值；实际 `Left/Top` 每次 `SizeChanged` 由 `ApplyPosition()` 从锚点重算再夹进所在屏工作区。屏幕中间悬停零位移；四缘同一条规则；**数学上不可能漂移**（从固定锚点重算而非累加）。拖动只在 `DragMove()` 返回后夹一次以保持跟手，并回写锚点。
    - 改动：`Views\FloatingWindow.xaml.cs`（模型主体）、`App.xaml.cs`（删掉只跑一次的 `ClampToWorkArea`，改调 `SetPosition`；退出保存改用锚点）、`Services\WorkAreaHelper.cs`（新增 `For(window, centerDip)` 重载，按「即将移动到的位置」判屏，避免贴屏幕交界判到隔壁屏）、`docs\ARCHITECTURE.md` §7.2。
    - 实测：四缘 + 极限位共 9 例、双屏 4 例全过；3 个位置各 15 轮 hover 进出最大偏移 **0.0000 DIP**；拖动跟手误差 ≤0.8 DIP。
    - 📎 记录一个**既有**（非本次引入）现象：混合 DPI 下 WPF 的 `Left/Top` 用主屏标度、`ActualWidth` 用本屏标度，`WorkAreaHelper` 在副屏会把窗口尺寸高估约 25%，即**夹得更保守**（可能留空隙，绝不会越界），故未动。
  - 📋 **v2.1 待办**（reviewer 提出，本轮未做）：**S5** 伪阴影 3px 偏移（用户已定保持，仅记录）；**O-a** `S.Badge` 默认色是绿色，`Type` 绑定失效会静默全绿而非报错；**O-b** `ToolTip` 是无 key 的应用级隐式样式且用 `TemplateBinding`，塞非 string 会空白；**O-c** `Dark.xaml` 全部 Brush 未 `Freeze`；**O-d** `TrayMenuTheme` 的 4/12 是设备像素常量，高 DPI 下偏窄；**O-f** 文案精简为「距下次」（设计稿建议，未落地）。
- 🚧 **v2.1 提示音进行中**（2026-07-26）：上一会话已产出 `ZenTone` 合成原型（scratchpad，三候选 颂钵/磬/滴水 + zen-1 三档响度增强 a/b/c），用户试听后选定 **c**。
  - ✅ 用户已定的三项需求：① 默认开启；② 多类同时到点只响一次；③ 点击完成播一声很轻的反馈音（用滴水，峰值 0.11 vs 提醒音 0.89，约 1/4 感知响度）。
  - ✅ developer 完成：新增 `Resources\ZenTones.cs` / `Services\SoundService.cs`，改 `IReminderScheduler`+`ReminderScheduler`（新增 `RemindersDueBatch`）/`AppConfig`(`SoundEnabled`)/`ConfigStore`(`SaveSoundEnabled` 定向保存)/`TrayIconService`/`Strings`/`App.xaml.cs`/`docs`+`README`。0 警告 0 错误；SHA256 与原型一致；逻辑层 + 端到端 GUI 冒烟全过（含 S1/S2 未回归、`Views\*.xaml` 与 S4 锚点模型未被碰）。
  - ✅ reviewer 审查：**放行、无阻断项**。独立复核用了比 developer 更强的口径（`dotnet build -c Release` 出真实 dll + `Assembly.LoadFrom` 反射调用，验的是构建产物而非源文件），SHA256 仍逐字节一致；反射驱动私有 `Evaluate` 跑 10 个场景（含休眠 3h 唤醒、勿扰前入睡/勿扰后醒、暂停/恢复）零误发声零漏发声；1500 次播放句柄稳定无泄漏；UI 首帧与 1Hz 抖动 A/B 对照无回归。提出 A1/A2 + S1–S4。
  - ✅ developer 收尾（用户已确认三项取舍：文案改动作式、winmm 截断可接受、音量不可调可接受）：修 **A1**（惰性预热，关声音 CPU 6.17s→**0.90s**，≈对照组 0.87s）、**S2**（关声音打断当前播放，Core Audio 实测关后回落基线）、**S1**（冷路径只挂一个续体、只留最后一次请求）、**S3**（托盘动作式文案）、**A2/S4**（文档口径统一 + 补首次播放固有开销）。0 警告 0 错误，SHA256 未变，8 线程并发切换实测预热恒只跑一次。
  - ✅ **v2.1 已上线**（2026-07-26 15:38）：commit `aedf9cc`（12 文件，+793/-26）已推到 `origin/main`；旧进程 PID 54316 已 `Stop-Process -Force`，新 exe（139.4MB，15:38:12）已覆盖 `publish\selfcontained\HealthMaster.exe` 并启动（PID 2716）。全程用户 `config.json` SHA256 `f1b11fe7…0082` **未变**，位置 `1353.6/769.6` 与勿扰设置原样保留。
    - 备份：`HealthMaster.v2-nosound.exe.bak`（上线前的无声版）+ 既有 `HealthMaster.v1.1-emoji.exe.bak`，各 139.4MB，**待用户定夺是否删旧的那个**。
    - 上线后实测（PID 2716）：空闲 CPU 0.32%（前几次采样 1.9%→1.0%→0.47%→0.32%，逐步收敛），工作集 157.6→172.3MB / 私有 96.8MB，增速逐次减半（0.31→0.019 MB/s）呈收敛态，句柄 505→491 稳中有降、线程 26–31 稳定——**判定为正常预热增长而非泄漏**，但长时间运行的内存曲线尚未观察，后续可复查。
    - 📎 `git push` 走凭据管理器会弹框；本轮用用户一次性提供的 PAT 以完整 URL 推送（**未落进 `.git\config`**，已核对）。副作用：**用 URL 推送不更新 `origin/main` 追踪引用**，`git status` 会假报 ahead，需补 `git fetch origin`。
    - ✅ 用户已复验托盘新菜单项「查了没问题」。仍待复验：真实到点时的听感。
    - ✅ 已按用户要求删除 `HealthMaster.v1.1-emoji.exe.bak`；现存备份仅 `HealthMaster.v2-nosound.exe.bak`（上线前的无声版）。

- 🔥 **重大踩坑：不要在沙箱化的 shell 里启动本应用**（2026-07-26，害用户以为悬浮窗丢了）
  - 现象：`Start-Process HealthMaster.exe` 后进程活着、UI 线程响应 `WM_NULL`、`IsWindowVisible=True`、`GetWindowRect` 坐标正确、topmost z-order 正常、`DWMWA_CLOAKED=0`、**`PrintWindow(PW_RENDERFULLCONTENT)` 还能抓到完整正确的 HUD 药丸**——唯独**屏幕上什么都没有**，且鼠标完全穿透（`WindowFromPoint` 打到底下的窗口）。
  - 根因：悬浮窗是 `AllowsTransparency=True` 的**分层窗口 + 软件渲染路径**，靠 `UpdateLayeredWindow` 提交画面。在沙箱化 shell 里启动时这条合成路径失效，而 `PrintWindow` 走的是进程内直接渲染可视树，**绕过了合成，所以照样成功**——两者一真一假，极易误判。
  - 解法：**用 `dangerouslyDisableSandbox: true` 启动**（实测同一个 exe、同一份 config，换非沙箱启动立刻正常上屏并可命中）。`SetWindowPos`/`RedrawWindow` 强制重绘**无效**，别浪费时间。
  - 📎 连带的排查坑：**`Graphics.CopyFromScreen` / 普通 `BitBlt` 抓不到分层窗口**，会拍出"窗口不存在"的假证据。必须用 `BitBlt` 加 `CAPTUREBLT`(0x40000000) 标志。我第一张截图就是这么误导自己的。
  - 📎 判断"是否真的上屏"最可靠的单一指标：`WindowFromPoint(窗口中心)` 的根窗口**是不是它自己**。`IsWindowVisible` 与 `PrintWindow` 都会骗人。

- v1.2 待办：O1 托盘 HICON 释放 / O3 全天勿扰 / O4 配置热加载 / O5 记录项；设置界面、开机自启、提示音。（**O2 多屏夹紧已并入 v1.1 的 B1 修复**）

## 关键实现事实（供后续参考）

- SDK 为**用户级安装**（`C:\Users\xiao\.dotnet`，dotnet 8.0.423），非管理员权限；用前需把 `%USERPROFILE%\.dotnet` 加入 PATH。
- 源码：`src\HealthMaster\`；启动 `dotnet run --project src\HealthMaster\HealthMaster.csproj`；打包见 `README.md`（自包含单文件约 140MB / 依赖框架版极小）。
- 配置：`%APPDATA%\HealthMaster\config.json`（勿扰时段、悬浮窗位置、间隔覆盖、`SoundEnabled`）。**勿扰默认关闭**，需手改 `Enabled:true`；**提示音默认开启**。
- 未做：设置界面、开机自启。

### 提示音（v2.1）关键事实

- 声音**纯代码合成**，仓库与产物里**没有任何音频文件**，依赖仍为零。`Resources\ZenTones.cs` = 非谐波泛音物理模型 + 拍频 + 相位优化（最小化波峰因数）+ 解析包络上行压缩 + tanh 软限幅。
- ⚠️ **`ZenTones.cs` 里的固定随机种子（`915231` / 48 次相位试验 / `20260726`）与全部配方常量不可改动**——改任何一个声音就变了。用户是**逐个试听后选定**「强化版 c」（原型 `LoudBowl.C`）的，落地代码产出的 WAV 与原型 `zen-1-loud-c.wav` **SHA256 逐字节一致**（`26177E75…8111`，255824 字节）。日后改动此文件必须重跑该哈希比对。
- **「只播一次」落在调度器而非 UI 时间窗**：`ReminderScheduler.Evaluate` 内汇总 `anyDue`，一拍末尾只发一次 `RemindersDueBatch`。副作用是**静音成了结构保证**——`RemindersReset`（进入勿扰清残留 / 暂停 / 恢复）根本不走这条路径，勿扰内到点只打标记不发事件，无需任何额外判断就必然无声。**不要把播音改回按图标增减触发**，那会同时打破「只响一次」与勿扰静音两条。
- 合成放**专用后台线程 + `BelowNormal` 优先级**预热，不进线程池（Release 实测约 4.4–4.5s；Debug 约 6.6s，含分层 JIT——**看到 6.6s 别当回归**）。预热末尾做**一次**（非周期）`GC.Collect(compacting)` 归还 5.4MB LOH，耗时 0.6–1.1ms。未新增任何定时器。
- ⚠️ **预热必须惰性**：`SoundEnabled=false` 时**不得**预热。曾经 ctor 无条件起预热线程，导致关了声音仍每次启动烧 6.17s CPU + 24MB（对照组仅 0.87s），顶在红线 2 上。现为 `Interlocked.CompareExchange` 保证全生命周期至多预热一次，托盘 false→true 时才惰性触发；修复后关声音回落到 **0.90s**（≈对照组）。**改 `SoundService` 时别把这条退回去。**
- 常驻开销两个口径别搞混：**从未播放过**时工作集 +5.2MB；**首次 `Play` 后**会拉起 Windows 音频栈，再 +约 100 句柄 / +6 线程 / 工作集 +约 11MB，此后稳定不涨——**这是播任何声音的固有成本，不是泄漏**（1500 次播放句柄稳定在 312–321）。
- 托盘用**动作式文字**「关闭提醒声音 / 开启提醒声音」（描述"点下去会发生什么"，与紧邻的「暂停全部」方向一致），而非勾选框——`TrayMenuTheme` 刻意关掉了 `ShowImageMargin`/`ShowCheckMargin`，勾选标记无处可画。**用户已确认**；要真勾选框须先派 ui-designer 调边距槽。
- 托盘关声音会 `Stop()` **打断正在响的那一声**（用户被吵到去关，最想要的就是立刻停）。
- 📎 **已知限制（用户已确认「问题不大」，勿当 bug 修）**：① winmm 进程内只有一个播放槽，提醒音（2.9s）未播完时点图标会被完成音截断；② 音量不可调，只有开/关，接缝在 `ZenTones` 的 `TargetPeak` 常量。
- 📎 **验证手法**：判断"是否真出声"别靠人耳，用 Core Audio `IAudioMeterInformation` 峰值表客观测量——静默基线约 0.001，两条音轨设计峰值 0.89 / 0.11 会在测量里精确复现。
- 🔧 **测试隔离的坑（已踩过一次，四组 CPU 数据全糊、结论完全错）**：**给子进程设 `APPDATA` 环境变量对本项目无效**——`Environment.GetFolderPath(SpecialFolder.ApplicationData)` 走 `SHGetFolderPath` 读已知文件夹，**不看环境变量**，于是测试进程照样读到用户真实 config。正确做法：在**临时副本源码**里给 `ConfigStore` 加 `HM_TEST_APPDATA` 环境变量开关（生产代码不含此钩子），并另换单实例互斥名。
- **成品 exe（自包含单文件，约 139MB，双击即用）**：`publish\selfcontained\HealthMaster.exe`（同目录 .pdb 可删）。覆盖前先备份，且需用户先退出正在运行的旧版进程（否则文件被占用）。
- 提醒图标为**独立的无边框透明 Topmost 窗**（`ReminderBadgeWindow`），AttachTo 悬浮窗跟随其位置/显隐，刻意不塞进悬浮窗内部，以免动到悬浮窗的尺寸与 Left/Top 语义（位置记忆逻辑依赖它）。
- WPF 不渲染彩色 emoji。~~v1.1 图标字形为单色白 emoji 字~~ → **v2 起改为自绘矢量 `Path`**（`Resources\IconGeometries.cs`，24×24 视框的 Path Mini-Language 常量，纯填充造型；护眼用 `F0`/EvenOdd 挖空瞳孔）。仍无任何外部图片/字体文件，靠彩色圆底区分四类。`ReminderBadgeItem.Glyph`(string) 已删除，改为 `Icon`(已 Freeze 的 `Geometry`)。
- ⚠️ **两处易被改坏的刻意设计**（改前先看 `docs/ARCHITECTURE.md` 说明）：
  1. 图标窗 `Width="46"` 写死 + `SizeToContent="Height"`——绕开无边框窗 min-track 钳制导致宽度不收敛。**不要改回 `WidthAndHeight`**。
  2. 图标增减经 `Dispatcher.BeginInvoke(DispatcherPriority.Loaded)` 延后——同步调用会重入 ItemsControl 容器生成器产生**重复图标**（实测 items=4/buttons=5）。锚点移动/尺寸变化仍同步，保证拖动跟手。
- `Services\WorkAreaHelper.cs`：WinForms `Screen` + 按窗口 DPI 换算 DIP，`App.ClampToWorkArea` 与图标窗共用，解决多屏被夹回主屏。

## 已归档（完成的任务）

- ✅ 需求对齐 + 项目骨架 + 红线 + 专家团队（architect/developer/reviewer）
- ✅ architect：技术选型（.NET 8 + WPF）+ 架构方案 `docs/ARCHITECTURE.md` + 默认间隔建议
- ✅ developer：SDK 用户级安装 + v1 全部 9 条确认实现，`dotnet build` 0 警告 0 错误，冒烟启动通过
- ✅ reviewer：审查 v1，结论放行（无阻断），提出 S1/S2/S3 + O1–O5
- ✅ developer：修复 S1（退出不再覆盖手改的勿扰/间隔，仅定向保存悬浮窗位置）/ S2（原子写 + 损坏配置备份回退）/ S3（悬浮窗隐藏时停刷新），编译通过 + 断言验证通过
- ✅ v1.1 需求对齐：用户反馈弹窗打断工作，确认改为「悬浮窗旁冒图标 + 单击完成」，彻底取消弹窗与贪睡
- ✅ developer：实现图标提醒改造（新增 ReminderBadgeWindow/ReminderBadgesViewModel/ReminderBadgeItem，删除 ReminderPopupWindow/PopupQueue/NativeMethods），0 警告 0 错误
- ✅ reviewer：审查改造，结论放行（无阻断），提出 B1–B5 + O-a–O-g
- ✅ developer：修复 B1（多屏，抽出 WorkAreaHelper）/ B3（倒计时不再卡 00:00）/ O-a（空白穿透）/ O-b（WS_EX_NOACTIVATE 不抢焦点）/ O-c/O-e/O-f/O-g + 去掉「隐藏悬浮窗」+ 勿扰清残留图标 + 修掉重复图标与宽度不收敛，逐项运行时实测通过
