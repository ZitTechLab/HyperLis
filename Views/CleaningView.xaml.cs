using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HyperLisUI.Views
{
    public partial class CleaningView : UserControl
    {
        public CleaningView()
        {
            InitializeComponent();
            Helpers.LocalizationService.LanguageChanged += ApplyTranslations;
            Loaded += (s, e) => ApplyTranslations();
        }

        private void ApplyTranslations()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ApplyTranslations);
                return;
            }

            // Headers
            lblCleanWindowsRam.Text = Helpers.LocalizationService.Get("lblCleanWindowsRam");
            lblCleanBrowsers.Text = Helpers.LocalizationService.Get("lblCleanBrowsers");
            lblCleanChooseBrowser.Text = Helpers.LocalizationService.Get("lblCleanChooseBrowser");
            lblCleanBrowserDesc.Text = Helpers.LocalizationService.Get("lblCleanBrowserDesc");

            // Checkboxes - Column 1
            ChkTempFiles.Content = Helpers.LocalizationService.Get("ChkTempFiles");
            ChkBSOD.Content = Helpers.LocalizationService.Get("ChkBSOD");
            ChkErrorReports.Content = Helpers.LocalizationService.Get("ChkErrorReports");
            ChkRecycleBin.Content = Helpers.LocalizationService.Get("ChkRecycleBin");

            // Checkboxes - Column 2
            ChkUpdateCache.Content = Helpers.LocalizationService.Get("ChkUpdateCache");
            ChkSystemRestore.Content = Helpers.LocalizationService.Get("ChkSystemRestore");
            ChkRamCache.Content = Helpers.LocalizationService.Get("ChkRamCache");
            ChkRamUsage.Content = Helpers.LocalizationService.Get("ChkRamUsage");

            // Browser Checkboxes
            ChkBrowserCache.Content = Helpers.LocalizationService.Get("ChkBrowserCache");
            ChkBrowserCookies.Content = Helpers.LocalizationService.Get("ChkBrowserCookies");
            ChkBrowserHistory.Content = Helpers.LocalizationService.Get("ChkBrowserHistory");
            ChkBrowserSession.Content = Helpers.LocalizationService.Get("ChkBrowserSession");
            ChkBrowserPasswords.Content = Helpers.LocalizationService.Get("ChkBrowserPasswords");

            // Buttons
            btnCleanHyperLis.Content = Helpers.LocalizationService.Get("btnCleanHyperLis");
            btnCleanDiskCleanup.Content = Helpers.LocalizationService.Get("btnCleanDiskCleanup");
            btnCleanBoth.Content = Helpers.LocalizationService.Get("btnCleanBoth");
            btnOptimizeDrives.Content = Helpers.LocalizationService.Get("btnOptimizeDrives");
        }

        private async void CleanByHyperLis_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                await ExecuteHyperLisCleaning();
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private async void UseBothMethods_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                // 1. Run HyperLis Cleaning logic (Wait for completion)
                await ExecuteHyperLisCleaning();

                // 2. Run Disk Cleanup
                CleanByDiskCleanup_Click(sender, e);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private async Task ExecuteHyperLisCleaning()
        {
            // Get MainWindow reference for loading overlay
            var mainWindow = Window.GetWindow(this) as MainWindow;

             // 1. Get Options
            bool temp = ChkTempFiles.IsChecked == true;
            bool bsod = ChkBSOD.IsChecked == true;
            bool error = ChkErrorReports.IsChecked == true;
            bool bin = ChkRecycleBin.IsChecked == true;

            // 2.2 Get Browser Options
            var selectedBrowserItem = CmbBrowser.SelectedItem as ComboBoxItem;
            string browserName = selectedBrowserItem?.Content?.ToString() ?? "";

            bool bCache = ChkBrowserCache.IsChecked == true;
            bool bCookies = ChkBrowserCookies.IsChecked == true;
            bool bHistory = ChkBrowserHistory.IsChecked == true;
            bool bSession = ChkBrowserSession.IsChecked == true;
            bool bPasswords = ChkBrowserPasswords.IsChecked == true;

            // === SHOW LOADING: Scanning with step-based progress ===
            var scanWatch = System.Diagnostics.Stopwatch.StartNew();

            // Build list of scan steps based on user selections
            bool hasBrowser = (bCache || bCookies || bHistory || bSession || bPasswords) &&
                              (browserName.Contains("Google Chrome") || browserName.Contains("Microsoft Edge") ||
                               browserName.Contains("Brave") || browserName.Contains("Firefox"));
            var scanSteps = new List<(string label, Action action)>();

            if (temp)
                scanSteps.Add((Helpers.LocalizationService.Get("ChkTempFiles"), () => Helpers.CleaningHelper.PreviewTemp()));
            if (bsod)
                scanSteps.Add(("BSOD Minidumps", () => Helpers.CleaningHelper.PreviewMinidumps()));
            if (error)
                scanSteps.Add((Helpers.LocalizationService.Get("ChkErrorReports"), () => Helpers.CleaningHelper.PreviewErrorReports()));
            if (hasBrowser)
            {
                scanSteps.Add((Helpers.LocalizationService.Get("lblCleanBrowsers"), () =>
                {
                    if (browserName.Contains("Google Chrome"))
                        Helpers.CleaningHelper.PreviewChromeClean(bCache, bCookies, bHistory, bSession, bPasswords);
                    else if (browserName.Contains("Microsoft Edge"))
                        Helpers.CleaningHelper.PreviewEdgeClean(bCache, bCookies, bHistory, bSession);
                    else if (browserName.Contains("Brave"))
                        Helpers.CleaningHelper.PreviewBraveClean(bCache, bCookies, bHistory, bSession, bPasswords);
                    else if (browserName.Contains("Firefox"))
                        Helpers.CleaningHelper.PreviewFireFoxClean(bCache, bCookies, bHistory);
                }));
            }

            if (scanSteps.Count > 0)
            {
                Helpers.CleaningHelper.ResetPreview();
                mainWindow?.ShowLoading("Scanning...", 0, 0);

                await Task.Run(() =>
                {
                    for (int i = 0; i < scanSteps.Count; i++)
                    {
                        int pct = (int)((double)i / scanSteps.Count * 100);
                        Dispatcher.Invoke(() => mainWindow?.UpdateLoadingProgress(0, pct, $"Scanning: {scanSteps[i].label}..."));

                        scanSteps[i].action();
                    }
                });

                Dispatcher.Invoke(() => mainWindow?.UpdateLoadingProgress(0, 100, "Scan complete"));
            }
            else
            {
                // No scan steps but might have recycle bin / RAM options
                Helpers.CleaningHelper.ResetPreview();
            }

            // Ensure minimum 800ms display so user sees the scanning progress
            scanWatch.Stop();
            int minDisplayMs = 800;
            int remaining = minDisplayMs - (int)scanWatch.ElapsedMilliseconds;
            if (remaining > 0) await Task.Delay(remaining);

            // === HIDE LOADING after scan (before confirmation dialog) ===
            mainWindow?.HideLoading();

            var previewItems = Helpers.CleaningHelper.GetDistinctPreviewItems();
            int count = previewItems.Count;

            bool isRamSelected = ChkRamCache.IsChecked == true || ChkRamUsage.IsChecked == true;
            bool isUpdateCache = ChkUpdateCache.IsChecked == true;
            bool isSystemRestore = ChkSystemRestore.IsChecked == true;

            if (count == 0 && !bin && !isRamSelected && !isUpdateCache && !isSystemRestore)
            {
                var noFilesWindow = new CleaningCompletedWindow(Helpers.LocalizationService.Get("msgNoFilesToClean"));
                noFilesWindow.Owner = Window.GetWindow(this);
                noFilesWindow.ShowDialog();
                return;
            }

            bool proceed = true;
            if (count > 0)
            {
                var confirmationWindow = new CleaningConfirmationWindow(previewItems);
                confirmationWindow.Owner = Window.GetWindow(this);

                if (confirmationWindow.ShowDialog() != true)
                {
                    proceed = false;
                }
            }

            if (proceed)
            {
                // === SHOW LOADING: Cleaning ===
                mainWindow?.ShowLoading("Cleaning files...", 0, 0);

                Helpers.CleanupResult? cleanResult = null;
                bool wasCancelled = false;

                try
                {
                    // Browser data often stays locked while browser process is running.
                    // Only skip BROWSER cleaning if browser isn't closed — still clean temp/recycle/RAM/etc.
                    bool browserReady = true;
                    if (count > 0 && (bCache || bCookies || bHistory || bSession || bPasswords))
                    {
                        browserReady = await EnsureSelectedBrowserClosedAsync(browserName);
                    }

                    // === Phase 1: Delete Items (0% → 50%) ===
                    if (count > 0)
                    {
                        if (browserReady)
                        {
                            // Clean everything (temp + browser) — pass isPreBuilt=true to skip redundant dedup
                            cleanResult = await Helpers.CleaningHelper.CleanAsync(previewItems, (processed, total, name) =>
                            {
                                int pct = (int)((double)processed / total * 50); // Map to 0-50%
                                Dispatcher.BeginInvoke(() => mainWindow?.UpdateLoadingProgress(0, pct, $"Deleting ({processed}/{total}): {name}"));
                            }, isPreBuilt: true);
                        }
                        else
                        {
                            // Browser not closed — clean only non-browser items (temp, minidumps, WER, etc.)
                            var nonBrowserItems = previewItems.Where(p =>
                                !p.Contains("\\Google\\Chrome\\", StringComparison.OrdinalIgnoreCase) &&
                                !p.Contains("\\Microsoft\\Edge\\", StringComparison.OrdinalIgnoreCase) &&
                                !p.Contains("\\BraveSoftware\\", StringComparison.OrdinalIgnoreCase) &&
                                !p.Contains("\\Mozilla\\Firefox\\", StringComparison.OrdinalIgnoreCase)
                            ).ToList();

                            if (nonBrowserItems.Count > 0)
                            {
                                cleanResult = await Helpers.CleaningHelper.CleanAsync(nonBrowserItems, (processed, total, name) =>
                                {
                                    int pct = (int)((double)processed / total * 50);
                                    Dispatcher.BeginInvoke(() => mainWindow?.UpdateLoadingProgress(0, pct, $"Deleting ({processed}/{total}): {name}"));
                                });
                            }
                        }
                    }

                    // === Phase 2: Deep Temp Clean (50% → 60%) ===
                    if (temp)
                    {
                        Dispatcher.BeginInvoke(() => mainWindow?.UpdateLoadingProgress(0, 50, "Deep cleaning temp folders..."));
                        await Task.Run(() => Helpers.CleaningHelper.ResetTempFolders());
                        Dispatcher.BeginInvoke(() => mainWindow?.UpdateLoadingProgress(0, 60, "Deep temp clean complete"));
                    }

                    // === Phase 3: Update Cache (60% → 72%) with sub-phase progress ===
                    if (isUpdateCache)
                    {
                        await CleanUpdateCache(mainWindow);
                    }

                    // === Phase 4: System Restore (72% → 80%) ===
                    if (isSystemRestore)
                    {
                        Dispatcher.BeginInvoke(() => mainWindow?.UpdateLoadingProgress(0, 72, "Cleaning system restore points..."));
                        await CleanSystemRestore();
                        Dispatcher.BeginInvoke(() => mainWindow?.UpdateLoadingProgress(0, 80, "System restore points cleaned"));
                    }

                    // === Phase 5: Recycle Bin (80% → 88%) — now runs on background thread ===
                    if (bin)
                    {
                        Dispatcher.BeginInvoke(() => mainWindow?.UpdateLoadingProgress(0, 80, "Emptying recycle bin (may take a few minutes)..."));
                        await Task.Run(() => Helpers.CleaningHelper.EmptyRecycleBin());
                        Dispatcher.BeginInvoke(() => mainWindow?.UpdateLoadingProgress(0, 88, "Recycle bin emptied"));
                    }

                    // === Phase 6: RAM Usage (88% → 94%) ===
                    bool ranUsage = false;
                    if (ChkRamUsage.IsChecked == true)
                    {
                        Dispatcher.BeginInvoke(() => mainWindow?.UpdateLoadingProgress(0, 88, "Cleaning RAM usage..."));
                        await CleanRAMUsage();
                        ranUsage = true;
                        Dispatcher.BeginInvoke(() => mainWindow?.UpdateLoadingProgress(0, 94, "RAM usage cleaned"));
                    }

                    // === Phase 7: RAM Cache (94% → 100%) ===
                    if (ChkRamCache.IsChecked == true)
                    {
                        Dispatcher.BeginInvoke(() => mainWindow?.UpdateLoadingProgress(0, 94, "Cleaning RAM cache..."));
                        if (ranUsage) await Task.Delay(1000);
                        await CleanRAMCache();
                    }

                    // === Done ===
                    Dispatcher.Invoke(() => mainWindow?.UpdateLoadingProgress(0, 100, "Cleaning completed!"));
                    await Task.Delay(300); // Brief pause so user sees 100%
                }
                catch (OperationCanceledException)
                {
                    // User cancelled via loading overlay — stop gracefully
                    wasCancelled = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Cleaning error: {ex.Message}");
                }
                finally
                {
                    mainWindow?.HideLoading();
                }

                if (wasCancelled) return;

                // === Build comprehensive summary message ===
                var summary = new System.Text.StringBuilder();

                // File deletion results
                if (cleanResult != null && cleanResult.ProcessedItems > 0)
                {
                    if (cleanResult.HasIssues)
                        summary.AppendLine(string.Format(Helpers.LocalizationService.Get("msgCleaningWithIssues"), cleanResult.DeletedTotal, cleanResult.ProcessedItems, cleanResult.TotalFailures));
                    else
                        summary.AppendLine(string.Format(Helpers.LocalizationService.Get("msgCleaningSuccess"), cleanResult.DeletedTotal));
                }

                // Deep temp clean
                if (temp)
                    summary.AppendLine(Helpers.LocalizationService.Get("msgCleaningDeepTemp"));

                // Update Cache
                if (isUpdateCache)
                    summary.AppendLine(Helpers.LocalizationService.Get("msgCleaningUpdateCache"));

                // System Restore
                if (isSystemRestore)
                    summary.AppendLine(Helpers.LocalizationService.Get("msgCleaningSystemRestore"));

                // Recycle Bin
                if (bin)
                    summary.AppendLine(Helpers.LocalizationService.Get("msgCleaningRecycleBin"));

                // RAM
                if (ChkRamUsage.IsChecked == true)
                    summary.AppendLine(Helpers.LocalizationService.Get("msgCleaningRamUsage"));
                if (ChkRamCache.IsChecked == true)
                    summary.AppendLine(Helpers.LocalizationService.Get("msgCleaningRamCache"));

                string completionMessage = summary.Length > 0
                    ? summary.ToString().TrimEnd()
                    : Helpers.LocalizationService.Get("msgCleaningCompletedDefault");

                var completedWindow = new CleaningCompletedWindow(completionMessage, "msgCleaningSummaryTitle");
                completedWindow.Owner = Window.GetWindow(this);
                completedWindow.ShowDialog();
            }
        }

        private async Task CleanRAMCache()
        {
             await Task.Run(() => 
             {
                 try
                 {
                     // Check/Set EulaAccepted (and remove legacy AcceptEula)
                     string regKey = @"HKEY_CURRENT_USER\Software\Sysinternals\RAMMap";
                     
                     // Remove legacy key if exists
                     try {
                        using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Sysinternals\RAMMap", true))
                        {
                            if (key != null)
                            {
                                if (key.GetValue("AcceptEula") != null)
                                    key.DeleteValue("AcceptEula");
                            }
                        }
                     } catch { }

                     var val = Microsoft.Win32.Registry.GetValue(regKey, "EulaAccepted", 0);
                     int eula = (val is int i) ? i : 0;
                     
                     if (eula != 1)
                     {
                         Microsoft.Win32.Registry.SetValue(regKey, "EulaAccepted", 1, Microsoft.Win32.RegistryValueKind.DWord);
                     }

                     // Run RAMMap64.exe -Et
                     string tool = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Tools", "RAMMap64.exe");
                     if (System.IO.File.Exists(tool))
                     {
                         var psi = new System.Diagnostics.ProcessStartInfo
                         {
                             FileName = tool,
                             Arguments = "-Et", // Empty Standby List
                             UseShellExecute = true,
                             Verb = "runas",
                             WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                         };
                         using (var p = System.Diagnostics.Process.Start(psi))
                         {
                             if (p != null)
                             {
                                 if (!p.WaitForExit(5000)) // 5 second timeout
                                 {
                                     try { p.Kill(); } catch { }
                                 }
                             }
                         }
                     }
                 }
                 catch { }
             });
        }

        private async Task CleanRAMUsage()
        {
             await Task.Run(() => 
             {
                 try
                 {
                      // Check/Set EulaAccepted (and remove legacy AcceptEula)
                     string regKey = @"HKEY_CURRENT_USER\Software\Sysinternals\RAMMap";
                     
                     // Remove legacy key if exists
                     try {
                        using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Sysinternals\RAMMap", true))
                        {
                            if (key != null)
                            {
                                var valCheck = key.GetValue("AcceptEula");
                                if (valCheck != null)
                                    key.DeleteValue("AcceptEula");
                            }
                        }
                     } catch { }

                     var val = Microsoft.Win32.Registry.GetValue(regKey, "EulaAccepted", 0);
                     int eula = (val is int i) ? i : 0;
                     
                     if (eula != 1)
                     {
                         Microsoft.Win32.Registry.SetValue(regKey, "EulaAccepted", 1, Microsoft.Win32.RegistryValueKind.DWord);
                     }

                     // Run RAMMap64.exe -Ew
                     string tool = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Tools", "RAMMap64.exe");
                     if (System.IO.File.Exists(tool))
                     {
                         var psi = new System.Diagnostics.ProcessStartInfo
                         {
                             FileName = tool,
                             Arguments = "-Ew", // Empty Working Sets
                             UseShellExecute = true,
                             Verb = "runas",
                             WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                         };
                         using (var p = System.Diagnostics.Process.Start(psi))
                         {
                             if (p != null)
                             {
                                 if (!p.WaitForExit(5000)) // 5 second timeout
                                 {
                                     try { p.Kill(); } catch { }
                                 }
                             }
                         }
                     }
                 }
                 catch { }
             });
        }

        private async Task CleanUpdateCache(MainWindow? mainWindow)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Sub-phase 1: Stop Services (60% → 63%)
                    Dispatcher.BeginInvoke(() => mainWindow?.UpdateLoadingProgress(0, 60, "Stopping Windows Update services..."));
                    RunCommand("net", "stop wuauserv");
                    RunCommand("net", "stop bits");
                    RunCommand("net", "stop cryptsvc");

                    // Sub-phase 2: Delete SoftwareDistribution\Download (63% → 68%)
                    Dispatcher.BeginInvoke(() => mainWindow?.UpdateLoadingProgress(0, 63, "Deleting SoftwareDistribution\\Download..."));
                    string downloadPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download");
                    if (System.IO.Directory.Exists(downloadPath))
                    {
                        try 
                        {
                            System.IO.Directory.Delete(downloadPath, true);
                            System.IO.Directory.CreateDirectory(downloadPath); // Recreate empty folder
                        } 
                        catch { }
                    }

                    // Sub-phase 3: Start Services (68% → 72%)
                    Dispatcher.BeginInvoke(() => mainWindow?.UpdateLoadingProgress(0, 68, "Starting Windows Update services..."));
                    RunCommand("net", "start wuauserv");
                    RunCommand("net", "start bits");
                    RunCommand("net", "start cryptsvc");

                    Dispatcher.BeginInvoke(() => mainWindow?.UpdateLoadingProgress(0, 72, "Update cache cleaned"));
                }
                catch { }
            });
        }

        private async Task CleanSystemRestore()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Delete all shadow copies quietly
                    RunCommand("vssadmin", "delete shadows /all /quiet");
                }
                catch { }
            });
        }

        private async Task<bool> EnsureSelectedBrowserClosedAsync(string browserName)
        {
            string[] processNames;
            if (browserName.Contains("Google Chrome")) processNames = new[] { "chrome" };
            else if (browserName.Contains("Microsoft Edge")) processNames = new[] { "msedge" };
            else if (browserName.Contains("Brave")) processNames = new[] { "brave" };
            else if (browserName.Contains("Firefox")) processNames = new[] { "firefox" };
            else return true;

            var running = processNames
                .SelectMany(Process.GetProcessesByName)
                .Where(p => !p.HasExited)
                .ToList();

            if (running.Count == 0) return true;

            var askClose = MessageBox.Show(
                string.Format(Helpers.LocalizationService.Get("msgBrowserRunning"), browserName),
                Helpers.LocalizationService.Get("msgBrowserRunningTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (askClose != MessageBoxResult.Yes)
            {
                return false;
            }

            await Task.Run(() =>
            {
                foreach (var process in running)
                {
                    try
                    {
                        process.CloseMainWindow();
                        if (!process.WaitForExit(2500))
                        {
                            process.Kill(true);
                            process.WaitForExit(2500);
                        }
                    }
                    catch
                    {
                        // Keep best-effort behavior. Cleanup result will report locked leftovers if any.
                    }
                }
            });

            return true;
        }
        private void RunCommand(string fileName, string arguments, int timeoutMs = 30000)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    Verb = "runas"
                };
                var p = System.Diagnostics.Process.Start(psi);
                if (p != null)
                {
                    if (!p.WaitForExit(timeoutMs))
                    {
                        try { p.Kill(); } catch { }
                        System.Diagnostics.Debug.WriteLine($"RunCommand timeout ({timeoutMs}ms): {fileName} {arguments}");
                    }
                }
            }
            catch { }
        }


        private void CleanByDiskCleanup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("cleanmgr", "/verylowdisk");
            }
            catch (Exception ex) 
            {
               // Silent fail or log if needed
            }
        }
        private void OptimizeDrives_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("dfrgui");
            }
            catch (Exception ex)
            {
                // Silent fail or log if needed
            }
        }
    }
}




