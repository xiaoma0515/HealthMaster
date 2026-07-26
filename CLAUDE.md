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
- 已确认默认策略：暂不开机自启、暂不加提示音（后续可加）。
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
    - ⚠️ v2 改动**尚未 git commit**，工作区有 11 个 M + 5 个未跟踪（`Themes\`、`Resources\IconGeometries.cs` 等），待用户定夺。
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
- v1.2 待办：O1 托盘 HICON 释放 / O3 全天勿扰 / O4 配置热加载 / O5 记录项；设置界面、开机自启、提示音。（**O2 多屏夹紧已并入 v1.1 的 B1 修复**）

## 关键实现事实（供后续参考）

- SDK 为**用户级安装**（`C:\Users\xiao\.dotnet`，dotnet 8.0.423），非管理员权限；用前需把 `%USERPROFILE%\.dotnet` 加入 PATH。
- 源码：`src\HealthMaster\`；启动 `dotnet run --project src\HealthMaster\HealthMaster.csproj`；打包见 `README.md`（自包含单文件约 140MB / 依赖框架版极小）。
- 配置：`%APPDATA%\HealthMaster\config.json`（勿扰时段、悬浮窗位置、间隔覆盖）。**勿扰默认关闭**，需手改 `Enabled:true`。
- 未做：设置界面、开机自启、提示音。
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
