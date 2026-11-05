using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HyperLisUI.Helpers;
namespace HyperLisUI.Views
{
    public partial class LicenseView : UserControl
    {
        public event EventHandler<string> NavigateRequested;
        private string _fullKey = "";
        private WindowsInfo _currentWindowsInfo = new WindowsInfo();

        public LicenseView()
        {
            InitializeComponent();
            LoadLicenseInfo();
            LocalizationService.LanguageChanged += ApplyTranslations;
            ApplyTranslations();
        }

        private void ApplyTranslations()
        {
            try {
                if (lblLicStatus != null) lblLicStatus.Text = LocalizationService.Get("lblLicStatus");
                if (lblLicMainTitle != null) lblLicMainTitle.Text = LocalizationService.Get("lblLicMainTitle");
                if (lblLicSubTitle != null) lblLicSubTitle.Text = LocalizationService.Get("lblLicSubTitle");
                if (lblLicMethod != null) lblLicMethod.Text = LocalizationService.Get("lblLicMethod");
                if (lblLicActivationDate != null) lblLicActivationDate.Text = LocalizationService.Get("lblLicActivationDate");
                if (lblLicCondition != null) lblLicCondition.Text = LocalizationService.Get("lblLicCondition");
                if (lblLicInfoTitle != null) lblLicInfoTitle.Text = LocalizationService.Get("lblLicInfoTitle");
                if (lblLicRecheckStatus != null) lblLicRecheckStatus.Text = LocalizationService.Get("btnLicRecheckStatus");
                
                UpdateLicenseStatusDisplay();

                if (lblLicChannel != null) lblLicChannel.Text = LocalizationService.Get("lblLicChannel");
                if (lblLicOsName != null) lblLicOsName.Text = LocalizationService.Get("lblLicOsName");
                if (lblLicCID != null) lblLicCID.Text = LocalizationService.Get("lblLicCID");
                if (lblLicProductKey != null) lblLicProductKey.Text = LocalizationService.Get("lblLicProductKey");
                if (lblLicOemKey != null) lblLicOemKey.Text = LocalizationService.Get("lblLicOemKey");
                if (lblLicOemEdition != null) lblLicOemEdition.Text = LocalizationService.Get("lblLicOemEdition");
                if (lblLicInstallDate != null) lblLicInstallDate.Text = LocalizationService.Get("lblLicInstallDate");
                if (lblLicArch != null) lblLicArch.Text = LocalizationService.Get("lblLicArch");
                if (lblLicCopyAll != null) lblLicCopyAll.Text = LocalizationService.Get("btnLicCopyAll");
                if (lblLicVersion != null) lblLicVersion.Text = LocalizationService.Get("lblLicVersion");
                if (lblLicOsBuild != null) lblLicOsBuild.Text = LocalizationService.Get("lblLicBuild");
                
                if (lblLicRemoveMAS != null) lblLicRemoveMAS.Text = LocalizationService.Get("lblLicRemoveMAS");
                if (lblLicRemoveMASDesc != null) lblLicRemoveMASDesc.Text = LocalizationService.Get("lblLicRemoveMASDesc");
                
                if (lblWinRemoveTitle != null) lblWinRemoveTitle.Text = LocalizationService.Get("lblWinRemoveTitle");
                if (lblOffRemoveTitle != null) lblOffRemoveTitle.Text = LocalizationService.Get("lblOffRemoveTitle");
                
                if (lblRemoveHWIDTitle != null) lblRemoveHWIDTitle.Text = LocalizationService.Get("lblRemoveHWIDTitle");
                if (lblRemoveHWIDDesc != null) lblRemoveHWIDDesc.Text = LocalizationService.Get("lblRemoveHWIDDesc");
                if (btnRemoveHWID != null) btnRemoveHWID.Text = LocalizationService.Get("btnRemoveHWID");
                
                if (lblRemoveWinKMSTitle != null) lblRemoveWinKMSTitle.Text = LocalizationService.Get("lblRemoveWinKMSTitle");
                if (lblRemoveWinKMSDesc != null) lblRemoveWinKMSDesc.Text = LocalizationService.Get("lblRemoveWinKMSDesc");
                if (btnRemoveWinKMS != null) btnRemoveWinKMS.Text = LocalizationService.Get("btnRemoveWinKMS");
                
                if (lblRemoveWinTSforceTitle != null) lblRemoveWinTSforceTitle.Text = LocalizationService.Get("lblRemoveWinTSforceTitle");
                if (lblRemoveWinTSforceDesc != null) lblRemoveWinTSforceDesc.Text = LocalizationService.Get("lblRemoveWinTSforceDesc");
                if (btnRemoveWinTSforce != null) btnRemoveWinTSforce.Text = LocalizationService.Get("btnRemoveWinTSforce");
                
                if (lblRemoveOhookTitle != null) lblRemoveOhookTitle.Text = LocalizationService.Get("lblRemoveOhookTitle");
                if (lblRemoveOhookDesc != null) lblRemoveOhookDesc.Text = LocalizationService.Get("lblRemoveOhookDesc");
                if (btnRemoveOhook != null) btnRemoveOhook.Text = LocalizationService.Get("btnRemoveOhook");
                
                if (lblRemoveOffTSforceTitle != null) lblRemoveOffTSforceTitle.Text = LocalizationService.Get("lblRemoveOffTSforceTitle");
                if (lblRemoveOffTSforceDesc != null) lblRemoveOffTSforceDesc.Text = LocalizationService.Get("lblRemoveOffTSforceDesc");
                if (btnRemoveOffTSforce != null) btnRemoveOffTSforce.Text = LocalizationService.Get("btnRemoveOffTSforce");
                
                if (lblRemoveOffKMSTitle != null) lblRemoveOffKMSTitle.Text = LocalizationService.Get("lblRemoveOffKMSTitle");
                if (lblRemoveOffKMSDesc != null) lblRemoveOffKMSDesc.Text = LocalizationService.Get("lblRemoveOffKMSDesc");
                if (btnRemoveOffKMS != null) btnRemoveOffKMS.Text = LocalizationService.Get("btnRemoveOffKMS");

                if (lblLicActivateTitle != null) lblLicActivateTitle.Text = LocalizationService.Get("lblLicActivateTitle");
                if (lblLicActivateDesc != null) lblLicActivateDesc.Text = LocalizationService.Get("lblLicActivateDesc");
                if (tbInputKeyPlaceholder != null) tbInputKeyPlaceholder.Text = LocalizationService.Get("tbLicInputKeyPlaceholder");
                if (lblLicActivateBtnText != null) lblLicActivateBtnText.Text = LocalizationService.Get("btnLicActivate") ?? "Activate";
                else if (btnLicActivate != null) btnLicActivate.Content = LocalizationService.Get("btnLicActivate");
                if (lblLicCheckStatus != null) lblLicCheckStatus.Text = LocalizationService.Get("btnLicCheckStatus");
                
                if (lblLicChangeTypeTitle != null) lblLicChangeTypeTitle.Text = LocalizationService.Get("lblLicChangeTypeTitle");
                if (lblLicChangeTypeDesc != null) lblLicChangeTypeDesc.Text = LocalizationService.Get("lblLicChangeTypeDesc");
                if (btnLicChangeType != null) btnLicChangeType.Content = LocalizationService.Get("btnLicChangeType");
                
                if (lblLicBackupTitle != null) lblLicBackupTitle.Text = LocalizationService.Get("lblLicBackupTitle");
                if (lblLicBackupSubTitle != null) lblLicBackupSubTitle.Text = LocalizationService.Get("lblLicBackupSubTitle");
                if (lblLicBackupDesc != null) lblLicBackupDesc.Text = LocalizationService.Get("lblLicBackupDesc");
                if (lblLicBackupBtn != null) lblLicBackupBtn.Text = LocalizationService.Get("btnLicBackup");
                
                if (lblLicRestoreSubTitle != null) lblLicRestoreSubTitle.Text = LocalizationService.Get("lblLicRestoreSubTitle");
                if (lblLicRestoreDesc != null) lblLicRestoreDesc.Text = LocalizationService.Get("lblLicRestoreDesc");
                if (lblLicRestoreBtn != null) lblLicRestoreBtn.Text = LocalizationService.Get("btnLicRestore");
                if (lblLicBackupNote != null) lblLicBackupNote.Text = LocalizationService.Get("lblLicBackupNote");
                
                if (lblLicIntroTitle != null) lblLicIntroTitle.Text = LocalizationService.Get("lblLicIntroTitle");
                if (lblLicIntroDesc != null) lblLicIntroDesc.Text = LocalizationService.Get("lblLicIntroDesc");
                if (lblLicIntro1 != null) lblLicIntro1.Text = LocalizationService.Get("lblLicIntro1");
                if (lblLicIntro2 != null) lblLicIntro2.Text = LocalizationService.Get("lblLicIntro2");
                if (lblLicIntro3 != null) lblLicIntro3.Text = LocalizationService.Get("lblLicIntro3");
                if (lblLicIntro4 != null) lblLicIntro4.Text = LocalizationService.Get("lblLicIntro4");
                
                if (lblLicNoteTitle != null) lblLicNoteTitle.Text = LocalizationService.Get("lblLicNoteTitle");
                if (lblLicNoteDesc != null) lblLicNoteDesc.Text = LocalizationService.Get("lblLicNoteDesc");
                
                if (lblLicSupportTitle != null) lblLicSupportTitle.Text = LocalizationService.Get("lblLicSupportTitle");
                if (btnLicHelp != null) btnLicHelp.Content = LocalizationService.Get("btnLicHelp");
                if (btnLicSupport != null) btnLicSupport.Content = LocalizationService.Get("btnLicSupport");
            } catch {}
        }

