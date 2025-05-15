[Setup]
AppName=RawVision
AppVersion=1.0
DefaultDirName={commonpf}\Daltonian Scientific\RawVision
DefaultGroupName=RawVision
OutputDir=.
OutputBaseFilename=RawVisionInstaller
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "RawVisionBuild\Release\net8.0-windows\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
; Shortcut to the main app
Name: "{commondesktop}\RawVision PDA"; Filename: "{app}\RawVisionPDA.exe"; Tasks: desktopicon_pda
Name: "{commondesktop}\RawVision MS"; Filename: "{app}\RawVisionMS.exe"; Tasks: desktopicon_ms


[Tasks]
; Tasks to let the user choose which desktop shortcuts to create
Name: "desktopicon_pda"; Description: "Create &RawVisionPDA desktop shortcut"; GroupDescription: "Additional shortcuts:"
Name: "desktopicon_ms"; Description: "Create &RawVisionMS desktop shortcut"; GroupDescription: "Additional shortcuts:"
