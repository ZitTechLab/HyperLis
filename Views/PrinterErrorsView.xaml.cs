using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HyperLisUI.Helpers;

namespace HyperLisUI.Views
{
    public class LogMessage
    {
        public string Text { get; set; }
        public string Color { get; set; }
    }

    public partial class PrinterErrorsView : UserControl
    {
        public ObservableCollection<LogMessage> LogMessages { get; } = new ObservableCollection<LogMessage>();

        public PrinterErrorsView()
        {
            InitializeComponent();
            this.Loaded += PrinterErrorsView_Loaded;
            LogList.ItemsSource = LogMessages;
            InitializeErrorList();
        }

        private void InitializeErrorList()
        {
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = "0x0000011b (KB5006670)" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = "0x00000709" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = "0x0000007c (RPC Privacy)" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = "0x00000bc4 (RPC TCP)" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = "0x00000040 (Firewall)" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = "0x000006d9 (Firewall Svc)" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = "0x000003e3 (SMB Share)" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = "0x00000012 (Credential)" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = "0x000003eb (Spool Folder)" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = "0x00000771 (Registry)" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = "Policy Connection Prevents" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = "Communication Error" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = "Cannot Connect" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = "All-In-One Fix (Fix All)" });
            ComboErrorSelector.Items.Add(new ComboBoxItem { Content = LocalizationService.Get("optClearPrintQueue") ?? "Clear Print Queue" });
            