        private void UpdateLicenseStatusDisplay()
        {
            if (_currentWindowsInfo == null) return;
            var info = _currentWindowsInfo;

            // Cập nhật thẻ trạng thái kích hoạt chính trong lưới chi tiết
            txtDetailActivationStatus.Text = info.GetActivationStatusText();
            txtDetailActivationStatus.Foreground = info.IsActivated 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A7C36")) 
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"));

            txtDetailMethod.Text = info.IsMasDetected ? $"{info.LicenseChannel} ({info.MasMethod})" : info.LicenseChannel;
            txtDetailDate.Text = info.InstallDate;
            txtDetailCondition.Text = info.IsActivated ? LocalizationService.Get("msgValid") : LocalizationService.Get("msgInvalid");
            txtDetailCondition.Foreground = txtDetailActivationStatus.Foreground;

            if (info.IsActivated)
            {
                if (info.IsMasDetected)
                {
                    // Phát hiện MAS hoặc không khớp OEM gốc
                    imgStatusIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Images/License/warning.png"));
                    brdLicenseTypeTag.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFBEB"));
                    brdLicenseTypeTag.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A"));
                    txtLicenseTypeTag.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                    txtLicenseTypeTag.Text = info.GetStatusTagText();

                    txtStatusTitle.Text = info.GetStatusTitle();
                    txtStatusTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                    txtStatusDesc.Text = info.GetActivationDescription();
                }
                else
                {
                    // Kích hoạt chuẩn (Khớp OEM gốc hoặc bản quyền chuẩn)
                    imgStatusIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Images/License/activated.png"));
                    brdLicenseTypeTag.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F4FCF7"));
                    brdLicenseTypeTag.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4EDDC"));
                    txtLicenseTypeTag.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A7C36"));
                    txtLicenseTypeTag.Text = info.GetStatusTagText();

                    txtStatusTitle.Text = info.GetStatusTitle();
                    txtStatusTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                    txtStatusDesc.Text = info.GetActivationDescription();
                }
            }
            else
            {
                imgStatusIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Images/License/UnActivated.png"));
                brdLicenseTypeTag.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF2F2"));
                brdLicenseTypeTag.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                txtLicenseTypeTag.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                txtLicenseTypeTag.Text = info.GetStatusTagText();

                txtStatusTitle.Text = info.GetStatusTitle();
                txtStatusTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                txtStatusDesc.Text = info.GetActivationDescription();
            }

            if (string.IsNullOrEmpty(_fullKey) && txtProductKey != null)
            {
                txtProductKey.Text = LocalizationService.Get("msgConvertSKU_NoOEMKey") ?? "Không có sẵn";
            }
            if (string.IsNullOrEmpty(info.OemKey))
            {
                if (txtOemKey != null) txtOemKey.Text = LocalizationService.Get("msgConvertSKU_NoOEMKey") ?? "Không có sẵn";
                if (txtOemEdition != null) txtOemEdition.Text = LocalizationService.Get("msgConvertSKU_NoOEMKey") ?? "Không có sẵn";
            }
        }

