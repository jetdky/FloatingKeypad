; 悬浮键鼠助手 - Inno Setup 安装脚本
; 编译：ISCC.exe FloatingKeypad.iss
; 产物：D:\FloatingKeypad\installer\FloatingKeypad-Setup-<版本>.exe

#define AppName "悬浮键鼠助手"
#define AppExe "FloatingKeypad.exe"
#define PublishDir "D:\FloatingKeypad\publish"
#define OutputDir "D:\FloatingKeypad\installer"
#define IconFile "..\src\FloatingKeypad\Assets\logo.ico"

; 版本号由 build-release.ps1 通过 ISCC /DAppVersion=<版本> 注入；单独编译时回退默认值
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{A1B2C3D4-0003-4000-8000-000000000003}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppName}
DefaultDirName={autopf}\FloatingKeypad
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=FloatingKeypad-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#AppExe}
SetupIconFile={#IconFile}
AllowNoIcons=yes

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent shellexec
