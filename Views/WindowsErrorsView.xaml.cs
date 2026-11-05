using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HyperLisUI.Helpers;

namespace HyperLisUI.Views
{
    public partial class WindowsErrorsView : UserControl
    {
        public ObservableCollection<LogMessage> LogMessages { get; } = new ObservableCollection<LogMessage>();

        public WindowsErrorsView()
        {
            InitializeComponent();
            this.Loaded += WindowsErrorsView_Loaded;
            LogList.ItemsSource = LogMessages;
            InitializeErrorList();
        }

        private void InitializeErrorList()
        {
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("optWinSystemScan"), Tag = "SFC" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("optWinProfile"), Tag = "PROFILE" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("optWinSMBGuest"), Tag = "SMB" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("optWinIconCache"), Tag = "ICON_CACHE" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("optWinBSODNetio"), Tag = "NETIO" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("optWinBitLocker"), Tag = "BITLOCKER" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("optWinFilePreview"), Tag = "PREVIEW" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("optWinSearch"), Tag = "SEARCH" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("optWinFontCache"), Tag = "FONT_CACHE" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("optWinUpdate"), Tag = "UPDATE" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("optWinNetwork"), Tag = "NETWORK" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("optWinStore"), Tag = "STORE" });
            
            ComboErrorSelector.SelectedIndex = 0;
        }

        private void WindowsErrorsView_Loaded(object sender, RoutedEventArgs e)
        {
            LocalizationService.LanguageChanged += OnLanguageChanged;
            ApplyTranslations();
        }

        private void OnLanguageChanged()
        {
            Dispatcher.Invoke(ApplyTranslations);
        }

        private void ApplyTranslations()
        {
            TextTitle.Text = LocalizationService.Get("titleWindowsErrors");
            TextDesc.Text = LocalizationService.Get("descWindowsErrors");
            TextSelectError.Text = LocalizationService.Get("lblSelectWindowsError");
            BtnStartFix.Content = LocalizationService.Get("btnStartFixWindows");
            if (BtnAutoDetect != null)
                BtnAutoDetect.Content = LocalizationService.Get("btnAutoDetectText") ?? "Auto Diagnose";
            TextLogLabel.Text = LocalizationService.Get("lblExecutionLogs");

            if (ComboErrorSelector.Items.Count == 12)
            {
                ((ComboBoxItem)ComboErrorSelector.Items[0]).Content = LocalizationService.Get("optWinSystemScan");
                ((ComboBoxItem)ComboErrorSelector.Items[1]).Content = LocalizationService.Get("optWinProfile");
                ((ComboBoxItem)ComboErrorSelector.Items[2]).Content = LocalizationService.Get("optWinSMBGuest");
                ((ComboBoxItem)ComboErrorSelector.Items[3]).Content = LocalizationService.Get("optWinIconCache");
                ((ComboBoxItem)ComboErrorSelector.Items[4]).Content = LocalizationService.Get("optWinBSODNetio");
                ((ComboBoxItem)ComboErrorSelector.Items[5]).Content = LocalizationService.Get("optWinBitLocker");
                ((ComboBoxItem)ComboErrorSelector.Items[6]).Content = LocalizationService.Get("optWinFilePreview");
                ((ComboBoxItem)ComboErrorSelector.Items[7]).Content = LocalizationService.Get("optWinSearch");
                ((ComboBoxItem)ComboErrorSelector.Items[8]).Content = LocalizationService.Get("optWinFontCache");
                ((ComboBoxItem)ComboErrorSelector.Items[9]).Content = LocalizationService.Get("optWinUpdate");
                ((ComboBoxItem)ComboErrorSelector.Items[10]).Content = LocalizationService.Get("optWinNetwork");
                ((ComboBoxItem)ComboErrorSelector.Items[11]).Content = LocalizationService.Get("optWinStore");
            }
        }

        private void AddLog(string msg, string color = "#333")
        {
            Dispatcher.Invoke(() => {
                LogMessages.Add(new LogMessage { Text = $"[{DateTime.Now:HH:mm:ss}] {msg}", Color = color });
                LogScrollViewer.ScrollToEnd();
            });
        }

        private void ExecuteCmd(string command, string taskName)
        {
            AddLog(string.Format(LocalizationService.Get("logExecuting"), taskName), "#555");
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var p = System.Diagnostics.Process.Start(processInfo))
                {
                    p.WaitForExit();
                    if (p.ExitCode == 0)
                        AddLog(string.Format(LocalizationService.Get("logOK"), taskName), "Green");
                    else
                        AddLog(string.Format(LocalizationService.Get("logWarn"), taskName, p.ExitCode), "Orange");
                }
            }
            catch (Exception ex)
            {
                AddLog(string.Format(LocalizationService.Get("logFailed"), taskName, ex.Message), "Red");
            }
        }

        private string ExecuteCmdAndGetOutput(string command)
        {
            try
            {
                var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                using (var p = Process.Start(processInfo))
                {
                    return p.StandardOutput.ReadToEnd();
                }
            }
            catch { return ""; }
        }

        private bool CheckServiceRunning(string serviceName)
        {
            string output = ExecuteCmdAndGetOutput($"sc query {serviceName}");
            return output.Contains("RUNNING");
        }

        private bool CheckRegistryValueExists(string keyPath, string valueName)
        {
            string output = ExecuteCmdAndGetOutput($"reg query \"{keyPath}\" /v {valueName}");
            return output.Contains(valueName);
        }

        private async Task ExecuteCmdWithProgress(string command, string taskName, Action<int, string> reportProgress)
        {
            AddLog(string.Format(LocalizationService.Get("logExecuting"), taskName), "#555");
            try
            {
                var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                using (var p = new Process { StartInfo = processInfo })
                {
                    p.Start();
                    
                    await Task.Run(async () => 
                    {
                        var buffer = new char[1];
                        string currentStr = "";

                        while (true)
                        {
                            int read = await p.StandardOutput.ReadAsync(buffer, 0, 1).ConfigureAwait(false);
                            if (read == 0) break;

                            char c = buffer[0];

                            if (c == '\r' || c == '\n')
                            {
                                if (!string.IsNullOrWhiteSpace(currentStr))
                                {
                                    int percent = ParsePercentage(currentStr);
                                    if (percent >= 0)
                                    {
                                        reportProgress?.Invoke(percent, currentStr.Trim());
                                    }
                                    else
                                    {
                                        AddLog($"> {currentStr.Trim()}", "#777");
                                    }
                                }
                                currentStr = "";
                            }
                            else
                            {
                                currentStr += c;
                            }
                        }
                    });

                    await p.WaitForExitAsync();

                    if (p.ExitCode == 0)
                        AddLog(string.Format(LocalizationService.Get("logOK"), taskName), "Green");
                    else
                        AddLog(string.Format(LocalizationService.Get("logWarn"), taskName, p.ExitCode), "Orange");
                }
            }
            catch (Exception ex)
            {
                AddLog(string.Format(LocalizationService.Get("logFailed"), taskName, ex.Message), "Red");
            }
        }

        private int ParsePercentage(string text)
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(text, @"(\d+(\.\d+)?)%");
                if (match.Success)
                {
                    if (double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double p))
                        return (int)p;
                }
            }
            catch {}
            return -1;
        }

        private async Task RunSystemScan()
        {
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 5, "Running SFC Scan..."));
            await ExecuteCmdWithProgress("sfc /scannow", "SFC System File Check", (percent, raw) => {
                int overall = 5 + (int)(percent * 0.45);
                Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, overall, $"SFC Scan: {percent}%"));
            });
            
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 50, "Running DISM Repair..."));
            await ExecuteCmdWithProgress("dism /Online /Cleanup-Image /RestoreHealth", "DISM Restore Health", (percent, raw) => {
                int overall = 50 + (int)(percent * 0.50);
                Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, overall, $"DISM Repair: {percent}%"));
            });
        }

        private async Task FixProfileLogin()
        {
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 50, "Cleaning Profile Registry..."));
            await Task.Run(() => {
                ExecuteCmd("powershell -NoProfile -Command \"$base='HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\ProfileList'; Get-ChildItem $base | Where-Object { $_.Name -like '*.bak' } | ForEach-Object { $sid = $_.Name.Replace('.bak', ''); reg copy \\\"$($_.Name)\\\" \\\"$sid\\\" /s /f; reg delete \\\"$($_.Name)\\\" /f }\"", "Registry Profile Fix");
            });
        }

        private async Task FixSMBGuest()
        {
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 50, "Setting SMB Registry..."));
            await Task.Run(() => {
                ExecuteCmd("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LanmanWorkstation\" /v AllowInsecureGuestAuth /t REG_DWORD /d 1 /f", "SMB Allow Insecure Guest (Policy)");
                ExecuteCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\LanmanWorkstation\\Parameters\" /v RequireSecuritySignature /t REG_DWORD /d 0 /f", "SMB Disable Security Signature");
                ExecuteCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\LanmanWorkstation\\Parameters\" /v EnableSecuritySignature /t REG_DWORD /d 0 /f", "SMB Disable Security Signature v2");
                ExecuteCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Lsa\" /v LmCompatibilityLevel /t REG_DWORD /d 1 /f", "Set NTLM Compatibility");
                ExecuteCmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\LanmanWorkstation\\Parameters\" /v AllowPlainTextPassword /t REG_DWORD /d 1 /f", "Allow PlainText Password");
            });
        }

        private async Task FixIconCache()
        {
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 50, "Rebuilding Icon Cache..."));
            await Task.Run(() => {
                ExecuteCmd("taskkill /f /im explorer.exe", "Kill Explorer");
                ExecuteCmd("del /f /q \"%localappdata%\\Microsoft\\Windows\\Explorer\\iconcache*.db\"", "Delete Explorer Icon Cache");
                ExecuteCmd("del /f /q \"%localappdata%\\Microsoft\\Windows\\Explorer\\thumbcache*.db\"", "Delete Explorer Thumb Cache");
                ExecuteCmd("attrib -h -s -r \"%localappdata%\\IconCache.db\"", "Prepare IconCache.db");
                ExecuteCmd("del /f /q \"%localappdata%\\IconCache.db\"", "Delete legacy IconCache.db");
                ExecuteCmd("start explorer.exe", "Restart Explorer");
            });
        }

        private async Task FixUpdate()
        {
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 30, "Stopping Services..."));
            await Task.Run(() => {
                ExecuteCmd("net stop wuauserv", "Stop Windows Update");
                ExecuteCmd("net stop bits", "Stop BITS");
                ExecuteCmd("rd /s /q \"%SystemRoot%\\SoftwareDistribution\"", "Clear Update Cache");
                ExecuteCmd("net start wuauserv", "Start Windows Update");
                ExecuteCmd("net start bits", "Start BITS");
            });
        }

        private async Task FixBitLocker()
        {
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 30, "Scanning for BitLocker..."));
            await Task.Run(() => {
                ExecuteCmd("manage-bde -off C:", "Decrypting Drive C:");
                ExecuteCmd("manage-bde -off D:", "Decrypting Drive D:");
                AddLog("Decryption started for C and D. Use 'manage-bde -status' in CMD to monitor.", "Orange");
            });
        }

        private async Task FixFilePreview()
        {
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 20, "Unblocking common directories..."));
            await Task.Run(() => {
                ExecuteCmd("powershell -Command \"Get-ChildItem -Path '%USERPROFILE%\\Downloads','%USERPROFILE%\\Desktop' -Recurse -File -ErrorAction SilentlyContinue | Unblock-File -ErrorAction SilentlyContinue\"", "Unblock Files");
                ExecuteCmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v ShowPreviewHandlers /t REG_DWORD /d 1 /f", "Enable Preview Handlers");
                ExecuteCmd("taskkill /f /im explorer.exe && start explorer.exe", "Restart Explorer");
            });
        }

        private async Task FixFontCache()
        {
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 50, "Cleaning Font Cache..."));
            await Task.Run(() => {
                ExecuteCmd("net stop FontCache", "Stop FontCache Svc");
                ExecuteCmd("del /f /q \"%WinDir%\\ServiceProfiles\\LocalService\\AppData\\Local\\FontCache\\*\"", "Delete Font Cache Files");
                ExecuteCmd("net start FontCache", "Start FontCache Svc");
            });
        }

        private async Task FixBSODNetio()
        {
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 50, "Resetting Network Stack..."));
            await Task.Run(() => {
                ExecuteCmd("netsh int ip reset", "IP Reset");
                ExecuteCmd("netsh winsock reset", "Winsock Reset");
                ExecuteCmd("netsh advfirewall reset", "Firewall Reset");
            });
        }

        private async void BtnAutoDetect_Click(object sender, RoutedEventArgs e)
        {
            LogMessages.Clear();
            AddLog("--------------------------------------------------", "#333");
            AddLog("Starting Auto Diagnose...", "#0078D7");

            int issues = 0;
            System.Collections.Generic.List<string> issuesToFix = new System.Collections.Generic.List<string>();

            await Task.Run(() =>
            {
                // 1. Spooler
                AddLog("[1/5] " + string.Format(LocalizationService.Get("logCheckService") ?? "Checking Service {0}...", "Print Spooler"), "#555");
                if (!CheckServiceRunning("spooler")) { AddLog("> [LỖI] Spooler is stopped", "Red"); issues++; issuesToFix.Add("Spooler"); } else AddLog("> [OK]", "Green");

                // 2. LanmanWorkstation
                AddLog("[2/5] " + string.Format(LocalizationService.Get("logCheckService") ?? "Checking Service {0}...", "LanmanWorkstation"), "#555");
                if (!CheckServiceRunning("LanmanWorkstation")) { AddLog("> [LỖI] LanmanWorkstation is stopped", "Red"); issues++; issuesToFix.Add("SMB"); } else AddLog("> [OK]", "Green");

                // 3. mscms.dll
                AddLog("[3/5] " + string.Format(LocalizationService.Get("logCheckReg") ?? "Checking Configuration {0}...", "mscms.dll"), "#555");
                string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
                if (!System.IO.File.Exists(System.IO.Path.Combine(sys32, @"spool\drivers\x64\3\mscms.dll")))
                { AddLog("> [LỖI] mscms.dll missing", "Red"); issues++; issuesToFix.Add("mscms.dll"); } else AddLog("> [OK]", "Green");

                // 4. MpsSvc
                AddLog("[4/5] " + string.Format(LocalizationService.Get("logCheckService") ?? "Checking Service {0}...", "MpsSvc (Firewall)"), "#555");
                if (!CheckServiceRunning("MpsSvc")) { AddLog("> [CẢNH BÁO] Firewall is stopped", "Orange"); issues++; issuesToFix.Add("Firewall"); } else AddLog("> [OK]", "Green");

                // 5. fdPHost
                AddLog("[5/5] " + string.Format(LocalizationService.Get("logCheckService") ?? "Checking Service {0}...", "fdPHost"), "#555");
                if (!CheckServiceRunning("fdPHost")) { AddLog("> [CẢNH BÁO] fdPHost is stopped", "Orange"); issues++; issuesToFix.Add("fdPHost"); } else AddLog("> [OK]", "Green");

            });

            if (issues > 0)
            {
                string msgFound = LocalizationService.Get("msgIssuesFound") ?? "Detected {0} issues. Do you want to automatically fix them?";
                string title = LocalizationService.Get("msgIssuesFoundTitle") ?? "Diagnostic Result";
                var confirmWindow = new ModernConfirmationWindow(string.Format(msgFound, issues), title);
                confirmWindow.Owner = Window.GetWindow(this);
                if (confirmWindow.ShowDialog() == true)
                {
                    LoadingOverlay.Visibility = Visibility.Visible;
                    LoadingOverlay.UpdateProgress(0, 50, LocalizationService.Get("msgFixingDetected") ?? "Fixing issues...");
                    
                    await Task.Run(() => {
                        if (issuesToFix.Contains("Spooler")) { ExecuteCmd("sc config spooler start= auto & sc start spooler", "Restart Spooler"); }
                        if (issuesToFix.Contains("SMB")) { ExecuteCmd("sc config LanmanWorkstation start= auto & sc start LanmanWorkstation", "Restart SMB"); }
                        if (issuesToFix.Contains("mscms.dll")) {
                            string src = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "mscms.dll");
                            string dst = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"spool\drivers\x64\3\mscms.dll");
                            if (System.IO.File.Exists(src)) ExecuteCmd($"copy /y \"{src}\" \"{dst}\"", "Copy mscms.dll");
                        }
                        if (issuesToFix.Contains("Firewall")) { ExecuteCmd("sc config MpsSvc start= auto & sc start MpsSvc", "Restart Firewall"); }
                        if (issuesToFix.Contains("fdPHost")) { ExecuteCmd("sc config fdPHost start= auto & sc start fdPHost", "Restart fdPHost"); }
                    });

                    LoadingOverlay.UpdateProgress(0, 100, "Done");
                    await Task.Delay(500);
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    AddLog(LocalizationService.Get("logRepairedSuccess"), "Green");
                    
                    var popup = new InfoPopupWindow(LocalizationService.Get("popupWinFixSuccess") ?? "Fixed completely!", "Success");
                    popup.Owner = Window.GetWindow(this);
                    popup.ShowDialog();
                }
                else
                {
                    AddLog("Canceled fixing issues.", "Orange");
                }
            }
            else
            {
                AddLog(LocalizationService.Get("logFoundNoIssues") ?? "No issues found!", "Green");
            }
        }

        private async void BtnStartFix_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = ComboErrorSelector.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;
            string tag = selectedItem.Tag.ToString();

            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingOverlay.UpdateProgress(0, 0, "Initializing...");
            LogMessages.Clear();

            AddLog($"Starting Fix for: {selectedItem.Content}", "#0078D7");

            switch (tag)
            {
                case "SFC": await RunSystemScan(); break;
                case "PROFILE": await FixProfileLogin(); break;
                case "SMB": await FixSMBGuest(); break;
                case "ICON_CACHE": await FixIconCache(); break;
                case "UPDATE": await FixUpdate(); break;
                case "BITLOCKER": await FixBitLocker(); break;
                case "PREVIEW": await FixFilePreview(); break;
                case "FONT_CACHE": await FixFontCache(); break;
                case "NETIO": await FixBSODNetio(); break;
                case "NETWORK":
                    await Task.Run(() => {
                        ExecuteCmd("netsh winsock reset", "Winsock Reset");
                        ExecuteCmd("netsh int ip reset", "IP Reset");
                        ExecuteCmd("ipconfig /flushdns", "Flush DNS");
                    });
                    break;
                case "STORE":
                    await Task.Run(() => {
                        ExecuteCmd("wsreset.exe", "WSReset");
                        ExecuteCmd("powershell -ExecutionPolicy Unrestricted -Command \"Get-AppXPackage -AllUsers | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \\\"$($_.InstallLocation)\\AppXManifest.xml\\\"}\"", "Re-register Store Apps");
                    });
                    break;
                case "SEARCH":
                    await Task.Run(() => {
                        ExecuteCmd("taskkill /f /im SearchHost.exe", "Kill SearchHost");
                        ExecuteCmd("powershell -Command \"Get-AppxPackage -AllUsers Microsoft.Windows.Search | Reset-AppxPackage\"", "Reset Search App");
                        ExecuteCmd("net stop WSearch", "Stop Search Svc");
                        ExecuteCmd("net start WSearch", "Start Search Svc");
                    });
                    break;
                default:
                    AddLog("This feature is under maintenance.", "Orange");
                    break;
            }

            LoadingOverlay.UpdateProgress(0, 100, "Done!");
            await Task.Delay(500);
            LoadingOverlay.Visibility = Visibility.Collapsed;
            AddLog("--------------------------------------------------", "#333");
            AddLog(LocalizationService.Get("logFixCompleted"), "Green");
            
            var popup = new InfoPopupWindow(LocalizationService.Get("popupWinFixSuccess") ?? "Windows repair completed successfully!", "Success");
            popup.Owner = Window.GetWindow(this);
            popup.ShowDialog();
        }
    }
}
