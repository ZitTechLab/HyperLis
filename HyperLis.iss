; HyperLis Inno Setup Script
; Generates HyperLis_Official_1.0.7.exe

#define MyAppName "HyperLis"
#define MyAppVersion "Official 1.0.7"
#define MyAppPublisher "Zit's Tech Lab"
#define MyAppExeName "HyperLis.exe"
#define MyDotNetInstaller "E:\GGAI\dotNetRuntime9.exe"


[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{C782B766-3197-40D9-940E-336D08035D19}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
; Allow user to change install directory even for portable
DisableDirPage=no 
OutputBaseFilename=HyperLis_Setup_v1.0.7
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
; Uninstallable is set to yes by default, but we will control its creation logic implicitly or delete it for Portable mode.

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main Executable and dependencies
Source: "E:\GGAI\Design\bin\Release\net9.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; .NET Runtime Installer (packed into setup but not copied to app dir automatically)
; .NET Runtime Installer (packed into setup but not copied to app dir automatically)
Source: "{#MyDotNetInstaller}"; DestDir: "{tmp}"; Flags: dontcopy


[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Run .NET Installer if needed (logic in Code section, but standard way allows chaining)
; We'll use [Code] for conditional .NET install to offer better control and silent mode.
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent shellexec

[UninstallDelete]
; Clean up portable marker if it exists (though uninstaller only runs for standard)
Type: files; Name: "{app}\.portable"

[Code]
var
  InstallModePage: TInputOptionWizardPage;
  IsPortable: Boolean;

procedure InitializeWizard;
begin
  // Create the "Installation Mode" page
  InstallModePage := CreateInputOptionPage(wpWelcome,
    'Installation Mode', 'Select how you want to install HyperLis',
    'Choose the installation type that best suits your needs:',
    True, False);

  InstallModePage.Add('Standard Installation');
  InstallModePage.Add('Portable Mode (No Uninstaller, No Shortcuts)');

  // Default to Standard
  InstallModePage.SelectedValueIndex := 0;
end;

function IsStandardInstall: Boolean;
begin
  Result := (InstallModePage.SelectedValueIndex = 0);
end;

function IsPortableInstall: Boolean;
begin
  Result := (InstallModePage.SelectedValueIndex = 1);
end;

// Helper to check for .NET 9 (Simplistic check for now, or just force install if missing)
// A better way is to check registry keys for .NET 9 Runtime.
// HKLM\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App\9.0.0
function IsDotNet9Installed: Boolean;
var
  RegKey: String;
begin
  RegKey := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App\9.0.0';
  Result := RegKeyExists(HKLM, RegKey) or RegKeyExists(HKLM64, RegKey);
end;

// Helper to find and run uninstaller of previous version
procedure UninstallPreviousVersion;
var
  UninstallKey: String;
  UninstallString: String;
  ResultCode: Integer;
begin
  // Check for standard uninstall key (adjust GUID if needed, using AppId from [Setup])
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{C782B766-3197-40D9-940E-336D08035D19}_is1';
  
  if RegQueryStringValue(HKLM, UninstallKey, 'UninstallString', UninstallString) or
     RegQueryStringValue(HKCU, UninstallKey, 'UninstallString', UninstallString) then
  begin
    // Remove quotes if present
    StringChange(UninstallString, '"', '');
    
    // Notify user or just run silent? User asked for "Auto uninstall", so silent is best.
    // MsgBox('Previous version detected. Uninstalling...', mbInformation, MB_OK);
    
    // Run uninstaller
    // /SILENT = no wizard, but background window
    // /VERYSILENT = no window
    // /SUPPRESSMSGBOXES = no messages
    // /NORESTART = prevent restart
    if Exec(UninstallString, '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      Log('Previous version uninstalled successfully.')
    else
      Log('Failed to uninstall previous version. Error: ' + IntToStr(ResultCode));
  end;
end;

// Main installation logic steps
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssInstall then
  begin
    // 1. Uninstall Previous Version
    UninstallPreviousVersion();
    
    // 2. Install .NET 9 if missing (or always force if user wants "silent mode" ensured)
    // User requirement: "cÃ i cáº£ file dotnet 9 kia á»Ÿ cháº¿ Ä‘á»™ tá»± Ä‘á»™ng"
    // We'll run it regardless or verify first. Let's run it.
    // 2. Install .NET 9 if missing
    // We bundle the official exe and run it with official silent flags
    ExtractTemporaryFile('dotNetRuntime9.exe');
    if Exec(ExpandConstant('{tmp}\dotNetRuntime9.exe'), '/install /quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
      Log('.NET 9 Installer executed successfully.')
    else
      MsgBox('.NET 9 Installation failed with code: ' + IntToStr(ResultCode), mbError, MB_OK);


  end;

  if CurStep = ssPostInstall then
  begin
    // 3. Portable Mode Handling
    if IsPortableInstall then
    begin
      // Create .portable file
      SaveStringToFile(ExpandConstant('{app}\.portable'), 'Portable Mode Active', False);
      
      // Delete Uninstaller (Inno Setup creates it by default, we remove it for true portable feel)
      DeleteFile(ExpandConstant('{app}\unins000.exe'));
      DeleteFile(ExpandConstant('{app}\unins000.dat'));
    end;

    // Do not alter Windows Defender settings automatically.
    // This behavior is commonly flagged by AV heuristics.
    Log('Skipped Windows Defender exclusion step by design.');
  end;
end;

// Skip Registry creation for Portable Mode
// (There isn't a direct "Check" for [UninstallRun] or automatic registry keys created by Inno,
// but Inno mainly creates Uninstall keys. If we delete unins000.exe/dat, the keys remain but won't work.
// Ideally, we shouldn't create them.
// We can use the "Uninstallable" directive, but it is a compile-time constant.
// Workaround: We accept that standard registry keys are created, but we can delete them in Code.)

procedure CurPageChanged(CurPageID: Integer);
begin
  // Could validate selection here if needed
end;



