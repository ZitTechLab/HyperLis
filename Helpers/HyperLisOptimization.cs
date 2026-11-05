using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace HyperLisUI.Helpers
{
    public static class HyperLisOptimization
    {
        public static async Task ExecuteAsync(IProgress<(int, string)>? progress = null)
        {
            progress?.Report((0, "Creating Restore Point..."));
            await CreateRestorePointAsync();

            progress?.Report((10, "Applying Registry Tweaks (Part 1-10)..."));
            await ApplyRegistryTweaksPart1Async();

            progress?.Report((30, "Optimizing Services..."));
            await OptimizeServicesAsync();

            progress?.Report((50, "Applying Additional Tweaks..."));
            await ApplyAdditionalTweaksAsync();

            progress?.Report((70, "Disabling Telemetry & Scheduled Tasks..."));
            await DisableTelemetryAndTasksAsync();

            progress?.Report((80, "Removing Bloatware (This may take a while)..."));
            await RemoveBloatwareAsync();

            progress?.Report((100, "Optimization Completed."));
        }

        private static async Task CreateRestorePointAsync()
        {
            // Basic system restore check and create
            string script = @"
                $status = Get-ComputerRestorePoint -LastStatus
                if ((Get-Service VSS).Status -ne 'Running') { Start-Service VSS }
                Checkpoint-Computer -Description 'HyperLis Optimization' -RestorePointType MODIFY_SETTINGS
            ";
            await Task.Run(() => OptimizationHelper.RunPowerShell(script));
        }

        private static async Task ApplyRegistryTweaksPart1Async()
        {
            await Task.Run(() =>
            {
                // Part 1: Activity History
                SetReg(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 0);
                SetReg(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities", 0);
                SetReg(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities", 0);

                // Part 2: Location
                SetReg(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location", "Value", "Deny");
                SetReg(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}", "SensorPermissionState", 0);
                SetReg(@"HKLM\SYSTEM\Maps", "AutoUpdateEnabled", 0);

                // Part 3: Storage Sense
                OptimizationHelper.RunPowerShell("Remove-Item -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy' -Recurse -ErrorAction SilentlyContinue");

                // Part 4: GameDVR
                SetReg(@"HKCU\System\GameConfigStore", "GameDVR_FSEBehavior", 2);
                SetReg(@"HKCU\System\GameConfigStore", "GameDVR_Enabled", 0);
                SetReg(@"HKCU\System\GameConfigStore", "GameDVR_DXGIHonorFSEWindowsCompatible", 1);
                SetReg(@"HKCU\System\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 1);
                SetReg(@"HKCU\System\GameConfigStore", "GameDVR_EFSEFeatureFlags", 0);
                SetReg(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0);
                
                // Part 7: Display Performance (Visual Effects)
                SetReg(@"HKCU\Control Panel\Desktop", "MenuShowDelay", "200");
                SetReg(@"HKCU\Control Panel\Desktop\WindowMetrics", "MinAnimate", "0");
                SetReg(@"HKCU\Control Panel\Keyboard", "KeyboardDelay", 0);
                SetReg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", 0);
                SetReg(@"HKCU\Software\Microsoft\Windows\DWM", "EnableAeroPeek", 0);

                if (Utilities.IsWindows11)
                {
                    // Fix: Trên Windows 11 24H2/26H2 (WinUI 3/XAML Islands), VisualFXSetting = 3 (Adjust for best performance)
                    // làm khung nền XAML của File Explorer bị mất cọ vẽ và chuyển sang màu đen kịt (#000000).
                    // Do đó trên Windows 11, giữ VisualFXSetting = 0 và bật DragFullWindows & ListviewAlphaSelect để bảo vệ giao diện.
                    SetReg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 0);
                    SetReg(@"HKCU\Control Panel\Desktop", "DragFullWindows", "1");
                    SetReg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ListviewAlphaSelect", 1);
                    SetReg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ListviewShadow", 1);
                }
                else
                {
                    SetReg(@"HKCU\Control Panel\Desktop", "DragFullWindows", "0");
                    SetReg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ListviewAlphaSelect", 0);
                    SetReg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ListviewShadow", 0);
                    SetReg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 3);
                }
                
                // Part 8: Notifications & Widgets/AI
                // Notification Center tweaks removed from here to prevent breaking the Calendar flyout on Windows 11.
                // It is now an individual toggle in the Taskbar settings.
                try { OptimizationHelper.DisableWidgets(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Failed to disable widgets: {ex.Message}"); }
                
                try
                {
                    if (Utilities.IsWindows11) OptimizationHelper.DisableCoPilotAI();
                    else OptimizationHelper.DisableCortana();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to disable AI/Cortana: {ex.Message}");
                }

                // Part 10: Mouse Acceleration
                SetReg(@"HKCU\Control Panel\Mouse", "MouseSpeed", "0");
                SetReg(@"HKCU\Control Panel\Mouse", "MouseThreshold1", "0");
                SetReg(@"HKCU\Control Panel\Mouse", "MouseThreshold2", "0");
            });
        }

        private static async Task OptimizeServicesAsync()
        {
            await Task.Run(() =>
            {
                // USER REQUEST: Run the original batch file instead of C# reimplementation.
                // The batch file is copied to Config\HyperLis.bat during build.
                string batPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "HyperLis.bat");
                
                if (System.IO.File.Exists(batPath))
                {
                   System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                   {
                       FileName = batPath,
                       UseShellExecute = true, // Required for .bat execution
                       Verb = "runas", // Admin privileges
                       WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                   })?.WaitForExit();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Batch file not found at: {batPath}");
                }
            });
        }

        private static async Task ApplyAdditionalTweaksAsync()
        {
             await Task.Run(() =>
            {
                // Part 12: WiFi
                OptimizationHelper.DisableWifiSense();

                // Part 13: Teredo
                SetReg(@"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "DisabledComponents", 1);

                // Part 14: Hibernation
                SetReg(@"HKLM\System\CurrentControlSet\Control\Session Manager\Power", "HibernateEnabled", 0);
                OptimizationHelper.RunProcess("powercfg.exe", "/hibernate off");

                // Part 17: Misc Optimizations -> Network Throttling etc
                SetReg(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", -1);
                SetReg(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0);
                
                // Gaming Priority
                SetReg(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority", 8);
                SetReg(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority", 6);
                SetReg(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Scheduling Category", "High");
                
                // Classic Context Menu (Win 11)
                try { OptimizationHelper.EnableClassicRightClickMenu(); } catch { }

                // Sticky Keys
                try { OptimizationHelper.DisableStickyKeys(); } catch { }

                // Show File Extensions
                try { OptimizationHelper.EnableFileExtensions(); } catch { }
            });

            await ApplyAdvancedSystemTweaksAsync();
        }

        private static async Task ApplyAdvancedSystemTweaksAsync()
        {
            await Task.Run(() =>
            {
                // Boot Menu Policy Legacy
                OptimizationHelper.RunProcess("bcdedit", "/set {current} bootmenupolicy Legacy");

                // Task Manager Preferences (Reset/Fix)
                string taskMgrScript = @"
                    if ((Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -Name CurrentBuild).CurrentBuild -lt 22557) {
                        $taskmgr = Start-Process -WindowStyle Hidden -FilePath taskmgr.exe -PassThru;
                        do {
                            Start-Sleep -Milliseconds 100;
                            $preferences = Get-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\TaskManager' -Name 'Preferences' -ErrorAction SilentlyContinue;
                        } until ($preferences);
                        Stop-Process $taskmgr;
                        $preferences.Preferences[28] = 0;
                        Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\TaskManager' -Name 'Preferences' -Type Binary -Value $preferences.Preferences;
                    }
                ";
                OptimizationHelper.RunPowerShell(taskMgrScript);

                // Remove specific Explorer Namespace key
                OptimizationHelper.RunPowerShell("Remove-Item -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace\\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}' -Recurse -ErrorAction SilentlyContinue");

                // Fix Managed by your organization in Edge
                OptimizationHelper.RunPowerShell("if (Test-Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge') { Remove-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge' -Recurse -ErrorAction SilentlyContinue }");

                // Group svchost.exe processes (Split Threshold)
                string svcHostScript = @"
                    $ram = (Get-CimInstance -ClassName Win32_PhysicalMemory | Measure-Object -Property Capacity -Sum).Sum / 1kb;
                    Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control' -Name 'SvcHostSplitThresholdInKB' -Type DWord -Value $ram -Force
                ";
                OptimizationHelper.RunPowerShell(svcHostScript);

                // Remove AutoLogger-Diagtrack-Listener
                string autoLoggerScript = @"
                    $autoLoggerDir = '$env:PROGRAMDATA\Microsoft\Diagnosis\ETLLogs\AutoLogger';
                    if (Test-Path '$autoLoggerDir\AutoLogger-Diagtrack-Listener.etl') { Remove-Item '$autoLoggerDir\AutoLogger-Diagtrack-Listener.etl' };
                    icacls $autoLoggerDir /deny SYSTEM:`(OI`)`(CI`)F | Out-Null
                ";
                OptimizationHelper.RunPowerShell(autoLoggerScript);

                // Disable Defender Auto Sample Submission
                OptimizationHelper.RunPowerShell("Set-MpPreference -SubmitSamplesConsent 2 -ErrorAction SilentlyContinue");
            });
        }

        private static async Task DisableTelemetryAndTasksAsync()
        {
            await Task.Run(() =>
            {
                var tasks = new[]
                {
                    @"Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                    @"Microsoft\Windows\Application Experience\ProgramDataUpdater",
                    @"Microsoft\Windows\Autochk\Proxy",
                    @"Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                    @"Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
                    @"Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
                    @"Microsoft\Windows\Feedback\Siuf\DmClient",
                    @"Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload",
                    @"Microsoft\Windows\Windows Error Reporting\QueueReporting",
                    @"Microsoft\Windows\Application Experience\MareBackup",
                    @"Microsoft\Windows\Application Experience\StartupAppTask",
                    @"Microsoft\Windows\Application Experience\PcaPatchDbTask",
                    @"Microsoft\Windows\Maps\MapsUpdateTask"
                };

                foreach(var t in tasks)
                {
                    OptimizationHelper.RunProcess("schtasks", $"/Change /TN \"{t}\" /Disable");
                }
                
                // Telemetry Registry
                SetReg(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 0);
                SetReg(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0);
            });
        }
        
        private static async Task RemoveBloatwareAsync()
        {
             await Task.Run(() =>
             {
                 // List of app packages to remove (from Part 18)
                 string[] apps = new[] {
                    "Microsoft.Microsoft3DViewer", "Microsoft.AppConnector", "Microsoft.BingFinance", 
                    "Microsoft.BingNews", "Microsoft.BingSports", "Microsoft.BingTranslator", 
                    "Microsoft.BingWeather", "Microsoft.BingFoodAndDrink", "Microsoft.BingHealthAndFitness", 
                    "Microsoft.BingTravel", "Microsoft.MinecraftUWP", "Microsoft.GamingServices", 
                    "Microsoft.GamingApp", "Microsoft.GetHelp", "Microsoft.Getstarted", "Microsoft.Messaging",
                    "Microsoft.MicrosoftOfficeHub", "Microsoft.MicrosoftSolitaireCollection", 
                    "Microsoft.MicrosoftStickyNotes", "Microsoft.MixedReality.Portal", "Microsoft.NetworkSpeedTest",
                    "Microsoft.News", "Microsoft.OneDrive", "Microsoft.Office.Lens", "Microsoft.OutlookForWindows",
                    "Microsoft.Office.Sway", "Microsoft.Office.OneNote", "Microsoft.OneConnect", "Microsoft.People",
                    "Microsoft.PowerAutomateDesktop", "Microsoft.Print3D", "Microsoft.SkypeApp", "Microsoft.Wallet",
                    "Microsoft.Whiteboard", "Microsoft.WindowsAlarms", "microsoft.windowscommunicationsapps",
                    "Microsoft.WindowsFeedbackHub", "Microsoft.WindowsMaps", "Microsoft.WindowsMeetNow",
                    "Microsoft.WindowsPhone", "Microsoft.WindowsSoundRecorder", "Microsoft.Windows.Ai.Copilot.Provider",
                    "Microsoft.XboxApp", "Microsoft.ConnectivityStore", "Microsoft.CommsPhone", 
                    "Microsoft.Xbox.TCUI", "Microsoft.XboxGameOverlay", "Microsoft.XboxGameCallableUI",
                    "Microsoft.XboxSpeechToTextOverlay", "Microsoft.XboxIdentityProvider", "Microsoft.ZuneMusic",
                    "Microsoft.ZuneVideo", "Microsoft.Todos", "Microsoft.YourPhone", "MicrosoftCorporationII.QuickAssist",
                    "*EclipseManager*", "*ActiproSoftwareLLC*", "*AdobeSystemsIncorporated.AdobePhotoshopExpress*",
                    "*Duolingo-LearnLanguagesforFree*", "*PandoraMediaInc*", "*CandyCrush*", "*BubbleWitch3Saga*",
                    "*Wunderlist*", "*Flipboard*", "*Twitter*", "*Facebook*", "*Royal Revolt*", "*Sway*", 
                    "*Speed Test*", "*MSTeams*", "*Dolby*", "*Viber*", "*ACGMediaPlayer*", "*Netflix*", 
                    "*OneCalendar*", "*LinkedInforWindows*", "*HiddenCityMysteryofShadows*", "*Hulu*", "*Clipchamp.Clipchamp*",
                    "*HiddenCity*", "*AdobePhotoshopExpress*", "*HotspotShieldFreeVPN*", "*Microsoft.Advertising.Xaml*"
                 };

                 foreach (var app in apps)
                 {
                     OptimizationHelper.RunPowerShell($"Get-AppxPackage -Name '{app}' | Remove-AppxPackage");
                 }
             });
        }

        private static void SetReg(string keyPath, string valueName, object value, RegistryValueKind kind = RegistryValueKind.String)
        {
            // Convert short root to full root
            keyPath = keyPath.Replace("HKLM", "HKEY_LOCAL_MACHINE").Replace("HKCU", "HKEY_CURRENT_USER");
            
            // Auto-detect type if not specified strictly/correctly for DWord
            if (value is int) kind = RegistryValueKind.DWord;
            
            try
            {
               Registry.SetValue(keyPath, valueName, value, kind);
            }
            catch(Exception ex) 
            {
               Debug.WriteLine($"Failed to set registry: {keyPath}\\{valueName} -> {ex.Message}");
            }
        }
    }
}
