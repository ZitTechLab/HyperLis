; HyperLis Inno Setup Script
; Generates HyperLis_Official_1.0.7.exe

#define MyAppName "HyperLis"
#define MyAppVersion "Official 1.0.7"
#define MyAppPublisher "Zit's Tech Lab"
#define MyAppExeName "HyperLis.exe"

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
OutputBaseFilename=HyperLis_Setup_v1.0.7_NoDotNet
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

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
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
begin
  if CurStep = ssInstall then
  begin
    // 1. Uninstall Previous Version
    UninstallPreviousVersion();
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

procedure CurPageChanged(CurPageID: Integer);
begin
  // Could validate selection here if needed
end;
