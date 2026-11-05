title Advanced Tweaks By Zit's Tech Lab
@echo off
color 7
chcp 65001 >nul 2>&1                        
echo.                           Advanced Tweaks By Zit's Tech Lab              
timeout 2 > nul              

:: Restore Point.      
cls
Color 7		                     
echo.				          Restore Point
echo.				  Create restore point for safety
chcp 437 >nul
@echo off
setlocal enabledelayedexpansion

echo Checking system protection status...
wmic /namespace:\\root\default Path SystemRestore get DisableSR >nul 2>&1
if %errorlevel% equ 0 (
    echo System protection is enabled on the C: drive.
) else (
    echo System protection is not enabled on the C: drive. Enabling it...
    powershell -Command "Enable-ComputerRestore -Drive 'C:' -Confirm:$false"
)

echo Creating restore point...
powershell -Command "Checkpoint-Computer -Description 'Zit Restore Point'"
if %errorlevel% equ 0 (
    echo Restore point successfully created.
) else (
    echo Failed to create restore point.
)
timeout 2 > nul


:: AIO tweaks.
cls
color 7
chcp 65001 >nul 2>&1                                               
echo. Start                                                                         
timeout 2 > nul


cls
chcp 437 >nul
color 7
REM Disabling Activity History...
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v "EnableActivityFeed" /t REG_DWORD /d 0 /f
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v "PublishUserActivities" /t REG_DWORD /d 0 /f
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v "UploadUserActivities" /t REG_DWORD /d 0 /f
echo Part 1 tweaks applied successfully.
timeout 2 > nul

cls
color 7
REM Disabling Location...
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location" /v "Value" /t REG_SZ /d "Deny" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}" /v "SensorPermissionState" /t REG_DWORD /d 0 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Services\lfsvc\Service\Configuration" /v "Status" /t REG_DWORD /d 0 /f
reg add "HKLM\SYSTEM\Maps" /v "AutoUpdateEnabled" /t REG_DWORD /d 0 /f
echo Part 2 tweaks applied successfully.
timeout 2 > nul

cls
color 7
REM Removing Storage Sense...
powershell -Command "Remove-Item -Path 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy' -Recurse -ErrorAction SilentlyContinue"
echo Part 3 tweaks applied successfully.
timeout 2 > nul

cls
color 7
REM Disabling GameDVR...
reg add "HKCU\System\GameConfigStore" /v GameDVR_FSEBehavior /t REG_DWORD /d 2 /f
reg add "HKCU\System\GameConfigStore" /v GameDVR_Enabled /t REG_DWORD /d 0 /f
reg add "HKCU\System\GameConfigStore" /v GameDVR_DXGIHonorFSEWindowsCompatible /t REG_DWORD /d 1 /f
reg add "HKCU\System\GameConfigStore" /v GameDVR_HonorUserFSEBehaviorMode /t REG_DWORD /d 1 /f
reg add "HKCU\System\GameConfigStore" /v GameDVR_EFSEFeatureFlags /t REG_DWORD /d 0 /f
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR" /v AllowGameDVR /t REG_DWORD /d 0 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Search" /v BingSearchEnabled /t REG_DWORD /d 0 /f
echo Part 4 tweaks applied successfully.
timeout 2 > nul

cls
color 7
REM Enabling Numlock on start up...
reg.exe add "HKU\.DEFAULT\Control Panel\Keyboard" /v InitialKeyboardIndicators /t REG_DWORD /d 80000002 /f
echo Part 5 tweaks applied successfully
timeout 2 > nul

cls
color 7
REM Show File Extensions...
reg.exe add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v HideFileExt /t REG_DWORD /d 0 /f
echo Part 6 tweaks applied successfully
timeout 2 > nul

