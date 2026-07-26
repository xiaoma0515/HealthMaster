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
- 📌 **v2 方向（用户已明确）**：UI 整体向**苹果设计语言**升级，已新建 `ui-designer` 专家承接。尚未开工，待用户发起。
- v1.2 待办：O1 托盘 HICON 释放 / O3 全天勿扰 / O4 配置热加载 / O5 记录项；设置界面、开机自启、提示音。（**O2 多屏夹紧已并入 v1.1 的 B1 修复**）

## 关键实现事实（供后续参考）

- SDK 为**用户级安装**（`C:\Users\xiao\.dotnet`，dotnet 8.0.423），非管理员权限；用前需把 `%USERPROFILE%\.dotnet` 加入 PATH。
- 源码：`src\HealthMaster\`；启动 `dotnet run --project src\HealthMaster\HealthMaster.csproj`；打包见 `README.md`（自包含单文件约 140MB / 依赖框架版极小）。
- 配置：`%APPDATA%\HealthMaster\config.json`（勿扰时段、悬浮窗位置、间隔覆盖）。**勿扰默认关闭**，需手改 `Enabled:true`。
- 未做：设置界面、开机自启、提示音。
- **成品 exe（自包含单文件，约 139MB，双击即用）**：`publish\selfcontained\HealthMaster.exe`（同目录 .pdb 可删）。覆盖前先备份，且需用户先退出正在运行的旧版进程（否则文件被占用）。
- 提醒图标为**独立的无边框透明 Topmost 窗**（`ReminderBadgeWindow`），AttachTo 悬浮窗跟随其位置/显隐，刻意不塞进悬浮窗内部，以免动到悬浮窗的尺寸与 Left/Top 语义（位置记忆逻辑依赖它）。
- WPF 不渲染彩色 emoji，图标字形为单色白字，靠彩色圆底区分四类。
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
