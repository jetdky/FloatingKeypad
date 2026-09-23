<p align="center">
  <img src="src/FloatingKeypad/Assets/logo.png" width="120" alt="FloatingKeypad logo" />
</p>

<h1 align="center">悬浮键鼠助手 · FloatingKeypad</h1>

<p align="center"><a href="README.md">English</a> | 简体中文</p>

适用于 Windows 的桌面悬浮按钮工具。按钮悬浮于桌面，支持自定义键盘组合与鼠标按键，点击后**不抢焦点**，动作直接作用在当前活动窗口。常用于手机远程连接电脑时触发快捷按键。

## 截图

| 主界面 | 悬浮效果 |
| --- | --- |
| ![主界面](docs/main-window.png) | ![悬浮效果](docs/floating-effect.png) |

## 功能

- 按钮悬浮于桌面，可拖动定位
- 自定义键盘组合键、鼠标按键，以及键鼠混合动作
- 点击不改变当前窗口焦点，动作发送到原窗口
- 全局外观配置：统一设置所有按钮的宽度 / 高度 / 透明度 / 背景色 / 文字色
- 中 / 英双语
- 系统托盘常驻，配置持久化到 `%APPDATA%\FloatingKeypad\config.json`

## 使用

双击 `FloatingKeypad.exe`（需要管理员权限）。首次运行会创建一个「复制」示例按钮。

- 拖动按钮调整位置
- 右键按钮打开设置，维护按钮名称与绑定动作
- 顶部「配置」按钮：设置语言与全局外观

## 构建

需要 **Windows** 上的 .NET 8 SDK（WPF 不支持 Linux 构建）。

```powershell
dotnet build FloatingKeypad.sln
```

输出：`D:\FloatingKeypad\build\Debug\FloatingKeypad.exe`，由 `Directory.Build.props` 的 `DeployDir` 控制，可用 `-p:DeployDir=...` 覆盖。

> 输出目录必须是 Windows 本地路径，不能是 WSL 的 UNC 路径（`\\wsl.localhost\...`）：从 UNC 路径启动 exe 会触发 Windows 安全警告，且部分第三方输入法可能拒绝工作。

源码位于 WSL 时，可在 Windows 侧用 UNC 路径构建：

```powershell
dotnet build \\wsl.localhost\<发行版>\www\FloatingKeypad\FloatingKeypad.sln
```

## 打包安装包

一条命令完成「自包含发布 → 编译安装包」：

```powershell
powershell -ExecutionPolicy Bypass -File build-release.ps1
```

- 版本号自动取自 `src/FloatingKeypad/FloatingKeypad.csproj` 的 `<Version>`
- 发布产物：`D:\FloatingKeypad\publish\`（自包含 win-x64，免装 .NET）
- 安装包产物：`D:\FloatingKeypad\installer\FloatingKeypad-Setup-<版本>.exe`

| 参数 | 说明 |
|---|---|
| `-Configuration` | 构建配置，默认 `Release` |
| `-Version` | 覆盖版本号（默认读 csproj） |
| `-InnoSetupDir` | 指定 Inno Setup 安装目录（默认自动探测） |
| `-FrameworkDependent` | 框架依赖发布（体积约 1 MB，需目标机装 .NET 8 Desktop Runtime） |

安装向导支持简体中文 / English、自定义安装路径、可选桌面快捷方式，装完可自动启动，控制面板可卸载。

## 项目结构

```
FloatingKeypad/
├── FloatingKeypad.sln
├── Directory.Build.props         # 统一输出到 D:\FloatingKeypad\build\
├── build-release.ps1             # 发布 + 生成安装包
├── installer/FloatingKeypad.iss  # Inno Setup 安装脚本
└── src/FloatingKeypad/
    ├── app.manifest              # requireAdministrator
    ├── App.xaml(.cs)             # 单实例 + 托盘 + 生命周期 + 语言应用
    ├── Models/                   # InputEvent / ButtonConfig / KeyNames / AppearanceConfig
    ├── Services/                 # InputSimulator / InputHook / WindowHelper / ConfigService / TrayService / Localization
    ├── Themes/                   # Antd.xaml 主题
    └── Views/                    # FloatingButtonWindow / SettingsWindow / GlobalConfigWindow / KeyCaptureDialog
```

## 配置

`%APPDATA%\FloatingKeypad\config.json`：

```json
{
  "Language": "zh-CN",
  "Appearance": {
    "Width": 120,
    "Height": 48,
    "Opacity": 0.88,
    "Background": "#CC2D7FF9",
    "Foreground": "#FFFFFFFF"
  },
  "Buttons": [
    {
      "Id": "…",
      "Label": "复制",
      "Left": 320,
      "Top": 320,
      "Events": [{ "kind": "key", "Vk": 17, "Extended": false, "Down": true }]
    }
  ]
}
```

## 技术要点

- **不抢焦点**：窗口设 `WS_EX_NOACTIVATE`，并拦截 `WM_MOUSEACTIVATE` 返回 `MA_NOACTIVATE`
- **键盘**：`SendInput` 全局模拟，发送给当前焦点窗口
- **鼠标**：`PostMessage` 定向发送到点击按钮前记录的 `GetForegroundWindow()`
- **按键捕获**：低级键盘 / 鼠标钩子 `WH_KEYBOARD_LL` / `WH_MOUSE_LL`

## 已知限制

- 目标程序若以管理员运行，本程序也需管理员（已通过清单强制）
- 游戏等使用 Raw Input 的程序可能不响应键盘模拟
- 鼠标动作通过 `PostMessage` 定向发送，Chrome / Electron 类程序可能不响应
- `Win+L` 等系统级组合被系统占用，无法模拟

## 许可证

[MIT](LICENSE)