cls
color 7
REM Setting Display For Performance...
reg add "HKCU\Control Panel\Desktop" /v "DragFullWindows" /t REG_SZ /d "0" /f
reg add "HKCU\Control Panel\Desktop" /v "MenuShowDelay" /t REG_SZ /d "200" /f
reg add "HKCU\Control Panel\Desktop\WindowMetrics" /v "MinAnimate" /t REG_SZ /d "0" /f
reg add "HKCU\Control Panel\Keyboard" /v "KeyboardDelay" /t REG_DWORD /d 0 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v "ListviewAlphaSelect" /t REG_DWORD /d 0 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v "ListviewShadow" /t REG_DWORD /d 0 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v "TaskbarAnimations" /t REG_DWORD /d 0 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" /v "VisualFXSetting" /t REG_DWORD /d 3 /f
reg add "HKCU\Software\Microsoft\Windows\DWM" /v "EnableAeroPeek" /t REG_DWORD /d 0 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v "TaskbarMn" /t REG_DWORD /d 0 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v "TaskbarDa" /t REG_DWORD /d 0 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v "ShowTaskViewButton" /t REG_DWORD /d 0 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Search" /v "SearchboxTaskbarMode" /t REG_DWORD /d 0 /f
echo Part 7 tweaks applied successfully
timeout 2 > nul

cls
color 7
REM Disabling Notifications...
reg add "HKCU\Software\Policies\Microsoft\Windows\Explorer" /v DisableNotificationCenter /t REG_DWORD /d 1 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\PushNotifications" /v ToastEnabled /t REG_DWORD /d 0 /f
echo Part 8 tweaks applied successfully.
timeout 2 > nul


cls
color 7
REM Enabling Win 10 Right-Click Menu...
powershell -Command "New-Item -Path 'HKCU:\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}' -Name 'InprocServer32' -Force -Value ''"
echo Part 9 tweaks applied successfully.
timeout 2 > nul

cls
color 7
REM Disabling Mouse Acceleration...
reg add "HKCU\Control Panel\Mouse" /v MouseSpeed /t REG_SZ /d 0 /f
reg add "HKCU\Control Panel\Mouse" /v MouseThreshold1 /t REG_SZ /d 0 /f
reg add "HKCU\Control Panel\Mouse" /v MouseThreshold2 /t REG_SZ /d 0 /f
echo Part 10 tweaks applied successfully.
timeout 2 > nul

:: Homegroup Detection.
@echo off
cls
 D
REM Starting HomeGroup Tweaks...

setlocal enabledelayedexpansion

for /f "delims=" %%i in ('powershell -Command "Get-CimInstance -ClassName Win32_OperatingSystem | Select-Object -ExpandProperty Caption"') do (
    set OS_VERSION=%%i
)

echo Detected Windows version: %OS_VERSION%

if /i "%OS_VERSION%"=="Microsoft Windows 11 Home" (
    echo Windows 11 Home detected. Skipping
    timeout 2 > nul

) else if /i "%OS_VERSION%"=="Microsoft Windows 11 Home N" (
    echo Windows 11 Home N detected. Skipping 
    timeout 2 > nul

) else if /i "%OS_VERSION%"=="Microsoft Windows 11 Pro" (
    echo Windows 11 Pro detected. Skipping 
    timeout 2 > nul

) else if /i "%OS_VERSION%"=="Microsoft Windows 11 Pro N" (
    echo Windows 11 Pro N detected. Skipping 
    timeout 2 > nul

) else if /i "%OS_VERSION%"=="Microsoft Windows 11 Pro for Workstations" (
    echo Windows 11 Pro for Workstations detected. Skipping 
    timeout 2 > nul

) else if /i "%OS_VERSION%"=="Microsoft Windows 11 Pro N for Workstations" (
    echo Windows 11 Pro N for Workstations detected. Skipping 
    timeout 2 > nul

) else if /i "%OS_VERSION%"=="Microsoft Windows 11 Enterprise" (
    echo Windows 11 Enterprise detected. Skipping 
    timeout 2 > nul


) else if /i "%OS_VERSION%"=="Microsoft Windows 10 Home" (
    echo Windows 10 Home detected. Starting tweak ...
    sc config HomeGroupListener start=demand
    sc config HomeGroupProvider start=demand
    echo Part 11 tweaks applied successfully.
    timeout 2 > nul

) else if /i "%OS_VERSION%"=="Microsoft Windows 10 Home N" (
    echo Windows 10 Home N detected. Starting tweak...
    sc config HomeGroupListener start=demand
    sc config HomeGroupProvider start=demand
    echo Part 11 tweaks applied successfully.
    timeout 2 > nul

) else if /i "%OS_VERSION%"=="Microsoft Windows 10 Pro" (
    echo Windows 10 Pro detected. Starting tweak ...
    sc config HomeGroupListener start=demand
    sc config HomeGroupProvider start=demand
    echo Part 11 tweaks applied successfully.
    timeout 2 > nul

) else if /i "%OS_VERSION%"=="Microsoft Windows 10 Pro N" (
    echo Windows 10 Pro N detected. Starting tweak ...
    sc config HomeGroupListener start=demand
    sc config HomeGroupProvider start=demand
    echo Part 11 tweaks applied successfully.
    timeout 2 > nul

) else if /i "%OS_VERSION%"=="Microsoft Windows 10 Pro for Workstations" (
    echo Windows 10 Pro for Workstations detected. Starting tweak ...
    sc config HomeGroupListener start=demand
    sc config HomeGroupProvider start=demand
    echo Part 11 tweaks applied successfully.
    timeout 2 > nul

) else if /i "%OS_VERSION%"=="Microsoft Windows 10 Pro N for Workstations" (
    echo Windows 10 Pro N for Workstations detected. Starting tweak ...
    sc config HomeGroupListener start=demand
    sc config HomeGroupProvider start=demand
    echo Part 11 tweaks applied successfully.
    timeout 2 > nul

) else if /i "%OS_VERSION%"=="Microsoft Windows 10 Enterprise" (
    echo Windows 10 Enterprise. Starting tweak ...
    sc config HomeGroupListener start=demand
    sc config HomeGroupProvider start=demand
    echo Part 11 tweaks applied successfully.
    timeout 2 > nul


) else (
    echo Unable to detect your Windows version. Let's skip Tweaks.
    echo Continuing...
)

