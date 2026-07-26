# Health Master v2 视觉设计方案（Apple 观感）

> 作者：ui-designer ｜ 状态：**方案稿，未落任何代码**（本轮不修改任何源文件）
> 基线：`src/HealthMaster/` v1.1 现行代码；红线依据 `CLAUDE.md`；实现约束依据 `docs/ARCHITECTURE.md` §7/§8
> 目标：把悬浮窗 / 提醒图标 / 托盘菜单的观感从「Material + 默认 WinForms」拉到 **Apple HIG / macOS HUD** 一侧，
> 同时**一步不越**红线 2（轻量低占用）与红线 5/6（非打断、零第三方）。

---

## 1. 现状诊断：具体丑在哪

### 1.1 全局

| # | 问题 | 位置 | 说明 |
|---|------|------|------|
| G1 | **没有任何设计 token** | `App.xaml:5-6`（`Application.Resources` 空） | 颜色、圆角、间距、字号全部硬编码散在两个 XAML 里，既无法统一，也做不了浅色/深色。这是所有其它问题的根因 |
| G2 | **字体族顺序把中文放在了首位** | `FloatingWindow.xaml:13`、`ReminderBadgeWindow.xaml:16`（`"Microsoft YaHei UI, Segoe UI"`） | 拉丁字符与**数字**都会用微软雅黑的字形。雅黑数字**非等宽**，倒计时每秒 `1→8` 宽度变化，文本整体左右抖动；且雅黑的西文观感与 Apple 相去甚远 |
| G3 | **无 Tabular Figures** | 同上 | 未设 `Typography.NumeralAlignment="Tabular"`，也没有固定宽度的数字列，四行时间列参差 |
| G4 | **层级只靠字号，没有字重层级** | `FloatingWindow.xaml:20,46-50` | 全局只有 13 / 11 两个字号，字重除折叠态一处 `SemiBold` 外全是默认 Normal。Apple 的层级主要靠 **字重 + 色阶（label / secondaryLabel / tertiaryLabel）**，当前二者都缺 |

### 1.2 常驻悬浮窗（`Views/FloatingWindow.xaml`）

| # | 问题 | 位置 |
|---|------|------|
| F1 | 背景 `#E6202020` 是**纯灰黑平涂**：无内描边、无外阴影、无高光，贴在任何桌面上都像一块「贴纸」，缺乏 macOS HUD 的材质分层 | :16 |
| F2 | `CornerRadius="10"` 对一个高约 30px 的卡片偏小、偏「方」；且与图标的 18px 圆角不成比例体系（无圆角尺度） | :16 |
| F3 | `Padding="14,9"` 不在 4pt 栅格上；行间 `Margin="0,1"`（13px 字 + 2px 行距）**行距过密**，四行挤成一坨；`Header` 下留白 5px 也不成节奏 | :16,46-50 |
| F4 | Header 用 `#8FD08F`（一个来历不明的浅绿）做小标题色，**高饱和色被用在了最次要的信息上**，视觉重心完全反了 | :46 |
| F5 | **名称与时间用三个空格拼接**（`$"{name}   {time}"`），时间列无法右对齐，四行数字列是歪的。这是最刺眼的排版硬伤，但它在 **ViewModel** 里，不是 XAML 能修的 | `ViewModels/FloatingViewModel.cs:68,72` |
| F6 | 四类之间**没有任何类别识别**（无色点、无图标），四行长得一模一样，只能读字 | :47-50 |
| F7 | **无鼠标反馈**：除了「展开」这个大动作，hover 时卡片本身零反应；无按下反馈；拖动时也无任何提示 | 整个文件 |
| F8 | 「已暂停」「待完成」仅靠文字，**无视觉状态区分**（无降饱和、无胶囊底），暂停状态一眼看不出 | `FloatingViewModel.cs:45,62,68-72` |
| F9 | 折叠 ↔ 展开是 `Visibility` 硬切，配合 `SizeToContent="WidthAndHeight"` 是一次**瞬时尺寸跳变**，没有任何过渡，很"廉价" | :22-45 |

### 1.3 提醒图标窗（`Views/ReminderBadgeWindow.xaml`）

| # | 问题 | 位置 |
|---|------|------|
| B1 | 四色取自 **Material Design 500/800 级**（`#2E7D32` / `#EF6C00` / `#0277BD` / `#6A1B9A`），深、闷、饱和度高，是典型 Android 观感；Apple 系统色更亮更"糖果"且明度更高 | `ViewModels/ReminderBadgesViewModel.cs:23-26` |
| B2 | 圆底是**纯色平涂 + 1px 白描边 `#59FFFFFF`**：白描边在浅色桌面上完全无效，在深色桌面上又显得脏；没有 Apple 图标那种极轻的顶部高光与外阴影 | `.xaml:39-40` |
| B3 | **无外阴影**，图标直接"贴"在桌面上，压在花哨壁纸上边界不清、可读性差 | `.xaml:37-46` |
| B4 | hover 反馈是**瞬时**跳到 `ScaleX/Y=1.15` **且描边从 1px 加粗到 2px** —— 无过渡、生硬；描边加粗还会让圆边"胖一圈"，观感像 bug | `.xaml:48-56` |
| B5 | pressed 只是 `Opacity=0.75`，**方向错了**：Apple 的按下是「轻微收缩 + 略微压暗」，纯降透明度会让图标"消失"而不是"被按下" | `.xaml:57-59` |
| B6 | emoji 字形 👁 🚶 💧 🤸 在 WPF 下是**单色白字**，且四个字形的视觉重量差异极大（🤸 线细面积大、💧 面积很小、🚶 在 17px 下细节糊成一团）。这是 `ARCHITECTURE.md` §8.7 已承认的限制 | `Services/DefaultConfigProvider.cs:23-26` |
| B7 | 图标间距 = 上下 `Margin="5"` 相邻叠加 = 10px，对 36px 的圆略挤，竖排时像一串"糖葫芦" | `.xaml:77` |

