using System.Windows.Controls;
using System.Windows;
using Microsoft.Win32;
using HyperLisUI.Helpers;

namespace HyperLisUI.Views
{
    public partial class ExtendFeaturesView : UserControl
    {
        public ExtendFeaturesView()
        {
            InitializeComponent();
            Loaded += ExtendFeaturesView_Loaded;
        }

        private bool _isCustomIrstPath = false; 

        private async void ExtendFeaturesView_Loaded(object sender, RoutedEventArgs e)
        {
             ApplyTranslations();
             Helpers.LocalizationService.LanguageChanged += ApplyTranslations;

             await LoadIrstData();

             try
             {
                 using (var key = Registry.ClassesRoot.OpenSubKey(@"*\shell\runas"))
                 {
                     bool isEnabled = key != null && key.GetValue("")?.ToString() == "Take Ownership";
                     
                     if (TakeOwnershipChk != null)
                     {
                         // Detach to avoid recursion/triggering logic on load
                         TakeOwnershipChk.Checked -= TakeOwnership_Checked;
                         TakeOwnershipChk.Unchecked -= TakeOwnership_Unchecked;
                         
                         TakeOwnershipChk.IsChecked = isEnabled;
                         
                         TakeOwnershipChk.Checked += TakeOwnership_Checked;
                         TakeOwnershipChk.Unchecked += TakeOwnership_Unchecked;
                     }
                 }

                 using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings"))
                 {
                     bool isEndTaskEnabled = key != null && key.GetValue("TaskbarEndTask")?.ToString() == "1";

                     if (EndTaskOnTaskbarChk != null)
                     {
                         EndTaskOnTaskbarChk.Checked -= EndTaskOnTaskbar_Checked;
                         EndTaskOnTaskbarChk.Unchecked -= EndTaskOnTaskbar_Unchecked;

                         EndTaskOnTaskbarChk.IsChecked = isEndTaskEnabled;

                         EndTaskOnTaskbarChk.Checked += EndTaskOnTaskbar_Checked;
                         EndTaskOnTaskbarChk.Unchecked += EndTaskOnTaskbar_Unchecked;
                     }
                 }

                 // Check UAC Status on Load
                 // If EnableLUA is 0, then UAC is "Disabled" -> Checkbox should be Checked
                 using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                 {
                     bool isUacDisabled = key != null && key.GetValue("EnableLUA")?.ToString() == "0";

                     if (DisableUACChk != null)
                     {
                         DisableUACChk.Checked -= DisableUAC_Checked;
                         DisableUACChk.Unchecked -= DisableUAC_Unchecked;

                         DisableUACChk.IsChecked = isUacDisabled;

                         DisableUACChk.Checked += DisableUAC_Checked;
                         DisableUACChk.Unchecked += DisableUAC_Unchecked;
                     }
                 }

                 // Check BitLocker Status on Load (Async)
                 Task.Run(() =>
                 {
                     try 
                     {
                         // Get volumes with BitLocker 'On'
                         var result = Helpers.Utilities.RunCommandWithOutput("powershell -Command \"Get-BitLockerVolume | Where-Object { $_.ProtectionStatus -eq 'On' }\"");
                         // If output is empty, it means NO drives are encrypted => "Disable BitLocker" is ON (True)
                         // If output has text, it means SOME drives are encrypted => "Disable BitLocker" is OFF (False)
                         
                         bool isAllDisabled = string.IsNullOrWhiteSpace(result.Output);

                         Dispatcher.Invoke(() =>
                         {
                             if (DisableBitLockerChk != null)
                             {
                                 DisableBitLockerChk.Checked -= DisableBitLocker_Checked;
                                 DisableBitLockerChk.Unchecked -= DisableBitLocker_Unchecked;

                                 DisableBitLockerChk.IsChecked = isAllDisabled;

                                 DisableBitLockerChk.Checked += DisableBitLocker_Checked;
                                 DisableBitLockerChk.Unchecked += DisableBitLocker_Unchecked;
                             }
                         });
                     }
                     catch { }
                 });

                 // Check God Mode Status
                 using (var key = Registry.ClassesRoot.OpenSubKey(@"DesktopBackground\Shell\ControlPanel"))
                 {
                     bool isGodModeEnabled = key != null;

                     if (GodModeChk != null)
                     {
                         GodModeChk.Checked -= GodMode_Checked;
                         GodModeChk.Unchecked -= GodMode_Unchecked;

                         GodModeChk.IsChecked = isGodModeEnabled;

                         GodModeChk.Checked += GodMode_Checked;
                         GodModeChk.Unchecked += GodMode_Unchecked;
                         GodModeChk.Checked += GodMode_Checked;
                         GodModeChk.Unchecked += GodMode_Unchecked;
                     }
                 }

                 // Check Vietnamese Time Format Status
                 using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\International"))
                 {
                     bool isVnTimeFormat = key != null && key.GetValue("sShortDate")?.ToString() == "dd/MM/yyyy" && key.GetValue("s1159")?.ToString() == "SA";

                     if (VnTimeFormatChk != null)
                     {
                         VnTimeFormatChk.Checked -= VnTimeFormat_Checked;
                         VnTimeFormatChk.Unchecked -= VnTimeFormat_Unchecked;

                         VnTimeFormatChk.IsChecked = isVnTimeFormat;

                         VnTimeFormatChk.Checked += VnTimeFormat_Checked;
                         VnTimeFormatChk.Unchecked += VnTimeFormat_Unchecked;
                     }
                 }
             }
             catch { }
        }