endlocal

cls
color 7
REM Disabling Unnecessary WiFi Settings...
reg add "HKLM\Software\Microsoft\PolicyManager\default\WiFi\AllowWiFiHotSpotReporting" /v "Value" /t REG_DWORD /d 0 /f
reg add "HKLM\Software\Microsoft\PolicyManager\default\WiFi\AllowAutoConnectToWiFiSenseHotspots" /v "Value" /t REG_DWORD /d 0 /f
echo Part 12 tweaks applied successfully.
timeout 2 > nul

cls
color 7
REM Disabling Teredo...
reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters" /v DisabledComponents /t REG_DWORD /d 1 /f
echo Part 13 tweaks applied successfully.
timeout 2 > nul


cls
color 7
REM Disabling Hibernation...
reg add "HKLM\System\CurrentControlSet\Control\Session Manager\Power" /v HibernateEnabled /t REG_DWORD /d 0 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings" /v ShowHibernateOption /t REG_DWORD /d 0 /f
powercfg.exe /hibernate off
echo Part 14 tweaks applied successfully.
timeout 2 > nul

cls
color 7
REM OOShutUp Optimize
echo Part 15 tweaks are running...
set "cfgFile=%TEMP%\ooshutup10.cfg"
set "exeFile=%TEMP%\OOSU10.exe"

curl.exe -s "https://drive.usercontent.google.com/download?id=1JWfScYNz-GUOGQieLsyHTyg3S3A-SWwM" -o "%cfgFile%"
if %errorlevel% neq 0 (
    echo Error: Failed to download the configuration file.
    exit /b 1
)

curl.exe -s "https://drive.usercontent.google.com/download?id=1cB9vJJu6p8V-tHddJeP31aBmm9ywS_VQ&confirm=t&uuid=720ae041-cbc2-488c-bbec-6c20f8df6fb7&at=APZUnTWshLlQIP-lERjiwD_IFB03%3A1718110465065" -o "%exeFile%"
if %errorlevel% neq 0 (
    echo Error: Failed to download the executable.
    exit /b 1
)

echo Downloaded configuration file and executable successfully.
"%exeFile%" "%cfgFile%" /quiet
if %errorlevel% equ 0 (
    echo Part 15 tweaks applied successfully.
) else (
    echo Error: Part 15 tweak-execution failed with exit code %errorlevel%.
)
timeout 2 > nul