### 1.4 托盘（`Services/TrayIconService.cs`）

| # | 问题 | 位置 |
|---|------|------|
| T1 | `ContextMenuStrip` 用 **WinForms 默认渲染**：亮白底、直角、左侧一条多余的「图标边距空槽」、默认 9pt 系统字体、分隔线横穿整宽 —— 与 Win11 和 Apple 观感都脱节，是全项目最土的一块 | :32-41 |
| T2 | 托盘图标 32×32 上画 3.5px 粗白十字，缩到 16px 显示时**线条发糊、边缘毛刺**；绿圆没有内边距余量，贴边被削 | :77-92 |

---

## 2. 设计语言定义

### 2.1 字体（不引入任何字体文件）

Windows 上**没有也不可分发 SF Pro**。近似方案：

```
主字体族串： "Segoe UI Variable Text, Segoe UI, Microsoft YaHei UI"
数字/大标题： "Segoe UI Variable Display, Segoe UI, Microsoft YaHei UI"
```

- **顺序必须把西文字体放在最前**（当前代码是反的，见 G2）：WPF 按族串顺序做**逐字符回退**，
  西文与数字取 Segoe UI Variable（Win11 自带、可变字重轴、几何感最接近 SF Pro），中文自动回退到微软雅黑 UI。
- `Segoe UI Variable` 三个光学尺寸子族在 Win11 22H2+ 均自带：`Small`（≤12px）/ `Text`（12–18px）/ `Display`（≥18px）。
  本项目字号都在 11–15px，**统一用 `Text`，11px 处用 `Small`**。
- **等宽数字**：所有倒计时 `TextBlock` 加 `Typography.NumeralAlignment="Tabular"`，
  **并且**把时间放进固定宽度列（`Width=64` + `TextAlignment="Right"`）双保险 —— 若某台机器缺 Segoe UI Variable 而回退到雅黑，
  固定列宽仍能保证不抖动。

**字体层级（Type Scale）**

| Token | 字号 | 字重 | 色阶 | 用途 |
|---|---|---|---|---|
| `Fs.Caption` | 11 | Medium (500) | tertiary | 展开态小标题「距下次」 |
| `Fs.Body` | 13 | Regular (400) | primary | 四类名称 |
| `Fs.Mono` | 13 | Medium (500) | primary | 倒计时数字（Tabular） |
| `Fs.Compact` | 13 | Semibold (600) | primary | 折叠态主文本 |
| `Fs.Badge` | 18 | — | — | 图标内矢量图形的 Viewbox 尺寸 |

> WPF 对可变字体的字重轴支持有限，`FontWeight="Medium"` 会命中 Segoe UI Variable 的 Medium 实例，实测可用；
> 若某字重缺失会回退到最近字重，属可接受降级。

### 2.2 色板（浅色 / 深色两套，同名 key）

> **重要设计主张**：悬浮窗与提醒图标是浮在**任意桌面壁纸**之上的 HUD，不是文档窗口。
> macOS 的 HUD 面板（音量/亮度 OSD、Spotlight 的深色态）**恒为深色材质**，这是 Apple 自己的做法。
> 故建议：**悬浮窗默认走 HUD 深色，浅色主题下只降低不透明度、提亮描边，而不整体翻白**。
> 下表仍给出完整的浅色一套（供未来的设置窗口使用，且满足「双主题」要求）。

**中性色（Neutrals）**

| Token | Dark（默认 HUD） | Light | 说明 |
|---|---|---|---|
| `Brush.Card.Bg` | `#D91C1C1E` (α85%) | `#F0F7F7F9` (α94%) | 卡片主体填充 |
| `Brush.Card.BgHover` | `#E62A2A2C` | `#F5FFFFFF` | 卡片 hover 提亮 |
| `Brush.Card.StrokeInner` | `#1FFFFFFF` | `#B3FFFFFF` | 1px 内高光描边（Apple 的"上缘光"） |
| `Brush.Card.StrokeOuter` | `#33000000` | `#1A000000` | 1px 外定界描边 |
| `Brush.Text.Primary` | `#F2FFFFFF` | `#FF1D1D1F` | labelColor |
| `Brush.Text.Secondary` | `#99EBEBF5` | `#993C3C43` | secondaryLabel（Apple 原值） |
| `Brush.Text.Tertiary` | `#4DEBEBF5` | `#4D3C3C43` | tertiaryLabel |
| `Brush.Fill.Quaternary` | `#14FFFFFF` | `#0A000000` | 「待完成 / 已暂停」胶囊底 |
| `Brush.Shadow.1/2/3` | `#26000000` / `#1A000000` / `#0F000000` | 同 | 三层伪阴影（见 2.5） |

**语义强调色（四类，取 Apple System Colors 并按明暗微调）**

| 类别 | Light 主色 | Dark 主色 | 圆底渐变（上 → 下） |
|---|---|---|---|
| 护眼 | `#34C759` systemGreen | `#30D158` | `#3FD06A` → `#24A94E` |
| 久坐 | `#FF9500` systemOrange | `#FF9F0A` | `#FFA51F` → `#F08300` |
| 补水 | `#32ADE6` systemCyan | `#64D2FF` | `#43BDF0` → `#1E9AD6` |
| 运动 | `#5856D6` systemIndigo | `#5E5CE6` | `#6B69E0` → `#4A48C4` |

> 圆底用 **2 停靠点垂直线性渐变**（约 ±8% 明度）而非平涂 —— 这是 macOS/iOS 图标最典型的"微立体"手法，
> 成本为零（`LinearGradientBrush` 是几何填充，非 shader）。
> 悬浮窗行首的类别小圆点用**纯主色**（不用渐变），避免 6px 尺寸下渐变变脏。

