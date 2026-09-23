# AGENTS.md

## 项目概述

悬浮键鼠助手（项目代号 FloatingKeypad）是一个 Windows 桌面悬浮按键工具，技术栈 **C# / WPF / .NET 8**，单一 exe（管理员权限）。

## 交互设计标准（改 UI 前必读）

来自用户明确的偏好，违反者视为需求不达标：

1. **按作用域分层，主次分明**
   - 主界面只放「当前对象」的核心字段：这里每个按钮只有**名称 + 绑定动作**
   - 全局项（语言、悬浮外观）收进「配置」按钮弹出的独立弹窗
   - 禁止把全局设置铺在主界面；不要把每个对象都挂一份全局配置

2. **直接生效，不设提交动作**
   - 轻量、可逆的配置改动（改名称、捕获绑定、改外观）**即时应用并持久化**
   - 不设「保存并应用 / 保存 / 取消」这类提交按钮，不做两段式确认
   - 只有破坏性、不可逆操作才需要确认

3. **文案极简，只描述功能**
   - 不加重复性标题（如「选中按钮」）、括号参数说明（如「透明度 (0.3-1.0)」）、操作步骤说明（如「按 Esc 取消…」）
   - 用户可见文案只说做什么，不解释为什么、不教怎么用

4. **减少步骤与多余选项**
   - 能「直接替换」就不要「先清除再输入」；捕获弹窗打开后按新键即替换
   - 不提供用户不需要的按钮（清除、取消等），能省则省

5. **视觉统一、现代**
   - 控件统一走 `Themes/Antd.xaml` 主题（圆角 6、主色 `#1677FF`、hover/pressed 态齐全），不依赖系统默认「复古」外观
   - 新增控件复用主题；颜色不要散落硬编码
   - 提供「恢复默认」等一键回退入口

6. **窗口再次唤起必须置顶激活**
   - 已打开的窗口被遮挡或最小化时，再次触发入口要恢复到最前（见关键约束 8）

## 构建

```bash
# Windows（推荐）
dotnet build FloatingKeypad.sln

# 源码在 WSL 时，从 Windows 用 UNC 路径
dotnet build '\\wsl.localhost\<distro>\www\FloatingKeypad\FloatingKeypad.sln'
```

- 必须用 **Windows** 的 .NET SDK 构建（WPF 不支持 Linux 构建）
- 输出在 `D:\FloatingKeypad\build\<Configuration>\`，由 `Directory.Build.props` 的 `DeployDir` 控制
- **不要**把输出放到 WSL 的 UNC 路径：从 UNC 启动 exe 会弹 Windows 安全警告，且部分第三方输入法可能无法工作
- **重新构建前必须先关闭运行中的实例**，否则 exe 被锁定（MSB3021 / MSB3027）
- 提交前确保 **0 warning / 0 error**

### 打包安装包

```powershell
powershell -ExecutionPolicy Bypass -File build-release.ps1
```

脚本自动读 csproj 的 `<Version>`，先 `dotnet publish -r win-x64 --self-contained true` 到 `D:\FloatingKeypad\publish`，再调 `ISCC.exe` 生成 `D:\FloatingKeypad\installer\FloatingKeypad-Setup-<版本>.exe`。

- 自包含发布：`dotnet publish -r win-x64 --self-contained true`，用户免装 .NET（安装包约 51 MB）
- 框架依赖：加 `-FrameworkDependent`，体积约 1 MB，但要求目标机装 .NET 8 Desktop Runtime
- `FloatingKeypad.iss` 顶部版本号用 `#ifndef AppVersion` 兜底，脚本通过 `ISCC /DAppVersion=<版本>` 注入

## 运行 / 调试

- 需要管理员权限（`app.manifest` 为 `requireAdministrator`），运行会弹 UAC
- 关闭运行中的实例：从托盘退出，或**以管理员权限**结束进程
  - WSL 的 `taskkill` 无法结束提权进程，会报「拒绝访问」
  - 可用：`powershell Start-Process taskkill -ArgumentList '/IM','FloatingKeypad.exe','/F' -Verb RunAs`
- 调试需以管理员启动 IDE，否则无法附加提权进程

## 结构