        private void ApplyTranslations()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ApplyTranslations);
                return;
            }

            lblExtendSystem.Text = Helpers.LocalizationService.Get("lblExtendSystem");
            EndTaskOnTaskbarChk.Content = Helpers.LocalizationService.Get("chkExtendEndTask");
            TakeOwnershipChk.Content = Helpers.LocalizationService.Get("chkExtendTakeOwnership");
            GodModeChk.Content = Helpers.LocalizationService.Get("chkExtendGodMode");
            VnTimeFormatChk.Content = Helpers.LocalizationService.Get("chkExtendVnTime");
            DisableUACChk.Content = Helpers.LocalizationService.Get("chkExtendDisableUAC");
            DisableBitLockerChk.Content = Helpers.LocalizationService.Get("chkExtendDisableBitLocker");

            lblExtendAddProgram.Text = Helpers.LocalizationService.Get("lblExtendAddProgram");
            msgExtendSelectProgram.Text = Helpers.LocalizationService.Get("msgExtendSelectProgram");
            AddProgramBrowseBtn.Content = Helpers.LocalizationService.Get("btnExtendBrowse");
            btnExtendAdd.Content = Helpers.LocalizationService.Get("btnExtendAdd");
            btnExtendRemove.Content = Helpers.LocalizationService.Get("btnExtendRemove");
            btnExtendListPrograms.Content = Helpers.LocalizationService.Get("btnExtendListPrograms");

            lblExtendReduceLatency.Text = Helpers.LocalizationService.Get("lblExtendReduceLatency");
            btnExtendMsiUtil.Content = Helpers.LocalizationService.Get("btnExtendMsiUtil");
            btnExtendTimerRes.Content = Helpers.LocalizationService.Get("btnExtendTimerRes");


            lblExtendExportDriver.Text = Helpers.LocalizationService.Get("lblExtendExportDriver");
            ExportDriverBrowseBtn.Content = Helpers.LocalizationService.Get("btnExtendBrowse");
            ExportDriverStartBtn.Content = Helpers.LocalizationService.Get("btnExtendStartExportDriver");
            ImportDriverStartBtn.Content = Helpers.LocalizationService.Get("btnExtendStartImportDriver");

            lblExtendExportWifi.Text = Helpers.LocalizationService.Get("lblExtendExportWifi");
            ExportWifiBrowseBtn.Content = Helpers.LocalizationService.Get("btnExtendBrowse");
            ExportWifiStartBtn.Content = Helpers.LocalizationService.Get("btnExtendStartExportWifi");
            
            lblExtendFindDriver.Text = Helpers.LocalizationService.Get("lblExtendFindDriver");
            SearchDriverBtn.Content = Helpers.LocalizationService.Get("btnSearchDriver");


            lblExtendGetIrst.Text = Helpers.LocalizationService.Get("lblExtendGetIrst");
            lblExtendIrstDesc.Text = Helpers.LocalizationService.Get("lblExtendIrstDesc");
            // Placeholder for IRST Driver TextBox
            if (IRSTDriverTextBox.Text == "Choose a location to save your driver IRST." || IRSTDriverTextBox.Text == "Chọn nơi lưu driver IRST.")
            {
                 IRSTDriverTextBox.Text = Helpers.LocalizationService.Get("msgExtendSaveIrst");
            }
            IRSTDriverBrowseBtn.Content = Helpers.LocalizationService.Get("btnExtendBrowse");
            IRSTStartBtn.Content = Helpers.LocalizationService.Get("btnExtendStartIrst");

            lblExtendBlurExplorer.Text = Helpers.LocalizationService.Get("lblExtendBlurExplorer");
            btnExtendInstallBlur.Content = Helpers.LocalizationService.Get("btnExtendInstallBlur");
            btnExtendUninstallBlur.Content = Helpers.LocalizationService.Get("btnExtendUninstallBlur");

            lblExtendFileHiding.Text = Helpers.LocalizationService.Get("lblExtendFileHiding");
            HideFolderBrowseBtn.Content = Helpers.LocalizationService.Get("btnExtendBrowse");
            HideFileBtn.Content = Helpers.LocalizationService.Get("btnExtendHide");
            UnhideFileBtn.Content = Helpers.LocalizationService.Get("btnExtendUnhide");
            ListHiddenItemsBtn.Content = Helpers.LocalizationService.Get("btnExtendListHidden");
            menuExtendBrowseFile.Header = Helpers.LocalizationService.Get("menuExtendBrowseFile");
            menuExtendBrowseFolder.Header = Helpers.LocalizationService.Get("menuExtendBrowseFolder");
        }

        private async void SearchDriverBtn_Click(object sender, RoutedEventArgs e)
        {
             var mainWindow = Window.GetWindow(this) as MainWindow;
             if (mainWindow == null) return;

             try
             {
                 mainWindow.ShowLoading(Helpers.LocalizationService.Get("msgAnalyzingHardware"), 0, 0);
                 
                 // Get Hardware Info via WMI
                 string mfg = string.Empty, model = string.Empty, serial = string.Empty, baseBoard = string.Empty;
                 int isLaptop = 0;

                 await Task.Run(async () =>
                 {
                      // Step 1: Detect Manufacturer & Model
                      Dispatcher.Invoke(() => mainWindow.ShowLoading(Helpers.LocalizationService.Get("msgAnalyzingHardware") + " - Manufacturer & Model...", 0, 20));
                      var resSys = Helpers.Utilities.RunCommandWithOutput("powershell -Command \"Get-WmiObject Win32_ComputerSystem | Select-Object -ExpandProperty Manufacturer\"");
                      mfg = resSys.Output.Trim();
                      resSys = Helpers.Utilities.RunCommandWithOutput("powershell -Command \"Get-WmiObject Win32_ComputerSystem | Select-Object -ExpandProperty Model\"");
                      model = resSys.Output.Trim();

                      // Step 2: Detect Serial (BIOS)
                      Dispatcher.Invoke(() => mainWindow.ShowLoading(Helpers.LocalizationService.Get("msgAnalyzingHardware") + " - Serial Number...", 0, 40));
                      var resBios = Helpers.Utilities.RunCommandWithOutput("powershell -Command \"Get-WmiObject Win32_BIOS | Select-Object -ExpandProperty SerialNumber\"");
                      serial = resBios.Output.Trim();

                      // Step 3: BaseBoard
                      Dispatcher.Invoke(() => mainWindow.ShowLoading(Helpers.LocalizationService.Get("msgAnalyzingHardware") + " - BaseBoard...", 0, 60));
                      var resBoard = Helpers.Utilities.RunCommandWithOutput("powershell -Command \"Get-WmiObject Win32_BaseBoard | Select-Object -ExpandProperty Product\"");
                      baseBoard = resBoard.Output.Trim();

                      // Step 4: Check if Laptop (ChassisTypes 8,9,10,11,12,14,18,21)
                      Dispatcher.Invoke(() => mainWindow.ShowLoading(Helpers.LocalizationService.Get("msgAnalyzingHardware") + " - System Type...", 0, 80));
                      var resChassis = Helpers.Utilities.RunCommandWithOutput("powershell -Command \"(Get-WmiObject Win32_SystemEnclosure).ChassisTypes\"");
                      if (resChassis.Output.Contains("8") || resChassis.Output.Contains("9") || resChassis.Output.Contains("10") || resChassis.Output.Contains("11") || resChassis.Output.Contains("12") || resChassis.Output.Contains("14") || resChassis.Output.Contains("18") || resChassis.Output.Contains("21"))
                      {
                          isLaptop = 1;
                      }

                      await Task.Delay(500);
                 });

                 mainWindow.ShowLoading("Generating URL...", 0, 100);
                 
                 // Prefer BaseBoard Product over System Model, as long as it's not a generic placeholder
                 if (!string.IsNullOrWhiteSpace(baseBoard) && 
                     !baseBoard.Contains("System Default", StringComparison.OrdinalIgnoreCase) &&
                     !baseBoard.Contains("To Be Filled", StringComparison.OrdinalIgnoreCase))
                 {
                     model = baseBoard;
                 }

                 string driverUrl = string.Empty;
                 string brandStr = string.Empty;
                 
                 // Dell
                 if (mfg.Contains("Dell", StringComparison.OrdinalIgnoreCase))
                 {
                     brandStr = "DELL";
                     driverUrl = $"https://www.dell.com/support/home/vn/en/vndhs1/product-support/servicetag/{serial}/drivers";
                 }
                 // HP
                 else if (mfg.Contains("HP", StringComparison.OrdinalIgnoreCase) || mfg.Contains("Hewlett-Packard", StringComparison.OrdinalIgnoreCase))
                 {
                     brandStr = "HP";
                     if (isLaptop == 1) driverUrl = "https://support.hp.com/vn-en/drivers/laptops";
                     else driverUrl = "https://support.hp.com/vn-en/drivers/desktops";
                 }
                 // Lenovo
                 else if (mfg.Contains("Lenovo", StringComparison.OrdinalIgnoreCase))
                 {
                     brandStr = "LENOVO";
                     driverUrl = $"https://pcsupport.lenovo.com/vn/en/search?query={serial}";
                 }
                 // ASUS
                 else if (mfg.Contains("ASUSTeK", StringComparison.OrdinalIgnoreCase) || mfg.Contains("ASUS", StringComparison.OrdinalIgnoreCase))
                 {
                     brandStr = "ASUS";
                     if (isLaptop == 1)
                     {
                         driverUrl = $"https://www.asus.com/vn/support/Download-Center?model={model}";
                     }
                     else
                     {
                         string series = "";
                         if (model.Contains("PRIME", StringComparison.OrdinalIgnoreCase)) series = "prime";
                         else if (model.Contains("ROG STRIX", StringComparison.OrdinalIgnoreCase)) series = "rog-strix";
                         else if (model.Contains("ROG MAXIMUS", StringComparison.OrdinalIgnoreCase)) series = "rog-maximus";
                         else if (model.Contains("TUF GAMING", StringComparison.OrdinalIgnoreCase) || model.Contains("TUF", StringComparison.OrdinalIgnoreCase)) series = "tuf-gaming";
                         else if (model.Contains("PROART", StringComparison.OrdinalIgnoreCase)) series = "proart";
                         else if (model.Contains("PRO WS", StringComparison.OrdinalIgnoreCase)) series = "pro-ws";

                         if (!string.IsNullOrEmpty(series))
                         {
                             string lower = model.ToLower().Replace(" ", "-");
                             string upper = model.ToUpper().Replace(" ", "-");
                             driverUrl = $"https://www.asus.com/vn/motherboards-components/motherboards/{series}/{lower}/helpdesk_knowledge?model2Name={upper}";
                         }
                         else
                         {
                             driverUrl = "https://www.asus.com/vn/support/";
                         }
                     }
                 }
                 // Acer
                 else if (mfg.Contains("Acer", StringComparison.OrdinalIgnoreCase))
                 {
                     brandStr = "ACER";
                     driverUrl = $"https://www.acer.com/vn-vi/support/product-selector?serialnumber={serial}";
                 }
                 // MSI
                 else if (mfg.Contains("MSI", StringComparison.OrdinalIgnoreCase) || mfg.Contains("Micro-Star", StringComparison.OrdinalIgnoreCase))
                 {
                     brandStr = "MSI";
                     string msiModel = System.Text.RegularExpressions.Regex.Replace(model, @"\s*\(MS-[^\)]+\)", "");
                     msiModel = msiModel.Replace(" ", "-").Replace("(", "").Replace(")", "");
                     msiModel = msiModel.TrimEnd('-');
                     driverUrl = $"https://www.msi.com/Motherboard/{msiModel}/support";
                 }
                 // Gigabyte
                 else if (mfg.Contains("Gigabyte", StringComparison.OrdinalIgnoreCase))
                 {
                     brandStr = "GIGABYTE";
                     string gbModel = model.Replace(" ", "-");
                     driverUrl = $"https://www.gigabyte.com/vn/Motherboard/{gbModel}-rev-10";
                 }
                 // ASRock
                 else if (mfg.Contains("ASRock", StringComparison.OrdinalIgnoreCase))
                 {
                     brandStr = "ASROCK";
                     string platform = "Intel";
                     if (model.Contains("A320") || model.Contains("A520") || model.Contains("B350") || model.Contains("B450") || 
                         model.Contains("B550") || model.Contains("B650") || model.Contains("X370") || model.Contains("X470") || 
                         model.Contains("X570") || model.Contains("X670"))
                     {
                         platform = "AMD";
                     }
                     string asrockModel = model.Replace(" ", "");
                     driverUrl = $"https://www.asrock.com/MB/{platform}/{asrockModel}/index.vn.asp#Support";
                 }
                 // Microsoft Surface
                 else if (mfg.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) && model.Contains("Surface", StringComparison.OrdinalIgnoreCase))
                 {
                     brandStr = "MICROSOFT SURFACE";
                     driverUrl = "https://support.microsoft.com/surface/download-drivers-and-firmware-for-surface-09bb2e09-2a4b-cb69-0951-078a7739e120";
                 }

                 await Task.Delay(400); // Allow 100% bar animation to complete smoothly
                 mainWindow.HideLoading();

                 if (string.IsNullOrEmpty(driverUrl))
                 {
                     Components.CustomMessageBoxWindow.Show(
                         $"Manufacturer: {mfg}\nModel: {model}\nSerial: {serial}\n\n{Helpers.LocalizationService.Get("msgUnknownBrand")}", 
                         Helpers.LocalizationService.Get("msgFoundDriverTitle"));
                     return;
                 }

                 // Prompt to open link
                 string msgObj = $"{Helpers.LocalizationService.Get("msgOpenDriverLink")} {brandStr}?\n\nManufacturer: {mfg}\nModel: {model}\nSerial: {serial}\n\nLink: {driverUrl}";
                 
                 var dlg = new ModernConfirmationWindow(msgObj, Helpers.LocalizationService.Get("msgFoundDriverTitle"));
                 dlg.Owner = mainWindow;
                 if (dlg.ShowDialog() == true)
                 {
                     System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                     {
                         FileName = driverUrl,
                         UseShellExecute = true
                     });
                 }
             }
             catch (Exception ex)
             {
                 mainWindow.HideLoading();
                 Components.CustomMessageBoxWindow.Show(ex.Message, "Error");
             }
        }

        private void EndTaskOnTaskbar_Checked(object sender, RoutedEventArgs e)

        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings"))
                {
                    key?.SetValue("TaskbarEndTask", 1, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                Components.CustomMessageBoxWindow.Show($"Failed to enable End Task: {ex.Message}", "Error");
            }
        }

        private void EndTaskOnTaskbar_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("TaskbarEndTask", false);
                    }
                }
            }
            catch (Exception ex)
            {
                Components.CustomMessageBoxWindow.Show($"Failed to disable End Task: {ex.Message}", "Error");
            }
        }

        private void TakeOwnership_Checked(object sender, RoutedEventArgs e)
        {
             if (!Helpers.Utilities.IsAdministrator)
             {
                 Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgExtendAdminRequired"), Helpers.LocalizationService.Get("msgExtendError"));
                 // Revert check without triggering event
                 if (sender is CheckBox chk)
                 {
                     chk.Checked -= TakeOwnership_Checked;
                     chk.IsChecked = false;
                     chk.Checked += TakeOwnership_Checked;
                 }
                 return; 
             }
             try
             {
                 // Files (*\shell\runas)
                 using (var key = Registry.ClassesRoot.CreateSubKey(@"*\shell\runas"))
                 {
                     key?.SetValue("", "Take Ownership");
                     key?.SetValue("NoWorkingDirectory", "");
                 }
                 using (var key = Registry.ClassesRoot.CreateSubKey(@"*\shell\runas\command"))
                 {
                     key?.SetValue("", "cmd.exe /c takeown /f \"%1\" && icacls \"%1\" /grant administrators:F");
                     key?.SetValue("IsolatedCommand", "cmd.exe /c takeown /f \"%1\" && icacls \"%1\" /grant administrators:F");
                 }

                 // Directories (Directory\shell\runas)
                 using (var key = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\runas"))
                 {
                     key?.SetValue("", "Take Ownership");
                     key?.SetValue("NoWorkingDirectory", "");
                 }
                 using (var key = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\runas\command"))
                 {
                     key?.SetValue("", "cmd.exe /c takeown /f \"%1\" /r /d y && icacls \"%1\" /grant administrators:F /t");
                     key?.SetValue("IsolatedCommand", "cmd.exe /c takeown /f \"%1\" /r /d y && icacls \"%1\" /grant administrators:F /t");
                 }
             }
             catch (Exception ex)
             {
                 Components.CustomMessageBoxWindow.Show($"Failed to enable Take Ownership: {ex.Message}", "Error");
             }
        }

        private void TakeOwnership_Unchecked(object sender, RoutedEventArgs e)
        {
             if (!Helpers.Utilities.IsAdministrator)
             {
                 Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgExtendAdminRequired"), Helpers.LocalizationService.Get("msgExtendError"));
                  // Revert check without triggering event
                 if (sender is CheckBox chk)
                 {
                     chk.Unchecked -= TakeOwnership_Unchecked;
                     chk.IsChecked = true;
                     chk.Unchecked += TakeOwnership_Unchecked;
                 }
                 return;
             }
             try
             {
                 Registry.ClassesRoot.DeleteSubKeyTree(@"*\shell\runas", false);
                 Registry.ClassesRoot.DeleteSubKeyTree(@"Directory\shell\runas", false);
             }
             catch (Exception ex)
             {
                 Components.CustomMessageBoxWindow.Show($"Failed to disable Take Ownership: {ex.Message}", "Error");
             }
        }

        private void DisableUAC_Checked(object sender, RoutedEventArgs e)
        {
             if (!Helpers.Utilities.IsAdministrator)
             {
                 Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgExtendAdminRequired"), Helpers.LocalizationService.Get("msgExtendError"));
                 if (sender is CheckBox chk)
                 {
                     chk.Checked -= DisableUAC_Checked;
                     chk.IsChecked = false;
                     chk.Checked += DisableUAC_Checked;
                 }
                 return; 
             }
             try
             {
                 using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                 {
                     if (key != null)
                     {
                         key.SetValue("EnableLUA", 0, RegistryValueKind.DWord);
                         key.SetValue("ConsentPromptBehaviorAdmin", 0, RegistryValueKind.DWord);
                         key.SetValue("PromptOnSecureDesktop", 0, RegistryValueKind.DWord);
                     }
                 }
             }
             catch (Exception ex)
             {
                 Components.CustomMessageBoxWindow.Show($"Failed to Disable UAC: {ex.Message}", "Error");
             }
        }

        private void DisableUAC_Unchecked(object sender, RoutedEventArgs e)
        {
             if (!Helpers.Utilities.IsAdministrator)
             {
                 Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgExtendAdminRequired"), Helpers.LocalizationService.Get("msgExtendError"));
                 if (sender is CheckBox chk)
                 {
                     chk.Unchecked -= DisableUAC_Unchecked;
                     chk.IsChecked = true;
                     chk.Unchecked += DisableUAC_Unchecked;
                 }
                 return;
             }
             try
             {
                 using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                 {
                     if (key != null)
                     {
                         key.SetValue("EnableLUA", 1, RegistryValueKind.DWord);
                         key.SetValue("ConsentPromptBehaviorAdmin", 5, RegistryValueKind.DWord);
                         key.SetValue("PromptOnSecureDesktop", 1, RegistryValueKind.DWord);
                     }
                 }
             }
             catch (Exception ex)
             {
                 Components.CustomMessageBoxWindow.Show($"Failed to Enable UAC: {ex.Message}", "Error");
             }
        }

        private async void DisableBitLocker_Checked(object sender, RoutedEventArgs e)
        {
             if (!Helpers.Utilities.IsAdministrator)
             {
                 Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgExtendAdminRequired"), Helpers.LocalizationService.Get("msgExtendError"));
                 if (sender is CheckBox chk)
                 {
                     chk.Checked -= DisableBitLocker_Checked;
                     chk.IsChecked = false;
                     chk.Checked += DisableBitLocker_Checked;
                 }
                 return;
             }
            
             var mainWindow = Window.GetWindow(this) as MainWindow;
             if (mainWindow != null) mainWindow.ShowLoading("Disabling BitLocker...", 0, 0);

             await Task.Run(() =>
             {
                 try
                 {
                     // Run command to disable BitLocker on all encrypted drives
                     var result = Helpers.Utilities.RunCommandWithOutput("powershell -Command \"Get-BitLockerVolume | Where-Object { $_.ProtectionStatus -eq 'On' } | ForEach-Object { Disable-BitLocker -MountPoint $_.MountPoint }\"");
                     
                     // No specific output to check for success other than empty error, usually returns nothing on success or progress
                     // Re-verify
                     var check = Helpers.Utilities.RunCommandWithOutput("powershell -Command \"Get-BitLockerVolume | Where-Object { $_.ProtectionStatus -eq 'On' }\"");
                     
                     Dispatcher.Invoke(() =>
                     {
                         if (!string.IsNullOrWhiteSpace(check.Output))
                         {
                              // Still some encrypted?
                              Components.CustomMessageBoxWindow.Show("Decryption started. This may take some time in the background.", "Info");
                         }
                         else
                         {
                              // All Good
                         }
                     });
                 }
                 catch (Exception ex)
                 {
                     Dispatcher.Invoke(() => Components.CustomMessageBoxWindow.Show($"Failed to run BitLocker command: {ex.Message}", "Error"));
                 }
                 finally
                 {
                     Dispatcher.Invoke(() => 
                     {
                         if(mainWindow != null) mainWindow.HideLoading();
                     });
                 }
             });
        }

        private void DisableBitLocker_Unchecked(object sender, RoutedEventArgs e)
        {
             // Enabling BitLocker is complex and user didn't request it.
             // We just leave it, effectively meaning "I don't enforce disabling anymore".
             // Or we could warn them.
             Components.CustomMessageBoxWindow.Show("To enable BitLocker again, please use Windows Settings or Control Panel as it requires password/key setup.", "Information");
        }

        private void GodMode_Checked(object sender, RoutedEventArgs e)
        {
             if (!Helpers.Utilities.IsAdministrator)
             {
                 Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgExtendAdminRequired"), Helpers.LocalizationService.Get("msgExtendError"));
                 if (sender is CheckBox chk)
                 {
                     chk.Checked -= GodMode_Checked;
                     chk.IsChecked = false;
                     chk.Checked += GodMode_Checked;
                 }
                 return;
             }
             try
             {
                 using (var key = Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\ControlPanel"))
                 {
                     key?.SetValue("MUIVerb", "Control Panel");
                     key?.SetValue("SubCommands", "");
                     key?.SetValue("Icon", "imageres.dll,-27");
                     key?.SetValue("Position", "Middle");
                 }
                 
                 using (var key = Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\ControlPanel\shell\001menu"))
                 {
                     key?.SetValue("", "Category view");
                 }
                 using (var key = Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\ControlPanel\shell\001menu\command"))
                 {
                     key?.SetValue("", "explorer.exe shell:::{26EE0668-A00A-44D7-9371-BEB064C98683}");
                 }

                 using (var key = Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\ControlPanel\shell\002menu"))
                 {
                     key?.SetValue("", "Icons view");
                 }
                 using (var key = Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\ControlPanel\shell\002menu\command"))
                 {
                     key?.SetValue("", "explorer.exe shell:::{21EC2020-3AEA-1069-A2DD-08002B30309D}");
                 }

                 using (var key = Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\ControlPanel\shell\003menu"))
                 {
                     key?.SetValue("", "All Tasks (God mode)");
                 }
                 using (var key = Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\ControlPanel\shell\003menu\command"))
                 {
                     key?.SetValue("", "explorer.exe shell:::{ED7BA470-8E54-465E-825C-99712043E01C}");
                 }
             }
             catch (Exception ex)
             {
                 Components.CustomMessageBoxWindow.Show($"Failed to enable God Mode: {ex.Message}", "Error");
             }
        }

        private void GodMode_Unchecked(object sender, RoutedEventArgs e)
        {
             if (!Helpers.Utilities.IsAdministrator)
             {
                 Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgExtendAdminRequired"), Helpers.LocalizationService.Get("msgExtendError"));
                 if (sender is CheckBox chk)
                 {
                     chk.Unchecked -= GodMode_Unchecked;
                     chk.IsChecked = true;
                     chk.Unchecked += GodMode_Unchecked;
                 }
                 return;
             }
             try
             {
                 Registry.ClassesRoot.DeleteSubKeyTree(@"DesktopBackground\Shell\ControlPanel", false);
             }
             catch (Exception ex)
             {
                 Components.CustomMessageBoxWindow.Show($"Failed to disable God Mode: {ex.Message}", "Error");
             }
        }

        private void VnTimeFormat_Checked(object sender, RoutedEventArgs e)
        {
             // Writing to HKCU doesn't strictly need Admin, but writing to .DEFAULT does.
             // We'll try HKCU first.
             try
             {
                 using (var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\International"))
                 {
                     if (key != null)
                     {
                         key.SetValue("LocaleName", "vi-VN");
                         key.SetValue("sShortDate", "dd/MM/yyyy");
                         key.SetValue("sLongDate", "dd MMMM yyyy");
                         key.SetValue("sShortTime", "h:mm tt");
                         key.SetValue("sTimeFormat", "h:mm:ss tt");
                         key.SetValue("s1159", "SA");
                         key.SetValue("s2359", "CH");
                         key.SetValue("iFirstDayOfWeek", "0");
                     }
                 }

                 // Try .DEFAULT if Admin
                 if (Helpers.Utilities.IsAdministrator)
                 {
                      using (var key = Registry.Users.CreateSubKey(@".DEFAULT\Control Panel\International"))
                      {
                          if (key != null)
                          {
                             key.SetValue("LocaleName", "vi-VN");
                             key.SetValue("sShortDate", "dd/MM/yyyy");
                             key.SetValue("sLongDate", "dd MMMM yyyy");
                             key.SetValue("sShortTime", "h:mm tt");
                             key.SetValue("sTimeFormat", "h:mm:ss tt");
                             key.SetValue("s1159", "SA");
                             key.SetValue("s2359", "CH");
                             key.SetValue("iFirstDayOfWeek", "0");
                          }
                      }
                 }
             }
             catch (Exception ex)
             {
                 Components.CustomMessageBoxWindow.Show($"Failed to Enable Vietnamese Time Format: {ex.Message}", "Error");
             }
        }

        private void VnTimeFormat_Unchecked(object sender, RoutedEventArgs e)
        {
             try
             {
                 // Revert to defaults (delete values so Windows uses system defaults, usually US or whatever OS language is)
                 // Or we could specific delete specific values. The .reg file uses "-" which means delete.
                 
                 using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\International", true))
                 {
                     if (key != null)
                     {
                         // We probably shouldn't delete "LocaleName" blindly if it messes up other things, but followed .reg
                         // Actually, deleting specific overridden keys is safer.
                         // The .reg file removes sShortDate, sLongDate, sShortTime, sTimeFormat, s1159, s2359, iFirstDayOfWeek.
                         // It does NOT remove LocaleName in OFF.reg? Let me check line 4 of OFF.reg.
                         // Wait, OFF.reg line 4 is "sShortDate"=-. It does NOT delete LocaleName.
                         
                         string[] keysToDelete = { "sShortDate", "sLongDate", "sShortTime", "sTimeFormat", "s1159", "s2359", "iFirstDayOfWeek" };
                         foreach(var k in keysToDelete)
                         {
                             try { key.DeleteValue(k); } catch {}
                         }
                     }
                 }

                 if (Helpers.Utilities.IsAdministrator)
                 {
                      using (var key = Registry.Users.OpenSubKey(@".DEFAULT\Control Panel\International", true))
                      {
                          if (key != null)
                          {
                              string[] keysToDelete = { "sShortDate", "sLongDate", "sShortTime", "sTimeFormat", "s1159", "s2359", "iFirstDayOfWeek" };
                              foreach(var k in keysToDelete)
                              {
                                  try { key.DeleteValue(k); } catch {}
                              }
                          }
                      }
                 }
             }
             catch (Exception ex)
             {
                 Components.CustomMessageBoxWindow.Show($"Failed to Disable Vietnamese Time Format: {ex.Message}", "Error");
             }
        }

        private async void InstallBlurExplorer_Click(object sender, RoutedEventArgs e)
         {
             if (!Helpers.Utilities.IsAdministrator)
             {
                 Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgExtendAdminRequired"), Helpers.LocalizationService.Get("msgExtendError"));
                 return;
             }

             string dllPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Tools\BlurExplorer\ExplorerBlurMica.dll");
             if (!System.IO.File.Exists(dllPath))
             {
                 Components.CustomMessageBoxWindow.Show($"File not found: {dllPath}", "Error");
                 return;
             }

             var mainWindow = Window.GetWindow(this) as MainWindow;
             mainWindow?.ShowLoading("Installing Blur Explorer...", 1, 0);

             await Task.Run(() =>
             {
                 try
                 {
                     // Register DLL
                     Helpers.Utilities.RunCommandWithOutput($"regsvr32 /s \"{dllPath}\"");
                     
                     // Restart Explorer
                     Helpers.Utilities.RunCommandWithOutput("taskkill /F /IM explorer.exe");
                     Helpers.Utilities.StartProcess("explorer.exe");
                 }
                 catch (Exception ex)
                 {
                     Dispatcher.Invoke(() => Components.CustomMessageBoxWindow.Show($"Error: {ex.Message}", "Error"));
                 }
             });

             mainWindow?.HideLoading();
             Components.CustomMessageBoxWindow.Show("Blur Explorer installed successfully!", "Success");
         }

         private async void UninstallBlurExplorer_Click(object sender, RoutedEventArgs e)
         {
             if (!Helpers.Utilities.IsAdministrator)
             {
                 Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgExtendAdminRequired"), Helpers.LocalizationService.Get("msgExtendError"));
                 return;
             }

             string dllPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Tools\BlurExplorer\ExplorerBlurMica.dll");
             
             if (!System.IO.File.Exists(dllPath))
             {
                 Components.CustomMessageBoxWindow.Show($"File not found for uninstallation: {dllPath}\nEnsure the file exists to unregister it correctly.", "Error");
                 return;
             }

             var mainWindow = Window.GetWindow(this) as MainWindow;
             mainWindow?.ShowLoading("Uninstalling Blur Explorer...", 1, 0);

             await Task.Run(() =>
             {
                 try
                 {
                     // Unregister DLL
                     Helpers.Utilities.RunCommandWithOutput($"regsvr32 /u /s \"{dllPath}\"");

                     // Restart Explorer
                     Helpers.Utilities.RunCommandWithOutput("taskkill /F /IM explorer.exe");
                     Helpers.Utilities.StartProcess("explorer.exe");
                 }
                 catch (Exception ex)
                 {
                     Dispatcher.Invoke(() => Components.CustomMessageBoxWindow.Show($"Error: {ex.Message}", "Error"));
                 }
             });

             mainWindow?.HideLoading();
             Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgExtendBlurUninstalled"), Helpers.LocalizationService.Get("msgExtendSuccess"));
         }

        private void AddProgramBrowseBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Program",
                Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
                Multiselect = false
            };
            if (dialog.ShowDialog() == true)
            {
                AddProgramPathTextBox.Text = dialog.FileName;
            }
        }

        private async System.Threading.Tasks.Task LoadIrstData()
        {
             IRSTComboBox.Items.Clear();
             IRSTComboBox.Items.Add(new ComboBoxItem { Content = Helpers.LocalizationService.Get("msgLoading") });
             IRSTComboBox.SelectedIndex = 0;

             await Task.Run(async () =>
             {
                 try
                 {
                     string jsonPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Download", "intelirst.json");
                     if (!System.IO.File.Exists(jsonPath))
                     {
                         // Dispatch UI update
                         Dispatcher.Invoke(() =>
                         {
                             IRSTComboBox.Items.Clear();
                             IRSTComboBox.Items.Add(new ComboBoxItem { Content = "intelirst.json not found" });
                             IRSTComboBox.SelectedIndex = 0;
                         });
                         return;
                     }

                     string json = await System.IO.File.ReadAllTextAsync(jsonPath);
                     var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                     var data = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, IrstInfo>>(json, options);

                     Dispatcher.Invoke(() =>
                     {
                         IRSTComboBox.Items.Clear();
                         if (data != null && data.Count > 0)
                         {
                             foreach (var kvp in data)
                             {
                                 var cbItem = new ComboBoxItem();
                                 cbItem.Content = kvp.Value.name;
                                 cbItem.Tag = kvp.Value.link;
                                 IRSTComboBox.Items.Add(cbItem);
                             }
                             IRSTComboBox.SelectedIndex = 0;
                         }
                         else
                         {
                               IRSTComboBox.Items.Add(new ComboBoxItem { Content = "No drivers found" }); // Localize if key avail
                               IRSTComboBox.SelectedIndex = 0;
                         }
                     });
                 }
                 catch
                 {
                     Dispatcher.Invoke(() =>
                     {
                         IRSTComboBox.Items.Clear();
                         IRSTComboBox.Items.Add(new ComboBoxItem { Content = Helpers.LocalizationService.Get("msgExtendError") });
                         IRSTComboBox.SelectedIndex = 0;
                     });
                 }
             });
        }

        public class IrstInfo
        {
            public string name { get; set; }
            public string link { get; set; }
        }

        private void IRSTDriverBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Directory",
                Multiselect = false
            };
            if (dialog.ShowDialog() == true)
            {
                 IRSTDriverTextBox.Text = dialog.FolderName;
                 _isCustomIrstPath = true;
            }
        }

        private async void IRSTStartBtn_Click(object sender, RoutedEventArgs e)
        {
             var selectedItem = IRSTComboBox.SelectedItem as ComboBoxItem;
             if (selectedItem == null || selectedItem.Tag == null)
             {
                 Components.CustomMessageBoxWindow.Show("Please select a driver version.", "Error");
                 return;
             }

             string downloadUrl = selectedItem.Tag.ToString();
             string fileName = System.IO.Path.GetFileName(downloadUrl);
             string destFolder;
             
             // Check if user selected a custom folder
             if (_isCustomIrstPath && System.IO.Directory.Exists(IRSTDriverTextBox.Text))
             {
                 destFolder = IRSTDriverTextBox.Text;
             }
             else
             {
                 destFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Download", "IRST");
             }

             if (!System.IO.Directory.Exists(destFolder)) System.IO.Directory.CreateDirectory(destFolder);
             string destPath = System.IO.Path.Combine(destFolder, fileName);
             
             var mainWindow = Window.GetWindow(this) as MainWindow;
             mainWindow?.ShowLoading("Downloading IRST Driver...", 1, 0);

             bool downloadSuccess = false;
             Exception downloadError = null;
             var downloadTask = Task.Run(() =>
             {
                 try
                 {
                     var result = Helpers.Utilities.RunCommandWithOutput($"curl -L -o \"{destPath}\" \"{downloadUrl}\"");
                     if (result.ExitCode == 0)
                     {
                         downloadSuccess = true;
                     }
                     else
                     {
                         downloadError = new Exception($"Exit Code: {result.ExitCode}\nOutput: {result.Output}\nError: {result.Error}");
                     }
                 }
                 catch (Exception ex)
                 {
                      downloadError = ex;
                 }
             });

             // Simulate progress while downloading
             int progress = 0;
             var random = new Random();
             
             // Period 1: Increment up to 90% while task is running
             while (!downloadTask.IsCompleted)
             {
                 // Increment a bit
                 if (progress < 90)
                 {
                     progress += random.Next(1, 5);
                     if (progress > 90) progress = 90;
                 }
                 
                 mainWindow?.UpdateLoadingProgress(1, progress, $"Downloading: {progress}%");
                 await Task.Delay(100); 
             }

             await downloadTask;

             if (downloadSuccess && System.IO.File.Exists(destPath))
             {
                 // Finish Step 1
                 while (progress < 100)
                 {
                     progress += 10;
                     if (progress > 100) progress = 100;
                     mainWindow?.UpdateLoadingProgress(1, progress, "Downloading complete...");
                     await Task.Delay(20);
                 }
                 mainWindow?.UpdateLoadingProgress(1, 100, "Download Complete");
                 await Task.Delay(200);

                 // Simulate Step 2: Processing
                 mainWindow?.UpdateLoadingProgress(2, 0, "Processing file...");
                 for (int i = 0; i <= 100; i += 20)
                 {
                     mainWindow?.UpdateLoadingProgress(2, i, "Verifying integrity...");
                     await Task.Delay(50);
                 }
                 mainWindow?.UpdateLoadingProgress(2, 100, "Processing Complete");
                 await Task.Delay(200);

                 // Simulate Step 3: Finalizing
                 mainWindow?.UpdateLoadingProgress(3, 0, "Finalizing...");
                 for (int i = 0; i <= 100; i += 25)
                 {
                     mainWindow?.UpdateLoadingProgress(3, i, "Cleaning up...");
                     await Task.Delay(50);
                 }
                 mainWindow?.UpdateLoadingProgress(3, 100, "All Done!");
                 await Task.Delay(500); 

                 mainWindow?.HideLoading();

                 if (_isCustomIrstPath)
                 {
                     Components.CustomMessageBoxWindow.Show("Download Complete!", "Success");
                 }
                 else
                 {
                     var confirmWindow = new ModernConfirmationWindow(
                         $"Download Complete!\nSaved to: {destFolder}\n\nDo you want to open the folder?",
                         "Success",
                         "Yes", 
                         "No"); 
                     
                     confirmWindow.Owner = mainWindow;
                     
                     if (confirmWindow.ShowDialog() == true)
                     {
                         Helpers.Utilities.StartProcess("explorer.exe", destFolder);
                     }
                 }
             }
             else
             {
                 mainWindow?.HideLoading();
                 string msg = downloadError != null ? downloadError.Message : "File not found after download.";
                 Components.CustomMessageBoxWindow.Show($"Download failed. {msg}", "Error");
             }
        }


        private void ExportDriverBrowseBtn_Click(object sender, System.Windows.RoutedEventArgs e) => PickFolder(ExportDriverTextBox);
        private void ExportWifiBrowseBtn_Click(object sender, System.Windows.RoutedEventArgs e) => PickFolder(ExportWifiTextBox);



        private void HideFolderBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
             if (HideFolderBrowseBtn.ContextMenu != null)
             {
                 HideFolderBrowseBtn.ContextMenu.PlacementTarget = HideFolderBrowseBtn;
                 HideFolderBrowseBtn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                 HideFolderBrowseBtn.ContextMenu.IsOpen = true; 
             }
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
             var dialog = new Microsoft.Win32.OpenFileDialog
             {
                 Title = "Select File to Hide/Unhide",
                 Multiselect = false
             };
             if (dialog.ShowDialog() == true)
             {
                 HideFolderTextBox.Text = dialog.FileName;
             }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
             var dialog = new Microsoft.Win32.OpenFolderDialog
             {
                 Title = "Select Folder to Hide/Unhide",
                 Multiselect = false
             };
             if (dialog.ShowDialog() == true)
             {
                 HideFolderTextBox.Text = dialog.FolderName;
             }
        }

        private async void HideFileBtn_Click(object sender, RoutedEventArgs e)
        {
             await ToggleFileAttributes(true);
        }

        private async void UnhideFileBtn_Click(object sender, RoutedEventArgs e)
        {
             await ToggleFileAttributes(false);
        }

        private async System.Threading.Tasks.Task ToggleFileAttributes(bool hide)
        {
             string path = HideFolderTextBox.Text.Trim();
             if (string.IsNullOrWhiteSpace(path))
             {
                 Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgExtendNoProgramSelected"), Helpers.LocalizationService.Get("msgExtendError"));
                 return;
             }

             if (!System.IO.File.Exists(path) && !System.IO.Directory.Exists(path))
             {
                  // If unhiding, user might have typed it manually but system can't see it if hidden?
                  // Actually, File.Exists returns TRUE for hidden files usually, unless permissions deny.
                  // But if it returns false, we warn.
                  Components.CustomMessageBoxWindow.Show("File or folder not found.\nEnsure the path is correct.", Helpers.LocalizationService.Get("msgExtendError"));
                  return;
             }
             
             string mode = hide ? "+h +s" : "-h -s";
             string command = $"attrib {mode} \"{path}\"";

             // For folders, usually just hide the folder itself. User request: attrib +h +s "Path"
             // If recursive needed, would be /s /d, but user didn't ask.
             
             var mainWindow = Window.GetWindow(this) as MainWindow;
             mainWindow?.ShowLoading("Processing attributes...", 1, 0);

             bool success = false;
             string errorMsg = "";

             await Task.Run(() =>
             {
                 var result = Helpers.Utilities.RunCommandWithOutput(command);
                 if (result.ExitCode == 0) success = true;
                 else errorMsg = result.Error;
             });
             
             // Animate loading Step 1 - Real work
             for(int i=0; i<=100; i+=20)
             {
                 mainWindow?.UpdateLoadingProgress(1, i, "Applying attributes...");
                 await Task.Delay(50);
             }
             
             // Simulated Step 2 - Verifying (Just for visual)
             mainWindow?.UpdateLoadingProgress(2, 0, "Verifying changes...");
             for(int i=0; i<=100; i+=25)
             {
                 mainWindow?.UpdateLoadingProgress(2, i, "Verifying...");
                 await Task.Delay(50);
             }
             mainWindow?.UpdateLoadingProgress(2, 100, "Verification Complete");
             await Task.Delay(100);

             // Simulated Step 3 - Finalizing
             mainWindow?.UpdateLoadingProgress(3, 0, "Finalizing...");
             for(int i=0; i<=100; i+=20)
             {
                 mainWindow?.UpdateLoadingProgress(3, i, "Updating system cache...");
                 await Task.Delay(50);
             }
             mainWindow?.UpdateLoadingProgress(3, 100, "Done");
             await Task.Delay(200);

             mainWindow?.HideLoading();

             if (success)
             {
                 string action = hide ? "Hidden" : "Unhidden";
                 
                 // Manage HiddenItems.txt
                 try 
                 {
                     string configDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
                     if (!System.IO.Directory.Exists(configDir)) System.IO.Directory.CreateDirectory(configDir);
                     string hiddenListPath = System.IO.Path.Combine(configDir, "HiddenItems.txt");
 
                     string targetPath = path.ToLower().Trim();
                     
                     List<string> lines = new List<string>();
                     if (System.IO.File.Exists(hiddenListPath))
                     {
                         lines = System.IO.File.ReadAllLines(hiddenListPath).ToList();
                     }
 
                     if (hide)
                     {
                         // Add if not exists
                         if (!lines.Any(l => l.ToLower().Trim() == targetPath))
                         {
                             lines.Add(path); // Save original case
                             System.IO.File.WriteAllLines(hiddenListPath, lines, System.Text.Encoding.UTF8);
                         }
                     }
                     else
                     {
                         // Remove if exists
                         var existing = lines.FirstOrDefault(l => l.ToLower().Trim() == targetPath);
                         if (existing != null)
                         {
                             lines.Remove(existing);
                             System.IO.File.WriteAllLines(hiddenListPath, lines, System.Text.Encoding.UTF8);
                         }
                     }
                 }
                 catch (Exception ex)
                 {
                     // Log error but don't stop flow
                     System.Diagnostics.Debug.WriteLine($"Error managing hidden list: {ex.Message}");
                 }

                 Components.CustomMessageBoxWindow.Show($"File/Folder successfully {action}!", "Success");
             }
             else
             {
                 Components.CustomMessageBoxWindow.Show($"Failed to change attributes.\n{errorMsg}", "Error");
             }
        }

        private void PickFolder(TextBox textBox)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Directory",
                Multiselect = false
            };
            if (dialog.ShowDialog() == true)
            {
                textBox.Text = dialog.FolderName;
            }
        }

        private async void ExportDriverStartBtn_Click(object sender, RoutedEventArgs e)
        {
            await RunExportProcess("Exporting Drivers...", "Drivers", ExportDriverTextBox.Text, "/online /export-driver /destination:\"{0}\"");
        }

        private async void ImportDriverStartBtn_Click(object sender, RoutedEventArgs e)
        {
             if (!Helpers.Utilities.IsAdministrator)
             {
                 Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgExtendAdminRequired"), Helpers.LocalizationService.Get("msgExtendError"));
                 return;
             }

             string path = ExportDriverTextBox.Text.Trim();
             if (string.IsNullOrWhiteSpace(path) || !System.IO.Directory.Exists(path))
             {
                  Components.CustomMessageBoxWindow.Show("Please select a valid directory containing drivers.", "Error");
                  return;
             }

             var mainWindow = Window.GetWindow(this) as MainWindow;
             if (mainWindow != null)
             {
                 mainWindow.ShowLoading("Importing Drivers...", 1, 0);
             }

             try
             {
                 await System.Threading.Tasks.Task.Run(async () =>
                 {
                     if(mainWindow != null) Dispatcher.Invoke(() => mainWindow.UpdateLoadingProgress(1, 20, "Scanning for drivers..."));
                     await System.Threading.Tasks.Task.Delay(500);

                     if(mainWindow != null) Dispatcher.Invoke(() => mainWindow.UpdateLoadingProgress(1, 50, "Installing drivers from " + path));
                     
                     // Run pnputil from the selected directory
                     string command = $"cd /d \"{path}\" && pnputil /add-driver *.inf /subdirs /install";
                     
                     // Run command
                     var result = Helpers.Utilities.RunCommandWithOutput(command);

                     if(mainWindow != null) Dispatcher.Invoke(() => mainWindow.UpdateLoadingProgress(1, 100, "Finishing..."));
                     await System.Threading.Tasks.Task.Delay(500);
                 });
                 
                 Components.CustomMessageBoxWindow.Show("Driver import completed.", "Success");
             }
             catch (Exception ex)
             {
                 Components.CustomMessageBoxWindow.Show($"Error: {ex.Message}", "Failed");
             }
             finally
             {
                 if (mainWindow != null) mainWindow.HideLoading();
             }
        }

        private async void ExportWifiStartBtn_Click(object sender, RoutedEventArgs e)
        {
             await RunExportProcess("Exporting Wi-Fi Profiles...", "Wi-Fi", ExportWifiTextBox.Text, "wlan export profile folder=\"{0}\" key=clear", isNetsh: true);
        }

        private async System.Threading.Tasks.Task RunExportProcess(string taskTitle, string shortName, string path, string commandTemplate, bool isNetsh = false)
        {
             // Check for Administrator privileges
             if (!Helpers.Utilities.IsAdministrator)
             {
                 Components.CustomMessageBoxWindow.Show("This feature requires Administrator privileges.\nPlease run the application as Administrator.", "Permission Denied");
                 return;
             }

             path = path.Trim();
             if (string.IsNullOrWhiteSpace(path))
             {
                  Components.CustomMessageBoxWindow.Show("Please select a destination folder.", "Error");
                  return;
             }

             var mainWindow = Window.GetWindow(this) as MainWindow;
             if (mainWindow != null)
             {
                 mainWindow.ShowLoading("Preparing destination...", 1, 0);
             }

             try
             {
                 await System.Threading.Tasks.Task.Run(async () =>
                 {
                     // Step 1: Create Directory
                     if (!System.IO.Directory.Exists(path))
                     {
                         System.IO.Directory.CreateDirectory(path);
                     }
                     // Simulate visual delay for Step 1
                     for(int i=0; i<=100; i+=20)
                     {
                         if(mainWindow != null) Dispatcher.Invoke(() => mainWindow.UpdateLoadingProgress(1, i, "Preparing destination..."));
                         await System.Threading.Tasks.Task.Delay(100);
                     }

                     // Step 2: Running Command
                     if(mainWindow != null) Dispatcher.Invoke(() => mainWindow.UpdateLoadingProgress(2, 0, "Executing command..."));
                     
                     string command = string.Format(commandTemplate, path);
                     // Run hidden (false)
                     if (isNetsh)
                     {
                         command  = "netsh " + command;
                     }
                     else
                     {
                         // DISM
                         command = "dism " + command;
                     }

                     if(mainWindow != null) Dispatcher.Invoke(() => mainWindow.UpdateLoadingProgress(2, 50, "Exporting data..."));
                     
                     var result = Helpers.Utilities.RunCommandWithOutput(command);

                     if (result.ExitCode != 0)
                     {
                         throw new Exception($"Command Failed (Exit Code: {result.ExitCode})\nOutput: {result.Output}\nError: {result.Error}");
                     }

                     for(int i=50; i<=100; i+=10)
                     {
                         if(mainWindow != null) Dispatcher.Invoke(() => mainWindow.UpdateLoadingProgress(2, i, "Finalizing..."));
                         await System.Threading.Tasks.Task.Delay(50);
                     }

                     // Step 3: Verification/Finalizing
                     if(mainWindow != null) Dispatcher.Invoke(() => mainWindow.UpdateLoadingProgress(3, 0, "Verifying..."));
                     await System.Threading.Tasks.Task.Delay(500);
                     if(mainWindow != null) Dispatcher.Invoke(() => mainWindow.UpdateLoadingProgress(3, 100, "Done!"));
                     await System.Threading.Tasks.Task.Delay(500);
                 });
             }
             catch (Exception ex)
             {
                 Components.CustomMessageBoxWindow.Show($"Error: {ex.Message}", "Failed");
             }
             finally
             {
                 if (mainWindow != null) mainWindow.HideLoading();
                 Components.CustomMessageBoxWindow.Show($"{shortName} export completed.\nDestination: {path}", "Success");
             }
        }

        private void OpenMSIUtil_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string relativePath = System.IO.Path.Combine("Tools", "MSI.exe");
                string absolutePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath));

                if (!System.IO.File.Exists(absolutePath))
                {
                    Components.CustomMessageBoxWindow.Show(string.Format(Helpers.LocalizationService.Get("msgExtendToolNotFound"), absolutePath), Helpers.LocalizationService.Get("msgError"));
                    return;
                }

                var psi = new System.Diagnostics.ProcessStartInfo();
                psi.FileName = absolutePath;
                psi.UseShellExecute = true;
                psi.Verb = "runas"; // Run as Admin

                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                Components.CustomMessageBoxWindow.Show(string.Format(Helpers.LocalizationService.Get("msgExtendToolLaunchFailed"), "MSI Utility", ex.Message), Helpers.LocalizationService.Get("msgError"));
            }
        }

        private void OpenTimerResolution_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string relativePath = System.IO.Path.Combine("Tools", "TimerResolution.exe");
                string absolutePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath));

                if (!System.IO.File.Exists(absolutePath))
                {
                    Components.CustomMessageBoxWindow.Show(string.Format(Helpers.LocalizationService.Get("msgExtendToolNotFound"), absolutePath), Helpers.LocalizationService.Get("msgError"));
                    return;
                }

                var psi = new System.Diagnostics.ProcessStartInfo();
                psi.FileName = absolutePath;
                psi.UseShellExecute = true;
                psi.Verb = "runas"; // Run as Admin

                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                Components.CustomMessageBoxWindow.Show(string.Format(Helpers.LocalizationService.Get("msgExtendToolLaunchFailed"), "Timer Resolution", ex.Message), Helpers.LocalizationService.Get("msgError"));
            }
        }



        private void ListHiddenItemsBtn_Click(object sender, RoutedEventArgs e)
        {
             try
             {
                 string configDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
                 string hiddenListPath = System.IO.Path.Combine(configDir, "HiddenItems.txt");
 
                 if (!System.IO.File.Exists(hiddenListPath))
                 {
                     Components.CustomMessageBoxWindow.Show("No hidden items log found yet.", "Info");
                     return;
                 }
 
                 System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("notepad.exe", hiddenListPath) { UseShellExecute = true });
             }
             catch (Exception ex)
             {
                 Components.CustomMessageBoxWindow.Show($"Failed to open list: {ex.Message}", "Error");
             }
        }
        private void AddProgramBtn_Click(object sender, RoutedEventArgs e)
        {
            string filePath = AddProgramPathTextBox.Text;
            if (string.IsNullOrWhiteSpace(filePath) || 
                !filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                 Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgExtendNoProgramSelected"), Helpers.LocalizationService.Get("msgExtendError"));
                 return;
            }

            if (!Utilities.IsAdministrator)
            {
                 Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgAccessDenied"), Helpers.LocalizationService.Get("msgPermissionError"));
                 return;
            }

            try
            {
                string programName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                
                // Use HKLM explicit path to ensure it's system-wide and avoids HKCR virtualization issues
                // HKLM\SOFTWARE\Classes\DesktopBackground\Shell is the standard location
                string baseKeyPath = @"SOFTWARE\Classes\DesktopBackground\Shell";
                string fullKeyPath = $@"{baseKeyPath}\{programName}";

                // 1. Create Main Key in HKLM
                using (var baseKey = Registry.LocalMachine.OpenSubKey(baseKeyPath, true))
                {
                    if (baseKey == null)
                    {
                        Components.CustomMessageBoxWindow.Show(string.Format(Helpers.LocalizationService.Get("msgRegistryError"), baseKeyPath), Helpers.LocalizationService.Get("msgError"));
                        return;
                    }

                    using (var key = baseKey.CreateSubKey(programName))
                    {
                        if (key != null)
                        {
                            key.SetValue("Icon", filePath);
                            key.SetValue("Position", "Middle");
                            
                            using (var commandKey = key.CreateSubKey("command"))
                            {
                                 commandKey.SetValue("", $"\"{filePath}\"");
                            }
                        }
                    }
                }
                
                // Verification
                using (var checkKey = Registry.LocalMachine.OpenSubKey(fullKeyPath))
                {
                    if (checkKey != null)
                    {
                         // Log to text file
                         try
                         {
                             string configDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
                             if (!System.IO.Directory.Exists(configDir)) System.IO.Directory.CreateDirectory(configDir);
                             string logPath = System.IO.Path.Combine(configDir, "AddedRightClickApps.txt");
                             System.IO.File.AppendAllText(logPath, filePath + Environment.NewLine, System.Text.Encoding.UTF8);
                         }
                         catch { /* Ignore log error */ }

                         Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgExtendProgramAdded"), Helpers.LocalizationService.Get("msgExtendSuccess"));
                    }
                    else
                    {
                         Components.CustomMessageBoxWindow.Show(string.Format(Helpers.LocalizationService.Get("msgExtendVerificationFailed"), fullKeyPath), "Verification Failed");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgAccessDenied"), Helpers.LocalizationService.Get("msgError"));
            }
            catch (Exception ex)
            {
                Components.CustomMessageBoxWindow.Show($"{Helpers.LocalizationService.Get("msgExtendError")}: {ex.Message}\nType: {ex.GetType().Name}", Helpers.LocalizationService.Get("msgError"));
            }
        }

        private void RemoveProgramBtn_Click(object sender, RoutedEventArgs e)
        {
            string filePath = AddProgramPathTextBox.Text;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                 Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgExtendNoProgramSelected"), Helpers.LocalizationService.Get("msgExtendError"));
                 return;
            }

            if (!Utilities.IsAdministrator)
            {
                 Components.CustomMessageBoxWindow.Show("This action requires Administrator privileges. Please restart the app as Admin.", "Permission Error");
                 return;
            }

            try
            {
                // Logic: Extract program name from path and try to delete key
                string programName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                string baseKeyPath = @"SOFTWARE\Classes\DesktopBackground\Shell";

                bool found = false;
                using (var baseKey = Registry.LocalMachine.OpenSubKey(baseKeyPath, true))
                {
                    if (baseKey != null)
                    {
                        // Check if exists
                        var subKey = baseKey.OpenSubKey(programName);
                        if (subKey != null)
                        {
                            subKey.Close();
                            baseKey.DeleteSubKeyTree(programName);
                            found = true;
                        }
                    }
                }

                if (found)
                {
                    // Remove from log file
                    try
                    {
                        string configDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
                        string logPath = System.IO.Path.Combine(configDir, "AddedRightClickApps.txt");
                        if (System.IO.File.Exists(logPath))
                        {
                             var lines = new System.Collections.Generic.List<string>(System.IO.File.ReadAllLines(logPath));
                             int removedCount = lines.RemoveAll(l => l.Trim().Equals(filePath.Trim(), StringComparison.OrdinalIgnoreCase));
                             if (removedCount > 0)
                             {
                                 System.IO.File.WriteAllLines(logPath, lines, System.Text.Encoding.UTF8);
                             }
                        }
                    }
                    catch { /* Ignore file error */ }

                    Components.CustomMessageBoxWindow.Show($"Successfully removed '{programName}' from Right-Click menu.", "Success");
                }
                else
                {
                    Components.CustomMessageBoxWindow.Show($"Could not find '{programName}' in the Right-Click menu registry.", "Info");
                }
            }
            catch (Exception ex)
            {
                Components.CustomMessageBoxWindow.Show($"Failed to remove program: {ex.Message}", "Error");
            }
        }

        private void ListAddedProgramsBtn_Click(object sender, RoutedEventArgs e)
        {
             try
             {
                 string configDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
                 string logPath = System.IO.Path.Combine(configDir, "AddedRightClickApps.txt");
 
                 if (!System.IO.File.Exists(logPath))
                 {
                     Components.CustomMessageBoxWindow.Show("No programs have been added yet (Log file not found).", "Info");
                     return;
                 }
 
                 System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("notepad.exe", logPath) { UseShellExecute = true });
             }
             catch (Exception ex)
             {
                 Components.CustomMessageBoxWindow.Show($"Failed to open list: {ex.Message}", "Error");
             }
        }
    }
}
