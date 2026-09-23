using System.ComponentModel;

namespace FloatingKeypad.Services;

public sealed class Localization : INotifyPropertyChanged
{
    public static Localization Instance { get; } = new();

    private string _lang = "zh-CN";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key]
    {
        get
        {
            var map = _lang == "en" ? En : Zh;
            return map.TryGetValue(key, out var value) ? value : key;
        }
    }

    public string Current => _lang;

    public void SetLanguage(string? lang)
    {
        var normalized = string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "zh-CN";
        if (normalized == _lang)
        {
            return;
        }

        _lang = normalized;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
    }

    public static string T(string key) => Instance[key];

    private static readonly Dictionary<string, string> Zh = new()
    {
        ["AppTitle"] = "悬浮键鼠助手",
        ["SettingsTitle"] = "悬浮键鼠助手 - 设置",
        ["Name"] = "名称",
        ["Action"] = "绑定动作",
        ["Capture"] = "捕获",
        ["Clear"] = "清除",
        ["Cancel"] = "取消",
        ["Ok"] = "确定",
        ["Width"] = "宽度",
        ["Height"] = "高度",
        ["Opacity"] = "透明度",
        ["Background"] = "背景色",
        ["Foreground"] = "文字色",
        ["Language"] = "语言",
        ["Config"] = "配置",
        ["Appearance"] = "悬浮外观",
        ["Pick"] = "选择",
        ["Preview"] = "预览",
        ["Text"] = "文本",
        ["Reset"] = "恢复默认",
        ["Export"] = "导出",
        ["Import"] = "导入",
        ["ExportTitle"] = "导出配置",
        ["ImportTitle"] = "导入配置",
        ["ExportFailed"] = "导出失败，请检查文件是否可写。",
        ["ImportFailed"] = "导入失败，文件不是有效的配置文件。",
        ["ImportConfirm"] = "导入将覆盖当前全部配置，确定继续？",
        ["SelectedButton"] = "选中按钮",
        ["SettingsHint"] = "提示：拖动悬浮按钮调整位置，右键悬浮按钮可快速编辑。",
        ["SaveApply"] = "保存并应用",
        ["Close"] = "关闭",
        ["AddButton"] = "添加按钮",
        ["Delete"] = "删除",
        ["NotSet"] = "（未设置）",
        ["NewButton"] = "新按钮",
        ["CaptureTitle"] = "捕获按键",
        ["CapturePrompt"] = "按下要绑定的键盘组合或鼠标操作",
        ["CaptureHint"] = "按 Esc 取消；组合键请先按住修饰键再按主键",
        ["AlreadyRunning"] = "悬浮键鼠助手已在运行。",
        ["TrayTip"] = "悬浮键鼠助手",
        ["TraySettings"] = "设置",
        ["TrayHide"] = "隐藏悬浮按钮",
        ["TrayShow"] = "显示悬浮按钮",
        ["TrayExit"] = "退出",
        ["ButtonTip"] = "左键触发 · 右键编辑 · 拖动移动",
        ["MouseLeftClick"] = "鼠标左键单击",
        ["MouseRightClick"] = "鼠标右键单击",
        ["MouseMiddleClick"] = "鼠标中键单击",
        ["MouseX1Click"] = "鼠标侧键1单击",
        ["MouseX2Click"] = "鼠标侧键2单击",
        ["MouseLeftDown"] = "鼠标左键按下",
        ["MouseRightDown"] = "鼠标右键按下",
        ["MouseMiddleDown"] = "鼠标中键按下",
        ["MouseX1Down"] = "鼠标侧键1按下",
        ["MouseX2Down"] = "鼠标侧键2按下",
        ["MouseLeftUp"] = "鼠标左键抬起",
        ["MouseRightUp"] = "鼠标右键抬起",
        ["MouseMiddleUp"] = "鼠标中键抬起",
        ["MouseX1Up"] = "鼠标侧键1抬起",
        ["MouseX2Up"] = "鼠标侧键2抬起",
        ["WheelUp"] = "滚轮上",
        ["WheelDown"] = "滚轮下",
        ["WheelLeft"] = "滚轮左",
        ["WheelRight"] = "滚轮右"
    };

    private static readonly Dictionary<string, string> En = new()
    {
        ["AppTitle"] = "Floating Keypad",
        ["SettingsTitle"] = "Floating Keypad - Settings",
        ["Name"] = "Name",
        ["Action"] = "Action",
        ["Capture"] = "Capture",
        ["Clear"] = "Clear",
        ["Cancel"] = "Cancel",
        ["Ok"] = "OK",
        ["Width"] = "Width",
        ["Height"] = "Height",
        ["Opacity"] = "Opacity",
        ["Background"] = "Background",
        ["Foreground"] = "Foreground",
        ["Language"] = "Language",
        ["Config"] = "Config",
        ["Appearance"] = "Floating Appearance",
        ["Pick"] = "Pick",
        ["Preview"] = "Preview",
        ["Text"] = "Text",
        ["Reset"] = "Reset Defaults",
        ["Export"] = "Export",
        ["Import"] = "Import",
        ["ExportTitle"] = "Export Config",
        ["ImportTitle"] = "Import Config",
        ["ExportFailed"] = "Export failed. Check that the file is writable.",
        ["ImportFailed"] = "Import failed: not a valid config file.",
        ["ImportConfirm"] = "Importing will overwrite all current settings. Continue?",
        ["SelectedButton"] = "Selected Button",
        ["SettingsHint"] = "Tip: drag a floating button to move it; right-click it to edit.",
        ["SaveApply"] = "Save & Apply",
        ["Close"] = "Close",
        ["AddButton"] = "Add Button",
        ["Delete"] = "Delete",
        ["NotSet"] = "(not set)",
        ["NewButton"] = "New Button",
        ["CaptureTitle"] = "Capture Key",
        ["CapturePrompt"] = "Press the key combination or mouse action to bind",
        ["CaptureHint"] = "Esc to cancel; hold modifiers before the main key",
        ["AlreadyRunning"] = "Floating Keypad is already running.",
        ["TrayTip"] = "Floating Keypad",
        ["TraySettings"] = "Settings",
        ["TrayHide"] = "Hide Buttons",
        ["TrayShow"] = "Show Buttons",
        ["TrayExit"] = "Exit",
        ["ButtonTip"] = "Left-click: trigger · Right-click: edit · Drag: move",
        ["MouseLeftClick"] = "Left Click",
        ["MouseRightClick"] = "Right Click",
        ["MouseMiddleClick"] = "Middle Click",
        ["MouseX1Click"] = "Mouse X1 Click",
        ["MouseX2Click"] = "Mouse X2 Click",
        ["MouseLeftDown"] = "Left Button Down",
        ["MouseRightDown"] = "Right Button Down",
        ["MouseMiddleDown"] = "Middle Button Down",
        ["MouseX1Down"] = "Mouse X1 Down",
        ["MouseX2Down"] = "Mouse X2 Down",
        ["MouseLeftUp"] = "Left Button Up",
        ["MouseRightUp"] = "Right Button Up",
        ["MouseMiddleUp"] = "Middle Button Up",
        ["MouseX1Up"] = "Mouse X1 Up",
        ["MouseX2Up"] = "Mouse X2 Up",
        ["WheelUp"] = "Wheel Up",
        ["WheelDown"] = "Wheel Down",
        ["WheelLeft"] = "Wheel Left",
        ["WheelRight"] = "Wheel Right"
    };
}