```
README.md / README.zh.md  # 中英文档（README.md 为英文默认；功能、构建、使用说明；改功能时同步两者）
AGENTS.md               # 本文件，开发约定
PITFALLS.md             # 踩坑记录
LICENSE                 # MIT
build-release.ps1       # 自包含发布 + ISCC 编译安装包（一条命令）
installer/
└── FloatingKeypad.iss  # Inno Setup 脚本，版本号从 csproj 读取（可用 /DAppVersion 覆盖）
src/FloatingKeypad/
├── app.manifest        # requireAdministrator
├── App.xaml(.cs)       # 单实例 + 托盘 + 生命周期 + 语言应用
├── Models/             # InputEvent(多态) / ButtonConfig / KeyNames / AppearanceConfig
├── Services/           # InputSimulator / InputHook / WindowHelper / ConfigService / TrayService / Localization
├── Themes/             # Antd.xaml 主题
└── Views/              # FloatingButtonWindow / SettingsWindow / GlobalConfigWindow / KeyCaptureDialog
```

## 国际化约定

- 文案集中在 `Services/Localization.cs` 的 `Zh` / `En` 两个字典，按 `key` 取用：`Localization.T("Key")`
- **新增任何用户可见文案必须同时补 Zh 和 En**，缺失时返回 key 本身（界面会直接显示 key）
- XAML 绑定固定写法：`{Binding Source={x:Static svc:Localization.Instance}, Path=[Key]}`
- 切换语言：`App.SetLanguage(lang)` → `Localization.SetLanguage` → 触发 `PropertyChanged("Item[]")` 刷新所有绑定，并 `RebuildWindows()` 重建悬浮按钮
- `Localization` 与 WPF 的 `System.Windows.Localization` 同名，文件内必须用别名：`using Localization = FloatingKeypad.Services.Localization;`
- 语言值只认 `"zh-CN"` / `"en"`，其它值一律归一化为 `zh-CN`；无配置时跟随系统 `CurrentUICulture`

## 外观配置结构

`AppConfig`（`config.json`）三个字段：

- `Language`：`""` / `"zh-CN"` / `"en"`
- `Appearance.Width / Height / Opacity / Background / Foreground`：**全局**外观，作用于所有按钮；颜色为 `#AARRGGBB`
- `Buttons`：每个按钮仅含 `Id / Label / Left / Top / Events`

按钮级旧外观字段（`Width/Height/Opacity/Background/Foreground` 写在按钮里）已废弃，反序列化时被 `System.Text.Json` 自动忽略，无需担心旧配置报错。

默认值集中在 `AppearanceConfig` 的 `DefaultWidth/DefaultHeight/…` 常量，`AppearanceConfig.Reset()` 一键还原（默认淡蓝底 `#CC2D7FF9` + 白字）。新增外观项时同步补常量与 `Reset()`。

首次运行（无 `config.json`）加载内置默认配置 `src/FloatingKeypad/Assets/default-config.json`（csproj 里声明为 `EmbeddedResource`）；若该配置为空，`App.EnsureDefaults()` 兜底创建一个「复制」示例按钮。

## 设置界面结构

- `SettingsWindow`（主界面）：只有**按钮列表 + 名称 + 绑定动作**；顶部「添加按钮」，列表每项右侧一个删除 ✕，底部「配置」按钮；**无保存/关闭按钮**
  - 改名称 / 捕获绑定**立即生效**：更新内存 → `App.RefreshWindows()` 刷新悬浮按钮
  - 持久化策略：捕获 / 添加 / 删除立即 `SaveConfig`；名称改动走 600ms 防抖 `_saveTimer`（避免 IME 组合期频繁写盘），窗口 `Closed` 再兜底保存
  - 添加 / 删除按钮：`App.SaveConfig()` + `App.RebuildWindows()`
- `GlobalConfigWindow`（配置弹窗，由 `SettingsWindow.Config_Click` 打开）：语言 + 悬浮外观 + 预览 + 导出 / 导入 + 恢复默认；**无取消按钮**，「保存并应用」、`Esc` 与窗口关闭都落地（`OnClosing` 里 `SaveConfig` + `RebuildWindows`）
- `KeyCaptureDialog`（捕获弹窗）：无任何按钮；打开即开始捕获，**捕获到完整动作立即写入 `Result` 并自动关闭**（直接替换原绑定）；`Esc` 或关闭窗口视为取消
- 语言 / 外观是**全局**的，不要下放到单个按钮；新增全局配置项加到 `GlobalConfigWindow`，不要塞回主界面