### 2.3 圆角体系

| Token | 值 | 用途 |
|---|---|---|
| `R.xs` | 4 | 极小指示件 |
| `R.s` | 7 | 「待完成 / 已暂停」胶囊（高 14 → 半高 7，实际为全圆角） |
| `R.m` | 10 | — |
| `R.l` | 13 | **悬浮窗卡片**（折叠 30 高 / 展开 138 高共用，保持形状语言一致） |
| `R.xl` | 20 | 未来设置窗 |
| `R.full` | h/2 | 提醒图标圆底（17） |

**关于 squircle（连续圆角）**：WPF 的 `CornerRadius` 是标准圆弧，做不出 Apple 的连续曲率。
真要做需用 `Path` + 三次贝塞尔手绘超椭圆（控制点系数 ≈0.55 对应 Apple 约 60% 平滑度）。
**结论：不做。** 在 13px 半径、30px 高的卡片上，squircle 与普通圆角的差异肉眼不可辨，
而代价是失去 `Border` 的 `Padding`/`Background`/命中测试便利，还要自己处理 DPI。收益 ≪ 成本。

### 2.4 间距节奏（4pt 栅格）

允许值：`2 / 4 / 6 / 8 / 12 / 16 / 20`。禁止再出现 9、14、5 这类离格数值（除非受 §5 的 46 宽度约束）。

| Token | 值 | 用途 |
|---|---|---|
| `Sp.1` | 4 | 图标与文字的最小间隙 |
| `Sp.2` | 8 | 色点 → 名称、行内元素间距 |
| `Sp.3` | 12 | 卡片水平内边距、图标间距 |
| `Sp.4` | 16 | 展开态卡片水平内边距 |
| `Row.H` | 22 | 展开态每行行高（13px 字 + 上下各 ~4.5） |

### 2.5 材质、描边与阴影（**性能红线关键节**）

**结论先行：悬浮窗禁用 `DropShadowEffect` / `BlurEffect`；提醒图标窗可以用轻量 `DropShadowEffect`。**

理由（这是本方案里最重要的性能判断）：

1. 两个窗都是 `AllowsTransparency="True"` 的**分层窗口（layered window）**。WPF 对分层窗口走
   **软件渲染路径**，`Effect`（像素着色器）由 CPU 执行。
2. **悬浮窗内容每秒变化一次**（倒计时 1Hz）→ 每秒触发一次全窗重绘。若挂 `DropShadowEffect`，
   就是每秒一次 CPU 模糊卷积。窗口虽小，但这是**常驻**开销，与红线 2 直接冲突 → **禁用**。
   替代方案：**三层伪阴影**（三个同心 `Border`，`CornerRadius` 递增、`Background` 用 `Brush.Shadow.1/2/3`、
   `Margin` 逐层外扩 1px）。纯几何填充，零着色器成本，在 13px 圆角、小尺寸下与真阴影几乎无差别。
3. **提醒图标窗内容是完全静态的**（`ReminderBadgeItem` 不可变、无倒计时、无动画循环），
   只在「图标增减」和「hover」时重绘 → 真 `DropShadowEffect` 的成本是**一次性**的，可接受。
   参数受 §5 的 46px 窗宽严格约束，见 §3.2 的余量计算。

**毛玻璃（Mica / Acrylic）**：

- `DwmSetWindowAttribute(DWMWA_SYSTEMBACKDROP_TYPE)` 要求窗口**不是**分层窗口。
  悬浮窗当前 `AllowsTransparency="True"`；要换真 Acrylic 必须关掉它，代价是：
  失去自定义圆角（只能用 `DWMWA_WINDOW_CORNER_PREFERENCE` 的系统 8px 圆角）、失去卡片外的透明留白（伪阴影没法做）。
- **提醒图标窗绝对不能动**：它靠 per-pixel alpha 才能让圆形之外的区域透明并穿透点击（`Background="{x:Null}"`）。
- **结论：P0/P1 不上真毛玻璃**，用「半透明深色 + 内高光描边」做**视觉近似**（macOS HUD 本身也是低模糊度的暗材质，近似度很高）。
  真 Acrylic 列为 P2 实验项，必须先小范围验证并保留纯色降级路径。

**文本渲染**：分层窗口上 ClearType 不可用，WPF 会自动降级为灰度抗锯齿。
统一显式声明 `TextOptions.TextFormattingMode="Ideal"`、`TextOptions.TextRenderingMode="Grayscale"`，
避免在小字号下走 Display 模式的整像素对齐（那会破坏可变字重的形状，看起来"糊"）。

### 2.6 动效

| 场景 | 时长 | 缓动 | 实现 |
|---|---|---|---|
| 图标 hover 放大 | 150ms | `CubicEase EaseOut` | `Trigger.EnterActions` 里的一次性 `Storyboard`（`DoubleAnimation` 打 `ScaleTransform`） |
| 图标 hover 退出 | 180ms | `CubicEase EaseOut` | `Trigger.ExitActions` |
| 图标按下 | 90ms | `EaseOut` | 缩到 0.94 + 底色压暗 |
| 展开面板淡入 | 140ms | `EaseOut` | 只做 `Opacity` 0→1，**不做尺寸动画** |
| 卡片 hover 底色 | 0（瞬时） | — | 纯 `Trigger` `Setter`，不值得动画 |

**硬约束**：
- 所有 `Storyboard` 均为**一次性触发**，`RepeatBehavior` 保持默认（1 次），**绝不出现 `Forever`**。
- **不做尺寸/位置动画**：悬浮窗 `SizeToContent="WidthAndHeight"`，动画尺寸会与布局系统打架，
  且会每帧触发 `SizeChanged` → 连带触发图标窗的 `UpdatePlacement()`（同步路径），性能与稳定性双输。