cls
color 7
chcp 437 >nul
@echo off
REM Setting Services to manual...
sc config AJRouter start=disabled
sc config ALG start=demand
sc config AppIDSvc start=demand
sc config AppMgmt start=demand
sc config AppReadiness start=demand
sc config AppVClient start=disabled
sc config AppXSvc start=demand
sc config Appinfo start=demand
sc config AssignedAccessManagerSvc start=disabled
sc config AudioEndpointBuilder start=auto
sc config AudioSrv start=auto
sc config Audiosrv start=auto
sc config AxInstSV start=demand
sc config BDESVC start=demand
sc config BFE start=auto
sc config BITS start=delayed-auto
sc config BTAGService start=demand
sc config BcastDVRUserService_dc2a4 start=demand            
sc config BluetoothUserService_dc2a4 start=demand
sc config BrokerInfrastructure start=auto
sc config Browser start=demand
sc config BthAvctpSvc start=auto
sc config BthHFSrv start=auto
sc config CDPSvc start=demand
sc config CDPUserSvc_dc2a4 start=auto
sc config COMSysApp start=demand
sc config CaptureService_dc2a4 start=demand
sc config CertPropSvc start=demand
sc config ClipSVC start=demand
sc config ConsentUxUserSvc_dc2a4 start=demand
sc config CoreMessagingRegistrar start=auto
sc config CredentialEnrollmentManagerUserSvc_dc2a4 start=demand
sc config CryptSvc start=auto
sc config CscService start=demand
sc config DPS start=auto
sc config DcomLaunch start=auto
sc config DcpSvc start=demand
sc config DevQueryBroker start=demand
sc config DeviceAssociationBrokerSvc_dc2a4 start=demand
sc config DeviceAssociationService start=demand
sc config DeviceInstall start=demand
sc config DevicePickerUserSvc_dc2a4 start=demand
sc config DevicesFlowUserSvc_dc2a4 start=demand
sc config Dhcp start=auto
sc config DiagTrack start=disabled
sc config DialogBlockingService start=disabled
sc config DispBrokerDesktopSvc start=auto
sc config DisplayEnhancementService start=demand
sc config DmEnrollmentSvc start=demand
sc config Dnscache start=auto
sc config DoSvc start=delayed-auto
sc config DsSvc start=demand
sc config DsmSvc start=demand
sc config DusmSvc start=auto
sc config EFS start=demand
sc config EapHost start=demand
sc config EntAppSvc start=demand
sc config EventLog start=auto
sc config EventSystem start=auto
sc config FDResPub start=demand
sc config Fax start=demand
sc config FontCache start=auto
sc config FrameServer start=demand
sc config FrameServerMonitor start=demand
sc config GraphicsPerfSvc start=demand
sc config HomeGroupListener start=demand
sc config HomeGroupProvider start=demand
sc config HvHost start=demand
sc config IEEtwCollectorService start=demand
sc config IKEEXT start=demand
sc config InstallService start=demand
sc config InventorySvc start=demand
sc config IpxlatCfgSvc start=demand
sc config KeyIso start=auto
sc config KtmRm start=demand
sc config LSM start=auto
sc config LanmanServer start=auto
sc config LanmanWorkstation start=auto
sc config LicenseManager start=demand
sc config LxpSvc start=demand
sc config MSDTC start=demand
sc config MSiSCSI start=demand
sc config MapsBroker start=delayed-auto
sc config McpManagementService start=demand
sc config MessagingService_dc2a4 start=demand
sc config MicrosoftEdgeElevationService start=demand
sc config MixedRealityOpenXRSvc start=demand
sc config MpsSvc start=auto
sc config MsKeyboardFilter start=demand
sc config NPSMSvc_dc2a4 start=demand
sc config NaturalAuthentication start=demand
sc config NcaSvc start=demand
sc config NcbService start=demand
sc config NcdAutoSetup start=demand
sc config NetSetupSvc start=demand
sc config NetTcpPortSharing start=disabled
sc config Netlogon start=demand
sc config Netman start=demand
sc config NgcCtnrSvc start=demand
sc config NgcSvc start=demand
sc config NlaSvc start=demand
sc config OneSyncSvc_dc2a4 start=auto
sc config P9RdrService_dc2a4 start=demand
sc config PNRPAutoReg start=demand
sc config PNRPsvc start=demand
sc config PcaSvc start=demand
sc config PeerDistSvc start=demand
sc config PenService_dc2a4 start=demand
sc config PerfHost start=demand
sc config PhoneSvc start=demand
sc config PimIndexMaintenanceSvc_dc2a4 start=demand
sc config PlugPlay start=demand
sc config PolicyAgent start=demand
sc config Power start=auto
sc config PrintNotify start=demand
sc config PrintWorkflowUserSvc_dc2a4 start=demand
sc config ProfSvc start=auto
sc config PushToInstall start=demand
sc config QWAVE start=demand
sc config RasAuto start=demand
sc config RasMan start=demand
sc config RemoteAccess start=disabled
sc config RemoteRegistry start=disabled
sc config RetailDemo start=demand
sc config RmSvc start=demand
sc config RpcEptMapper start=auto
sc config RpcLocator start=demand
sc config RpcSs start=auto
sc config SCPolicySvc start=demand
sc config SCardSvr start=demand
sc config SDRSVC start=demand
sc config SEMgrSvc start=demand
sc config SENS start=auto
sc config SNMPTRAP start=demand
sc config SNMPTrap start=demand
sc config SSDPSRV start=demand
sc config SamSs start=auto
sc config ScDeviceEnum start=demand
sc config Schedule start=auto
sc config SecurityHealthService start=demand
sc config Sense start=demand
sc config SensorDataService start=demand
sc config SensorService start=demand
sc config SensrSvc start=demand
sc config SessionEnv start=demand
sc config SgrmBroker start=auto
sc config SharedAccess start=demand
sc config SharedRealitySvc start=demand
sc config ShellHWDetection start=auto
sc config SmsRouter start=demand
sc config Spooler start=auto
sc config SstpSvc start=demand
sc config StateRepository start=demand
sc config StiSvc start=demand
sc config StorSvc start=demand
sc config SysMain start=auto
sc config SystemEventsBroker start=auto
sc config TabletInputService start=demand
sc config TapiSrv start=demand
sc config TermService start=auto
sc config TextInputManagementService start=demand
sc config Themes start=auto
sc config TieringEngineService start=demand
sc config TimeBroker start=demand
sc config TimeBrokerSvc start=demand
sc config TokenBroker start=demand
sc config TrkWks start=auto
sc config TroubleshootingSvc start=demand
sc config TrustedInstaller start=demand
sc config UI0Detect start=demand
sc config UdkUserSvc_dc2a4 start=demand
sc config UevAgentService start=disabled
sc config UmRdpService start=demand
sc config UnistoreSvc_dc2a4 start=demand
sc config UserDataSvc_dc2a4 start=demand
sc config UserManager start=auto
sc config UsoSvc start=demand
sc config VGAuthService start=auto
sc config VMTools start=auto
sc config VSS start=demand
sc config VacSvc start=demand
sc config VaultSvc start=auto
sc config W32Time start=demand
sc config WEPHOSTSVC start=demand
sc config WFDSConMgrSvc start=demand
sc config WMPNetworkSvc start=demand
sc config WManSvc start=demand
sc config WPDBusEnum start=demand
sc config WSService start=demand
sc config WSearch start=delayed-auto
sc config WaaSMedicSvc start=demand
sc config WalletService start=demand
sc config WarpJITSvc start=demand
sc config WbioSrvc start=demand
sc config Wcmsvc start=auto
sc config WcsPlugInService start=demand
sc config WdNisSvc start=demand
sc config WdiServiceHost start=demand
sc config WdiSystemHost start=demand
sc config WebClient start=demand
sc config Wecsvc start=demand
sc config WerSvc start=demand
sc config WiaRpc start=demand
sc config WinDefend start=auto
sc config WinHttpAutoProxySvc start=demand
sc config WinRM start=demand
sc config Winmgmt start=auto
sc config WlanSvc start=auto
sc config WpcMonSvc start=demand
sc config WpnService start=demand
sc config WpnUserService_dc2a4 start=auto
sc config WwanSvc start=demand
sc config XblAuthManager start=demand
sc config XblGameSave start=demand
sc config XboxGipSvc start=demand
sc config XboxNetApiSvc start=demand
sc config autotimesvc start=demand
sc config bthserv start=demand
sc config camsvc start=demand
sc config cbdhsvc_dc2a4 start=demand
sc config cloudidsvc start=demand
sc config dcsvc start=demand
sc config defragsvc start=demand
sc config diagnosticshub.standardcollector.service start=demand
sc config diagsvc start=demand
sc config dmwappushservice start=demand
sc config dot3svc start=demand
sc config edgeupdate start=demand
sc config edgeupdatem start=demand
sc config embeddedmode start=demand
sc config fdPHost start=demand
sc config fhsvc start=demand
sc config gpsvc start=auto
sc config hidserv start=demand
sc config icssvc start=demand
sc config iphlpsvc start=auto
sc config lfsvc start=demand
sc config lltdsvc start=demand
sc config lmhosts start=demand
sc config mpssvc start=auto
sc config msiserver start=demand
sc config netprofm start=demand
sc config nsi start=auto
sc config p2pimsvc start=demand
sc config p2psvc start=demand
sc config perceptionsimulation start=demand
sc config pla start=demand
sc config seclogon start=demand
sc config shpamsvc start=disabled
sc config smphost start=demand
sc config spectrum start=demand
sc config sppsvc start=delayed-auto
sc config ssh-agent start=disabled
sc config svsvc start=demand
sc config swprv start=demand
sc config tiledatamodelsvc start=auto
sc config tzautoupdate start=disabled
sc config uhssvc start=disabled
sc config upnphost start=demand
sc config vds start=demand
sc config vm3dservice start=demand
sc config vmicguestinterface start=demand
sc config vmicheartbeat start=demand
sc config vmickvpexchange start=demand
sc config vmicrdv start=demand
sc config vmicshutdown start=demand
sc config vmictimesync start=demand
sc config vmicvmsession start=demand
sc config vmicvss start=demand
sc config vmvss start=demand
sc config wbengine start=demand
sc config wcncsvc start=demand
sc config webthreatdefsvc start=demand
sc config webthreatdefusersvc_dc2a4 start=auto
sc config wercplsupport start=demand
sc config wisvc start=demand
sc config wlidsvc start=demand
sc config wlpasvc start=demand
sc config wmiApSrv start=demand
sc config workfolderssvc start=demand
sc config wscsvc start=delayed-auto
sc config wuauserv start=demand
sc config wudfsvc start=demand                   
echo Part 16 tweaks applied successfully.
timeout 2 > nul

