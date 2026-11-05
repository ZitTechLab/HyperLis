using System.Windows.Controls;

namespace HyperLisUI.Views
{
    public partial class USBBootView : UserControl
    {
        public USBBootView()
        {
            InitializeComponent();
            this.Loaded += USBBootView_Loaded;
            
            Helpers.LocalizationService.LanguageChanged += ApplyTranslations;
            this.Loaded += (s, e) => ApplyTranslations();
        }

        private void ApplyTranslations()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ApplyTranslations);
                return;
            }

            lblUsbSelectDrive.Text = Helpers.LocalizationService.Get("lblUsbSelectDrive");
            lblUsbRescanBtn.Text = Helpers.LocalizationService.Get("lblUsbRescanBtn") ?? "Scan USB/Drive Again";
            lblUsbSelectWinVer.Text = Helpers.LocalizationService.Get("lblUsbSelectWinVer");
            lblUsbIsoDescription.Text = Helpers.LocalizationService.Get("lblUsbIsoDescription") ?? "Description: Built-in ISO repository...";
            lblUsbSelectIso.Text = Helpers.LocalizationService.Get("lblUsbSelectIso");
            lblUsbGetOtherIso.Text = Helpers.LocalizationService.Get("lblUsbGetOtherIso");
            lblUsbChooseResource.Text = Helpers.LocalizationService.Get("lblUsbChooseResource");
            lblUsbSelectSource.Text = Helpers.LocalizationService.Get("lblUsbSelectSource");
            lblUsbAuxFunctions.Text = Helpers.LocalizationService.Get("lblUsbAuxFunctions") ?? "AUXILIARY FUNCTIONS & MANAGEMENT";

            BtnStart.Content = Helpers.LocalizationService.Get("btnUsbStart");
            BtnRestartBios.Content = Helpers.LocalizationService.Get("btnUsbRestartBios");
            BtnFixWmic.Content = Helpers.LocalizationService.Get("btnUsbFixWmic");
            BtnOpenResource.Content = Helpers.LocalizationService.Get("btnUsbLetsGo");
            SelectISOBrowseBtn.Content = Helpers.LocalizationService.Get("btnUsbBrowse");
            
            BtnDownloadOnly.Content = Helpers.LocalizationService.Get("btnUsbDownloadOnly") ?? "Download ISO only";
            BtnAddIsoToUsb.Content = Helpers.LocalizationService.Get("btnUsbAddIso") ?? "Add ISO to existing USB";
            BtnCreateBootloader.Content = Helpers.LocalizationService.Get("btnUsbCreateBootloader") ?? "Create Bootloader only";

            // Helper to update placeholder if currently showing placeholder
            if (SelectISOTextBox.Text == "Select directory to your iso" || SelectISOTextBox.Text == "Chọn đường dẫn tới file ISO")
            {
                 SelectISOTextBox.Text = Helpers.LocalizationService.Get("msgUsbSelectIsoDir");
            }
        }

        private async void USBBootView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await LoadPhysicalDisks();
            await LoadISOVersions();
            await LoadResources();
        }

        private async void BtnRescanDrives_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await LoadPhysicalDisks();
        }

        private void BtnOpenResource_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var selectedItem = CmbResources.SelectedItem as ComboBoxItem;
                if (selectedItem != null && selectedItem.Tag != null)
                {
                    string url = selectedItem.Tag.ToString();
                    if (!string.IsNullOrEmpty(url))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    ShowMessage(Helpers.LocalizationService.Get("msgUsbChooseResource"), Helpers.LocalizationService.Get("msgInfo"));
                }
            }
            catch (System.Exception ex)
            {
                ShowMessage($"{Helpers.LocalizationService.Get("msgError")}: {ex.Message}", Helpers.LocalizationService.Get("msgError"));
            }
        }

        private async System.Threading.Tasks.Task LoadISOVersions()
        {
            await LoadJsonDataToCombo<IsoInfo>("iso.json", CmbWindowsVersion);
        }

        private async System.Threading.Tasks.Task LoadResources()
        {
            await LoadJsonDataToCombo<ResourceInfo>("resources.json", CmbResources);
        }

        private async System.Threading.Tasks.Task LoadJsonDataToCombo<T>(string fileName, ComboBox comboBox) where T : class
        {
             comboBox.Items.Clear();
             comboBox.Items.Add(new ComboBoxItem { Content = Helpers.LocalizationService.Get("msgUsbLoadingVersions") });
             comboBox.SelectedIndex = 0;

             try
             {
                 string jsonPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Download", fileName);
                 if (!System.IO.File.Exists(jsonPath))
                 {
                     jsonPath = System.IO.Path.GetFullPath(System.IO.Path.Combine("Download", fileName)); 
                 }

                 if (System.IO.File.Exists(jsonPath))
                 {
                     string json = await System.IO.File.ReadAllTextAsync(jsonPath);
                     var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                     // Both JSONs use "key": { "name": "...", "link": "..." } structure
                     var data = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, T>>(json, options);

                    if (data != null && data.Count > 0)
                    {
                        comboBox.Items.Clear();
                        if (comboBox.Name == "CmbWindowsVersion")
                        {
                            comboBox.Items.Add(new ComboBoxItem { Content = Helpers.LocalizationService.Get("msgUsbSelectVersion") });
                        }
                        foreach (var kvp in data)
                        {
                            // Reflection or dynamic to access Name/Link common properties if strict type not shared, 
                            // but define compatible classes is easier.
                            dynamic item = kvp.Value; 
                            var cbItem = new ComboBoxItem();
                            cbItem.Content = item.name;
                            cbItem.Tag = item.link;
                            comboBox.Items.Add(cbItem);
                        }
                         comboBox.SelectedIndex = 0;
                     }
                     else
                     {
                          comboBox.Items.Clear();
                          comboBox.Items.Add(new ComboBoxItem { Content = Helpers.LocalizationService.Get("msgUsbNoDrives") });
                          comboBox.SelectedIndex = 0;
                     }
                 }
                 else
                 {
                      comboBox.Items.Clear();
                      comboBox.Items.Add(new ComboBoxItem { Content = $"{fileName} {Helpers.LocalizationService.Get("msgError")}" });
                      comboBox.SelectedIndex = 0;
                 }
             }
             catch (System.Exception)
             {
                 comboBox.Items.Clear();
                 comboBox.Items.Add(new ComboBoxItem { Content = Helpers.LocalizationService.Get("msgError") });
                 comboBox.SelectedIndex = 0;
             }
        }

        public class IsoInfo
        {
            public string name { get; set; }
            public string link { get; set; }
        }

        public class ResourceInfo
        {
            public string name { get; set; }
            public string link { get; set; }
        }

        private async System.Threading.Tasks.Task LoadPhysicalDisks()
        {
            CmbUSBDevices.Items.Clear();
            CmbUSBDevices.Items.Add(new ComboBoxItem { Content = Helpers.LocalizationService.Get("msgUsbLoadingDrives") });
            CmbUSBDevices.SelectedIndex = 0;

            try
            {
                var disks = await GetPhysicalDisksAsync();
                
                CmbUSBDevices.Items.Clear();
                if (disks.Count > 0)
                {
                    foreach (var disk in disks)
                    {
                        CmbUSBDevices.Items.Add(new ComboBoxItem { Content = disk, Tag = disk.DeviceId });
                    }
                    CmbUSBDevices.SelectedIndex = 0;
                }
                else
                {
                    CmbUSBDevices.Items.Add(new ComboBoxItem { Content = Helpers.LocalizationService.Get("msgUsbNoDrives") });
                    CmbUSBDevices.SelectedIndex = 0;
                }
            }
            catch
            {
                CmbUSBDevices.Items.Clear();
                CmbUSBDevices.Items.Add(new ComboBoxItem { Content = Helpers.LocalizationService.Get("msgUsbFailedLoadDrives") });
                CmbUSBDevices.SelectedIndex = 0;
            }
        }

        private bool _isCustomIsoSelected = false;

        private void SelectISOBrowseBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select ISO File",
                Filter = "ISO Files (*.iso)|*.iso|All files (*.*)|*.*",
                Multiselect = false
            };
            if (dialog.ShowDialog() == true)
            {
                SelectISOTextBox.Text = dialog.FileName;
                _isCustomIsoSelected = true;
                if (CmbWindowsVersion.Items.Count > 0)
                {
                    CmbWindowsVersion.SelectedIndex = 0;
                }
            }
        }

        private void CmbWindowsVersion_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CmbWindowsVersion.SelectedIndex > 0 && SelectISOTextBox != null)
            {
                _isCustomIsoSelected = false;
                SelectISOTextBox.Text = Helpers.LocalizationService.Get("msgUsbSelectIsoDir") ?? "Select directory to your iso file";
            }
        }
        private async void RestartBios_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var confirmWindow = new ModernConfirmationWindow(Helpers.LocalizationService.Get("msgUsbConfirmRestartBios"), Helpers.LocalizationService.Get("msgUsbConfirmRestartTitle"), Helpers.LocalizationService.Get("btnYes"), Helpers.LocalizationService.Get("btnNo"));
            confirmWindow.Owner = System.Windows.Window.GetWindow(this);
            if (confirmWindow.ShowDialog() == true)
            {
                if (!IsRunningAsAdmin())
                {
                    ShowMessage(Helpers.LocalizationService.Get("msgUsbRestartBiosParams"), Helpers.LocalizationService.Get("msgUsbAdminRequired"));
                    return;
                }

                try
                {
                    await System.Threading.Tasks.Task.Run(async () =>
                    {
                        // Handle 32-bit process on 64-bit OS to access 64-bit shutdown.exe for /fw support
                        string shutdownPath = "shutdown";
                        if (System.Environment.Is64BitOperatingSystem && !System.Environment.Is64BitProcess)
                        {
                            string sysnative = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "Sysnative", "shutdown.exe");
                            if (System.IO.File.Exists(sysnative))
                            {
                                shutdownPath = sysnative;
                            }
                        }

                        int attempts = 0;
                        bool success = false;
                        string lastError = "";

                        while (attempts < 2 && !success)
                        {
                            attempts++;
                            var psi = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = shutdownPath,
                                Arguments = "/r /fw /t 0",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardError = true,
                                RedirectStandardOutput = true
                            };

                            using (var p = System.Diagnostics.Process.Start(psi))
                            {
                                if (p == null) throw new System.Exception("Could not start shutdown process.");
                                
                                string output = p.StandardOutput.ReadToEnd();
                                string error = p.StandardError.ReadToEnd();
                                p.WaitForExit();

                                if (p.ExitCode == 0)
                                {
                                    success = true;
                                }
                                else
                                {
                                    // Error 203 specific handling
                                    if (p.ExitCode == 203)
                                    {
                                         lastError = $"Error 203: System environment option not found. (Typically Legacy BIOS)";
                                         // Retry if it's the first attempt, as user reports it works 2nd time sometimes
                                         if (attempts < 2)
                                         {
                                             await System.Threading.Tasks.Task.Delay(500); // Wait 500ms before retry
                                             continue;
                                         }
                                         
                                         // If failed twice
                                         throw new System.Exception($"{lastError}\n\nVerify that your PC is using UEFI mode.");
                                    }

                                    string msg = !string.IsNullOrWhiteSpace(error) ? error : output;
                                    if (string.IsNullOrWhiteSpace(msg)) msg = $"Unknown error (Code {p.ExitCode})";
                                    throw new System.Exception(msg.Trim());
                                }
                            }
                        }
                    });
                }
                catch (System.Exception ex)
                {
                    ShowMessage($"{Helpers.LocalizationService.Get("msgError")}: {ex.Message}", Helpers.LocalizationService.Get("msgError"));
                }
            }
        }

        private void FixWmic_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!IsRunningAsAdmin())
            {
                ShowMessage(Helpers.LocalizationService.Get("msgUsbFixWmicParams"), Helpers.LocalizationService.Get("msgUsbAdminRequired"));
                return;
            }

            try
            {
                 // DISM /Online /Add-Capability /CapabilityName:WMIC~~~~
                 var psi = new System.Diagnostics.ProcessStartInfo();
                 psi.FileName = "cmd.exe";
                 psi.Arguments = "/C DISM /Online /Add-Capability /CapabilityName:WMIC~~~~";
                 psi.UseShellExecute = true; // Required for WindowStyle to work with cmd/console app if we want to minimize it effectively from UI app
                 psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                 psi.Verb = "runas"; // Ensure admin
                 System.Diagnostics.Process.Start(psi);
                 
                 // Show Modern Dialog concurrently (Processing...)
                 var infoWindow = new ModernConfirmationWindow(
                     Helpers.LocalizationService.Get("msgUsbWmicFixing"), 
                     Helpers.LocalizationService.Get("msgUsbProcessing"), 
                     Helpers.LocalizationService.Get("btnOK"), 
                     null); // null cancelText to hide Cancel button
                 infoWindow.Owner = System.Windows.Window.GetWindow(this);
                 infoWindow.ShowDialog();
            }
            catch (System.Exception ex)
            {
                ShowMessage($"{Helpers.LocalizationService.Get("msgError")}: {ex.Message}", Helpers.LocalizationService.Get("msgError"));
            }
        }
        private async void Start_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!IsRunningAsAdmin())
            {
                ShowMessage(Helpers.LocalizationService.Get("msgUsbFeatureAdminParams"), Helpers.LocalizationService.Get("msgUsbAdminRequired"));
                return;
            }

            // 1. Validate USB Drive
            var selectedDriveItem = CmbUSBDevices.SelectedItem as ComboBoxItem;
            if (selectedDriveItem == null || selectedDriveItem.Tag == null)
            {
                ShowMessage(Helpers.LocalizationService.Get("msgUsbSelectValidDrive"), Helpers.LocalizationService.Get("msgError"));
                return;
            }
            string diskId = selectedDriveItem.Tag.ToString();
            string diskName = selectedDriveItem.Content.ToString();

            // 2. Determine ISO Path
            var resolvedIso = ResolveIsoPath(allowDownload: true);
            string isoPath = resolvedIso.IsoPath;
            string isoUrl = resolvedIso.IsoUrl;
            bool isDownloadNeeded = resolvedIso.IsDownloadNeeded;

            if (string.IsNullOrEmpty(isoPath))
            {
                ShowMessage(Helpers.LocalizationService.Get("msgUsbSelectWinVer"), Helpers.LocalizationService.Get("msgError"));
                return;
            }

            // 3. Confirm
            // Reformat message to prevent display glitches if disk name has control chars
            string diskDisplay = Sanitize(diskName); // Ensure clean
            string confirmMsg = string.Format(Helpers.LocalizationService.Get("msgUsbConfirmFormatBody"), diskDisplay, diskId, System.IO.Path.GetFileName(isoPath));
            var confirmWindow = new ModernConfirmationWindow(confirmMsg, Helpers.LocalizationService.Get("msgUsbConfirmFormatTitle"), Helpers.LocalizationService.Get("btnYes"), Helpers.LocalizationService.Get("btnNo"));
            confirmWindow.Owner = System.Windows.Window.GetWindow(this);
            if (confirmWindow.ShowDialog() != true) return;

            BtnStart.IsEnabled = false;

            var mainWindow = System.Windows.Window.GetWindow(this) as MainWindow;
            
            // Start Progress Overlay
            if (mainWindow != null) mainWindow.ShowLoading(Helpers.LocalizationService.Get("msgUsbInitializing"), 1, 0);

            try
            {
                // 4. Download if needed
                if (isDownloadNeeded)
                {
                     if (mainWindow != null) mainWindow.UpdateLoadingProgress(1, 10, string.Format(Helpers.LocalizationService.Get("msgUsbDownloadingIso"), System.IO.Path.GetFileName(isoPath)));
                     // Note: Real progress requires a better download client than curl or parsing curl output. 
                     // For now we just show Indeterminate/Working state by not updating % frequently.
                     // Run interactively (minimized) so user can see progress if they want
                     var downloadRes = await RunInteractiveCommandAsync("curl", $"-L -o \"{isoPath}\" \"{isoUrl}\"");
                     if (!downloadRes.Success || !System.IO.File.Exists(isoPath))
                     {
                         throw new System.Exception($"Failed to download ISO file. (Process exit code was not 0 or file missing)");
                     }
                     if (mainWindow != null) mainWindow.UpdateLoadingProgress(1, 100, Helpers.LocalizationService.Get("msgUsbDownloadComplete"));
                }
                else
                {
                     if (mainWindow != null) mainWindow.UpdateLoadingProgress(1, 100, Helpers.LocalizationService.Get("msgUsbIsoReady"));
                }

                // 2. Install Ventoy
                if (mainWindow != null) mainWindow.UpdateLoadingProgress(2, 0, Helpers.LocalizationService.Get("msgUsbInstallingVentoy"));
                
                string ventoyPath = FindVentoyPath();
                if (string.IsNullOrEmpty(ventoyPath))
                {
                     // Debug info
                     string baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
                     throw new System.Exception($"Ventoy2Disk.exe not found.\nBaseDir: {baseDir}\nRe-check 'Warehouse' folder in project root.");
                }

                var installRes = await RunCommandAsync(ventoyPath, $"VTOYCLI /I /PhyDrive:{diskId} /FS:EXFAT");
                if (!installRes.Success)
                {
                    throw new System.Exception($"Ventoy installation failed.\nCmd: {ventoyPath}\nArgs: VTOYCLI /I /PhyDrive:{diskId} /FS:EXFAT\nOutput: {installRes.Output}");
                }
                if (mainWindow != null) mainWindow.UpdateLoadingProgress(2, 100, Helpers.LocalizationService.Get("msgUsbVentoyInstalled"));

                string targetDrive = await WaitForWritableUsbVolumeAsync(diskId);
                await LabelUsbBootAsync(targetDrive);

                // 5. Copy Configs/Structure (bin\Copy to USB Root)
                if (mainWindow != null) mainWindow.UpdateLoadingProgress(3, 10, Helpers.LocalizationService.Get("msgUsbCopyingBoot"));
                string binPath = null;
                if (!string.IsNullOrEmpty(ventoyPath))
                {
                     var vDir = System.IO.Path.GetDirectoryName(ventoyPath);
                     var mDir = System.IO.Path.GetDirectoryName(vDir);
                     binPath = System.IO.Path.GetDirectoryName(mDir);
                }

                if (!string.IsNullOrEmpty(binPath))
                {
                     string copySource = System.IO.Path.Combine(binPath, "Copy");
                     
                     // Helper local function to copy directory
                     async System.Threading.Tasks.Task CopyDir(string source, string dest)
                     {
                         if (System.IO.Directory.Exists(source))
                         {
                             // Use PowerShell to forcefully create target directory and copy items instead of native IO to avoid Access Denied locks
                             await RunCommandAsync("powershell", $"-Command \"if (-not (Test-Path '{dest}')) {{ New-Item -ItemType Directory -Force -Path '{dest}' -ErrorAction SilentlyContinue }}; Copy-Item -Path '{source}\\*' -Destination '{dest}' -Recurse -Force\"");
                         }
                     }

                     if (System.IO.Directory.Exists(copySource))
                     {
                         // Explicitly copy ISO folder (to targetDrive\ISO)
                         string isoSrc = System.IO.Path.Combine(copySource, "ISO");
                         string isoDst = System.IO.Path.Combine(targetDrive, "ISO");
                         await CopyDir(isoSrc, isoDst);
                         
                         // Explicitly copy ventoy folder (to targetDrive\ventoy)
                         string ventoySrc = System.IO.Path.Combine(copySource, "ventoy");
                         string ventoyDst = System.IO.Path.Combine(targetDrive, "ventoy");
                         await CopyDir(ventoySrc, ventoyDst);

                         // Also copy files in Copy root
                         await RunCommandAsync("powershell", $"-Command \"Copy-Item -Path '{copySource}\\*' -Destination '{targetDrive}' -Force\""); 
                     }
                     
                     // Also ensure Theme is copied if needed
                     if (mainWindow != null) mainWindow.UpdateLoadingProgress(3, 30, Helpers.LocalizationService.Get("msgUsbCopyingTheme"));
                     string themeSrc = System.IO.Path.Combine(binPath, "Module", "theme");
                     if (System.IO.Directory.Exists(themeSrc))
                     {
                          await RunCommandAsync("powershell", $"-Command \"Copy-Item -Path '{themeSrc}\\*' -Destination '{targetDrive}' -Recurse -Force\"");
                     }
                     
                     // Attribute command
                     await RunCommandAsync("powershell", $"-Command \"attrib +s +h \\\"{targetDrive}\\ventoy\\\" /S /D\"");
                }

                // 6. Copy ISO to USB:\ISO
                if (mainWindow != null) mainWindow.UpdateLoadingProgress(3, 50, string.Format(Helpers.LocalizationService.Get("msgUsbCopyingIso"), targetDrive));
                var copyProgress = new System.Progress<int>(pct =>
                {
                    if (mainWindow != null)
                    {
                        mainWindow.UpdateLoadingProgress(3, pct, string.Format(Helpers.LocalizationService.Get("msgUsbCopyingIso") ?? "Copying to {0}...", targetDrive) + $" ({pct}%)");
                    }
                });
                await CopyIsoToUsbAsync(isoPath, targetDrive, copyProgress);
                if (mainWindow != null) mainWindow.UpdateLoadingProgress(3, 100, Helpers.LocalizationService.Get("msgUsbFinalizing"));

                // Completed
                if (mainWindow != null) mainWindow.HideLoading();
                
                var successWindow = new ModernConfirmationWindow(Helpers.LocalizationService.Get("msgUsbSuccessBody"), Helpers.LocalizationService.Get("msgUsbSuccessTitle"), Helpers.LocalizationService.Get("btnOK"), null);
                successWindow.Owner = System.Windows.Window.GetWindow(this);
                successWindow.ShowDialog();
            }
            catch (System.Exception ex)
            {
                if (mainWindow != null) mainWindow.HideLoading();
                ShowMessage($"{Helpers.LocalizationService.Get("msgError")}: {ex.Message}", Helpers.LocalizationService.Get("msgError")); // Using "Failed" as generic error title for consistency
            }
            finally
            {
                BtnStart.IsEnabled = true;
                if (mainWindow != null) mainWindow.HideLoading(); // Ensure hidden
            }
        }

        private async void DownloadOnly_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var selectedIsoItem = CmbWindowsVersion.SelectedItem as ComboBoxItem;
            if (selectedIsoItem == null || selectedIsoItem.Tag == null)
            {
                ShowMessage(Helpers.LocalizationService.Get("msgUsbSelectIsoBeforeDownload") ?? "Please select a Windows version.", Helpers.LocalizationService.Get("msgError"));
                return;
            }

            string versionFolder = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Download");
            if (!System.IO.Directory.Exists(versionFolder)) System.IO.Directory.CreateDirectory(versionFolder);
            
            string fileName = selectedIsoItem.Content.ToString().Replace(" ", "_") + ".iso";
            string isoPath = System.IO.Path.Combine(versionFolder, fileName);
            string isoUrl = selectedIsoItem.Tag.ToString();
            
            if (System.IO.File.Exists(isoPath))
            {
                ShowMessage((Helpers.LocalizationService.Get("msgUsbIsoReady") ?? "ISO Ready") + "\n" + isoPath, Helpers.LocalizationService.Get("msgInfo") ?? "Info");
                return;
            }

            BtnDownloadOnly.IsEnabled = false;
            var mainWindow = System.Windows.Window.GetWindow(this) as MainWindow;
            if (mainWindow != null) mainWindow.ShowLoading(string.Format(Helpers.LocalizationService.Get("msgUsbDownloadingIso") ?? "Downloading: {0}", fileName), 0, 0);

            try
            {
                var downloadRes = await RunInteractiveCommandAsync("curl", $"-L -o \"{isoPath}\" \"{isoUrl}\"");
                if (!downloadRes.Success || !System.IO.File.Exists(isoPath))
                {
                    throw new System.Exception($"Failed to download ISO file.");
                }
                
                if (mainWindow != null) mainWindow.HideLoading();
                
                string msgTemplate = Helpers.LocalizationService.Get("msgUsbDownloadPrompt") ?? "ISO downloaded to:\n{0}\n\nDo you want to open its location?";
                string msg = string.Format(msgTemplate, isoPath);
                var win = new ModernConfirmationWindow(msg, Helpers.LocalizationService.Get("msgUsbDownloadComplete") ?? "Download Complete", Helpers.LocalizationService.Get("btnYes") ?? "Yes", Helpers.LocalizationService.Get("btnNo") ?? "No");
                win.Owner = System.Windows.Window.GetWindow(this);
                if (win.ShowDialog() == true)
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{isoPath}\"",
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                }
            }
            catch (System.Exception ex)
            {
                if (mainWindow != null) mainWindow.HideLoading();
                ShowMessage($"{Helpers.LocalizationService.Get("msgError")}: {ex.Message}", Helpers.LocalizationService.Get("msgError"));
            }
            finally
            {
                BtnDownloadOnly.IsEnabled = true;
                if (mainWindow != null) mainWindow.HideLoading();
            }
        }

        private async void AddIsoToUsb_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var selectedDriveItem = CmbUSBDevices.SelectedItem as ComboBoxItem;
            if (selectedDriveItem == null || selectedDriveItem.Tag == null)
            {
                ShowMessage(Helpers.LocalizationService.Get("msgUsbSelectUsbBeforeAdd") ?? "Please select a valid USB drive.", Helpers.LocalizationService.Get("msgError"));
                return;
            }
            string diskId = selectedDriveItem.Tag.ToString();
            
            var resolvedIso = ResolveIsoPath(allowDownload: false);
            string isoPath = resolvedIso.IsoPath;

            if (string.IsNullOrEmpty(isoPath))
            {
                ShowMessage(Helpers.LocalizationService.Get("msgUsbSelectWinVer") ?? "Please select ISO", Helpers.LocalizationService.Get("msgError"));
                return;
            }

            if (!System.IO.File.Exists(isoPath))
            {
                ShowMessage(Helpers.LocalizationService.Get("msgUsbIsoNotDownloaded") ?? "The selected ISO has not been downloaded yet. Please download it first or browse to an existing ISO file.", Helpers.LocalizationService.Get("msgError"));
                return;
            }

            string targetDrive;
            try
            {
                targetDrive = await WaitForWritableUsbVolumeAsync(diskId);
            }
            catch (System.Exception ex)
            {
                ShowMessage($"{Helpers.LocalizationService.Get("msgError")}: {ex.Message}", Helpers.LocalizationService.Get("msgError"));
                return;
            }

            BtnAddIsoToUsb.IsEnabled = false;
            var mainWindow = System.Windows.Window.GetWindow(this) as MainWindow;
            if (mainWindow != null) mainWindow.ShowLoading(string.Format(Helpers.LocalizationService.Get("msgUsbCopyingIso") ?? "Copying to {0}...", targetDrive), 0, 0);

            try
            {
                var copyProgress = new System.Progress<int>(pct =>
                {
                    if (mainWindow != null)
                    {
                        mainWindow.UpdateLoadingProgress(0, pct, string.Format(Helpers.LocalizationService.Get("msgUsbCopyingIso") ?? "Copying to {0}...", targetDrive) + $" ({pct}%)");
                    }
                });
                await CopyIsoToUsbAsync(isoPath, targetDrive, copyProgress);

                if (mainWindow != null) mainWindow.HideLoading();
                ShowMessage("ISO file copied to USB successfully!", Helpers.LocalizationService.Get("btnOK") ?? "Success");
            }
            catch (System.Exception ex)
            {
                if (mainWindow != null) mainWindow.HideLoading();
                ShowMessage($"{Helpers.LocalizationService.Get("msgError")}: {ex.Message}", Helpers.LocalizationService.Get("msgError"));
            }
            finally
            {
                BtnAddIsoToUsb.IsEnabled = true;
                if (mainWindow != null) mainWindow.HideLoading();
            }
        }

        private async void CreateBootloaderOnly_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!IsRunningAsAdmin())
            {
                ShowMessage(Helpers.LocalizationService.Get("msgUsbFeatureAdminParams"), Helpers.LocalizationService.Get("msgUsbAdminRequired"));
                return;
            }

            var selectedDriveItem = CmbUSBDevices.SelectedItem as ComboBoxItem;
            if (selectedDriveItem == null || selectedDriveItem.Tag == null)
            {
                ShowMessage(Helpers.LocalizationService.Get("msgUsbSelectValidDrive"), Helpers.LocalizationService.Get("msgError"));
                return;
            }
            string diskId = selectedDriveItem.Tag.ToString();
            string diskName = selectedDriveItem.Content.ToString();

            string diskDisplay = Sanitize(diskName);
            string confirmMsg = string.Format(Helpers.LocalizationService.Get("msgUsbConfirmFormatBody"), diskDisplay, diskId, "Bootloader Only");
            var confirmWindow = new ModernConfirmationWindow(confirmMsg, Helpers.LocalizationService.Get("msgUsbConfirmFormatTitle"), Helpers.LocalizationService.Get("btnYes"), Helpers.LocalizationService.Get("btnNo"));
            confirmWindow.Owner = System.Windows.Window.GetWindow(this);
            if (confirmWindow.ShowDialog() != true) return;

            BtnCreateBootloader.IsEnabled = false;
            var mainWindow = System.Windows.Window.GetWindow(this) as MainWindow;
            if (mainWindow != null) mainWindow.ShowLoading(Helpers.LocalizationService.Get("msgUsbInstallingVentoy"), 1, 0);

            try
            {
                string ventoyPath = FindVentoyPath();
                if (string.IsNullOrEmpty(ventoyPath))
                {
                     throw new System.Exception($"Ventoy2Disk.exe not found.");
                }

                var installRes = await RunCommandAsync(ventoyPath, $"VTOYCLI /I /PhyDrive:{diskId} /FS:EXFAT");
                if (!installRes.Success)
                {
                    throw new System.Exception($"Ventoy installation failed.\nCmd: {ventoyPath}\nArgs: VTOYCLI /I /PhyDrive:{diskId} /FS:EXFAT\nOutput: {installRes.Output}");
                }

                if (mainWindow != null) mainWindow.UpdateLoadingProgress(2, 20, "Waiting for Windows to mount the volume...");
                string targetDrive = await WaitForWritableUsbVolumeAsync(diskId);
                await LabelUsbBootAsync(targetDrive);

                if (mainWindow != null) mainWindow.UpdateLoadingProgress(2, 50, Helpers.LocalizationService.Get("msgUsbCopyingBoot"));
                string binPath = null;
                if (!string.IsNullOrEmpty(ventoyPath))
                {
                     var vDir = System.IO.Path.GetDirectoryName(ventoyPath);
                     var mDir = System.IO.Path.GetDirectoryName(vDir);
                     binPath = System.IO.Path.GetDirectoryName(mDir);
                }

                if (!string.IsNullOrEmpty(binPath))
                {
                     string copySource = System.IO.Path.Combine(binPath, "Copy");
                     
                     async System.Threading.Tasks.Task CopyDir(string source, string dest)
                     {
                         if (System.IO.Directory.Exists(source))
                         {
                             // Use PowerShell to forcefully create target directory and copy items instead of native IO to avoid Access Denied locks
                             await RunCommandAsync("powershell", $"-Command \"if (-not (Test-Path '{dest}')) {{ New-Item -ItemType Directory -Force -Path '{dest}' -ErrorAction SilentlyContinue }}; Copy-Item -Path '{source}\\*' -Destination '{dest}' -Recurse -Force\"");
                         }
                     }

                     if (System.IO.Directory.Exists(copySource))
                     {
                         string isoSrc = System.IO.Path.Combine(copySource, "ISO");
                         string isoDst = System.IO.Path.Combine(targetDrive, "ISO");
                         await CopyDir(isoSrc, isoDst);
                         
                         string ventoySrc = System.IO.Path.Combine(copySource, "ventoy");
                         string ventoyDst = System.IO.Path.Combine(targetDrive, "ventoy");
                         await CopyDir(ventoySrc, ventoyDst);

                         await RunCommandAsync("powershell", $"-Command \"Copy-Item -Path '{copySource}\\*' -Destination '{targetDrive}' -Force\""); 
                     }
                     
                     if (mainWindow != null) mainWindow.UpdateLoadingProgress(3, 80, Helpers.LocalizationService.Get("msgUsbCopyingTheme"));
                     string themeSrc = System.IO.Path.Combine(binPath, "Module", "theme");
                     if (System.IO.Directory.Exists(themeSrc))
                     {
                          await RunCommandAsync("powershell", $"-Command \"Copy-Item -Path '{themeSrc}\\*' -Destination '{targetDrive}' -Recurse -Force\"");
                     }
                     
                     await RunCommandAsync("powershell", $"-Command \"attrib +s +h \\\"{targetDrive}\\ventoy\\\" /S /D\"");
                }

                if (mainWindow != null) mainWindow.HideLoading();
                var successWindow = new ModernConfirmationWindow(Helpers.LocalizationService.Get("msgUsbSuccessBody"), Helpers.LocalizationService.Get("msgUsbSuccessTitle"), Helpers.LocalizationService.Get("btnOK"), null);
                successWindow.Owner = System.Windows.Window.GetWindow(this);
                successWindow.ShowDialog();
            }
            catch (System.Exception ex)
            {
                if (mainWindow != null) mainWindow.HideLoading();
                ShowMessage($"{Helpers.LocalizationService.Get("msgError")}: {ex.Message}", Helpers.LocalizationService.Get("msgError"));
            }
            finally
            {
                BtnCreateBootloader.IsEnabled = true;
                if (mainWindow != null) mainWindow.HideLoading();
            }
        }

        private class ResolvedIsoInfo
        {
            public string IsoPath { get; set; } = "";
            public string IsoUrl { get; set; } = "";
            public bool IsDownloadNeeded { get; set; }
        }

        private ResolvedIsoInfo ResolveIsoPath(bool allowDownload)
        {
            string customText = SelectISOTextBox.Text?.Trim() ?? "";
            bool isPlaceholder = IsIsoPlaceholder(customText);

            if (_isCustomIsoSelected && !isPlaceholder && System.IO.File.Exists(customText))
            {
                return new ResolvedIsoInfo { IsoPath = customText };
            }

            var selectedIsoItem = CmbWindowsVersion.SelectedItem as ComboBoxItem;
            if (selectedIsoItem != null && selectedIsoItem.Tag != null)
            {
                string versionFolder = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Download");
                if (!System.IO.Directory.Exists(versionFolder))
                {
                    System.IO.Directory.CreateDirectory(versionFolder);
                }

                string fileName = BuildIsoFileName(selectedIsoItem.Content?.ToString());
                string isoPath = System.IO.Path.Combine(versionFolder, fileName);
                string isoUrl = selectedIsoItem.Tag.ToString() ?? "";

                return new ResolvedIsoInfo
                {
                    IsoPath = isoPath,
                    IsoUrl = isoUrl,
                    IsDownloadNeeded = allowDownload && !System.IO.File.Exists(isoPath)
                };
            }

            if (!isPlaceholder && System.IO.File.Exists(customText))
            {
                return new ResolvedIsoInfo { IsoPath = customText };
            }

            return new ResolvedIsoInfo();
        }

        private bool IsIsoPlaceholder(string text)
        {
            return string.IsNullOrWhiteSpace(text)
                || text == "Select directory to your iso"
                || text == "Chá»n Ä‘Æ°á»ng dáº«n tá»›i file ISO"
                || text == Helpers.LocalizationService.Get("msgUsbSelectIsoDir");
        }

        private string BuildIsoFileName(string displayName)
        {
            string fileName = string.IsNullOrWhiteSpace(displayName) ? "windows.iso" : displayName.Trim().Replace(" ", "_");
            foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            if (!fileName.EndsWith(".iso", System.StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".iso";
            }

            return fileName;
        }

        private async System.Threading.Tasks.Task<string> WaitForWritableUsbVolumeAsync(string diskId)
        {
            System.Exception lastWriteError = null;

            for (int attempt = 0; attempt < 15; attempt++)
            {
                if (attempt > 0)
                {
                    await System.Threading.Tasks.Task.Delay(1500);
                }

                string targetDrive = await GetUsbBootDriveLetter(diskId);
                if (string.IsNullOrEmpty(targetDrive))
                {
                    continue;
                }

                try
                {
                    EnsureDriveIsReadyAndWritable(targetDrive);
                    return targetDrive;
                }
                catch (System.Exception ex)
                {
                    lastWriteError = ex;
                }
            }

            string message = $"Could not find a writable 'Ventoy' or 'USB_BOOT' volume on the selected USB disk ({diskId}). Windows AutoMount might still be assigning the drive letter.";
            if (lastWriteError != null)
            {
                message += $"\nLast Windows error: {lastWriteError.Message}";
            }

            throw new System.Exception(message);
        }

        private async System.Threading.Tasks.Task LabelUsbBootAsync(string targetDrive)
        {
            if (string.IsNullOrWhiteSpace(targetDrive) || targetDrive.Length < 2)
            {
                return;
            }

            await RunCommandAsync("cmd.exe", $"/c label {targetDrive.Substring(0, 2)} USB_BOOT");
        }

        private void EnsureDriveIsReadyAndWritable(string targetDrive)
        {
            string root = System.IO.Path.GetPathRoot(targetDrive) ?? targetDrive;
            var driveInfo = new System.IO.DriveInfo(root);

            if (!driveInfo.IsReady)
            {
                throw new System.IO.IOException($"Drive {root} is not ready.");
            }

            string probePath = System.IO.Path.Combine(root, $".hyperlis_write_test_{System.Guid.NewGuid():N}.tmp");
            try
            {
                using (var probe = new System.IO.FileStream(probePath, System.IO.FileMode.CreateNew, System.IO.FileAccess.Write, System.IO.FileShare.None))
                {
                    byte[] marker = System.Text.Encoding.ASCII.GetBytes("ok");
                    probe.Write(marker, 0, marker.Length);
                }
            }
            finally
            {
                try
                {
                    if (System.IO.File.Exists(probePath))
                    {
                        System.IO.File.Delete(probePath);
                    }
                }
                catch
                {
                }
            }
        }

        private string EnsureIsoDirectory(string targetDrive)
        {
            EnsureDriveIsReadyAndWritable(targetDrive);

            string isoDestDir = System.IO.Path.Combine(targetDrive, "ISO");
            try
            {
                System.IO.Directory.CreateDirectory(isoDestDir);
            }
            catch (System.Exception ex)
            {
                throw new System.IO.IOException($"Cannot create ISO directory '{isoDestDir}'. Windows reported: {ex.Message}", ex);
            }

            if (!System.IO.Directory.Exists(isoDestDir))
            {
                throw new System.IO.DirectoryNotFoundException($"ISO directory was not created: {isoDestDir}");
            }

            return isoDestDir;
        }

        private async System.Threading.Tasks.Task CopyIsoToUsbAsync(string isoPath, string targetDrive, System.IProgress<int> progress)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(isoPath) || !System.IO.File.Exists(isoPath))
                {
                    throw new System.IO.FileNotFoundException("ISO file does not exist.", isoPath);
                }

                string isoDestDir = EnsureIsoDirectory(targetDrive);
                string destIso = System.IO.Path.Combine(isoDestDir, System.IO.Path.GetFileName(isoPath));
                var sourceInfo = new System.IO.FileInfo(isoPath);
                var driveInfo = new System.IO.DriveInfo(System.IO.Path.GetPathRoot(targetDrive) ?? targetDrive);

                if (driveInfo.AvailableFreeSpace < sourceInfo.Length)
                {
                    throw new System.IO.IOException($"The selected USB does not have enough free space. Required: {FormatBytes(sourceInfo.Length)}, available: {FormatBytes(driveInfo.AvailableFreeSpace)}.");
                }

                if (System.IO.File.Exists(destIso))
                {
                    var attributes = System.IO.File.GetAttributes(destIso);
                    if ((attributes & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
                    {
                        System.IO.File.SetAttributes(destIso, attributes & ~System.IO.FileAttributes.ReadOnly);
                    }
                }

                try
                {
                    using (var sourceStream = new System.IO.FileStream(isoPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read, 1024 * 1024, System.IO.FileOptions.SequentialScan))
                    using (var destStream = new System.IO.FileStream(destIso, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 1024 * 1024, System.IO.FileOptions.SequentialScan))
                    {
                        long totalBytes = sourceStream.Length;
                        long totalCopied = 0;
                        byte[] buffer = new byte[1024 * 1024];
                        int bytesRead;
                        int lastPct = -1;

                        progress?.Report(0);

                        while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            destStream.Write(buffer, 0, bytesRead);
                            totalCopied += bytesRead;

                            if (totalBytes > 0)
                            {
                                int pct = (int)((totalCopied * 100) / totalBytes);
                                if (pct != lastPct)
                                {
                                    lastPct = pct;
                                    progress?.Report(pct);
                                }
                            }
                        }

                        destStream.Flush(true);
                    }
                }
                catch (System.UnauthorizedAccessException ex)
                {
                    throw new System.UnauthorizedAccessException($"Cannot write ISO to '{destIso}'. Windows reported: {ex.Message}", ex);
                }
                catch (System.IO.DirectoryNotFoundException ex)
                {
                    throw new System.IO.DirectoryNotFoundException($"Cannot write ISO to '{destIso}' because the destination directory is missing. Windows reported: {ex.Message}", ex);
                }
                catch (System.IO.IOException ex)
                {
                    throw new System.IO.IOException($"Cannot write ISO to '{destIso}'. Windows reported: {ex.Message}", ex);
                }
            });
        }

        private string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            int unitIndex = 0;

            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return $"{value:0.##} {units[unitIndex]}";
        }

        private string FindVentoyPath()
        {
            string baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            
            // Check build/deployed location first
            string localPath = System.IO.Path.Combine(baseDir, "Assets", "USB_Boot", "Module", "ventoy-1.1.05", "Ventoy2Disk.exe");
            if (System.IO.File.Exists(localPath)) return localPath;

            string currentPath = baseDir;
            // Limit search to root of drive to avoid infinite loops, though loop below handles null parent
            while (!string.IsNullOrEmpty(currentPath))
            {
                // Check original path (Legacy/Standard)
                string potentialPath1 = System.IO.Path.Combine(currentPath, "Warehouse", "Ver6_FullSupport", "bin", "Module", "ventoy-1.1.05", "Ventoy2Disk.exe");
                if (System.IO.File.Exists(potentialPath1)) return potentialPath1;

                // Check user-provided path (Direct bin)
                string potentialPath2 = System.IO.Path.Combine(currentPath, "Warehouse", "bin", "Module", "ventoy-1.1.05", "Ventoy2Disk.exe");
                if (System.IO.File.Exists(potentialPath2)) return potentialPath2;
                
                var parent = System.IO.Directory.GetParent(currentPath);
                if (parent == null) break;
                currentPath = parent.FullName;
            }
            return null;
        }

        private struct ProcessResult { public bool Success; public string Output; }

        private async System.Threading.Tasks.Task<ProcessResult> RunCommandAsync(string fileName, string arguments)
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo();
                    psi.FileName = fileName;
                    psi.Arguments = arguments;
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;
                    
                    using (var p = System.Diagnostics.Process.Start(psi))
                    {
                        if (p == null) return new ProcessResult { Success = false, Output = "Failed to start process" };
                        string error = "";
                        p.ErrorDataReceived += (sender, e) => { if (e.Data != null) error += e.Data + "\n"; };
                        p.BeginErrorReadLine();
                        string output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit();
                        return new ProcessResult { Success = p.ExitCode == 0, Output = output + "\n" + error };
                    }
                }
                catch (System.Exception ex) { return new ProcessResult { Success = false, Output = ex.Message }; }
            });
        }

        private async System.Threading.Tasks.Task<ProcessResult> RunInteractiveCommandAsync(string fileName, string arguments)
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo();
                    psi.FileName = fileName;
                    psi.Arguments = arguments;
                    psi.UseShellExecute = true; // Use shell so we can control window style
                    psi.CreateNoWindow = false; // We want a window
                    psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized; // Start minimized
                    // Cannot redirect output when UseShellExecute is true

                    using (var p = System.Diagnostics.Process.Start(psi))
                    {
                        if (p == null) return new ProcessResult { Success = false, Output = "Failed to start process" };
                        p.WaitForExit();
                        return new ProcessResult { Success = p.ExitCode == 0, Output = "Interactive process completed." };
                    }
                }
                catch (System.Exception ex) { return new ProcessResult { Success = false, Output = ex.Message }; }
            });
        }
        
        private bool IsRunningAsAdmin()
        {
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
        }

        private async System.Threading.Tasks.Task<string> GetUsbBootDriveLetter(string diskId)
        {
             return await System.Threading.Tasks.Task.Run(() =>
             {
                 try
                 {
                     if (!int.TryParse(Sanitize(diskId), out int diskNumber))
                     {
                         return null;
                     }

                     var psi = new System.Diagnostics.ProcessStartInfo();
                     psi.FileName = "powershell.exe";
                     string script = "$ErrorActionPreference='SilentlyContinue'; "
                         + $"$diskNumber={diskNumber}; "
                         + "$parts=Get-Partition -DiskNumber $diskNumber | Where-Object { $_.Type -ne 'Unknown' -and $_.Type -ne 'Reserved' }; "
                         + "foreach($part in $parts) { if (-not $part.DriveLetter) { $part | Set-Partition -AssignDriveLetter -ErrorAction SilentlyContinue } }; "
                         + "$match = Get-Partition -DiskNumber $diskNumber | Where-Object { $_.DriveLetter -and $_.Type -ne 'Unknown' -and $_.Type -ne 'Reserved' } | ForEach-Object { $vol = Get-Volume -DriveLetter $_.DriveLetter -ErrorAction SilentlyContinue; if ($vol -and ($vol.FileSystemLabel -eq 'Ventoy' -or $vol.FileSystemLabel -eq 'USB_BOOT')) { $_.DriveLetter } } | Select-Object -First 1; "
                         + "$match";
                     psi.Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"";
                     psi.RedirectStandardOutput = true;
                     psi.UseShellExecute = false;
                     psi.CreateNoWindow = true;
                     var p = System.Diagnostics.Process.Start(psi);
                     string output = p.StandardOutput.ReadToEnd();
                     p.WaitForExit();
                     
                     if (!string.IsNullOrWhiteSpace(output))
                     {
                         var lines = output.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                         foreach(var line in lines)
                         {
                             string letter = line.Trim();
                             if (letter.Length == 1 && char.IsLetter(letter[0]))
                             {
                                 return letter + ":\\";
                             }
                         }
                     }
                 }
                 catch { }
                 return null;
             });
        }

        private async System.Threading.Tasks.Task<System.Collections.Generic.List<DiskInfo>> GetPhysicalDisksAsync()
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                var list = new System.Collections.Generic.List<DiskInfo>();
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo();
                    psi.FileName = "powershell.exe";
                    psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-PhysicalDisk | Sort-Object DeviceID | Select-Object DeviceID, FriendlyName, Model, MediaType, Size | ConvertTo-Csv -NoTypeInformation\"";
                    psi.RedirectStandardOutput = true;
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;

                    using (var process = System.Diagnostics.Process.Start(psi))
                    {
                        if (process != null)
                        {
                            var output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();

                            var lines = output.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                            // Skip header row
                            for (int i = 1; i < lines.Length; i++)
                            {
                                var line = lines[i];
                                var parts = line.Split(',');
                                if (parts.Length >= 5)
                                {
                                    string id = Sanitize(parts[0]);
                                    string name = Sanitize(parts[1]);
                                    string model = Sanitize(parts[2]);
                                    string type = Sanitize(parts[3]);
                                    string sizeStr = Sanitize(parts[4]);
                                    
                                    if (string.IsNullOrEmpty(name)) name = model; 
                                    if (string.IsNullOrEmpty(name)) name = "Unknown Disk";

                                    string display = "";
                                    if (long.TryParse(sizeStr, out long sizeBytes))
                                    {
                                        double gb = sizeBytes / (1024.0 * 1024.0 * 1024.0);
                                        display = $"{name} - {gb:F1} GB";
                                    }
                                    else
                                    {
                                         display = $"{name}";
                                    }
                                    list.Add(new DiskInfo { DeviceId = id, Display = display });
                                }
                            }
                        }
                    }
                }
                catch { }
                return list;
            });
        }
        
        public class DiskInfo
        {
            public string DeviceId { get; set; }
            public string Display { get; set; }
            public override string ToString() => Display;
        }

        private string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            // Remove control characters (including \r \n \t) and quotes
            string cleaned = System.Text.RegularExpressions.Regex.Replace(input, @"[\p{C}]", ""); 
            return cleaned.Trim().Trim('"').Trim();
        }

        private void ShowMessage(string message, string title)
        {
            var win = new ModernConfirmationWindow(message, title, Helpers.LocalizationService.Get("btnOK"), null);
            win.Owner = System.Windows.Window.GetWindow(this);
            win.ShowDialog();
        }
    }
}