- 动画只在**用户主动交互期间**发生，空闲时 CPU 必须回到 ~0。

---

## 3. 逐界面重设计

### 3.1 常驻悬浮窗

#### 折叠态（默认，宽度自适应，min 116 × 高 30）

```
 ┌──────────────────────────────┐   ← R=13，Bg #D91C1C1E
 │  ●   护眼            18:42   │      内高光描边 1px #1FFFFFFF
 └──────────────────────────────┘      外三层伪阴影（各 1px）
   ↑12  ↑6  ↑8            ↑右对齐 ↑12
   Padding-L  dot  Sp.2  时间列 W=64/Right  Padding-R

 高度 = 6(上) + 18(行) + 6(下) = 30
 dot：6×6 纯色圆（当前最近一项的类别色）
 名称：13px / Regular / Text.Primary
 时间：13px / Medium / Tabular / Text.Primary
```

- 「已暂停」态：整卡 `Opacity=0.72`，dot 改为 `Text.Tertiary` 灰，文本显示 `已暂停`（无数字列）。
- 「全部待完成」态：显示胶囊 `[ 待完成 ]`（`Fill.Quaternary` 底、R=7、Padding 6,1、11px Medium、Secondary 色）。
- hover：卡片底色切到 `Card.BgHover`（瞬时，无动画）。

#### 展开态（悬停，固定宽 176 × 高 138）

```
 ┌────────────────────────────────────┐
 │                                    │  ↕12 (Padding-T)
 │  距下次                            │  11px Medium / Text.Tertiary
 │                                    │  ↕8
 │  ●  护眼                  18:42    │  行高 22
 │  ●  久坐                  41:07    │
 │  ●  补水                1:02:33    │
 │  ●  运动               [ 待完成 ]  │  ← 胶囊，右对齐
 │                                    │  ↕12 (Padding-B)
 └────────────────────────────────────┘
   ↑16              ↑ 时间列 W=68 右对齐  ↑16
   Padding-L                              Padding-R

 高度 = 12 + 15(小标题) + 8 + 22×4 + 12 = 135 ≈ 138
 每行：dot 6 + Sp.2(8) + 名称(Auto) + * + 时间列(68, Right)
```

- **去掉所有分割线**（当前也没有，保持），层次靠 22px 行高的呼吸感 + 三级色阶。
- 小标题文案从「健康提醒 · 距下次」精简为「**距下次**」——
  卡片本身已在屏幕上常驻，重复品牌名是噪音（Apple 的做法是不重复标题）。
  该文案在 `Resources/Strings.cs:20`，需一行改动。
- 折叠 → 展开：`Visibility` 仍是硬切（尺寸不能动画，见 2.6），但展开面板加 140ms `Opacity` 淡入，
  消解"啪"的一下的廉价感。
- **已知取舍**：`SizeToContent` 下卡片向右生长（`Left` 不变），若悬浮窗已贴屏幕右缘，展开态会被
  `ClampToWorkArea` 之外的部分裁到屏幕外。当前 v1.1 已存在同样行为，本方案**不改变**它（改需动 C#）。
  展开宽度从「自适应」收敛到固定 176，反而让这个跳变量变得可预期。

### 3.2 提醒图标窗（**红线区，窗宽 46 绝对不动**）

```
   窗口宽度 46 DIP  ← 写死，SizeToContent="Height"（不可改，见 §5）

   ┌────────────┐
   │ ←6→ ◉ ←6→ │   护眼（绿渐变） 直径 34
   │            │
   │      ◉     │   久坐（橙渐变）  ← 间距 12 = Margin 6+6
   │            │
   │      ◉     │   补水（蓝渐变）
   │            │
   │      ◉     │   运动（靛渐变）
   └────────────┘

   宽度校验： 34 + 6 + 6 = 46  ✅ 与现行窗宽完全一致，Width="46" 一个字都不用改
```

**关键：直径从 36 改为 34、Margin 从 5 改为 6，总宽仍是 46。**
这是为了给外阴影与 hover 放大腾出 1px 余量，同时保住 `Width="46"` 这条刻意设计。

**外扩余量计算（必须满足 ≤ 6px，否则会被窗口边缘裁切）**

| 项 | 外扩量 |
|---|---|
| hover 放大 1.10 → (34×1.10 − 34)/2 | 1.70 px |
| `DropShadowEffect` `BlurRadius=4` | 4.00 px |
| 合计（水平方向最坏情况） | **5.70 px ≤ 6 ✅** |
| `ShadowDepth=1`（仅向下）叠加底部 | 底部 6.70 px —— 竖直方向由 `SizeToContent="Height"` 自适应，不裁切 ✅ |

**单枚图标的图层结构（由外到内）**

1. `DropShadowEffect`：`BlurRadius=4`、`ShadowDepth=1`、`Direction=270`、`Opacity=0.32`、`Color=#000`
   （静态内容，只在增减/hover 时重绘，见 2.5）
2. 圆底 `Border`：34×34、`CornerRadius=17`、`Background` = 该类**垂直双停靠点渐变**
3. 顶部内高光：叠一层同尺寸 `Border`，`Background` = `LinearGradient #26FFFFFF(0.0) → #00FFFFFF(0.55)`
4. 定界细环：`BorderThickness=1`、`BorderBrush=#26FFFFFF`（比现行 `#59FFFFFF` 弱一半，不抢眼）
5. 内容：**矢量 `Path`（放弃 emoji）**，白色 `#FFFFFF`，装进 `Viewbox Width/Height=18`

**四类矢量图形（24×24 viewBox，Path Data 为起稿，落地时需目视微调）**