        private async System.Threading.Tasks.Task<bool> LoadLicenseInfo()
        {
            string loadingText = LocalizationService.Get("lblConvertSKU_Loading") ?? "Đang tải...";
            txtDetailDate.Text = loadingText;
            txtProductKey.Text = loadingText;
            txtOsName.Text = loadingText;
            txtVersion.Text = loadingText;
            txtBuild.Text = loadingText;
            txtArchitecture.Text = loadingText;
            txtInstallDate.Text = loadingText;
            txtChannel.Text = loadingText;
            txtCID.Text = loadingText;
            txtOemEdition.Text = loadingText;
            txtOemKey.Text = loadingText;
            
            txtStatusTitle.Text = loadingText;
            txtStatusDesc.Text = loadingText;
            txtDetailActivationStatus.Text = loadingText;
            txtDetailMethod.Text = loadingText;
            txtDetailCondition.Text = loadingText;
            
            var brushLoading = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666666"));
            txtDetailActivationStatus.Foreground = brushLoading;
            txtDetailCondition.Foreground = brushLoading;
            
            var info = await System.Threading.Tasks.Task.Run(() => HyperLisUI.Helpers.WindowsInfoHelper.GetCurrentWindowsInfo());
            _currentWindowsInfo = info;

            txtOsName.Text = info.OsName?.Replace("Microsoft ", "").Trim();
            txtVersion.Text = info.Version;
            txtBuild.Text = info.Build;
            txtArchitecture.Text = info.Architecture;
            txtInstallDate.Text = info.InstallDate;
            
            txtChannel.Text = info.LicenseChannel;
            txtCID.Text = info.ProductId;
            
            if (!string.IsNullOrEmpty(info.InstalledKey))
            {
                _fullKey = info.InstalledKey;
                txtProductKey.Text = _fullKey;
            }
            else if (!string.IsNullOrEmpty(info.PartialKey))
            {
                _fullKey = $"*****-*****-*****-*****-{info.PartialKey}";
                txtProductKey.Text = _fullKey;
            }
            else
            {
                _fullKey = "";
                txtProductKey.Text = LocalizationService.Get("msgConvertSKU_NoOEMKey") ?? "Không có sẵn";
            }

            if (string.IsNullOrEmpty(info.OemKey))
            {
                txtOemKey.Text = LocalizationService.Get("msgConvertSKU_NoOEMKey") ?? "Không có sẵn";
                txtOemEdition.Text = LocalizationService.Get("msgConvertSKU_NoOEMKey") ?? "Không có sẵn";
            }
            else
            {
                txtOemKey.Text = info.OemKey;
                txtOemEdition.Text = string.IsNullOrEmpty(info.OemEdition) ? (LocalizationService.Get("msgConvertSKU_NoOEMKey") ?? "Không có sẵn") : info.OemEdition;
            }

            // Cập nhật thẻ trạng thái và mô tả
            UpdateLicenseStatusDisplay();

            return info.IsActivated;
        }

