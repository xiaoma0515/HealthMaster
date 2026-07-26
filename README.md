# Health Master — 健康提醒小助手

Windows 11 桌面端健康提醒工具：常驻小悬浮窗（悬停显示倒计时）+ 到点在悬浮窗旁冒出提醒图标（**非打断式**，单击即"已完成"）。
四类提醒各自独立计时：**护眼 / 久坐 / 补水 / 运动**。

- 技术栈：C# / .NET 8 + WPF，**零第三方依赖**，纯本地运行（不联网、不上传任何数据）。
- 平台：Windows 11（x64）。界面：简体中文。

---

## 功能一览（v1）

| 能力 | 说明 |
|------|------|
| 四类提醒 | 护眼 20 分钟、久坐 45 分钟、补水 60 分钟、运动 120 分钟（默认间隔，可改） |
| 常驻悬浮窗 | 无边框、置顶、可拖动；**鼠标悬停展开四路倒计时**；窗口位置**跨重启记忆** |
| 到点提醒图标 | **不弹窗、不抢焦点**：在悬浮窗旁冒出该类图标（emoji 字形 + 彩色圆底，悬停放大并显示 tooltip）；**左键单击 = 已完成**，图标消失并重置该类计时；不点则一直挂着 |
| 多类同时到点 | 多枚图标**从上到下竖排一列**（护眼 / 久坐 / 补水 / 运动 顺序固定），各自独立点击 |
| 夜间勿扰时段 | 可配置一段时间（可跨零点），期间不冒图标；勿扰结束后对错过的每类**只补偿提醒一次**（不连环提醒） |
| 休眠 / 唤醒 | 单一 1Hz 时钟 + **绝对墙钟时间**判定到点；订阅电源/会话事件，唤醒/解锁后立即重算 |
| 托盘菜单 | 显示/隐藏悬浮窗、暂停/恢复全部、打开配置文件夹、退出 |

v1 **不做**：开机自启、提示音、完整设置界面（预留接缝，见下）。

---

## 如何运行

### 前置：.NET 8 SDK（用户级，无需管理员）

本机若无 SDK，可用官方脚本装到用户目录（不改系统目录）：

```powershell
Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile "$env:TEMP\dotnet-install.ps1"
& "$env:TEMP\dotnet-install.ps1" -Channel 8.0 -Quality GA -InstallDir "$env:USERPROFILE\.dotnet"
$env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"   # 本会话生效
```

（仅运行、不开发时，只需 **.NET 8 Desktop Runtime**；开发/构建才需 SDK。）

### 编译并运行

```powershell
cd C:\Users\xiao\Health_master
dotnet build HealthMaster.sln -c Debug
dotnet run --project src\HealthMaster\HealthMaster.csproj
```

启动后：屏幕右下角出现小悬浮窗；鼠标移上去展开四路倒计时；系统托盘出现绿色图标（右键有菜单）。
程序常驻后台，**只从托盘「退出」真正退出**。

---

## 如何打包成 exe

在项目根目录执行其一：

**A. 自包含单文件（推荐分发，目标机零预装）**

```powershell
dotnet publish src\HealthMaster\HealthMaster.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish\selfcontained
```

产物：单个 `HealthMaster.exe`（约 140 MB，已内含 .NET 运行时），双击即用。

**B. 依赖框架单文件（体积极小，目标机需装 .NET 8 Desktop Runtime）**

```powershell
dotnet publish src\HealthMaster\HealthMaster.csproj -c Release -r win-x64 `
  --self-contained false -p:PublishSingleFile=true `
  -o publish\framework
```

产物：很小的 `HealthMaster.exe`；本机已有 8.0.x Runtime 时可直接跑。

---

## 配置文件（本地，纯离线）

首次拖动悬浮窗或正常退出后，配置写入：

```
%APPDATA%\HealthMaster\config.json
```

v1 无设置界面，可**手动编辑此 JSON**。**重要：手改 `config.json` 后需重启程序才生效**（程序仅在启动时读取一次；运行期间程序只会定向写回悬浮窗位置，不会覆盖你手改的勿扰 / 间隔设置）。示例：

```json
{
  "SchedulerVersion": 1,
  "IntervalMinutes": { "Eye": 20, "Sedentary": 45, "Water": 60, "Exercise": 120 },
  "FloatingX": 1720.0,
  "FloatingY": 980.0,
  "DoNotDisturb": {
    "Enabled": true,
    "Start": "22:00",
    "End": "07:00"
  }
}
```

- `IntervalMinutes`：各类间隔（分钟）。键为 `Eye/Sedentary/Water/Exercise`，缺省用内置默认值。
- `DoNotDisturb`：夜间勿扰时段。`Enabled` 默认 `false`；置 `true` 并设 `Start`/`End`（`HH:mm`，可跨零点）即启用。
- `FloatingX/Y`：悬浮窗记忆位置，一般无需手改。

托盘菜单「打开配置文件夹」可直接定位到该目录。
异常日志（若有）写入 `%APPDATA%\HealthMaster\logs\`。**所有文件均在本机，不联网、不上传。**

---

## 项目结构

```
Health_master/
├─ HealthMaster.sln
├─ README.md
├─ docs/ARCHITECTURE.md              # 架构设计基线
└─ src/HealthMaster/
   ├─ HealthMaster.csproj
   ├─ app.manifest                   # Per-Monitor V2 DPI
   ├─ App.xaml / App.xaml.cs         # 入口：单实例、托盘、组装、全局异常
   ├─ Models/                        # ReminderType/Definition/State/AppConfig
   ├─ Services/                      # 调度、配置读写、勿扰判定、电源监听、托盘
   ├─ ViewModels/                    # FloatingViewModel、ReminderBadgesViewModel/Item
   ├─ Views/                         # FloatingWindow、ReminderBadgeWindow（提醒图标）
   └─ Resources/Strings.cs           # 集中中文文案
```

---

## 已知限制（v1）

1. **无设置界面**：勿扰时段、间隔等通过手动编辑 `config.json` 调整（预留了 `IConfigProvider` 接缝，后续可加 UI）。
2. **勿扰默认关闭**：需在配置里把 `DoNotDisturb.Enabled` 设为 `true` 才生效。
3. **置顶**：极少数全屏独占程序（如某些游戏/放映）可能压过悬浮窗与提醒图标，属系统 Topmost 限制，v1 未做额外强制前台 hack。
4. **无开机自启、无提示音**（v1 已定策略）。提醒为**非打断式**：只冒图标、不弹窗、不闪任务栏、不发声，用户不点则一直挂着。
5. **运动提醒**采用固定间隔（默认 120 分钟）近似「每日目标」，非按日历目标建模。
6. **自包含 exe 体积约 140 MB**（WPF + 运行时打包所致）；对体积敏感可用「依赖框架」方式（方案 B）。
7. 悬浮窗位置在**拖动完成时**或**正常退出时**保存；若进程被强制结束（任务管理器 kill），最后一次未保存的位置可能丢失。