| 类别 | 造型 | Path 起稿 |
|---|---|---|
| 护眼 | 杏仁眼廓 + 实心瞳孔 | 眼廓 `M2,12 C5,6.5 8.6,4.5 12,4.5 C15.4,4.5 19,6.5 22,12 C19,17.5 15.4,19.5 12,19.5 C8.6,19.5 5,17.5 2,12 Z`（描边 1.8，无填充）+ 瞳孔 `EllipseGeometry` r=3.0 实心 |
| 补水 | 水滴 | `M12,3 C12,3 5,10.8 5,15.2 A7,7 0 0 0 19,15.2 C19,10.8 12,3 12,3 Z`（实心） |
| 久坐 | 人形起身：头圆 + 躯干 + 一条抬起的腿 | 头 `EllipseGeometry(12,4.6) r=2.3` + 躯干/腿用 2.0 粗 `RoundLineCap` 折线 |
| 运动 | 哑铃：中杆 + 两端配重 | 中杆 `RectangleGeometry(8,11,8,2) R=1` + 两端 `RectangleGeometry(4.5,8.5,3.5,7) R=1.5` ×2 |

> 为什么放弃 emoji：`ARCHITECTURE.md` §8.7 已确认 WPF 不渲染彩色 emoji，四个字形在 17px 下**视觉重量严重不均**
> （🤸 线细、💧 偏小、🚶 糊）。改为自绘 `Path` 后**线宽、留白、光学尺寸可统一**，
> 且**仍不引入任何外部图片/字体文件**（红线 7 满足），这是本次能拿到的最大观感提升之一。
> 代价：`ReminderBadgeItem.Glyph`（string）需改为 `Geometry`/资源 key —— **属于 VM 改动，需 PM 授权**（见 §6 P1）。

**交互三态**

| 状态 | 视觉 | 实现 |
|---|---|---|
| 默认 | 上述 5 层 | — |
| hover | `Scale 1.10`（150ms EaseOut）+ 细环提到 `#40FFFFFF` + 阴影 Opacity 0.32→0.42 | `Trigger.EnterActions/ExitActions` 一次性 `Storyboard` |
| pressed | `Scale 0.94`（90ms）+ 圆底叠 `#26000000` 压暗层 | 同上；**取消现行的 `Opacity=0.75`**（B5） |
| disabled | 不存在（图标本身即待办） | — |

**tooltip**：保持现有三行内容不变，但换成自定义 `ToolTip` 模板：
R=8、`Card.Bg` 深色底、内高光描边、Padding 10,6、首行 12px Semibold、次行 12px Regular Secondary、
第三行 11px Tertiary，`Placement="Left"`、`HorizontalOffset=-8`、`HasDropShadow=False`（自己用伪阴影）。
`ToolTipService.InitialShowDelay=400`、`ShowDuration=8000`。

### 3.3 托盘右键菜单

WinForms `ContextMenuStrip` 无法用 XAML 改样式，但可以用**内置**的 `ToolStripRenderer` /
`ProfessionalColorTable` 完全接管绘制（`System.Windows.Forms` 内置，零第三方）。

```
        ╭──────────────────────────╮   ← 圆角 8（用 Region 或 DWM 圆角）
        │  暂停全部                │      项高 28，左内边距 14
        │                          │
        │  ──────────────────      │   ← 分隔线内缩 12，色 #26FFFFFF
        │                          │
        │  打开配置文件夹          │
        │  ──────────────────      │
        │  退出                    │
        ╰──────────────────────────╯
          宽度 ≥ 168
```

要点：
1. `ShowImageMargin = false` —— **干掉左侧那条永远空着的图标槽**（T1 里最土的一点），文字直接左对齐。
2. `Font = new Font("Segoe UI Variable Text", 9.5f)`，缺字体时 GDI+ 自动回退，安全。
3. 自定义 `ToolStripProfessionalRenderer + ProfessionalColorTable`：
   - 菜单底 `#2C2C2E`（深）/ `#FBFBFD`（浅），边框 `#3A3A3C` / `#E0E0E5`
   - 选中项底 = 圆角 6 的 `#3A3A3C` / `#E8E8ED` 填充（覆写 `OnRenderMenuItemBackground` 自绘圆角矩形），
     **不用系统那条蓝色高亮条**
   - 分隔线覆写 `OnRenderSeparator`，左右各内缩 12
4. 圆角：`menu.HandleCreated` 时 `DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE=33, 2 /*Round*/)`，
   Win11 原生圆角 + 阴影；失败则降级为方角（不影响功能）。
5. 托盘图标重绘（T2）：改为 **绿色圆底 + 白色"心跳/十字"**，画布仍 32×32 但内容内缩到 3px 边距，
   线宽从 3.5 降到 2.6，端点 `LineCap=Round`，并显式设 `g.PixelOffsetMode = HighQuality`，改善 16px 下的清晰度。

> **这一节 100% 是 C# 改动**（`TrayIconService.cs`），不含 XAML。按角色边界，需 PM 决定是我改还是派 developer。

---

## 4. 落地映射（示例代码仅供参考，本轮不落盘）

### 4.1 新增文件（纯新增，不碰现有逻辑）

```
src/HealthMaster/Themes/
├─ Tokens.xaml       # 圆角/间距/字号/字体族/时长/四类矢量 Geometry（与主题无关）
├─ Dark.xaml         # 中性色 + 四类渐变（深色，默认）
├─ Light.xaml        # 同名 key 的浅色一套
└─ Controls.xaml     # FloatingCard / CountdownRow / BadgeButton / BadgeToolTip 样式
```

> `.csproj` **无需改动**：WPF SDK 默认把 `**/*.xaml` 作为 `Page` 编入（`App.xaml` 除外）。

