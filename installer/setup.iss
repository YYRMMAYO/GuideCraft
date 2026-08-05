; GuideCraft 引导式AI助手 — Inno Setup 安装脚本
; 打包对象：自包含单文件发布产物

#define MyAppName "GuideCraft"
#define MyAppVersion "1.3.0"
#define MyAppPublisher "YYRMMAYO"
#define MyAppExeName "GuideCraft.exe"
#define MyAppUrl "https://github.com/YYRMMAYO/GuideCraft"

[Setup]
AppId={{A27B5232-5F4A-40E0-934D-6F15CEE3890B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppUrl}
AppSupportURL={#MyAppUrl}
DefaultDirName={autopf}\GuideCraft
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename=GuideCraft-Setup-{#MyAppVersion}
OutputDir=G:\AIOP\dist
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=G:\AIOP\GuideCraft\Assets\app.ico

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务:"

[Files]
Source: "G:\AIOP\GuideCraft\bin\Release\net10.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "立即运行 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\GuideCraft"