        private async void BtnCheckStatus_Click(object sender, RoutedEventArgs e)
        {
            // Tải lại thông tin bản quyền (cập nhật UI)
            bool isActivated = await LoadLicenseInfo();

            string title = Helpers.LocalizationService.Get("btnLicCheckStatus") ?? "Kiểm tra trạng thái";
            string message = _currentWindowsInfo.GetActivationDescription();

            Components.CustomMessageBoxWindow.Show(message, title);
        }

        private async void BtnCopyCID_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtCID.Text);
            await ShowCopySuccessAsync(sender as Button);
        }

        private async void BtnCopyKey_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_fullKey);
            await ShowCopySuccessAsync(sender as Button);
        }

        private async void BtnCopyOemKey_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtOemKey.Text);
            await ShowCopySuccessAsync(sender as Button);
        }

        private async void BtnCopyAll_Click(object sender, RoutedEventArgs e)
        {
            string allInfo = $"HĐH: {txtOsName.Text}\n" +
                             $"Phiên bản: {txtVersion.Text}\n" +
                             $"Bản dựng: {txtBuild.Text}\n" +
                             $"Kiến trúc: {txtArchitecture.Text}\n" +
                             $"Ngày cài đặt: {txtInstallDate.Text}\n" +
                             $"Kênh phân phối: {txtChannel.Text}\n" +
                             $"Khóa OEM: {txtOemKey.Text}\n" +
                             $"Phiên bản OEM: {txtOemEdition.Text}\n" +
                             $"ID bản quyền (CID): {txtCID.Text}\n" +
                             $"Khóa sản phẩm: {_fullKey}";
            Clipboard.SetText(allInfo);
            await ShowCopySuccessAsync(sender as Button);
        }

        private async Task ShowCopySuccessAsync(Button btn)
        {
            if (btn == null) return;
            
            var originalContent = btn.Content;
            
            var tickIcon = new TextBlock
            {
                Text = "\uE73E",
                FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 16,
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078D4")),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            if (originalContent is StackPanel sp)
            {
                var newSp = new StackPanel { Orientation = Orientation.Horizontal };
                tickIcon.Margin = new Thickness(0, 0, 8, 0);
                newSp.Children.Add(tickIcon);
                
                string text = "";
                foreach(var child in sp.Children)
                {
                    if (child is TextBlock tb) text = tb.Text;
                }
                
                var textBlock = new TextBlock
                {
                    Text = text,
                    VerticalAlignment = VerticalAlignment.Center
                };
                newSp.Children.Add(textBlock);
                btn.Content = newSp;
            }
            else
            {
                btn.Content = tickIcon;
            }

            btn.IsEnabled = false;
            await Task.Delay(1500);
            btn.Content = originalContent;
            btn.IsEnabled = true;
        }

        private async void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            string productKey = tbInputKey.Text?.Trim();

            // Simple Validation
            if (string.IsNullOrEmpty(productKey))
            {
                Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgInputKey"), Helpers.LocalizationService.Get("msgWarning"));
                return;
            }

            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.ShowLoading("Activating Windows...", 0, 0);

            await Task.Run(async () =>
            {
                try
                {
                    // Step 1: Install Product Key
                    Dispatcher.Invoke(() => mainWindow?.UpdateLoadingProgress(0, 30, Helpers.LocalizationService.Get("msgInstallingKey")));
                    
                    string slmgrPath = System.IO.Path.Combine(Environment.SystemDirectory, "slmgr.vbs");
                    
                    var installResult = Helpers.Utilities.RunCommandWithOutput($"cscript //nologo \"{slmgrPath}\" /ipk {productKey}");
                    
                    if (installResult.ExitCode != 0 && !installResult.Output.Contains("successfully"))
                    {
                        throw new Exception($"Failed to install key.\n{installResult.Output}\n{installResult.Error}");
                    }

                    Dispatcher.Invoke(() => mainWindow?.UpdateLoadingProgress(0, 60, Helpers.LocalizationService.Get("msgKeyInstalled")));
                    await Task.Delay(500);

                    // Step 2: Activate
                    var activateResult = Helpers.Utilities.RunCommandWithOutput($"cscript //nologo \"{slmgrPath}\" /ato");

                    if (activateResult.ExitCode != 0 && !activateResult.Output.Contains("successfully"))
                    {
                         throw new Exception($"Activation failed.\n{activateResult.Output}\n{activateResult.Error}");
                    }

                    Dispatcher.Invoke(() => mainWindow?.UpdateLoadingProgress(0, 100, Helpers.LocalizationService.Get("msgActivationSuccess")));
                    await Task.Delay(500);

                    Dispatcher.Invoke(() => 
                    {
                        Components.CustomMessageBoxWindow.Show($"{Helpers.LocalizationService.Get("msgActivationSuccess")}\n\n{activateResult.Output}", Helpers.LocalizationService.Get("msgSuccess"));
                    });

                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => 
                    {
                        Components.CustomMessageBoxWindow.Show(string.Format(Helpers.LocalizationService.Get("msgActivationFail"), ex.Message), Helpers.LocalizationService.Get("msgError"));
                    });
                }
                finally
                {
                    Dispatcher.Invoke(() => mainWindow?.HideLoading());
                }
            });
        }

        private void BtnChangeKey_Click(object sender, RoutedEventArgs e)
        {
            NavigateRequested?.Invoke(this, "ConvertSKU");
        }

        private async void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            var backupWindow = new Components.LicenseBackupWindow();
            backupWindow.Owner = Window.GetWindow(this);
            
            if (backupWindow.ShowDialog() == true)
            {
                string path = backupWindow.SelectedPath;
                
                var mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow?.ShowLoading("Backing up license...", 0, 0);

                bool result = await Helpers.LicenseBackupHelper.BackupLicenseAsync(path, (step, percentage, msg) => 
                {
                    Dispatcher.Invoke(() => mainWindow?.UpdateLoadingProgress(step, percentage, msg));
                });

                mainWindow?.HideLoading();

                if (result)
                {
                    Components.CustomMessageBoxWindow.Show(
                        $"Đã sao lưu bản quyền thành công tại:\n{path}",
                        Helpers.LocalizationService.Get("msgSuccess"));
                }
                else
                {
                    Components.CustomMessageBoxWindow.Show(
                        "Quá trình sao lưu thất bại. Vui lòng thử chạy với quyền Administrator.",
                        Helpers.LocalizationService.Get("msgError"));
                }
            }
        }

        private async void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Components.LicenseRestoreWindow();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                string path = dialog.SelectedPath;

                var mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow?.ShowLoading("Restoring license...", 0, 0);

                bool result = await Helpers.LicenseBackupHelper.RestoreLicenseAsync(path, (step, percentage, msg) => 
                {
                    Dispatcher.Invoke(() => mainWindow?.UpdateLoadingProgress(step, percentage, msg));
                });

                mainWindow?.HideLoading();

                if (result)
                {
                    Components.CustomMessageBoxWindow.Show(
                        "Đã khôi phục bản quyền thành công. Hệ thống sẽ tự động cập nhật trạng thái.",
                        Helpers.LocalizationService.Get("msgSuccess"));
                    
                    await LoadLicenseInfo();
                }
                else
                {
                    Components.CustomMessageBoxWindow.Show(
                        "Khôi phục thất bại. Đường dẫn không đúng định dạng hoặc không có quyền truy cập.",
                        Helpers.LocalizationService.Get("msgError"));
                }
            }
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://support.microsoft.com/en-us/windows/activate-windows-c39005d4-95ee-b91e-b399-2820fda32227";
            if (Environment.OSVersion.Version.Build >= 22000)
            {
                url += "#windowsversion=windows_11";
            }
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void BtnSupport_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.facebook.com/ZitTechLab") { UseShellExecute = true });
        }

        private async void BtnRemoveWinHWID_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteRemovalTaskAsync(
                title: LocalizationService.Get("lblRemoveHWIDTitle") ?? "HWID Activation",
                confirmMsg: LocalizationService.Get("msgConfirmRemoveHWID") ?? "Bạn có chắc chắn muốn gỡ bỏ kích hoạt bản quyền số (HWID)?",
                taskFunc: progress => MasRemovalHelper.RemoveWindowsHwidAsync(progress)
            );
        }

        private async void BtnRemoveWinKMS_Click(object sender, RoutedEventArgs e)
        {
            Components.CustomMessageBoxWindow.Show(
                LocalizationService.Get("msgWarningOemKeyLoss") ?? "Lưu ý: Việc thực hiện gỡ bỏ MAS có thể làm ảnh hưởng tạm thời tới khóa OEM. Sau khi thực hiện xong, bắt buộc phải khởi động lại máy và nhập lại khóa OEM để kích hoạt lại.",
                LocalizationService.Get("msgWarning") ?? "Warning"
            );

            await ExecuteRemovalTaskAsync(
                title: LocalizationService.Get("lblRemoveWinKMSTitle") ?? "Online KMS",
                confirmMsg: LocalizationService.Get("msgConfirmRemoveWinKMS") ?? "Bạn có chắc chắn muốn gỡ bỏ kích hoạt Online KMS cho Windows?",
                taskFunc: progress => MasRemovalHelper.RemoveWindowsKmsAsync(progress)
            );
        }

        private async void BtnRemoveWinTSforce_Click(object sender, RoutedEventArgs e)
        {
            Components.CustomMessageBoxWindow.Show(
                LocalizationService.Get("msgWarningOemKeyLoss") ?? "Lưu ý: Việc thực hiện gỡ bỏ MAS có thể làm ảnh hưởng tạm thời tới khóa OEM. Sau khi thực hiện xong, bắt buộc phải khởi động lại máy và nhập lại khóa OEM để kích hoạt lại.",
                LocalizationService.Get("msgWarning") ?? "Warning"
            );

            await ExecuteRemovalTaskAsync(
                title: LocalizationService.Get("lblRemoveWinTSforceTitle") ?? "TSforce Activation",
                confirmMsg: LocalizationService.Get("msgConfirmRemoveWinTSforce") ?? "Bạn có chắc chắn muốn gỡ bỏ kích hoạt TSforce cho Windows?",
                taskFunc: progress => MasRemovalHelper.RemoveWindowsTsforgeAsync(progress)
            );
        }

        private async void BtnRemoveOhook_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteRemovalTaskAsync(
                title: LocalizationService.Get("lblRemoveOhookTitle") ?? "Ohook Activation",
                confirmMsg: LocalizationService.Get("msgConfirmRemoveOhook") ?? "Bạn có chắc chắn muốn gỡ bỏ kích hoạt Ohook cho Office?",
                taskFunc: progress => MasRemovalHelper.RemoveOfficeOhookAsync(progress)
            );
        }

        private async void BtnRemoveOffTSforce_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteRemovalTaskAsync(
                title: LocalizationService.Get("lblRemoveOffTSforceTitle") ?? "TSforce Activation",
                confirmMsg: LocalizationService.Get("msgConfirmRemoveOffTSforce") ?? "Bạn có chắc chắn muốn gỡ bỏ kích hoạt TSforce cho Office?",
                taskFunc: progress => MasRemovalHelper.RemoveOfficeTsforgeAsync(progress)
            );
        }

        private async void BtnRemoveOffKMS_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteRemovalTaskAsync(
                title: LocalizationService.Get("lblRemoveOffKMSTitle") ?? "Online KMS",
                confirmMsg: LocalizationService.Get("msgConfirmRemoveOffKMS") ?? "Bạn có chắc chắn muốn gỡ bỏ kích hoạt Online KMS cho Office?",
                taskFunc: progress => MasRemovalHelper.RemoveOfficeKmsAsync(progress)
            );
        }

        private async Task ExecuteRemovalTaskAsync(string title, string confirmMsg, Func<Action<int, int, string>, Task<bool>> taskFunc)
        {
            bool confirm = Components.CustomConfirmWindow.ShowConfirm(
                confirmMsg,
                title,
                LocalizationService.Get("btnYes") ?? "Có",
                LocalizationService.Get("btnNo") ?? "Không",
                Window.GetWindow(this)
            );

            if (!confirm) return;

            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.ShowLoading("Đang gỡ bỏ kích hoạt...", 0, 0);

            try
            {
                bool success = await taskFunc((step, percentage, msg) =>
                {
                    Dispatcher.Invoke(() => mainWindow?.UpdateLoadingProgress(step, percentage, msg));
                });

                mainWindow?.HideLoading();

                if (success)
                {
                    Components.CustomMessageBoxWindow.Show(
                        LocalizationService.Get("msgRemoveSuccess") ?? "Đã thực hiện gỡ bỏ kích hoạt thành công!",
                        LocalizationService.Get("msgSuccess") ?? "Thành công");

                    await LoadLicenseInfo();
                }
                else
                {
                    Components.CustomMessageBoxWindow.Show(
                        LocalizationService.Get("msgRemoveFailed") ?? "Quá trình gỡ bỏ kích hoạt thất bại. Vui lòng thử lại với quyền Administrator.",
                        LocalizationService.Get("msgError") ?? "Lỗi");
                }
            }
            catch (Exception ex)
            {
                mainWindow?.HideLoading();
                Components.CustomMessageBoxWindow.Show(
                    ex.Message,
                    LocalizationService.Get("msgError") ?? "Lỗi");
            }
        }
    }
}