`App.xaml` 只加合并字典（这是 `App.xaml` 唯一的改动）：

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="Themes/Tokens.xaml" />
      <ResourceDictionary Source="Themes/Dark.xaml" />   <!-- 主题切换时由代码替换这一项 -->
      <ResourceDictionary Source="Themes/Controls.xaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

### 4.2 Tokens.xaml（节选示意）

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=System.Runtime">
  <FontFamily x:Key="Font.UI">Segoe UI Variable Text, Segoe UI, Microsoft YaHei UI</FontFamily>
  <sys:Double x:Key="Fs.Caption">11</sys:Double>
  <sys:Double x:Key="Fs.Body">13</sys:Double>
  <CornerRadius x:Key="R.l">13</CornerRadius>
  <CornerRadius x:Key="R.s">7</CornerRadius>
  <Thickness   x:Key="Pad.Card.Expanded">16,12</Thickness>
  <sys:Double  x:Key="Row.H">22</sys:Double>
  <Duration    x:Key="Dur.Hover">0:0:0.15</Duration>

  <!-- 四类矢量图形（不引入外部图片/字体文件） -->
  <PathGeometry x:Key="Geo.Water"
      Figures="M12,3 C12,3 5,10.8 5,15.2 A7,7 0 0 0 19,15.2 C19,10.8 12,3 12,3 Z" />
  <!-- Geo.Eye / Geo.Sedentary / Geo.Exercise 同理 -->
</ResourceDictionary>
```

### 4.3 悬浮窗（`Views/FloatingWindow.xaml` 全量重写，code-behind 不动）

结构骨架：

```xml
<Window ... FontFamily="{StaticResource Font.UI}"
        TextOptions.TextFormattingMode="Ideal"
        TextOptions.TextRenderingMode="Grayscale">
  <!-- 三层伪阴影：纯几何填充，零着色器成本（替代 DropShadowEffect，见 §2.5） -->
  <Border Background="{DynamicResource Brush.Shadow.3}" CornerRadius="16" Margin="0">
   <Border Background="{DynamicResource Brush.Shadow.2}" CornerRadius="15" Margin="1">
    <Border Background="{DynamicResource Brush.Shadow.1}" CornerRadius="14" Margin="1">
      <Border x:Name="Card" CornerRadius="{StaticResource R.l}" Margin="1"
              Background="{DynamicResource Brush.Card.Bg}"
              BorderBrush="{DynamicResource Brush.Card.StrokeInner}" BorderThickness="1">
        <Grid>
          <!-- 折叠态 / 展开态：仍用绑定 Window.IsMouseOver 的 DataTrigger 切 Visibility -->
        </Grid>
      </Border>
    </Border>
   </Border>
  </Border>
</Window>
```

单行倒计时（关键：色点 + 固定宽右对齐数字列）：

```xml
<Grid Height="{StaticResource Row.H}">
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="Auto"/><ColumnDefinition Width="*"/><ColumnDefinition Width="68"/>
  </Grid.ColumnDefinitions>
  <Ellipse Grid.Column="0" Width="6" Height="6" VerticalAlignment="Center"
           Fill="{DynamicResource Brush.Accent.Eye}" Margin="0,0,8,0"/>
  <TextBlock Grid.Column="1" Text="护眼" FontSize="{StaticResource Fs.Body}"
             Foreground="{DynamicResource Brush.Text.Primary}" VerticalAlignment="Center"/>
  <TextBlock Grid.Column="2" Text="{Binding EyeTime}" TextAlignment="Right"
             FontWeight="Medium" Typography.NumeralAlignment="Tabular"
             Foreground="{DynamicResource Brush.Text.Primary}" VerticalAlignment="Center"/>
</Grid>
```

> ⚠️ 上面的 `{Binding EyeTime}` **依赖 ViewModel 改造**：
> `FloatingViewModel` 现在暴露的是 `"护眼   18:42"` 这样的**已拼接字符串**（`FloatingViewModel.cs:68,72`）。
> 需要拆成 `EyeTime` / `EyeIsHeld` 之类的结构化字段（名称是静态中文，可直接写在 XAML 里）。
> **这是本方案唯一必须改 ViewModel 的地方**，也是修掉 F5「时间列不对齐」的前提。请 PM 决定由谁改。

### 4.4 图标窗（`Views/ReminderBadgeWindow.xaml` 只改 `Window.Resources` + `ItemTemplate`）

```xml
<Style x:Key="BadgeButton" TargetType="Button">
  <Setter Property="Cursor" Value="Hand"/>
  <Setter Property="Focusable" Value="False"/>   <!-- 保留：避免焦点视觉残留（§8.4） -->
  <Setter Property="Template">
   <Setter.Value>
    <ControlTemplate TargetType="Button">
      <Grid Width="34" Height="34" RenderTransformOrigin="0.5,0.5">
        <Grid.RenderTransform><ScaleTransform x:Name="sc" ScaleX="1" ScaleY="1"/></Grid.RenderTransform>
        <Grid.Effect>
          <!-- 图标窗内容静态，只在增减/hover 时重绘 → 真阴影可接受（§2.5） -->
          <DropShadowEffect BlurRadius="4" ShadowDepth="1" Direction="270" Opacity="0.32" Color="Black"/>
        </Grid.Effect>
        <Border CornerRadius="17" Background="{TemplateBinding Background}"
                BorderBrush="#26FFFFFF" BorderThickness="1"/>
        <Border CornerRadius="17">           <!-- 顶部内高光 -->
          <Border.Background>
            <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
              <GradientStop Offset="0" Color="#26FFFFFF"/><GradientStop Offset="0.55" Color="#00FFFFFF"/>
            </LinearGradientBrush>
          </Border.Background>
        </Border>
        <Path x:Name="ico" Width="18" Height="18" Stretch="Uniform" Fill="White"
              Data="{Binding Icon}" HorizontalAlignment="Center" VerticalAlignment="Center"/>
        <Border x:Name="dim" CornerRadius="17" Background="#26000000" Opacity="0"/>
      </Grid>
      <ControlTemplate.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
          <Trigger.EnterActions><BeginStoryboard><Storyboard>
            <DoubleAnimation Storyboard.TargetName="sc" Storyboard.TargetProperty="ScaleX"
                             To="1.10" Duration="0:0:0.15">
              <DoubleAnimation.EasingFunction><CubicEase EasingMode="EaseOut"/></DoubleAnimation.EasingFunction>
            </DoubleAnimation>
            <!-- ScaleY 同 -->
          </Storyboard></BeginStoryboard></Trigger.EnterActions>
          <Trigger.ExitActions><!-- To="1.0" Duration=0:0:0.18 --></Trigger.ExitActions>
        </Trigger>
        <Trigger Property="IsPressed" Value="True">
          <!-- sc → 0.94 (90ms) + dim.Opacity → 1 -->
        </Trigger>
      </ControlTemplate.Triggers>
    </ControlTemplate>
   </Setter.Value>
  </Setter>