cls
color 7
REM Disabling Telemetry...
schtasks /Change /TN "Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser" /Disable 
schtasks /Change /TN "Microsoft\Windows\Application Experience\ProgramDataUpdater" /Disable 
schtasks /Change /TN "Microsoft\Windows\Autochk\Proxy" /Disable
schtasks /Change /TN "Microsoft\Windows\Customer Experience Improvement Program\Consolidator" /Disable 
schtasks /Change /TN "Microsoft\Windows\Customer Experience Improvement Program\UsbCeip" /Disable 
schtasks /Change /TN "Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector" /Disable 
schtasks /Change /TN "Microsoft\Windows\Feedback\Siuf\DmClient" /Disable 
schtasks /Change /TN "Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload" /Disable 
schtasks /Change /TN "Microsoft\Windows\Windows Error Reporting\QueueReporting" /Disable 
schtasks /Change /TN "Microsoft\Windows\Application Experience\MareBackup" /Disable 
schtasks /Change /TN "Microsoft\Windows\Application Experience\StartupAppTask" /Disable 
schtasks /Change /TN "Microsoft\Windows\Application Experience\PcaPatchDbTask" /Disable
schtasks /Change /TN "Microsoft\Windows\Maps\MapsUpdateTask" /Disable
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection" /v AllowTelemetry /t REG_DWORD /d 0 /f
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection" /v AllowTelemetry /t REG_DWORD /d 0 /f
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v ContentDeliveryAllowed /t REG_DWORD /d 0 /f
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v OemPreInstalledAppsEnabled /t REG_DWORD /d 0 /f
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v PreInstalledAppsEnabled /t REG_DWORD /d 0 /f
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v PreInstalledAppsEverEnabled /t REG_DWORD /d 0 /f
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SilentInstalledAppsEnabled /t REG_DWORD /d 0 /f
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SubscribedContent-338387Enabled /t REG_DWORD /d 0 /f
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SubscribedContent-338388Enabled /t REG_DWORD /d 0 /f
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SubscribedContent-338389Enabled /t REG_DWORD /d 0 /f
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SubscribedContent-353698Enabled /t REG_DWORD /d 0 /f
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SystemPaneSuggestionsEnabled /t REG_DWORD /d 0 /f
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent" /v DisableWindowsConsumerFeatures /t REG_DWORD /d 1 /f
reg add "HKCU\SOFTWARE\Microsoft\Siuf\Rules" /v NumberOfSIUFInPeriod /t REG_DWORD /d 0 /f
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection" /v DoNotShowFeedbackNotifications /t REG_DWORD /d 1 /f
reg add "HKCU\SOFTWARE\Policies\Microsoft\Windows\CloudContent" /v DisableTailoredExperiencesWithDiagnosticData /t REG_DWORD /d 1 /f
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo" /v DisabledByGroupPolicy /t REG_DWORD /d 1 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting" /v Disabled /t REG_DWORD /d 1 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config" /v DODownloadMode /t REG_DWORD /d 1 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Remote Assistance" /v fAllowToGetHelp /t REG_DWORD /d 0 /f
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\OperationStatusManager" /v EnthusiastMode /t REG_DWORD /d 1 /f
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v ShowTaskViewButton /t REG_DWORD /d 0 /f
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\People" /v PeopleBand /t REG_DWORD /d 0 /f
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v LaunchTo /t REG_DWORD /d 1 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v LongPathsEnabled /t REG_DWORD /d 1 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching" /v SearchOrderConfig /t REG_DWORD /d 1 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v SystemResponsiveness /t REG_DWORD /d 0 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v NetworkThrottlingIndex /t REG_DWORD /d 4294967295 /f
reg add "HKCU\Control Panel\Desktop" /v MenuShowDelay /t REG_DWORD /d 1 /f
reg add "HKCU\Control Panel\Desktop" /v AutoEndTasks /t REG_DWORD /d 1 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v ClearPageFileAtShutdown /t REG_DWORD /d 0 /f
reg add "HKLM\SYSTEM\ControlSet001\Services\Ndu" /v Start /t REG_DWORD /d 2 /f
reg add "HKCU\Control Panel\Mouse" /v MouseHoverTime /t REG_SZ /d 400 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters" /v IRPStackSize /t REG_DWORD /d 30 /f
reg add "HKCU\SOFTWARE\Policies\Microsoft\Windows\Windows Feeds" /v EnableFeeds /t REG_DWORD /d 0 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Feeds" /v ShellFeedsTaskbarViewMode /t REG_DWORD /d 2 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer" /v HideSCAMeetNow /t REG_DWORD /d 1 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "GPU Priority" /t REG_DWORD /d 8 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v Priority /t REG_DWORD /d 6 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Scheduling Category" /t REG_SZ /d High /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\UserProfileEngagement" /v ScoobeSystemSettingEnabled /t REG_DWORD /d 0 /f
echo Part 17 tweaks applied successfully.
timeout 2 > nul


