; ASLT - ANNA Synthetic data Labeling Toolkit v1.0.0 Installer
; Per Phase 5 CONTEXT.md decisions D-01..D-17
; Compile with: "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" ASLT-Setup.iss

#define MyAppName "ANNA 합성데이터 라벨링 툴킷 (ASLT)"
#define MyAppNameEn "ASLT - ANNA Synthetic data Labeling Toolkit"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "ANNA"
#define MyAppExeName "ASLTv1.exe"
#define PublishDir "..\bin\Release\net8.0-windows\win-x64\publish"

[Setup]
; Unique AppId — do not regenerate between versions (upgrades in place).
AppId={{B4A2C1F0-8E4D-4A6B-9F3A-ASLT10000001}}
AppName={#MyAppNameEn}
AppVersion={#MyAppVersion}
AppVerName={#MyAppNameEn} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoCopyright=Copyright (C) ANNA 2026
; D-02: Default install path C:\Program Files\ANNA\ASLT
DefaultDirName={autopf}\ANNA\ASLT
DefaultGroupName=ANNA\ASLT
DisableProgramGroupPage=yes
; D-17: Installer filename ASLT-Setup-v1.0.0.exe
OutputBaseFilename=ASLT-Setup-v{#MyAppVersion}
OutputDir=Output
Compression=lzma2
SolidCompression=yes
; 64-bit only per PORT-01 (Win10/11 x64)
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
MinVersion=10.0.17763
PrivilegesRequired=admin
WizardStyle=modern
; D-10: No code signing
; UninstallDisplayName / UninstallDisplayIcon shown in Add/Remove Programs
UninstallDisplayName={#MyAppNameEn} {#MyAppVersion}
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
; Claude's discretion: Korean primary, English fallback
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; D-03: Desktop shortcut optional via checkbox; Start Menu always created
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; D-04/D-05: Self-contained publish output — bundles .NET 8 runtime + OpenCvSharp natives
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; D-07: Bundled FFmpeg → {app}\ffmpeg\ffmpeg.exe (consumed by VideoService.SetupFFmpegPath)
Source: "ffmpeg\ffmpeg.exe"; DestDir: "{app}\ffmpeg"; Flags: ignoreversion

[Icons]
; Start Menu (always)
Name: "{autoprograms}\ANNA\ASLT"; Filename: "{app}\{#MyAppExeName}"
Name: "{autoprograms}\ANNA\ASLT 제거"; Filename: "{uninstallexe}"
; Desktop (optional, via task)
Name: "{autodesktop}\ASLT"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,ASLT}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; D-15: Remove everything under <InstallDir> including logs/ — but NOT user JSON outside InstallDir
; Legacy path (pre-84c498e builds wrote logs here)
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\ffmpeg"
; Runtime logs moved to LocalAppData in 84c498e — clean those on uninstall too
Type: filesandordirs; Name: "{localappdata}\ANNA\ASLT\logs"
Type: dirifempty; Name: "{localappdata}\ANNA\ASLT"
Type: dirifempty; Name: "{localappdata}\ANNA"
; Final sweep: any leftover files inside {app} that were created post-install
Type: dirifempty; Name: "{app}"
