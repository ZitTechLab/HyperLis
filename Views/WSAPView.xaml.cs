using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using HyperLisUI.Helpers;
using HyperLisUI.Models;
using HyperLisUI.Services;
using System.IO;

namespace HyperLisUI.Views
{
    public partial class WSAPView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ObservableCollection<DiskInfo> Disks { get; set; } = new();
        public ObservableCollection<PartitionInfo> Partitions { get; set; } = new();
        public ObservableCollection<PartitionInfo> EfiPartitions { get; set; } = new();
        public ObservableCollection<PartitionInfo> BootPartitions { get; set; } = new();
        public ObservableCollection<string> WindowsEditions { get; set; } = new ObservableCollection<string> { Helpers.LocalizationService.Get("wsapPlaceholderIso") };

        public WSAPView()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += WSAPView_Loaded;
            
            // Default selection to placeholder, ensure UI is updated
            EditionBox.SelectedIndex = 0;
            EditionBox.IsEnabled = false; // Disable until ISO selected
        }

        private void ApplyTranslations()
        {
            // Headers & Cards
            WsapHeaderDestination.Text = Helpers.LocalizationService.Get("wsapHeaderDestination");
            WsapCardDiskTitle.Text = Helpers.LocalizationService.Get("wsapCardDiskTitle");
            WsapCardDiskDesc.Text = Helpers.LocalizationService.Get("wsapCardDiskDesc");
            
            WsapHeaderPartition.Text = Helpers.LocalizationService.Get("wsapHeaderPartition");
            WsapTablePartId.Text = Helpers.LocalizationService.Get("wsapTablePartId");
            WsapTableType.Text = Helpers.LocalizationService.Get("wsapTableType");
            WsapTableSize.Text = Helpers.LocalizationService.Get("wsapTableSize");
            WsapTableStyle.Text = Helpers.LocalizationService.Get("wsapTableStyle");
            WsapTableFileSystem.Text = Helpers.LocalizationService.Get("wsapTableFileSystem");
            WsapTableDrive.Text = Helpers.LocalizationService.Get("wsapTableDrive");
            WsapTableInfo.Text = Helpers.LocalizationService.Get("wsapTableInfo");
            
            WsapWarningDynamic.Text = Helpers.LocalizationService.Get("wsapWarningDynamic");
            
            WsapCardEfiTitle.Text = Helpers.LocalizationService.Get("wsapCardEfiTitle");
            WsapCardEfiDesc.Text = Helpers.LocalizationService.Get("wsapCardEfiDesc");
            WsapCardBootTitle.Text = Helpers.LocalizationService.Get("wsapCardBootTitle");
            WsapCardBootDesc.Text = Helpers.LocalizationService.Get("wsapCardBootDesc");
            
            BtnReclaimX.Content = Helpers.LocalizationService.Get("btnReclaimX") ?? "Reclaim Drive X";
            
            WsapHeaderVersion.Text = Helpers.LocalizationService.Get("wsapHeaderVersion");
            WsapCardVersionTitle.Text = Helpers.LocalizationService.Get("wsapCardVersionTitle");
            WsapCardVersionDesc.Text = Helpers.LocalizationService.Get("wsapCardVersionDesc");
            
            WsapHeaderDriver.Text = Helpers.LocalizationService.Get("wsapHeaderDriver");
            WsapHeaderUnattend.Text = Helpers.LocalizationService.Get("wsapHeaderUnattend");
            
            WsapCardAdvancedTitle.Text = Helpers.LocalizationService.Get("wsapCardAdvancedTitle");
            WsapCardAdvancedDesc.Text = Helpers.LocalizationService.Get("wsapCardAdvancedDesc");
            
            // Buttons
            WindowsISOBrowseBtn.Content = Helpers.LocalizationService.Get("wsapBtnBrowse");
            DriverLocationBrowseBtn.Content = Helpers.LocalizationService.Get("wsapBtnBrowse");
            UnattendFileBrowseBtn.Content = Helpers.LocalizationService.Get("wsapBtnBrowse");
            BtnStart.Content = Helpers.LocalizationService.Get("wsapBtnStart");
            
            // Checkboxes
            ChkAutoFormat.Content = Helpers.LocalizationService.Get("wsapChkAutoFormat");
            ChkAutoRestart.Content = Helpers.LocalizationService.Get("wsapChkAutoRestart");
            ChkAddBootMenu.Content = Helpers.LocalizationService.Get("wsapChkAddBootMenu");
            ChkBypassWin11.Content = Helpers.LocalizationService.Get("wsapChkBypassWin11");
            ChkBypassNRO.Content = Helpers.LocalizationService.Get("wsapChkBypassNRO");
            
            // Placeholders
            WindowsISOTextBox.Text = Helpers.LocalizationService.Get("wsapPlaceholderIso");
            WindowsISOTextBox.Tag = Helpers.LocalizationService.Get("wsapPlaceholderIso");
            DriverLocationTextBox.Text = Helpers.LocalizationService.Get("wsapPlaceholderDriver");
            UnattendFileTextBox.Text = Helpers.LocalizationService.Get("wsapPlaceholderUnattend");
            
            // Update Edition Placeholder if it is currently set to placeholder
            if (WindowsEditions.Count == 1)
            {
                 WindowsEditions[0] = Helpers.LocalizationService.Get("wsapPlaceholderIso");
            }
        }

        private async void WSAPView_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTranslations();
            
            if (Disks.Count == 0) // Only load if empty
            {
                 await ReloadDisks();
            }
            
            // Ensure placeholder is selected if list is empty or just placeholder
            if (WindowsEditions.Count == 1 && WindowsEditions[0] == Helpers.LocalizationService.Get("wsapPlaceholderIso"))
            {
                EditionBox.SelectedIndex = 0;
            }
        }

        // ... existing ReloadDisks ...

        private async void WindowsISOBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Windows ISO",
                Filter = "ISO Files (*.iso)|*.iso|All files (*.*)|*.*",
                Multiselect = false
            };
            if (dialog.ShowDialog() == true)
            {
                WindowsISOTextBox.Text = dialog.FileName;
                WindowsISOTextBox.Foreground = System.Windows.Media.Brushes.Black;
                
                // Load Editions
                EditionBox.IsEnabled = false;
                WindowsEditions.Clear();
                WindowsEditions.Add(Helpers.LocalizationService.Get("msgWsapLoadingEditions"));
                EditionBox.SelectedIndex = 0;

                string isoPath = dialog.FileName;
                var editions = await Helpers.IsoHelper.GetWindowsEditionsAsync(isoPath);

                WindowsEditions.Clear();
                if (editions != null && editions.Count > 0)
                {
                    foreach (var ed in editions) WindowsEditions.Add(ed);
                    EditionBox.SelectedIndex = 0;
                    EditionBox.IsEnabled = true;
                }
                else
                {
                    WindowsEditions.Add("No editions found or failed to read ISO");
                    EditionBox.SelectedIndex = 0;
                    EditionBox.IsEnabled = false;
                }
            }
            else
            {
                // If user cancels and box is empty/placeholder, revert editions
                if (string.IsNullOrWhiteSpace(WindowsISOTextBox.Text) || WindowsISOTextBox.Text == Helpers.LocalizationService.Get("wsapPlaceholderIso"))
                {
                     WindowsEditions.Clear();
                     WindowsEditions.Add(Helpers.LocalizationService.Get("wsapPlaceholderIso"));
                     EditionBox.SelectedIndex = 0;
                     EditionBox.IsEnabled = false;
                }
            }
        }

        public async Task ReloadDisks()
        {
            Disks.Clear();
            Disks.Add(new DiskInfo { Model = Helpers.LocalizationService.Get("msgWsapLoadingDrives"), Index = -1 });
            DiskBox.SelectedIndex = 0;
            DiskBox.IsEnabled = false;

            var disks = await DiskHelper.GetPhysicalDisksAsync();
            
            // Fetch all partitions and styles globally to improve reliability and speed
            var allPartitions = await DiskHelper.GetAllPartitionsAsync();
            var allStyles = await DiskHelper.GetAllDiskStylesAsync();

            foreach (var d in disks)
            {
                if (d.Index >= 0)
                {
                    // Map partitions to this disk
                    d.Partitions = allPartitions.Where(p => p.DiskIndex == d.Index).ToList();
                    
                    // Fallback for Dynamic Disks or failures (DiskPart)
                    if (d.Partitions.Count == 0)
                    {
                         // Try DiskPart fallback
                         var manualParts = await DiskHelper.GetPartitionsUsingDiskPartAsync(d.Index);
                         if (manualParts.Count > 0)
                         {
                             d.Partitions = manualParts;
                             d.Style = "Dynamic"; // Assume dynamic if regular scan failed but diskpart worked
                         }
                    }

                    // Map style to this disk
                    if (d.Style != "Dynamic") // Only map if not already set by fallback
                    {
                        if (allStyles.TryGetValue(d.Index, out string style))
                        {
                            d.Style = style;
                        }
                        else
                        {
                            d.Style = "Unknown";
                        }
                    }
                }
            }

            // Sort disks by Index (natural order)
            var sortedDisks = disks
                .Where(d => d.Index >= 0)
                .OrderBy(d => d.Index)
                .ToList();

            Disks.Clear();
            foreach (var d in sortedDisks) Disks.Add(d);
            
            if (Disks.Count > 0)
            {
                DiskBox.IsEnabled = true;
                
                // Auto-select the first compatible (non-Dynamic) disk
                var compatibleDisk = Disks.FirstOrDefault(d => d.Style != "Dynamic");
                if (compatibleDisk != null)
                {
                    DiskBox.SelectedItem = compatibleDisk;
                }
                else
                {
                    DiskBox.SelectedIndex = 0; 
                }
            }
            else
            {
                Disks.Add(new DiskInfo { Model = "No disks found", Index = -1 });
                DiskBox.SelectedIndex = 0;
                DiskBox.IsEnabled = false;
            }
        }

        private Visibility _dynamicWarningVisibility = Visibility.Collapsed;
        public Visibility DynamicWarningVisibility
        {
            get => _dynamicWarningVisibility;
            set { _dynamicWarningVisibility = value; OnPropertyChanged(); }
        }

        private Visibility _selectionVisibility = Visibility.Visible;
        public Visibility SelectionVisibility
        {
            get => _selectionVisibility;
            set { _selectionVisibility = value; OnPropertyChanged(); }
        }

        private void DiskBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DiskBox.SelectedItem is DiskInfo selectedDisk && selectedDisk.Index >= 0)
            {
                // Check for dynamic disk
                bool isDynamic = selectedDisk.Style == "Dynamic";
                DynamicWarningVisibility = isDynamic ? Visibility.Visible : Visibility.Collapsed;
                SelectionVisibility = isDynamic ? Visibility.Collapsed : Visibility.Visible;

                // Use cached partitions
                Partitions.Clear();
                EfiPartitions.Clear();
                BootPartitions.Clear();

                if (selectedDisk.Partitions != null)
                {
                    foreach (var p in selectedDisk.Partitions)
                    {
                        p.Style = selectedDisk.Style; // Inherit style
                        Partitions.Add(p);
                        
                        // Add to EFI/Boot selection logic...
                        // Simple filtering here or you can allow all
                        EfiPartitions.Add(p);
                        BootPartitions.Add(p);
                    }
                }
                
                // Auto-select EFI and Boot if possible
                var efi = Partitions.FirstOrDefault(p => p.Info.Contains("System")) ?? Partitions.FirstOrDefault(p => p.FileSystem == "FAT32");
                if (efi != null) EfiBox.SelectedItem = efi;

                var boot = Partitions.FirstOrDefault(p => p.Info.Contains("Boot")) ?? Partitions.FirstOrDefault(p => p.Type == "Primary" && !string.IsNullOrEmpty(p.DriveLetter));
                if (boot != null) BootBox.SelectedItem = boot;
            }
        }



        private void DriverLocationBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Driver Directory",
                Multiselect = false
            };
            if (dialog.ShowDialog() == true)
            {
                DriverLocationTextBox.Text = dialog.FolderName;
            }
        }

        private void UnattendFileBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
             var dialog = new OpenFileDialog
            {
                Title = "Select Unattend File",
                Filter = "XML Files (*.xml)|*.xml|JSON Files (*.json)|*.json|All files (*.*)|*.*",
                Multiselect = false
            };
            if (dialog.ShowDialog() == true)
            {
                UnattendFileTextBox.Text = dialog.FileName;
            }
        }

        private void WindowsISOTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Tag is string placeholder && tb.Text == placeholder)
            {
                tb.Text = string.Empty;
                tb.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void WindowsISOTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Tag is string placeholder && string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.Text = placeholder;
                tb.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#555"));
            }
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validate Inputs
            if (DiskBox.SelectedItem is not DiskInfo selectedDisk || selectedDisk.Index < 0)
            {
                ShowMessage(Helpers.LocalizationService.Get("msgWsapNoDisk"), Helpers.LocalizationService.Get("msgError"));
                return;
            }

            if (selectedDisk.Style == "Dynamic")
            {
                ShowMessage(Helpers.LocalizationService.Get("msgWsapDynamicCheck"), Helpers.LocalizationService.Get("msgError"));
                return;
            }

            string isoPath = WindowsISOTextBox.Text;
            if (string.IsNullOrWhiteSpace(isoPath) || !File.Exists(isoPath) || isoPath == (string)WindowsISOTextBox.Tag)
            {
                ShowMessage(Helpers.LocalizationService.Get("msgWsapNoIso"), Helpers.LocalizationService.Get("msgError"));
                return; // Fixed ISO typo logic to use Get
            }

            if (EditionBox.Items.Count == 0 || EditionBox.SelectedItem == null || EditionBox.SelectedItem.ToString() == Helpers.LocalizationService.Get("wsapPlaceholderIso") || EditionBox.SelectedItem.ToString() == Helpers.LocalizationService.Get("msgWsapLoadingEditions"))
            {
                ShowMessage(Helpers.LocalizationService.Get("msgWsapNoEdition"), Helpers.LocalizationService.Get("msgError"));
                return;
            }

            // Target Partition Validation
            var targetPart = BootBox.SelectedItem as PartitionInfo;
            if (targetPart == null)
            {
                ShowMessage(Helpers.LocalizationService.Get("msgWsapNoBoot"), Helpers.LocalizationService.Get("msgError"));
                return;
            }

            // Pre-flight Security Preconditions Check (BitLocker & Secure Boot)
            var secResult = await DiskHelper.CheckSecurityPreconditionsAsync();
            if (!secResult.IsPassed)
            {
                var errors = new List<string>();
                if (!secResult.IsBitLockerDisabled)
                {
                    string drives = secResult.BitLockerActiveDrives.Count > 0 
                        ? string.Join(", ", secResult.BitLockerActiveDrives) 
                        : Helpers.LocalizationService.Get("msgWsapBitlockerActiveSystem");
                    errors.Add(string.Format(Helpers.LocalizationService.Get("msgWsapBitlockerActive"), drives));
                }
                if (!secResult.IsSecureBootDisabled)
                {
                    errors.Add(Helpers.LocalizationService.Get("msgWsapSecureBootActive"));
                }

                string errorMsg = Helpers.LocalizationService.Get("msgWsapSecurityHeader") + "\n\n" +
                    string.Join("\n\n", errors) +
                    "\n\n" + Helpers.LocalizationService.Get("msgWsapSecurityCheckCancelled");

                ShowMessage(errorMsg, Helpers.LocalizationService.Get("msgWsapSecurityCheckFailedTitle"));
                return;
            }

            // Confirm
            string securityStatus = Helpers.LocalizationService.Get("msgWsapSecurityPassedHeader");
            var confirmMsg = securityStatus + string.Format(Helpers.LocalizationService.Get("msgWsapConfirmShrinkBody"), $"{selectedDisk.Index} ({selectedDisk.Model})");
            var confirmTitle = Helpers.LocalizationService.Get("msgWsapConfirmShrinkTitle");
            var btnYes = Helpers.LocalizationService.Get("btnYes");
            var btnNo = Helpers.LocalizationService.Get("btnNo");
            
            var confirmWindow = new ModernConfirmationWindow(confirmMsg, confirmTitle, btnYes, btnNo);
            confirmWindow.Owner = Window.GetWindow(this);
            
            if (confirmWindow.ShowDialog() != true) return;

            var warnMsg = Helpers.LocalizationService.Get("msgWsapWarningsBody");
            var warnTitle = Helpers.LocalizationService.Get("msgWsapWarningsTitle");
            var warnYes = Helpers.LocalizationService.Get("btnWsapYesUnderstand");
            var warnNo = Helpers.LocalizationService.Get("btnCancel");

            var warningWindow = new ModernConfirmationWindow(warnMsg, warnTitle, warnYes, warnNo);
            warningWindow.Owner = Window.GetWindow(this);
            
            if (warningWindow.ShowDialog() != true) return;

            // 2. Prepare Options
            var options = new InstallationOptions
            {
                // Source
                WinSource = "ISO", 
                WimIndex = EditionBox.SelectedIndex + 1,
                WimName = EditionBox.SelectedItem.ToString(),
                
                // Target
                RecommendedDisk = selectedDisk.Index,
                RecommendedOSPart = targetPart.PartitionIndex,
                RecommendedEFIPart = (EfiBox.SelectedItem as PartitionInfo)?.PartitionIndex ?? 0,
                
                // Advanced
                AutoFormat = ChkAutoFormat.IsChecked == true,
                AutoRestart = ChkAutoRestart.IsChecked == true,
                AutoBypassWindows11 = ChkBypassWin11.IsChecked == true,
                AutoBypassNRO = ChkBypassNRO.IsChecked == true,
                AutoOtherWindowsBootMenu = ChkAddBootMenu.IsChecked == true,
                
                // Firmware
                FirmwareType = await GetFirmwareTypeAsync(selectedDisk.Style)
            };

            // Resolve Drivers
            if (Directory.Exists(DriverLocationTextBox.Text))
            {
                string path = DriverLocationTextBox.Text;
                string root = Path.GetPathRoot(path);
                if (!string.IsNullOrEmpty(root))
                {
                    string driveLetter = root.TrimEnd('\\', ':');
                    var partInfo = await DiskHelper.GetPartitionInfoByDriveLetterAsync(driveLetter);
                    if (partInfo != null)
                    {
                        options.DriversDisk = partInfo.DiskIndex;
                        options.DriversPartition = partInfo.PartitionIndex;
                        options.DriversPathNoDrive = path.Substring(root.Length - 1);
                    }
                }
            }

             // Resolve Unattend
            string unattendFile = UnattendFileTextBox.Text;
            if (File.Exists(unattendFile) && unattendFile != "Leave it blank if you want to configure by yourself")
            {
                options.SourceUnattendPath = unattendFile;
            }

            // 3. Start Process
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null) mainWindow.ShowLoading("Starting...", 0, 0);
            
            BtnStart.IsEnabled = false;
            
            try
            {
                var service = new InstallerService();
                var progress = new Progress<InstallProgressReport>(p => 
                {
                    Dispatcher.Invoke(() => {
                        if (mainWindow != null)
                        {
                            if (p.IsDetailed)
                            {
                                mainWindow.UpdateDetailedProgress(0, p.OverallPercentage, p.FilePercentage, p.Message, p.CurrentFileName, p.TransferredSize, p.Speed);
                            }
                            else
                            {
                                mainWindow.UpdateLoadingProgress(0, p.OverallPercentage, p.Message);
                            }
                        }
                    });
                });

                await service.StartInstallationAsync(options, isoPath, progress);
                
                if (options.AutoRestart)
                {
                    if (mainWindow != null) mainWindow.UpdateLoadingProgress(0, 100, "Restarting...");
                    await Task.Delay(2000);
                    Process.Start("shutdown", "/r /t 0");
                }
                else
                {
                    if (mainWindow != null) mainWindow.HideLoading();
                    ShowMessage(Helpers.LocalizationService.Get("msgWsapSuccessBody"), Helpers.LocalizationService.Get("msgWsapSuccessTitle"));
                    BtnStart.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                if (mainWindow != null) mainWindow.HideLoading();
                ShowMessage($"Installation Failed: {ex.Message}", Helpers.LocalizationService.Get("lblError"));
                BtnStart.IsEnabled = true;
            }
        }

        private async void BtnReclaimX_Click(object sender, RoutedEventArgs e)
        {
            var confirmMsg = "Are you sure you want to reclaim Drive X? This will delete the X drive and extend the preceding partition.";
            var confirmTitle = "Confirm Reclaim Drive X";
            var btnYes = "Yes, Reclaim";
            var btnNo = "Cancel";

            var confirmWindow = new ModernConfirmationWindow(confirmMsg, confirmTitle, btnYes, btnNo);
            confirmWindow.Owner = Window.GetWindow(this);
            
            if (confirmWindow.ShowDialog() != true) return;

            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null) mainWindow.ShowLoading("Reclaiming Drive X...", 1, 0);
            
            BtnReclaimX.IsEnabled = false;

            try
            {
                await ReclaimDriveXAsync();
                ShowMessage("Drive X successfully reclaimed!", "Success");
            }
            catch (Exception ex)
            {
                ShowMessage($"Failed to reclaim Drive X: {ex.Message}", "Error");
            }
            finally
            {
                if (mainWindow != null) mainWindow.HideLoading();
                BtnReclaimX.IsEnabled = true;
            }
        }

        private async Task ReclaimDriveXAsync()
        {
            // 1. Remove Boot Entries for X:
            string bcdCommand = @"$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';
            $bcd = bcdedit /enum all
            $entriesToDelete = @()
            $currentId = ''
            foreach ($line in $bcd) {
                if ($line -match 'identifier\s+(.+)') {
                    $currentId = $matches[1].Trim()
                }
                if ($line -match 'partition=X:') {
                    if ($currentId) { $entriesToDelete += $currentId }
                }
            }
            foreach ($id in $entriesToDelete) {
                bcdedit /delete $id /f
            }
            ";
            string bcdCommandBase64 = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(bcdCommand));
            var bcdResult = await Task.Run(() => Utilities.RunCommandWithOutput($"powershell -NoProfile -ExecutionPolicy Bypass -EncodedCommand {bcdCommandBase64}"));
            if (bcdResult.ExitCode != 0)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing BCD entries: {bcdResult.Error}");
            }

            // 2. Identify Disks and Partitions, Delete X, and Extend previous partition
            string script = @"$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';
            $xVol = Get-Partition -DriveLetter X -ErrorAction SilentlyContinue
            if (!$xVol) { throw 'Drive X not found. Already reclaimed or not created yet.' }
            
            $diskNum = $xVol.DiskNumber
            $xOffset = $xVol.Offset
            
            # Get all partitions on this disk sorted by offset
            $allParts = Get-Partition -DiskNumber $diskNum | Sort-Object Offset
            
            # Find the partition immediately before X
            $prevPart = $null
            for ($i = 1; $i -lt $allParts.Count; $i++) {
                if ($allParts[$i].Offset -eq $xOffset) {
                    $prevPart = $allParts[$i-1]
                    break
                }
            }
            
            if (!$prevPart) { throw 'Could not find a preceding partition on this disk to extend into the vacated space.' }
            
            $prevPartNum = $prevPart.PartitionNumber
            
            # Remove Drive X
            Remove-Partition -DriveLetter X -Confirm:$false
            
            # Give Windows a moment to update partition table state
            Start-Sleep -Seconds 1
            
            # Extend preceding partition to max
            $maxSize = (Get-PartitionSupportedSize -DiskNumber $diskNum -PartitionNumber $prevPartNum).SizeMax
            Resize-Partition -DiskNumber $diskNum -PartitionNumber $prevPartNum -Size $maxSize
            ";
            string scriptBase64 = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
            var result = await Task.Run(() => Utilities.RunCommandWithOutput($"powershell -NoProfile -ExecutionPolicy Bypass -EncodedCommand {scriptBase64}"));
            
            if (result.ExitCode != 0)
            {
                // Clean up XML-like progress records if they still appear
                string cleanError = System.Text.RegularExpressions.Regex.Replace(result.Error ?? result.Output, "<[^>]+>", "").Trim();
                throw new Exception(string.IsNullOrWhiteSpace(cleanError) ? "An unknown error occurred during partition resize." : cleanError);
            }
        }

        private void ShowMessage(string message, string title)
        {
            Components.CustomMessageBoxWindow.Show(message, title);
        }

        private async Task<string> GetFirmwareTypeAsync(string diskStyle)
        {
            // Prefer detection
            try 
            {
                 var psi = new ProcessStartInfo("bcdedit", "/enum {current}")
                 {
                     RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
                 };
                 using (var p = Process.Start(psi))
                 {
                     string outT = await p.StandardOutput.ReadToEndAsync();
                     await p.WaitForExitAsync();
                     if (outT.Contains("winload.efi", StringComparison.OrdinalIgnoreCase)) return "UEFI";
                     if (outT.Contains("winload.exe", StringComparison.OrdinalIgnoreCase)) return "BIOS";
                 }
            } catch {}

            // Fallback to Disk Style
            if (diskStyle == "GPT") return "UEFI";
            if (diskStyle == "MBR") return "BIOS";
            
            return "UEFI"; // Default
        }

        private void ResolvePathToOption(string fullPath, out string disk, out string part, out string pathNoDrive)
        {
            disk = ""; part = ""; pathNoDrive = "";
            try
            {
                string root = Path.GetPathRoot(fullPath); // "D:\"
                if (string.IsNullOrEmpty(root)) return;
                
                string driveLetter = root.TrimEnd('\\').TrimEnd(':'); // "D"
                
                pathNoDrive = fullPath.Substring(root.Length - 1); // "\Folder"
            }
            catch {}
        }
    }
}