REM Set boot menu policy to Legacy
bcdedit /set {current} bootmenupolicy Legacy

REM Check Windows version and configure Task Manager if necessary
powershell -command ^
    "if ((Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -Name CurrentBuild).CurrentBuild -lt 22557) { " ^
    "$taskmgr = Start-Process -WindowStyle Hidden -FilePath taskmgr.exe -PassThru; " ^
    "do { " ^
    "Start-Sleep -Milliseconds 100; " ^
    "$preferences = Get-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\TaskManager' -Name 'Preferences' -ErrorAction SilentlyContinue; " ^
    "} until ($preferences); " ^
    "Stop-Process $taskmgr; " ^
    "$preferences.Preferences[28] = 0; " ^
    "Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\TaskManager' -Name 'Preferences' -Type Binary -Value $preferences.Preferences; " ^
    "}"

REM Remove specific registry key
powershell -command "Remove-Item -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}' -Recurse -ErrorAction SilentlyContinue"

REM Fix Managed by your organization in Edge
powershell -command "if (Test-Path 'HKLM:\SOFTWARE\Policies\Microsoft\Edge') { Remove-Item -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Edge' -Recurse -ErrorAction SilentlyContinue }"

REM Group svchost.exe processes
powershell -command ^
    "$ram = (Get-CimInstance -ClassName Win32_PhysicalMemory | Measure-Object -Property Capacity -Sum).Sum / 1kb; " ^
    "Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control' -Name 'SvcHostSplitThresholdInKB' -Type DWord -Value $ram -Force"

