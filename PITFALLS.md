# 踩坑记录

开发 FloatingKeypad 过程中踩过的坑，按「现象 → 原因 → 解决」记录，避免重复。

---

## 1. 键盘模拟完全无效（鼠标却正常）

**现象**：点击悬浮按钮，键盘动作（按 `a`、`Ctrl+C`）毫无反应；但鼠标右键动作正常。

**原因**：`SendInput` 的 `INPUT` 结构体定义错误。union 里只放了 `KEYBDINPUT`（x64 下 24 字节），而 Windows 要求 union 大小等于最大的 `MOUSEINPUT`（x64 下 32 字节），导致 `cbSize` 不匹配、`SendInput` 静默失败返回 0。鼠标动作走的是 `PostMessage`，不经过这个结构体，所以正常。

**解决**：`InputUnion` 里补齐 `MOUSEINPUT` / `HARDWAREINPUT`，让结构体大小与 Windows 一致。

```csharp
[StructLayout(LayoutKind.Explicit)]
private struct InputUnion
{
    [FieldOffset(0)] public MOUSEINPUT mi;
    [FieldOffset(0)] public KEYBDINPUT ki;
    [FieldOffset(0)] public HARDWAREINPUT hi;
}
```

---

## 2. 悬浮窗抢焦点 + 鼠标动作打到自己（焦点与定向）

**现象**：
- 点击悬浮按钮后，动作发到了悬浮窗自己，而不是原来的活动窗口；
- 触发「鼠标右键」时，模拟的点击作用在当前光标位置——而点击悬浮按钮时光标正在按钮上，于是点到了按钮。

**原因**：普通窗口被点击会激活、抢走前台焦点；且 `SendInput` 的鼠标事件是位置敏感的（作用于光标处），只有键盘才是发给焦点窗口的。

**解决**：
- 悬浮窗设扩展样式 `WS_EX_NOACTIVATE`（点击不激活）+ `WS_EX_TOOLWINDOW`，并拦截 `WM_MOUSEACTIVATE` 返回 `MA_NOACTIVATE`；
- 鼠标动作改用 `PostMessage` 定向发送到「点击按钮前记录的 `GetForegroundWindow()`」，不依赖光标位置。

---

## 3. 无法输入中文 + 光标跳到开头（最隐蔽）

**现象**：设置界面名称框切到中文输入法后，打字母只出字母、不出候选词；光标还会跳到开头（如 `aaaa` 在第二个 `a` 后输入 `b`，变成 `aabaa` 但光标回到行首）。

**原因**：`TextBox.TextChanged` 里调用了重建 ListBox `ItemsSource` 的方法：

```
TextChanged → RefreshList() → ButtonList.ItemsSource = null
→ 触发 SelectionChanged → LoadCurrent() → NameBox.Text = ...
→ 光标重置 + IME 组合被反复打断
```

每次按键都把输入框重置一次，导致 IME 无法组合、光标复位。与 UIPI / 提权权限无关。

**解决**：
- `TextChanged` 里改用 `ButtonList.Items.Refresh()`，不重建 `ItemsSource`
- `LoadCurrent` 里只在 `NameBox.Text != b.Label` 时才赋值

**教训**：输入框行为异常时，先怀疑自己的代码有没有在输入事件里重置控件。

---

## 4. 圆角按钮四周有方角阴影

**现象**：按钮是圆角，但四周出现一个方形的浅色阴影。

**原因**：WPF 的 `DropShadowEffect` 需要空间向外扩散，但 `Border` 紧贴窗口边界，阴影被窗口边界裁剪成方形。

**解决**：给 `Border` 留出 `Margin`（≈ `BlurRadius`），窗口尺寸相应放大；透明区域用 `Background="{x:Null}"`（不响应点击）。

---

## 5. WSL 源码 + Windows 构建环境

**现象**：
- 构建输出在 `\\wsl.localhost\...` 时，启动 exe 弹「我们无法确认是谁创建了此文件」；
- 重新构建报 `MSB3021 / MSB3027 文件被 FloatingKeypad.exe 锁定`，用 WSL 的 `taskkill` 结束进程报「拒绝访问」。

**原因**：Windows 把 UNC 路径当作网络位置，对网络来源的可执行文件默认拦截，且部分第三方输入法在网络路径下可能拒绝工作；主程序以管理员运行，WSL interop 启动的 `taskkill` 是普通权限，无法结束提权进程。

**解决**：
- 构建输出改到 **Windows 本地盘**（`Directory.Build.props` 的 `DeployDir`，默认 `D:\FloatingKeypad`）；
- 提权结束进程：

```powershell
Start-Process -FilePath taskkill -ArgumentList '/IM','FloatingKeypad.exe','/F' -Verb RunAs
```

---

## 6. Directory.Build.props 里用 $(TargetFramework) 取值为空

**现象**：想按 TFM 分目录输出，结果 `$(TargetFramework)` 是空字符串。

**原因**：`Directory.Build.props` 在项目文件**之前**导入，此时 `TargetFramework` 还没定义。