</Style>
```

`ItemTemplate` 只改 `Margin="5"` → `Margin="6"`，其余（`Tag`/`Click`/`ToolTip`）**一字不动**。

**图标窗 XAML 中必须保持原样的行**：`Width="46"`(:13)、`SizeToContent="Height"`(:14)、
`Background="{x:Null}"`(:7)、`Opacity="0"`(:8)、`ShowActivated="False"`(:11)、`Topmost="True"`(:12)。

### 4.5 主题切换（P1，需 C#）

```csharp
// 读取：HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme (DWORD)
// 监听：SystemEvents.UserPreferenceChanged (Category == General) —— 复用现有 SystemEvents 订阅模式，
//       不新增任何定时器；切换时替换 App.Resources.MergedDictionaries[1] 即可（DynamicResource 自动生效）
```
所有颜色画刷用 `DynamicResource` 引用（尺寸/字号用 `StaticResource`，省一层查找开销）。

---

## 5. 风险与红线检查

### 5.1 六条产品红线逐条核对

| 红线 | 结论 | 说明 |
|---|---|---|
| 1 纯本地、不联网 | ✅ 无违反 | 方案不引入任何网络调用；无在线字体、无 CDN、无遥测 |
| 2 轻量低占用 | ✅ 已按最保守取舍设计 | 悬浮窗（1Hz 重绘）**禁用一切 `Effect`**，改三层几何伪阴影；图标窗（静态内容）才允许轻量 `DropShadowEffect`；动画全部一次性、150–180ms、仅交互期触发；**不新增任何定时器**、无 `RepeatBehavior="Forever"`、无 `BitmapCache`、无每帧重绘。空闲 CPU 应仍为 ~0 |
| 3 中文界面 | ✅ | 所有文案不变（仅建议把「健康提醒 · 距下次」精简为「距下次」，仍为中文，改 `Strings.cs` 一行） |
| 4 Windows 11 桌面 | ✅ | Segoe UI Variable 为 Win11 自带；DWM 圆角为可选增强，失败自动降级 |
| 5 非打断式 | ✅ | 不新增任何窗口、不调 `Activate()`、不闪任务栏；图标窗 `ShowActivated="False"` + `WS_EX_NOACTIVATE` 保持不变；tooltip 是被动悬停，不抢焦点 |
| 6 零第三方依赖 | ✅ | 只用 WPF 原生 + `System.Windows.Forms`（已在用）+ user32/dwmapi（系统 API，不算第三方）。**无任何新 `PackageReference`** |
| 7 无外部图片/字体文件 | ✅ | 四类图标改为 XAML `PathGeometry`（内联在 `Tokens.xaml`）；托盘图标仍是 `System.Drawing` 运行时绘制；**产物中依旧零二进制资源** |

### 5.2 「两处易被改坏的刻意设计」——本方案如何保全

| 刻意设计 | 出处 | 本方案的处理 |
|---|---|---|
| **① 图标窗 `Width="46"` 写死 + `SizeToContent="Height"`** | `ReminderBadgeWindow.xaml:13-14`；`ARCHITECTURE.md §8.3` | **一个字都不改。** 新尺寸刻意选为 `直径 34 + Margin 6×2 = 46`，与现值完全相等，因此**无需**同步修改 `Width`。所有阴影/hover 外扩量已做余量计算（最坏 5.70px ≤ 6px 边距，见 §3.2），保证不被窗口边缘裁切。**严禁**任何人以"让阴影更大"为由把 `SizeToContent` 改回 `WidthAndHeight` —— 那会触发 min-track 钳制，宽度被撑到 130+ 且不收敛，图标严重偏位 |
| **② 图标增减走 `Dispatcher.BeginInvoke(DispatcherPriority.Loaded)` 延后** | `ReminderBadgeWindow.xaml.cs:113-124`；`ARCHITECTURE.md §8.5` | **`ReminderBadgeWindow.xaml.cs` 本方案完全不碰**（改动只在 `Window.Resources` 的 `Style` 与 `ItemTemplate` 的 `Margin`）。特别注意：hover `Storyboard` 只动 `ScaleTransform`（`RenderTransform`，**不参与布局**），不会触发 `SizeChanged`，因此不会经由 `OnSelfSizeChanged` 反复进入 `UpdatePlacement()`，与延后机制零交互。**严禁**为了做"图标出现的入场动画"去改这段延后逻辑或加 `Loaded` 动画——会重入 `ItemsControl` 容器生成器，复现重复图标（items=4 / buttons=5） |

### 5.3 其它风险

| 风险 | 概率 | 缓解 |
|---|---|---|
| 目标机缺 `Segoe UI Variable`（Win10 或精简版 Win11） | 低 | 族串已带 `Segoe UI` 与 `Microsoft YaHei UI` 双重回退；时间列固定 68px + 右对齐，即使回退到非等宽数字也不抖动 |
| `FontWeight="Medium"` 在部分环境回退到 Regular/Bold | 中 | 属可接受降级，层级仍由色阶承担 |
| 分层窗口下三层伪阴影在浅色壁纸上偏"脏" | 中 | 浅色主题下把 `Brush.Shadow.*` 的 alpha 减半；实测后微调 |
| 展开态固定 176 宽在超高 DPI 下偏窄（中文换行） | 低 | 中文名固定 2 字，68px 时间列足够 `1:02:33`；仍需在 150%/200% 缩放下目视验证 |
| 自绘 `Path` 图标在 18px 下细节糊 | 中 | Path Data 为起稿，落地必须在 100%/150%/200% 三档缩放下目视调整线宽与留白，不能只看设计稿 |
| 托盘菜单自定义渲染器在不同 Win11 版本表现差异 | 中 | 只覆写背景/分隔线/选中项，不接管布局；DWM 圆角失败自动降级方角 |
| P2 真 Acrylic 需关掉 `AllowsTransparency` | 高 | **默认不做**；若尝试，必须保留一键回退，且**绝不施加于图标窗** |

---

## 6. 分期实施建议

### P0 — 纯 XAML 重塑（**建议先做，风险最低，收益最大**）

| 内容 | 文件 | 改动量 |
|---|---|---|
| 新建 `Themes/Tokens.xaml` `Dark.xaml` `Controls.xaml`（深色一套先行） | 新增 3 文件 | ~250 行新增 |
| `App.xaml` 合并字典 | `App.xaml` | +8 行 |
| 悬浮窗全量重写：材质/伪阴影/圆角/4pt 间距/字体族修正/Tabular/类别色点/hover 反馈/展开淡入 | `Views/FloatingWindow.xaml` | 全量重写（~120 行），**code-behind 不动** |
| 图标窗样式重塑：34+6 尺寸、渐变圆底、内高光、轻量阴影、150ms hover、0.94 pressed、自定义 tooltip | `Views/ReminderBadgeWindow.xaml`（仅 `Window.Resources` + `ItemTemplate.Margin`） | ~90 行，**code-behind 不动、`Width="46"` 不动** |

- **风险**：低。不触碰任何逻辑与两处刻意设计。
- **遗留**：时间列仍是 VM 拼接的字符串（F5 未修），只能整行左对齐或整体居中，对齐问题**待 P1 解决**。
- **验证**：`dotnet build` 0 警告 0 错误；跑起来在 100%/150% 缩放、深色/浅色壁纸下各看一遍；
  任务管理器确认空闲 CPU ≈ 0；拖动悬浮窗确认图标跟手无拖影。

### P1 — 少量 C# 配合（需 PM 授权 / 协调 developer）

| 内容 | 文件 | 风险 |
|---|---|---|
| `FloatingViewModel` 输出**结构化字段**（`EyeTime`/`EyeState` 等，名称移到 XAML 静态文本）→ 修好时间列右对齐（F5） | `ViewModels/FloatingViewModel.cs` | 中（改公开属性，需同步改 XAML 绑定） |
| `ReminderBadgeItem.Glyph`(string emoji) → `Icon`(Geometry 资源 key)，四类矢量图标落地（B6） | `ReminderBadgeItem.cs` / `ReminderBadgesViewModel.cs` / `DefaultConfigProvider.cs` | 中（`Glyph` 字段跨 3 个文件） |
| 四类主色从 VM 硬编码搬进 `Themes/*.xaml`（`Accents` 字典改为资源 key） | `ReminderBadgesViewModel.cs:21-27` | 低 |
| `Themes/Light.xaml` + 主题服务（注册表读取 + `UserPreferenceChanged` 监听，**不新增定时器**） | 新增 `Services/ThemeService.cs` | 中 |
| 托盘菜单重塑（`ShowImageMargin=false` + 自定义 Renderer + DWM 圆角）与托盘图标重绘 | `Services/TrayIconService.cs` | 中（WinForms 绘制，跨版本差异需实测） |
| 「距下次」文案精简 | `Resources/Strings.cs:20` | 极低 |

### P2 — 实验 / 可选（收益不确定，**不建议在 v2 首版做**）

| 内容 | 结论 |
|---|---|
| 悬浮窗真 Acrylic/Mica（关 `AllowsTransparency` + `DWMWA_SYSTEMBACKDROP_TYPE`） | 需先做一次性验证；会丢失自定义圆角与伪阴影；**图标窗绝不参与** |
| 临近到点高亮（剩余 <60s 时间字变强调色 + 色点呼吸感） | 需 VM 增字段；「呼吸」必须用状态切换而非循环动画（红线 2） |
| 连续圆角 squircle 自绘 | **不建议做**，13px 半径下肉眼不可辨，成本 ≫ 收益 |
| 设置窗口（勿扰/间隔）的视觉设计 | 等 developer 做出功能骨架后再介入 |
| 图标出现的入场动画 | **明确否决**：会与 §5.2 ② 的延后机制冲突，风险复现重复图标 |

---

## 7. 待 PM 决策的三个问题

1. **P1 的 `FloatingViewModel` 结构化改造**由谁做？（这是修掉"时间列不对齐"的唯一途径，属逻辑代码）
2. **四类图标从 emoji 改为矢量 `Path`** 是否批准？（观感提升最大的单项，但要动 `ReminderBadgeItem.Glyph` 及其 3 个引用点）
3. **悬浮窗是否接受"恒为深色 HUD"**（macOS 自身做法，浅色主题只调不透明度），
   还是必须做完整的浅色翻白一套？前者可省掉约一半的主题工作量。