## 关键约束（改代码前必读）

### 1. 悬浮窗不抢焦点（核心功能）
悬浮窗必须保持 `WS_EX_NOACTIVATE`，并拦截 `WM_MOUSEACTIVATE` 返回 `MA_NOACTIVATE`。
任何改动都不能让点击悬浮按钮时抢走前台窗口焦点，否则「焦点保持」功能失效。

### 2. SendInput 结构体大小
`InputSimulator` 的 `INPUT` 结构体 union 必须包含 `MOUSEINPUT`（union 中最大的成员），否则 `cbSize` 不匹配、`SendInput` 静默失败（表现为键盘完全无效）。
**不要**只放 `KEYBDINPUT`。

### 3. 鼠标动作穿透点击
鼠标事件用 `SendInput` 在**光标当前位置**发送，光标不移动。**不要**改回 `PostMessage`：伪造的 `WM_*BUTTON*` 消息现代程序（Windows 11 资源管理器、Chrome / Electron）不响应。
发送期间把所有悬浮窗临时设为 `WS_EX_TRANSPARENT` 鼠标穿透，点击落到浮层下面的窗口；否则按钮叠在目标窗口上时会先命中按钮自己。
不要记录 / 计算目标窗口坐标：按钮拖到哪，光标就在哪，穿透后点击自然作用于下面同位置的窗口。

### 4. 设置界面的输入框不要重建 ItemsSource
`SettingsWindow` 的名称框 `TextChanged` 里**不能**重建 ListBox 的 `ItemsSource`——会触发 `SelectionChanged` → 重新赋值 `TextBox.Text` → 光标跳到开头、且 IME 组合被反复打断导致无法输入中文。
用 `ButtonList.Items.Refresh()`，并让 `LoadCurrent` 只在值不同时才赋值。详见 PITFALLS.md 坑 3。

### 5. WPF + WinForms 类型冲突
项目同时启用 `UseWPF` 和 `UseWindowsForms`，必须在 csproj 里移除冲突的全局 using：

```xml
<Using Remove="System.Windows.Forms" />
<Using Remove="System.Drawing" />
```

需要时在文件内显式 using 或用别名（如 `using Application = System.Windows.Application;`）。

### 6. 悬浮窗阴影留白
`AllowsTransparency` 窗口的圆角阴影需要空间展开，`Border` 必须有 `Margin ≈ BlurRadius`，窗口尺寸相应放大；透明区域用 `Background="{x:Null}"` 使其不响应点击。否则阴影被裁剪成方形。

### 7. XAML 解析期事件会早于控件字段初始化

`GlobalConfigWindow` 的 `Slider` 在 XAML 里设置 `Minimum/Maximum` 时会立刻触发 `ValueChanged`，此时后面的 `WidthBox` / `HeightBox` / `OpacityBox` 字段还是 `null`，处理器里直接访问会 `NullReferenceException`（打开配置弹窗即崩）。
**约定**：`GlobalConfigWindow._loading`（以及任何含此类控件的窗口）字段初始值必须为 `true`，构造末尾再置 `false`，让 XAML 解析期间所有事件处理器提前返回。新增滑块 / 下拉 / 输入框回调时沿用同一守卫。详见 PITFALLS.md 坑 8。

### 8. 设置窗口重新打开必须恢复到最前

右键悬浮按钮或点托盘「设置」时，若 `_settings` 已存在，不能只调 `Activate()`——被遮挡或最小化时不会回到前台。
统一走 `App.BringToFront(window)`：最小化先还原、必要时 `Show()`、切换一次 `Topmost` 再 `Activate()`，确保激活到最前。详见 PITFALLS.md 坑 12。

## 测试清单

- 记事本选中文字 → 点「复制」按钮 → **焦点不跳**、内容已复制
- 捕获 `Ctrl+Shift+S`、鼠标右键、`Ctrl+左键` 各测一次
- 拖动按钮后重启，位置恢复
- 设置界面名称框能正常输入中文、光标稳定
- 设置保存后悬浮窗立即更新
- 右击悬浮按钮能打开设置（历史 bug，见坑 8）
- 切换语言后界面 / 托盘 / 动作描述实时刷新
- 悬浮外观改动保存后所有悬浮按钮同步生效

## 踩坑记录

见 [PITFALLS.md](PITFALLS.md)。