**解决**：不要在 `Directory.Build.props` 里用 `TargetFramework`，输出路径直接到 `build\$(Configuration)\`。

---

## 7. WPF + WinForms 全局 using 冲突

**现象**：大量 `CS0104: 不明确的引用`，涉及 `Application` / `MessageBox` / `MouseEventArgs` / `Color` / `Brushes` / `ColorConverter`。

**原因**：同时启用 `UseWPF` 和 `UseWindowsForms` 时，SDK 会全局引入 `System.Windows.Forms` 和 `System.Drawing`，与 `System.Windows` 同名类型冲突。

**解决**：csproj 里移除这两个全局 using，需要时在文件内显式 using 或用别名。

```xml
<Using Remove="System.Windows.Forms" />
<Using Remove="System.Drawing" />
```

---

## 8. 右击悬浮按钮打开设置，程序直接崩溃

**现象**：右击悬浮按钮想打开设置，FloatingKeypad 进程直接退出（表现为「点了没反应 / 卡死」）。Windows 应用日志里是 `System.NullReferenceException`：

```
at FloatingKeypad.Views.SettingsWindow.WidthSlider_ValueChanged(...) SettingsWindow.xaml.cs:line 70
...
at FloatingKeypad.Views.SettingsWindow.InitializeComponent()
```

**原因**：`SettingsWindow` 的 XAML 里 `Slider` 设了 `Minimum="60" Maximum="400"`。XAML 解析时会立即设置这两个依赖属性，进而触发 `ValueChanged` → `WidthSlider_ValueChanged`。但此时 XAML 里排在 `Slider` **后面**的 `WidthBox`（以及 `HeightBox` / `OpacityBox`）字段还没创建，仍是 `null`，处理器里 `WidthBox.Text = ...` 直接 NRE。构造函数里那句 `_loading = true` 发生在 `InitializeComponent()` **之后**，起不到守卫作用；字段默认值 `false`。

**解决**：`private bool _loading = true;`（字段初始化为 `true`）。字段初始化在 `InitializeComponent()` 之前执行，XAML 解析期间所有 `_loading` 守卫的处理器都提前返回；构造末尾照旧置 `false`。

**教训**：WPF 里只要处理器可能在 `InitializeComponent()` 期间被触发（`Slider` 的 `Minimum/Maximum/Value`、`ComboBox.Items` 等），就必须用一个「加载中」标志守卫，且该标志的初值要为 `true`。别指望构造函数体内的赋值能挡住 XAML 解析。

---

## 9. `Localization` 类名与 WPF 的 `System.Windows.Localization` 冲突

**现象**：新建 `Services/Localization.cs` 后，`App.xaml.cs` / `SettingsWindow.xaml.cs` 出现 `CS0104: Localization 是 … 和 … 之间的不明确引用`。

**原因**：WPF 自带 `System.Windows.Localization` 类型，与自定义的 `FloatingKeypad.Services.Localization` 同名。

**解决**：文件内用别名：

```csharp
using Localization = FloatingKeypad.Services.Localization;
```

---

## 10. Inno Setup 环境准备

**现象**：
- 访问 `https://jrsoftware.org/download.php/is.exe` 得到的是 HTML 页面而不是安装文件；
- 编译或安装向导报找不到 `compiler:Languages\ChineseSimplified.isl`。

**原因**：前者返回的是下载页，真实安装包链接指向 GitHub Releases（页面里异步拼出来）；Inno Setup 默认只带 `Default.isl`（英文）等少量语言文件，简体中文在 `issrc` 仓库的 `Files/Languages/` 下（注意不是 `Unofficial/` 目录）。

**解决**：
- 从 `https://jrsoftware.org/isdl.php` 页面里找 GitHub Releases 链接下载 `innosetup-*.exe`；
- 从 `issrc` 仓库取 `Files/Languages/ChineseSimplified.isl`，放到 Inno Setup 的 `Languages\` 目录。换机器编译需重新放置（或升级到已内置的 Inno Setup 6.3+）。

---

## 11. 自包含发布才能免装 .NET 运行时

**现象**：安装到没装过 .NET 的机器，程序无法启动。

**原因**：默认 `dotnet publish` 为框架依赖，依赖目标机的 .NET 8 Desktop Runtime。

**解决**：发布加 `-r win-x64 --self-contained true`，产物内置运行时（约 51 MB）。代价是体积大、每次都要带运行时；若要体积小，就得在安装时检测并引导安装 .NET 8 Desktop Runtime。

---

## 12. 已打开的设置窗口不会重新激活到最前

**现象**：设置窗口已经打开但被其它窗口挡住（或最小化），再次右键悬浮按钮，设置窗口没有回到前台。

**原因**：`App.OpenSettings` 里只调用了 `_settings.Activate()`。`Window.Activate()` 受前台窗口锁定（foreground lock）影响，且不会还原最小化窗口，经常不起作用。

**解决**：统一走 `App.BringToFront(window)`：

```csharp
if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
if (!window.IsVisible) window.Show();
var wasTopmost = window.Topmost;
window.Topmost = true;      // 切换一次 Topmost 强制改变 Z 序
window.Topmost = wasTopmost;
window.Activate();
```

---

## 13. 离屏渲染测试宿主覆盖了真实配置

**现象**：用外部 WPF 宿主加载主程序集、构造 `SettingsWindow` 并 `Close()` 后，用户 `%APPDATA%\FloatingKeypad\config.json` 里真实的按钮 / 外观被替换成宿主里的假数据。

**原因**：`SettingsWindow` 构造函数里挂了 `Closed += (_, _) => App.Current.SaveConfig();`。宿主 `Close()` 窗口时就触发了保存，而宿主 `App.Config` 是自己造的测试数据。

**解决**：测试宿主渲染完成后**不要调 `w.Close()`**，直接 `Environment.Exit(0)` 结束进程，`Closed` 不会触发，也就不会写盘。任何「加载真实 App 做测试」的宿主都要记住：**不要触发任何 SaveConfig 路径**。