REM Remove AutoLogger-Diagtrack-Listener.etl if it exists and deny SYSTEM access to AutoLogger directory
powershell -command ^
    "$autoLoggerDir = '$env:PROGRAMDATA\Microsoft\Diagnosis\ETLLogs\AutoLogger'; " ^
    "if (Test-Path '$autoLoggerDir\AutoLogger-Diagtrack-Listener.etl') { Remove-Item '$autoLoggerDir\AutoLogger-Diagtrack-Listener.etl' };" ^
    "icacls $autoLoggerDir /deny SYSTEM:`(OI`)`(CI`)F | Out-Null"


timeout 2 > nul

echo Removing ALL MS Store Apps - USE WITH CAUTION!!!!!

REM Disable the ability to break the batch script
setlocal enabledelayedexpansion

REM Define the list of app packages to remove
set appxPackages=^
    Microsoft.Microsoft3DViewer^
    Microsoft.AppConnector^
    Microsoft.BingFinance^
    Microsoft.BingNews^
    Microsoft.BingSports^
    Microsoft.BingTranslator^
    Microsoft.BingWeather^
    Microsoft.BingFoodAndDrink^
    Microsoft.BingHealthAndFitness^
    Microsoft.BingTravel^
    Microsoft.MinecraftUWP^
    Microsoft.GamingServices^
    Microsoft.GetHelp^
    Microsoft.Getstarted^
    Microsoft.Messaging^
    Microsoft.Microsoft3DViewer^
    Microsoft.MicrosoftSolitaireCollection^
    Microsoft.NetworkSpeedTest^
    Microsoft.News^
    Microsoft.Office.Lens^
    Microsoft.Office.Sway^
    Microsoft.Office.OneNote^
    Microsoft.OneConnect^
    Microsoft.People^
    Microsoft.Print3D^
    Microsoft.SkypeApp^
    Microsoft.Wallet^
    Microsoft.Whiteboard^
    Microsoft.WindowsAlarms^
    microsoft.windowscommunicationsapps^
    Microsoft.WindowsFeedbackHub^
    Microsoft.WindowsMaps^
    Microsoft.WindowsPhone^
    Microsoft.WindowsSoundRecorder^
    Microsoft.XboxApp^
    Microsoft.ConnectivityStore^
    Microsoft.CommsPhone^
    Microsoft.ScreenSketch^
    Microsoft.Xbox.TCUI^
    Microsoft.XboxGameOverlay^
    Microsoft.XboxGameCallableUI^
    Microsoft.XboxSpeechToTextOverlay^
    Microsoft.MixedReality.Portal^
    Microsoft.XboxIdentityProvider^
    Microsoft.ZuneMusic^
    Microsoft.ZuneVideo^
    Microsoft.Getstarted^
    Microsoft.MicrosoftOfficeHub^
    *EclipseManager*^
    *ActiproSoftwareLLC*^
    *AdobeSystemsIncorporated.AdobePhotoshopExpress*^
    *Duolingo-LearnLanguagesforFree*^
    *PandoraMediaInc*^
    *CandyCrush*^
    *BubbleWitch3Saga*^
    *Wunderlist*^
    *Flipboard*^
    *Twitter*^
    *Facebook*^
    *Royal Revolt*^
    *Sway*^
    *Speed Test*^
    *Dolby*^
    *Viber*^
    *ACGMediaPlayer*^
    *Netflix*^
    *OneCalendar*^
    *LinkedInforWindows*^
    *HiddenCityMysteryofShadows*^
    *Hulu*^
    *HiddenCity*^
    *AdobePhotoshopExpress*^
    *HotspotShieldFreeVPN*^
    *Microsoft.Advertising.Xaml*

REM Loop through the list and uninstall each app
for %%A in (%appxPackages%) do (
    echo Removing %%A
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-AppxPackage -Name '%%A' | Remove-AppxPackage"
)

echo All specified MS Store apps have been removed.
timeout 2 >nul
powershell -Command "Set-ExecutionPolicy RemoteSigned -Scope CurrentUser"
timeout 20 >nul
echo Restart may nha
pause > nul
del "%~f0"