            ComboErrorSelector.SelectedIndex = 0; // Select first item by default
        }

        private void PrinterErrorsView_Loaded(object sender, RoutedEventArgs e)
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
            TextTitle.Text = LocalizationService.Get("titlePrinterErrors");
            TextDesc.Text = LocalizationService.Get("descPrinterErrors");
            TextSelectError.Text = LocalizationService.Get("lblSelectErrorSymp");
            BtnStartFix.Content = LocalizationService.Get("btnStartFixPrinter");
            BtnUninstallDrivers.Content = LocalizationService.Get("btnUninstallPrinterDrivers");
            BtnPrintManagement.Content = LocalizationService.Get("btnPrintManagement");
            BtnAddPrinter.Content = LocalizationService.Get("btnAddPrinter");
            TextLogLabel.Text = LocalizationService.Get("lblExecutionLogs");
        }

        private void AddLog(string msg, string color = "#333")
        {
            Dispatcher.Invoke(() => {
                LogMessages.Add(new LogMessage { Text = $"[{DateTime.Now:HH:mm:ss}] {msg}", Color = color });
                LogScrollViewer.ScrollToEnd();
            });
        }

        private const int CMD_TIMEOUT_MS = 120_000; // 120 seconds

        private void ExecuteCmdV2(string command, string taskName)
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
                    // Read stdout/stderr BEFORE WaitForExit to prevent deadlock
                    // when output buffer fills up (4KB default)
                    string stdout = p.StandardOutput.ReadToEnd();
                    string stderr = p.StandardError.ReadToEnd();

                    if (p.WaitForExit(CMD_TIMEOUT_MS))
                    {
                        if (p.ExitCode == 0)
                            AddLog(string.Format(LocalizationService.Get("logOK"), taskName), "Green");
                        else
                        {
                            AddLog(string.Format(LocalizationService.Get("logWarn"), taskName, p.ExitCode), "Orange");
                        }
                    }
                    else
                    {
                        // Timeout - force kill the process
                        try { p.Kill(); } catch { }
                        AddLog(string.Format(LocalizationService.Get("logWarn"), taskName, "TIMEOUT"), "Orange");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog(string.Format(LocalizationService.Get("logFailed"), taskName, ex.Message), "Red");
            }
        }

        /// <summary>
        /// Delete all files in a spool directory using .NET API instead of cmd.exe del.
        /// Handles large file counts without deadlock, with per-file error handling
        /// and optional progress reporting.
        /// </summary>
        private void DeleteSpoolFiles(string folderPath, string taskName, Action<int, int> onProgress = null)
        {
            AddLog(string.Format(LocalizationService.Get("logExecuting"), taskName), "#555");
            try
            {
                string expandedPath = Environment.ExpandEnvironmentVariables(folderPath);
                if (!Directory.Exists(expandedPath))
                {
                    AddLog(string.Format(LocalizationService.Get("logOK"), taskName + " (folder empty/missing)"), "Green");
                    return;
                }

                string[] files = Directory.GetFiles(expandedPath);
                int total = files.Length;
                int deleted = 0;
                int failed = 0;

                if (total == 0)
                {
                    AddLog(string.Format(LocalizationService.Get("logOK"), taskName + " (0 files)"), "Green");
                    return;
                }

                AddLog($"  Found {total} file(s) to delete...", "#555");

                for (int i = 0; i < files.Length; i++)
                {
                    try
                    {
                        File.Delete(files[i]);
                        deleted++;
                    }
                    catch
                    {
                        // Try removing read-only attribute and retry
                        try
                        {
                            File.SetAttributes(files[i], FileAttributes.Normal);
                            File.Delete(files[i]);
                            deleted++;
                        }
                        catch
                        {
                            failed++;
                        }
                    }

                    // Report progress every 50 files or at the end
                    if (onProgress != null && (i % 50 == 0 || i == files.Length - 1))
                    {
                        onProgress(i + 1, total);
                    }
                }

                if (failed == 0)
                    AddLog(string.Format(LocalizationService.Get("logOK"), $"{taskName} ({deleted}/{total})"), "Green");
                else
                    AddLog(string.Format(LocalizationService.Get("logWarn"), $"{taskName} ({deleted}/{total}, {failed} failed)", failed), "Orange");
            }
            catch (Exception ex)
            {
                AddLog(string.Format(LocalizationService.Get("logFailed"), taskName, ex.Message), "Red");
            }
        }

        private void RestartSpooler()
        {
            ExecuteCmdV2("net stop spooler", "Stop Print Spooler");
            ExecuteCmdV2("taskkill /f /im spoolsv.exe", "Kill Spooler Process");
            ExecuteCmdV2("net start spooler", "Start Print Spooler");
        }
        
        private void Fix11B()
        {
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 30, LocalizationService.Get("progressRegistry")));
            ExecuteCmdV2("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Print\" /v RpcAuthnLevelPrivacyEnabled /t REG_DWORD /d 0 /f", "Set RpcAuthnLevelPrivacyEnabled = 0");
            ExecuteCmdV2("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\Printers\\RPC\" /v RpcOverNamedPipes /t REG_DWORD /d 1 /f", "Set RpcOverNamedPipes = 1");
            ExecuteCmdV2("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\Printers\\RPC\" /v RpcOverTcp /t REG_DWORD /d 1 /f", "Set RpcOverTcp = 1");
            
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 70, LocalizationService.Get("progressRestartSpooler")));
            RestartSpooler();
        }

        private string? GetSpooler64Folder()
        {
            string[] candidatePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "Spooler64"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Warehouse", "Fix loi 709 - SGC", "Spooler64"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Warehouse\Fix loi 709 - SGC\Spooler64")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Warehouse\Fix loi 709 - SGC\Spooler64")),
                @"E:\GGAI\Design\Warehouse\Fix loi 709 - SGC\Spooler64"
            };

            foreach (var path in candidatePaths)
            {
                if (Directory.Exists(path) &&
                    File.Exists(Path.Combine(path, "win32spl.dll")) &&
                    File.Exists(Path.Combine(path, "localspl.dll")) &&
                    File.Exists(Path.Combine(path, "spoolsv.exe")))
                {
                    return path;
                }
            }
            return null;
        }

        private void Fix709()
        {
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 15, LocalizationService.Get("progressStopSpooler")));
            ExecuteCmdV2("net stop spooler", "Stop Spooler Service");
            ExecuteCmdV2("taskkill /f /im spoolsv.exe", "Kill Spooler Process");

            string? spooler64Path = GetSpooler64Folder();
            string sys32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32");

            string[] spoolerFiles = new[] { "win32spl.dll", "localspl.dll", "spoolsv.exe" };

            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 35, LocalizationService.Get("progressGrantPermissions") ?? "Granting Permissions..."));
            foreach (var file in spoolerFiles)
            {
                string targetPath = Path.Combine(sys32, file);
                if (File.Exists(targetPath))
                {
                    ExecuteCmdV2($"takeown /A /F \"{targetPath}\"", $"Takeown {file}");
                    ExecuteCmdV2($"icacls \"{targetPath}\" /grant builtin\\administrators:F", $"Grant Administrators on {file}");
                    ExecuteCmdV2($"icacls \"{targetPath}\" /grant SYSTEM:F", $"Grant SYSTEM on {file}");

                    string oldPath = Path.Combine(sys32, file + ".old");
                    try
                    {
                        if (File.Exists(oldPath))
                        {
                            File.Delete(oldPath);
                        }
                        File.Move(targetPath, oldPath);
                        AddLog($"Renamed {file} to {file}.old", "Green");
                    }
                    catch (Exception ex)
                    {
                        AddLog($"Warning renaming {file}: {ex.Message}", "Orange");
                    }
                }
            }

            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 60, LocalizationService.Get("progressReplaceSpoolerFiles") ?? "Replacing Spooler Files..."));
            if (!string.IsNullOrEmpty(spooler64Path) && Directory.Exists(spooler64Path))
            {
                foreach (var file in spoolerFiles)
                {
                    string srcFile = Path.Combine(spooler64Path, file);
                    string destFile = Path.Combine(sys32, file);
                    if (File.Exists(srcFile))
                    {
                        try
                        {
                            File.Copy(srcFile, destFile, true);
                            AddLog($"Copied {file} -> System32", "Green");
                        }
                        catch (Exception ex)
                        {
                            AddLog($"Error copying {file}: {ex.Message}. Trying cmd copy...", "Orange");
                            ExecuteCmdV2($"copy /y \"{srcFile}\" \"{destFile}\"", $"Copy {file} via CMD");
                        }
                    }
                    else
                    {
                        AddLog($"Source file not found: {srcFile}", "Orange");
                    }
                }
            }
            else
            {
                AddLog("Spooler64 folder not found. Skipping DLL replacement.", "Orange");
            }

            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 80, LocalizationService.Get("progressRegistry")));
            ExecuteCmdV2("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Print\" /v RpcAuthnLevelPrivacyEnabled /t REG_DWORD /d 0 /f", "Set RpcAuthnLevelPrivacyEnabled = 0");
            ExecuteCmdV2("reg add \"HKCU\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Windows\" /v LegacyDefaultPrinterMode /t REG_DWORD /d 1 /f", "Set LegacyDefaultPrinterMode = 1");

            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 95, LocalizationService.Get("progressStartSpooler")));
            ExecuteCmdV2("net start spooler", "Start Spooler Service");
        }

        private void FixGenericRPC()
        {
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 30, LocalizationService.Get("progressRegistry")));
            ExecuteCmdV2("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Print\" /v RpcAuthnLevelPrivacyEnabled /t REG_DWORD /d 0 /f", "Set RpcAuthnLevelPrivacyEnabled = 0");
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 70, LocalizationService.Get("progressRestartSpooler")));
            RestartSpooler();
        }

        private void ClearPrintQueue()
        {
            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 20, LocalizationService.Get("progressStopSpooler")));
            ExecuteCmdV2("net stop spooler", "Stop Print Spooler");
            ExecuteCmdV2("taskkill /f /im spoolsv.exe", "Kill Spooler Process");

            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 30, LocalizationService.Get("progressUninstallFiles")));
            DeleteSpoolFiles(
                @"%SystemRoot%\System32\spool\PRINTERS",
                "Delete Print Queue",
                (current, total) =>
                {
                    // Map progress from 30% to 85%
                    int pct = 30 + (int)(55.0 * current / total);
                    Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, pct,
                        $"{LocalizationService.Get("progressUninstallFiles")} ({current}/{total})"));
                }
            );

            Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 90, LocalizationService.Get("progressStartSpooler")));
            ExecuteCmdV2("net start spooler", "Start Print Spooler");
        }

        private void BtnPrintManagement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("printmanagement.msc")
                {
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
                AddLog(LocalizationService.Get("logOK"), "Green"); // Reusing log OK without specific text or we can just say opened
            }
            catch (Exception ex)
            {
                AddLog(string.Format(LocalizationService.Get("logFailed"), "printmanagement.msc", ex.Message), "Red");
            }
        }

        private void BtnAddPrinter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", "shell:::{A8A91A66-3A7D-4424-8D24-04E180695C7A}");
                AddLog(string.Format(LocalizationService.Get("logExecuting"), LocalizationService.Get("btnAddPrinter")), "#555");
            }
            catch (Exception ex)
            {
                AddLog(string.Format(LocalizationService.Get("logFailed"), "Explorer", ex.Message), "Red");
            }
        }

        private async void BtnUninstallDrivers_Click(object sender, RoutedEventArgs e)
        {
            var btnStyle = this.FindResource("ActionButtonStyle") as Style;
            var confirmWindow = new ModernConfirmationWindow(
                LocalizationService.Get("confirmUninstallPrinterMessage"),
                LocalizationService.Get("confirmUninstallPrinterTitle")
            );
            confirmWindow.Owner = Window.GetWindow(this);
            confirmWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (confirmWindow.ShowDialog() != true)
                return;

            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingOverlay.UpdateProgress(0, 10, LocalizationService.Get("progressUninstallInit"));
            LogMessages.Clear();

            AddLog(LocalizationService.Get("logStartUninstall"), "#0078D7");

            await Task.Run(() => 
            {
                Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 20, LocalizationService.Get("progressStopSpooler")));
                ExecuteCmdV2("net stop spooler", "Stop Print Spooler");
                ExecuteCmdV2("taskkill /f /im spoolsv.exe", "Kill Spooler Process");

                Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 30, LocalizationService.Get("progressUninstallFiles")));
                DeleteSpoolFiles(@"%SystemRoot%\System32\spool\PRINTERS", "Delete Print Queue");
                DeleteSpoolFiles(@"%SystemRoot%\System32\spool\drivers\x64\3", "Delete x64 Print Drivers Cache");
                DeleteSpoolFiles(@"%SystemRoot%\System32\spool\drivers\w32x86\3", "Delete x86 Print Drivers Cache");

                Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 50, LocalizationService.Get("progressUninstallDrivers")));
                try
                {
                    AddLog(string.Format(LocalizationService.Get("logExecuting"), "Enumerate PnPUtil Drivers"), "#555");
                    var processInfo = new System.Diagnostics.ProcessStartInfo("pnputil", "/enum-drivers")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                    using (var p = System.Diagnostics.Process.Start(processInfo))
                    {
                        string output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit();

                        string currentPublishedName = "";
                        var lines = output.Split('\n');
                        int countRemoved = 0;
                        foreach(var line in lines)
                        {
                            if (line.Contains("Published Name:"))
                            {
                                currentPublishedName = line.Split(':')[1].Trim();
                            }
                            if (line.Contains("Class Name:") && line.Contains("Printer"))
                            {
                                if (!string.IsNullOrEmpty(currentPublishedName))
                                {
                                    AddLog($"Found Printer Driver: {currentPublishedName}. Removing...", "#555");
                                    ExecuteCmdV2($"pnputil /delete-driver {currentPublishedName} /uninstall /force", $"Delete Driver {currentPublishedName}");
                                    countRemoved++;
                                }
                            }
                        }
                        AddLog(string.Format(LocalizationService.Get("logOK"), $"Removed {countRemoved} driver(s)"), "Green");
                    }
                }
                catch (Exception ex)
                {
                    AddLog(string.Format(LocalizationService.Get("logFailed"), "PnPUtil Enumerate", ex.Message), "Red");
                }

                Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 70, LocalizationService.Get("progressRegistry")));
                ExecuteCmdV2("reg delete \"HKCU\\Printers\\Connections\" /f", "Delete User Printer Connections");
                ExecuteCmdV2("reg delete \"HKCU\\Software\\Microsoft\\Windows NT\\CurrentVersion\\PrinterPorts\" /f", "Delete User Printer Ports");
                ExecuteCmdV2("reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Print\\Printers\" /f", "Delete Local Printers");
                ExecuteCmdV2("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Print\\Printers\" /f", "Recreate Empty Printers Key");
                
                ExecuteCmdV2("reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Print\\Monitors\\Standard TCP/IP Port\\Ports\" /f", "Delete TCP/IP Ports");
                ExecuteCmdV2("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Print\\Monitors\\Standard TCP/IP Port\\Ports\" /f", "Recreate Empty TCP/IP Ports Key");

                Dispatcher.Invoke(() => LoadingOverlay.UpdateProgress(0, 90, LocalizationService.Get("progressStartSpooler")));
                ExecuteCmdV2("net start spooler", "Start Print Spooler");
            });

            LoadingOverlay.UpdateProgress(0, 100, LocalizationService.Get("progressDone"));
            await Task.Delay(500);
            
            LoadingOverlay.Visibility = Visibility.Collapsed;

            AddLog("--------------------------------------------------", "#333");
            AddLog(LocalizationService.Get("logFixCompleted"), "Green");
            
            var popup = new InfoPopupWindow(LocalizationService.Get("popupUninstallSuccess"), "Success");
            popup.Owner = Window.GetWindow(this);
            popup.ShowDialog();
        }

        private async void BtnStartFix_Click(object sender, RoutedEventArgs e)
        {
            string selectedError = (ComboErrorSelector.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Unknown";

            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingOverlay.UpdateProgress(0, 10, LocalizationService.Get("progressInit"));
            LogMessages.Clear();

            AddLog(string.Format(LocalizationService.Get("logStartFix"), selectedError), "#0078D7");

            await Task.Run(() => 
            {
                if (selectedError.Contains("0x0000011b"))
                {
                    Fix11B();
                }
                else if (selectedError.Contains("0x00000709"))
                {
                    Fix709();
                }
                else if (selectedError.Contains(LocalizationService.Get("optClearPrintQueue") ?? "Clear Print Queue") || selectedError.Contains("Clear Print Queue") || selectedError.Contains("Xóa hàng đợi in"))
                {
                    ClearPrintQueue();
                }
                else
                {
                    AddLog(LocalizationService.Get("logNoMapping"), "#FFA500");
                    FixGenericRPC();
                }
            });

            LoadingOverlay.UpdateProgress(0, 100, LocalizationService.Get("progressDone"));
            await Task.Delay(500);
            
            LoadingOverlay.Visibility = Visibility.Collapsed;

            AddLog("--------------------------------------------------", "#333");
            AddLog(LocalizationService.Get("logFixCompleted"), "Green");
            
            var popup = new InfoPopupWindow(LocalizationService.Get("popupFixSuccess"), "Success");
            popup.Owner = Window.GetWindow(this);
            popup.ShowDialog();
        }
    }
}
