using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HyperLisUI.Helpers;

namespace HyperLisUI.Views
{
    public partial class ConvertSKUView : UserControl
    {
        private bool _isConverting;
        private WindowsInfo _windowsInfo;
        public ObservableCollection<LogMessage> LogMessages { get; } = new ObservableCollection<LogMessage>();

        private static readonly Dictionary<string, string> EditionToKey = new()
        {
            ["Home"] = "YTMG3-N6DKC-DKB77-7M9GH-8HVX7",
            ["Home Single Language"] = "BT79Q-G7N6G-PGBYW-4YWX6-6F4BT",
            ["Pro"] = "VK7JG-NPHTM-C97JM-9MPGT-3V66T",
            ["Pro for Workstations"] = "DXG7C-N36C4-C4HTG-X4T3X-2YV77",
            ["Pro Education"] = "8PTT6-RNW4C-6V7J2-C2D3X-MHBPB",
            ["Pro Single Language"] = "G3KNM-CHG6T-R36X3-9QDG6-8M8K9",
            ["Education"] = "YNMGQ-8RYV3-4PGQ3-C8XTP-7CFBY",
            ["Enterprise"] = "XGVPP-NMH47-7TTHJ-W3FW7-8HV2C",
            ["IoT Enterprise"] = "XQQYW-NFFMW-XJPBH-K8732-CKFFD",
            ["Enterprise LTSB 2015"] = "WNMTR-4C88C-JK8YV-HQ7T2-76DF9",
            ["Enterprise LTSB 2016"] = "DCPHK-NFMTC-H88MJ-PFHPY-QJ4BJ",
            ["Enterprise LTSC 2019"] = "M7XTQ-FN8P6-TTKYV-9D4CC-J462D",
            ["Enterprise LTSC 2021"] = "M7XTQ-FN8P6-TTKYV-9D4CC-J462D",
            ["Enterprise LTSC"] = "M7XTQ-FN8P6-TTKYV-9D4CC-J462D",
            ["IoT Enterprise LTSC"] = "QPM6N-7J2WJ-P88HH-P3YRH-YY74H",
            ["IoT Enterprise Subscription"] = "NBDJB-VW9CR-DFXGD-9F4D4-HFP4Q"
        };

        public ConvertSKUView() { InitializeComponent(); this.Loaded += ConvertSKUView_Loaded; LocalizationService.LanguageChanged += ApplyTranslations; LogList.ItemsSource = LogMessages; ApplyTranslations(); } 
        private void ApplyTranslations()
        {
            if (lblSection1Title != null) lblSection1Title.Text = LocalizationService.Get("lblConvertSKU_Sec1Title") ?? "1. Current Windows Info";
            if (lblConvertSKU_Title != null) lblConvertSKU_Title.Text = LocalizationService.Get("lblConvertSKU_Title") ?? "Convert SKUs - Change Windows Edition";
            if (lblConvertSKU_SubTitle != null) lblConvertSKU_SubTitle.Text = LocalizationService.Get("lblConvertSKU_SubTitle") ?? "Changing the current Edition of Windows to another Edition quickly and safely.";
            if (lblConvertSKU_EditionWarning != null) lblConvertSKU_EditionWarning.Text = LocalizationService.Get("lblConvertSKU_EditionWarning") ?? "Note: Some Editions may require a valid key for activation after conversion.";
            if (LogMessages.Count == 0)
            {
                AppendLog(LocalizationService.Get("lblConvertSKU_InitialLog") ?? "[System] Press the 'Convert Now' button to proceed...", "#444");
            }
            else if (LogMessages.Count == 1 && (LogMessages[0].Text.Contains("[System]") || LogMessages[0].Text.Contains("[Hệ thống]")))
            {
                var tempColor = LogMessages[0].Color;
                LogMessages.Clear();
                AppendLog(LocalizationService.Get("lblConvertSKU_InitialLog") ?? "[System] Press the 'Convert Now' button to proceed...", tempColor);
            }

            if (_windowsInfo != null && txtLicenseStatus != null && txtActivationTag != null)
            {
                UpdateLicenseStatusDisplay();
            }
            if (lblVersion != null) lblVersion.Text = LocalizationService.Get("lblConvertSKU_Version") ?? "Version";
            if (lblBuild != null) lblBuild.Text = LocalizationService.Get("lblConvertSKU_Build") ?? "Build";
            if (lblArchitecture != null) lblArchitecture.Text = LocalizationService.Get("lblConvertSKU_Arch") ?? "Architecture";
            if (lblProductID != null) lblProductID.Text = LocalizationService.Get("lblConvertSKU_ProductID") ?? "Product ID";
            if (lblInstallDate != null) lblInstallDate.Text = LocalizationService.Get("lblConvertSKU_InstallDate") ?? "Install date";
            if (lblLastBoot != null) lblLastBoot.Text = LocalizationService.Get("lblConvertSKU_LastBoot") ?? "Last boot";
            if (lblLicenseStatus != null) lblLicenseStatus.Text = LocalizationService.Get("lblConvertSKU_LicenseStatus") ?? "License status";
            if (lblConvertSKU_OEMEdition != null) lblConvertSKU_OEMEdition.Text = LocalizationService.Get("lblConvertSKU_OEMEdition") ?? "OEM Edition:";
            
            if (lblSection2Title != null) lblSection2Title.Text = LocalizationService.Get("lblConvertSKU_Sec2Title") ?? "2. Choose Windows Version";
            if (lblSection3Title != null) lblSection3Title.Text = LocalizationService.Get("lblConvertSKU_Sec3Title") ?? "3. Choose Target Edition";
            if (lblSection4Title != null) lblSection4Title.Text = LocalizationService.Get("lblConvertSKU_Sec4Title") ?? "4. Conversion Preview";
            if (lblPreviewCurrent != null) lblPreviewCurrent.Text = LocalizationService.Get("lblConvertSKU_PreviewCurrent") ?? "Current";
            if (lblPreviewTarget != null) lblPreviewTarget.Text = LocalizationService.Get("lblConvertSKU_PreviewTarget") ?? "Target";
            
            if (lblCompatTitle != null) lblCompatTitle.Text = LocalizationService.Get("lblConvertSKU_CompatTitle") ?? "Compatibility check";
            if (lblCompatStatus != null) lblCompatStatus.Text = LocalizationService.Get("lblConvertSKU_CompatStatus") ?? "Compatible";
            if (lblCompatDesc != null) lblCompatDesc.Text = LocalizationService.Get("lblConvertSKU_CompatDesc") ?? "Your device meets the requirements for conversion.";
            if (btnDetailsText != null) btnDetailsText.Text = LocalizationService.Get("lblConvertSKU_BtnDetails") ?? "Details";
            
            if (lblSection5Title != null) lblSection5Title.Text = LocalizationService.Get("lblConvertSKU_Sec5Title") ?? "5. Overview";
            if (lblTargetWin != null) lblTargetWin.Text = LocalizationService.Get("lblConvertSKU_TargetWin") ?? "Target Windows";
            if (lblTargetEdition != null) lblTargetEdition.Text = LocalizationService.Get("lblConvertSKU_TargetEdition") ?? "Target Edition";
            if (lblTargetArch != null) lblTargetArch.Text = LocalizationService.Get("lblConvertSKU_TargetArch") ?? "Architecture";
            if (lblMethod != null) lblMethod.Text = LocalizationService.Get("lblConvertSKU_Method") ?? "Method";
            if (lblActReq != null) lblActReq.Text = LocalizationService.Get("lblConvertSKU_ActReq") ?? "Activation Req";
            if (lblActReqText != null) lblActReqText.Text = LocalizationService.Get("lblConvertSKU_ActReqText") ?? "May require reactivation";
            if (lblRisk != null) lblRisk.Text = LocalizationService.Get("lblConvertSKU_Risk") ?? "Risk";
            if (lblRiskText != null) lblRiskText.Text = LocalizationService.Get("lblConvertSKU_RiskText") ?? "Low";
            
            if (lblSection6Title != null) lblSection6Title.Text = LocalizationService.Get("lblConvertSKU_Sec6Title") ?? "6. Logs";
            if (btnClearLogText != null) btnClearLogText.Text = LocalizationService.Get("lblConvertSKU_BtnClearLog") ?? "Clear Log";
            
            if (lblSection7Title != null) lblSection7Title.Text = LocalizationService.Get("lblConvertSKU_Sec7Title") ?? "7. Actions";
            if (btnLearnMoreText != null) btnLearnMoreText.Text = LocalizationService.Get("lblConvertSKU_BtnLearnMore") ?? "Learn more about SKUs";
            if (btnStartConvertText != null) btnStartConvertText.Text = LocalizationService.Get("lblConvertSKU_BtnStartConvert") ?? "Convert Now";
            if (txtActionWarning != null) txtActionWarning.Text = LocalizationService.Get("lblConvertSKU_ActionWarning") ?? "* The conversion process may take a few minutes and will restart the PC.";
            
            if (lblOverlayDetailsTitle != null) lblOverlayDetailsTitle.Text = LocalizationService.Get("lblConvertSKU_DetailsTitle") ?? "Device Compatibility Details";
            if (lblOverlaySysInfo != null) lblOverlaySysInfo.Text = LocalizationService.Get("lblConvertSKU_DetailsSysInfo") ?? "System Information:";
            if (lblOverlayOS != null) lblOverlayOS.Text = LocalizationService.Get("lblConvertSKU_DetailsOS") ?? "Operating System:";
            if (lblOverlayCPU != null) lblOverlayCPU.Text = LocalizationService.Get("lblConvertSKU_DetailsCPU") ?? "CPU Architecture:";
            if (lblOverlayReq != null) lblOverlayReq.Text = LocalizationService.Get("lblConvertSKU_DetailsReq") ?? "System Requirements:";
            if (lblOverlayReqAdmin != null) lblOverlayReqAdmin.Text = LocalizationService.Get("lblConvertSKU_DetailsReqAdmin") ?? "Application is running with Administrator privileges";
            if (lblOverlayReqSppsvc != null) lblOverlayReqSppsvc.Text = LocalizationService.Get("lblConvertSKU_DetailsReqSppsvc") ?? "Software Licensing Service (sppsvc) is running normally";
            if (lblOverlayWarning != null) lblOverlayWarning.Text = LocalizationService.Get("lblConvertSKU_DetailsWarning") ?? "Note: You may need to reactivate your license using a Product Key or Digital License after the system restarts.";
            
            if (btnOverlayCloseText != null) btnOverlayCloseText.Text = LocalizationService.Get("lblConvertSKU_BtnClose") ?? "Close";

            if (lblOverlayConfirmTitle != null) lblOverlayConfirmTitle.Text = LocalizationService.Get("lblConvertSKU_OverlayConfirmTitle") ?? "Confirm SKU Conversion";
            if (lblConfirmMsg != null) lblConfirmMsg.Text = LocalizationService.Get("lblConvertSKU_ConfirmMsg") ?? "Are you sure you want to switch from:";
            if (lblConfirmWarning != null) lblConfirmWarning.Text = LocalizationService.Get("lblConvertSKU_ConfirmWarning") ?? "After completion, you need to restart your computer.";
            if (btnConfirmNo != null) btnConfirmNo.Text = LocalizationService.Get("lblConvertSKU_BtnCancel") ?? "Cancel";
            if (btnConfirmYes != null) btnConfirmYes.Text = LocalizationService.Get("lblConvertSKU_BtnAgree") ?? "Agree";

            if (lblOverlayRestartTitle != null) lblOverlayRestartTitle.Text = LocalizationService.Get("lblConvertSKU_OverlayRestartTitle") ?? "Confirmation";
            if (lblRestartMsg != null) lblRestartMsg.Text = LocalizationService.Get("lblConvertSKU_RestartMsg") ?? "Restart your computer right now?";
            if (btnRestartNo != null) btnRestartNo.Text = LocalizationService.Get("lblConvertSKU_BtnCancel") ?? "Cancel";
            if (btnRestartYes != null) btnRestartYes.Text = LocalizationService.Get("lblConvertSKU_BtnRestart") ?? "Restart";

            if (lblProcessingTitle != null) lblProcessingTitle.Text = LocalizationService.Get("lblConvertSKU_ProcessingTitle") ?? "Processing...";
            if (lblProcessingSub != null) lblProcessingSub.Text = LocalizationService.Get("lblConvertSKU_ProcessingSub") ?? "Please wait a moment";
            if (lblStep1 != null) lblStep1.Text = LocalizationService.Get("lblConvertSKU_Step1") ?? "Preparing data";
            if (lblStep2 != null) lblStep2.Text = LocalizationService.Get("lblConvertSKU_Step2") ?? "Removing old settings";
            if (lblStep3 != null) lblStep3.Text = LocalizationService.Get("lblConvertSKU_Step3") ?? "Applying changes";
            if (lblStep4 != null) lblStep4.Text = LocalizationService.Get("lblConvertSKU_Step4") ?? "Completed";
            if (btnProcessingCancel != null) btnProcessingCancel.Text = LocalizationService.Get("btnProcessingCancel") ?? "Processing...";
            if (lblProcessingNote != null) lblProcessingNote.Text = LocalizationService.Get("lblConvertSKU_ProcessingNote") ?? "Do not close this window during processing.";

            // Success Overlay
            if (lblSuccessTitle != null) lblSuccessTitle.Text = LocalizationService.Get("lblConvertSKU_SuccessTitle") ?? "Successfully Completed!";
            if (lblSuccessSub != null) lblSuccessSub.Text = LocalizationService.Get("lblConvertSKU_SuccessSub") ?? "Windows SKU has been successfully converted.";
            if (lblSuccessNewVer != null) lblSuccessNewVer.Text = LocalizationService.Get("lblConvertSKU_SuccessNewVer") ?? "New Version:";
            if (lblSuccessTime != null) lblSuccessTime.Text = LocalizationService.Get("lblConvertSKU_SuccessTime") ?? "Time:";
            if (lblSuccessReq != null) lblSuccessReq.Text = LocalizationService.Get("lblConvertSKU_SuccessReq") ?? "Action Required:";
            if (txtSuccessReqVal != null) txtSuccessReqVal.Text = LocalizationService.Get("lblConvertSKU_SuccessReqVal") ?? "Restart";
            if (btnSuccessClose != null) btnSuccessClose.Text = LocalizationService.Get("btnSuccessClose") ?? "Close";
            if (btnSuccessRestart != null) btnSuccessRestart.Text = LocalizationService.Get("btnSuccessRestart") ?? "Restart Now";
            if (btnRestartText != null) btnRestartText.Text = LocalizationService.Get("lblConvertSKU_BtnRestartNow") ?? "Restart Now";

            // Bridge Upgrade Overlay
            if (lblOverlayBridgeTitle != null) lblOverlayBridgeTitle.Text = LocalizationService.Get("lblOverlayBridgeTitle") ?? "2-Step Upgrade Required";
            if (lblBridgeMsg1 != null) lblBridgeMsg1.Text = LocalizationService.Get("lblBridgeMsg1") ?? "Upgrading from Home to LTSC requires 2 steps:";
            if (lblBridgeMsg2 != null) lblBridgeMsg2.Text = LocalizationService.Get("lblBridgeMsg2") ?? "Step 1: Upgrade the system to Pro edition and restart.";
            if (lblBridgeMsg3 != null) lblBridgeMsg3.Text = LocalizationService.Get("lblBridgeMsg3") ?? "Step 2: Reopen this app and continue converting to LTSC.";
            if (lblBridgeMsg4 != null) lblBridgeMsg4.Text = LocalizationService.Get("lblBridgeMsg4") ?? "Do you want to start Step 1 (Upgrade to Pro) right now?";
            if (btnBridgeNo != null) btnBridgeNo.Text = LocalizationService.Get("btnBridgeNo") ?? "Cancel";
            if (btnBridgeYes != null) btnBridgeYes.Text = LocalizationService.Get("btnBridgeYes") ?? "Start Step 1";

            if (txtVersion != null && (txtVersion.Text == "Đang tải..." || txtVersion.Text == "Loading...")) txtVersion.Text = LocalizationService.Get("lblConvertSKU_Loading") ?? "Loading...";
            if (txtOverviewArchitecture != null && (txtOverviewArchitecture.Text == "Đang tải..." || txtOverviewArchitecture.Text == "Loading...")) txtOverviewArchitecture.Text = LocalizationService.Get("lblConvertSKU_Loading") ?? "Loading...";
        }

        private void UpdateLicenseStatusDisplay()
        {
            if (_windowsInfo == null) return;
            var info = _windowsInfo;

            string statusText = info.GetActivationStatusText();
            txtLicenseStatus.Text = statusText;

            if (info.IsActivated)
            {
                if (info.IsMasDetected)
                {
                    txtLicenseStatus.Text = $"{statusText} ({info.MasMethod})";
                    txtLicenseStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                    txtActivationTag.Text = info.MasMethod;
                    txtActivationTag.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                }
                else
                {
                    txtLicenseStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A7C36"));
                    txtActivationTag.Text = info.IsOemOriginalMatch 
                        ? (LocalizationService.Get("lblTagOemOriginalShort") ?? "OEM Gốc") 
                        : statusText;
                    txtActivationTag.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A7C36"));
                }
            }
            else
            {
                txtLicenseStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828")); // Red
                txtActivationTag.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"));
                txtActivationTag.Text = statusText;
            }

            txtChannelTag.Text = info.IsMasDetected ? $"{info.LicenseChannel} (MAS)" : info.LicenseChannel;

            if (txtOemEdition != null)
            {
                txtOemEdition.Text = string.IsNullOrEmpty(info.OemEdition) 
                    ? (LocalizationService.Get("msgConvertSKU_NoOEMKey") ?? "Not found") 
                    : info.OemEdition;
            }
        }

        private async void ConvertSKUView_Loaded(object sender, RoutedEventArgs e)
        {
            // Initial loading state is already set in XAML, but we can ensure "Đang tải..." here
            txtOsName.Text = LocalizationService.Get("lblConvertSKU_Loading") ?? "Loading...";
            txtVersion.Text = LocalizationService.Get("lblConvertSKU_Loading") ?? "Loading...";
            txtBuild.Text = LocalizationService.Get("lblConvertSKU_Loading") ?? "Loading...";
            txtArchitecture.Text = LocalizationService.Get("lblConvertSKU_Loading") ?? "Loading...";
            txtProductId.Text = LocalizationService.Get("lblConvertSKU_Loading") ?? "Loading...";
            txtInstallDate.Text = LocalizationService.Get("lblConvertSKU_Loading") ?? "Loading...";
            txtLastBoot.Text = LocalizationService.Get("lblConvertSKU_Loading") ?? "Loading...";
            txtLicenseStatus.Text = LocalizationService.Get("lblConvertSKU_Loading") ?? "Loading...";
            txtActivationTag.Text = "...";
            txtChannelTag.Text = "...";
            if (txtOemEdition != null) txtOemEdition.Text = LocalizationService.Get("lblConvertSKU_Loading") ?? "Loading...";

            // Run WMI queries on a background thread
            var info = await Task.Run(() => WindowsInfoHelper.GetCurrentWindowsInfo());
            _windowsInfo = info;

            // Update UI
            txtOsName.Text = info.OsName;
            txtVersion.Text = info.Version;
            txtBuild.Text = info.Build;
            txtArchitecture.Text = info.Architecture;
            txtProductId.Text = info.ProductId;
            txtInstallDate.Text = info.InstallDate;
            txtLastBoot.Text = info.LastBoot;
            
            UpdateLicenseStatusDisplay();

            // Set Logo based on OS Name
            if (info.OsName.Contains("Windows 10"))
            {
                imgOsLogo.Source = new BitmapImage(new Uri("pack://application:,,,/Images/windows10.png"));
                rbWin10.Visibility = Visibility.Visible;
                rbWin10LTS.Visibility = Visibility.Visible;
                rbWin11.Visibility = Visibility.Collapsed;
                rbWin11LTSC.Visibility = Visibility.Collapsed;
                rbWin10.IsChecked = true;
            }
            else
            {
                imgOsLogo.Source = new BitmapImage(new Uri("pack://application:,,,/Images/windows11.png"));
                rbWin11.Visibility = Visibility.Visible;
                rbWin11LTSC.Visibility = Visibility.Visible;
                rbWin10.Visibility = Visibility.Collapsed;
                rbWin10LTS.Visibility = Visibility.Collapsed;
                rbWin11.IsChecked = true;
            }

            txtPreviewCurrentName.Text = info.OsName.Replace("Microsoft ", "");
            if (info.OsName.Contains("Windows 10"))
                imgPreviewCurrentLogo.Source = new BitmapImage(new Uri("pack://application:,,,/Images/windows10.png"));
            else
                imgPreviewCurrentLogo.Source = new BitmapImage(new Uri("pack://application:,,,/Images/windows11.png"));
            
            txtOverviewArchitecture.Text = info.Architecture;

            UpdatePreviewTarget();
        }

        private void rbOSVersion_Checked(object sender, RoutedEventArgs e)
        {
            if (panelRegularEditions == null || panelWin10LTS == null || panelWin11LTSC == null) return;

            if (rbWin10.IsChecked == true || rbWin11.IsChecked == true)
            {
                panelRegularEditions.Visibility = Visibility.Visible;
                panelWin10LTS.Visibility = Visibility.Collapsed;
                panelWin11LTSC.Visibility = Visibility.Collapsed;
            }
            else if (rbWin10LTS.IsChecked == true)
            {
                panelRegularEditions.Visibility = Visibility.Collapsed;
                panelWin10LTS.Visibility = Visibility.Visible;
                panelWin11LTSC.Visibility = Visibility.Collapsed;
            }
            else if (rbWin11LTSC.IsChecked == true)
            {
                panelRegularEditions.Visibility = Visibility.Collapsed;
                panelWin10LTS.Visibility = Visibility.Collapsed;
                panelWin11LTSC.Visibility = Visibility.Visible;
            }

            UpdatePreviewTarget();
        }

        private void Edition_Checked(object sender, RoutedEventArgs e)
        {
            UpdatePreviewTarget();
        }

        private void UpdatePreviewTarget()
        {
            if (txtPreviewTargetName == null || imgPreviewTargetLogo == null) return;

            string baseOs = "";
            string edition = "";
            
            // Determine Base OS
            if (rbWin10.IsChecked == true || rbWin10LTS.IsChecked == true)
            {
                baseOs = "Windows 10";
                imgPreviewTargetLogo.Source = new BitmapImage(new Uri("pack://application:,,,/Images/windows10.png"));
            }
            else if (rbWin11.IsChecked == true || rbWin11LTSC.IsChecked == true)
            {
                baseOs = "Windows 11";
                imgPreviewTargetLogo.Source = new BitmapImage(new Uri("pack://application:,,,/Images/windows11.png"));
            }

            // Determine Edition
            if (panelRegularEditions.Visibility == Visibility.Visible)
            {
                foreach (var child in FindVisualChildren<RadioButton>(panelRegularEditions))
                {
                    if (child.IsChecked == true)
                    {
                        var stackPanel = child.Content as StackPanel;
                        if (stackPanel != null && stackPanel.Children.Count > 1)
                        {
                            var textBlock = stackPanel.Children[1] as TextBlock;
                            if (textBlock != null) edition = textBlock.Text;
                        }
                        break;
                    }
                }
            }
            else if (panelWin10LTS.Visibility == Visibility.Visible)
            {
                foreach (var child in FindVisualChildren<RadioButton>(panelWin10LTS))
                {
                    if (child.IsChecked == true)
                    {
                        var stackPanel = child.Content as StackPanel;
                        if (stackPanel != null && stackPanel.Children.Count > 1)
                        {
                            var textBlock = stackPanel.Children[1] as TextBlock;
                            if (textBlock != null) edition = "Enterprise " + textBlock.Text; 
                        }
                        break;
                    }
                }
            }
            else if (panelWin11LTSC.Visibility == Visibility.Visible)
            {
                foreach (var child in FindVisualChildren<RadioButton>(panelWin11LTSC))
                {
                    if (child.IsChecked == true)
                    {
                        var stackPanel = child.Content as StackPanel;
                        if (stackPanel != null && stackPanel.Children.Count > 1)
                        {
                            var textBlock = stackPanel.Children[1] as TextBlock;
                            if (textBlock != null) edition = textBlock.Text; 
                        }
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(baseOs) && !string.IsNullOrEmpty(edition))
            {
                txtPreviewTargetName.Text = baseOs + " " + edition;
                txtOverviewTargetWindows.Text = baseOs;
                txtOverviewTargetEdition.Text = edition;
            }
            else
            {
                txtPreviewTargetName.Text = LocalizationService.Get("lblConvertSKU_PreviewTargetSelect") ?? "Select target edition";
                txtOverviewTargetWindows.Text = "...";
                txtOverviewTargetEdition.Text = "...";
            }
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void BtnLearnMore_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://learn.microsoft.com/en-us/windows/deployment/upgrade/windows-edition-upgrades") { UseShellExecute = true });
        }

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            txtDetailsOS.Text = txtPreviewCurrentName.Text;
            txtDetailsArch.Text = txtOverviewArchitecture.Text;

            var winVer = txtOverviewTargetWindows.Text.Contains("10") ? "10" : "11";
            var targetEdition = txtOverviewTargetEdition.Text;
            
            string skuFolderName = $"SKUs-Windows-{winVer}";
            if (targetEdition.Contains("LTS") || targetEdition.Contains("IoT"))
            {
                skuFolderName += (winVer == "10") ? "-LTS" : "-LTSC";
            }

            var skuPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "SKUs", skuFolderName);
            if (Directory.Exists(skuPath))
            {
                iconDetailsSKUs.Text = "\uF29A"; // Checked
                iconDetailsSKUs.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#107C41"));
                txtDetailsSKUs.Text = LocalizationService.Get("lblConvertSKU_SkuReady")?.Replace("{0}", skuFolderName) ?? $"The {skuFolderName} packages are ready";
            }
            else
            {
                iconDetailsSKUs.Text = "\uF36E"; // Error
                iconDetailsSKUs.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D83B01"));
                txtDetailsSKUs.Text = LocalizationService.Get("lblConvertSKU_SkuNotExist")?.Replace("{0}", skuFolderName) ?? $"The {skuFolderName} packages do not exist";
            }

            OverlayDetails.Visibility = Visibility.Visible;
        }

        private void BtnCloseDetails_Click(object sender, RoutedEventArgs e)
        {
            OverlayDetails.Visibility = Visibility.Collapsed;
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            OverlayRestart.Visibility = Visibility.Visible;
        }

        private void BtnCancelRestart_Click(object sender, RoutedEventArgs e)
        {
            OverlayRestart.Visibility = Visibility.Collapsed;
        }

        private void BtnYesRestart_Click(object sender, RoutedEventArgs e)
        {
            string restartMsg = LocalizationService.Get("lblConvertSKU_RestartApply") ?? "Restarting to apply SKU conversion";
            Process.Start("shutdown", $"/r /t 5 /c \"{restartMsg}\"");
            Application.Current.Shutdown();
        }

        private void BtnStartConversion_Click(object sender, RoutedEventArgs e)
        {
            if (_isConverting) return;
            
            var targetWindows = txtOverviewTargetWindows.Text;
            var targetEdition = txtOverviewTargetEdition.Text;

            if (targetWindows == "..." || targetEdition == "..." || string.IsNullOrEmpty(targetWindows))
            {
                string msg = LocalizationService.Get("lblConvertSKU_MsgSelectTarget") ?? "Please select the Target Windows version and Edition.";
                string title = LocalizationService.Get("msgTitleNotice") ?? "Notice";
                MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isHomeToLTSC = (txtPreviewCurrentName.Text.IndexOf("Home", StringComparison.OrdinalIgnoreCase) >= 0 || txtPreviewCurrentName.Text.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0) &&
                                (targetEdition.IndexOf("LTS", StringComparison.OrdinalIgnoreCase) >= 0 || targetEdition.IndexOf("Enterprise", StringComparison.OrdinalIgnoreCase) >= 0);

            if (isHomeToLTSC)
            {
                OverlayBridgeUpgrade.Visibility = Visibility.Visible;
                return;
            }

            ShowConfirmOverlay(targetWindows, targetEdition);
        }

        private void BtnCancelBridge_Click(object sender, RoutedEventArgs e)
        {
            OverlayBridgeUpgrade.Visibility = Visibility.Collapsed;
        }

        private void BtnYesBridge_Click(object sender, RoutedEventArgs e)
        {
            OverlayBridgeUpgrade.Visibility = Visibility.Collapsed;
            var targetWindows = txtOverviewTargetWindows.Text;
            var targetEdition = "Pro";
            txtOverviewTargetEdition.Text = "Pro";

            ShowConfirmOverlay(targetWindows, targetEdition);
        }

        private void ShowConfirmOverlay(string targetWindows, string targetEdition)
        {
            txtConfirmCurrent.Text = txtPreviewCurrentName.Text;
            txtConfirmTarget.Text = $"{targetWindows} {targetEdition}";
            
            if (targetWindows.Contains("11"))
            {
                imgConfirmOS.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Images/windows11.png"));
            }
            else
            {
                imgConfirmOS.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Images/windows10.png"));
            }
            
            OverlayConfirm.Visibility = Visibility.Visible;
        }

        private void BtnCancelConfirm_Click(object sender, RoutedEventArgs e)
        {
            OverlayConfirm.Visibility = Visibility.Collapsed;
        }

        private void BtnYesConfirm_Click(object sender, RoutedEventArgs e)
        {
            OverlayConfirm.Visibility = Visibility.Collapsed;
            _isConverting = true;
            btnStartConversion.IsEnabled = false;
            LogMessages.Clear();
            btnRestart.Visibility = Visibility.Collapsed;
            
            var targetWindows = txtOverviewTargetWindows.Text;
            var targetEdition = txtOverviewTargetEdition.Text;
            
            OverlayProcessing.Visibility = Visibility.Visible;
            ResetProcessingUI();
            
            _ = RunConversionAsync(targetWindows, targetEdition);
        }

        private void BtnCloseSuccess_Click(object sender, RoutedEventArgs e)
        {
            OverlaySuccess.Visibility = Visibility.Collapsed;
        }

        private void ResetProcessingUI()
        {
            progressBar.Value = 0;
            txtProgressPercentOutside.Text = "0%";
            var grayBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E5E5"));
            iconStep1.Foreground = grayBrush;
            iconStep2.Foreground = grayBrush;
            iconStep3.Foreground = grayBrush;
            iconStep4.Foreground = grayBrush;
            
            iconStep1.Text = "\uF29A"; 
            iconStep2.Text = "\uF29A";
            iconStep3.Text = "\uF29A";
            iconStep4.Text = "\uF29A";
        }

        private void SetProgress(int value)
        {
            Dispatcher.Invoke(() => 
            { 
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = value,
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                progressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, animation);
                txtProgressPercentOutside.Text = $"{value}%"; 
            });
        }

        private void SetStepActive(TextBlock icon)
        {
            Dispatcher.Invoke(() => 
            { 
                icon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4")); 
                icon.Text = "\uF29A"; 
            });
        }

        private async Task RunConversionAsync(string targetWindows, string targetEdition)
        {
            try
            {
                bool isEn = LocalizationService.CurrentLanguage.Equals("EN", StringComparison.OrdinalIgnoreCase);
                var winVer = targetWindows.Contains("10") ? "10" : "11";
                if (!EditionToKey.TryGetValue(targetEdition, out var productKey))
                {
                    string msgErr = LocalizationService.Get("lblConvertSKU_LogErrNoKey")?.Replace("{0}", targetEdition) ?? $"❌ Error: No Product Key found for edition '{targetEdition}'.";
                    AppendLog(msgErr, "Red");
                    ShowDone(false);
                    return;
                }

                SetProgress(5);
                string msgStart = LocalizationService.Get("lblConvertSKU_LogStarting")?.Replace("{0}", txtPreviewCurrentName.Text).Replace("{1}", targetWindows).Replace("{2}", targetEdition) 
                                  ?? $"🔄 Starting: {txtPreviewCurrentName.Text} → {targetWindows} {targetEdition}";
                AppendLog(msgStart, "#0078D7");
                AppendLog($"🔑 Product Key: {productKey}\n", "#0078D7");

                string skuFolderName = $"SKUs-Windows-{winVer}";
                if (targetEdition.Contains("LTS") || targetEdition.Contains("IoT"))
                {
                    skuFolderName += (winVer == "10") ? "-LTS" : "-LTSC";
                }

                var skuBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "SKUs");
                var skuSource = Path.Combine(skuBase, skuFolderName);
                
                if (!Directory.Exists(skuSource)) 
                { 
                    string msgNoSku = LocalizationService.Get("lblConvertSKU_LogNoSku")?.Replace("{0}", skuSource) ?? $"❌ SKU files directory not found at:\n   {skuSource}";
                    AppendLog(msgNoSku, "Red"); 
                    ShowDone(false); 
                    return; 
                }

                var target = @"C:\Windows\System32\spp\tokens\skus";
                
                SetProgress(10);
                string logStep1 = LocalizationService.Get("lblConvertSKU_LogStep1") ?? "🔐 [Step 1] Taking ownership of target directory...";
                AppendLog(logStep1, "#0078D7");
                await RunCommandAsync($"takeown /F \"{target}\" /R /D Y");
                string logTakeown = LocalizationService.Get("lblConvertSKU_LogTakeown") ?? "   ✓ Takeown completed";
                AppendLog(logTakeown, "Green");
                
                SetProgress(15);
                await RunCommandAsync($"icacls \"{target}\" /grant Administrators:F /T /Q");
                string logGrant = LocalizationService.Get("lblConvertSKU_LogGrant") ?? "   ✓ Grant permission completed\n";
                AppendLog(logGrant, "Green");

                SetProgress(20);
                string logStep2 = LocalizationService.Get("lblConvertSKU_LogStep2") ?? "📁 [Step 2] Copying SKU files...";
                AppendLog(logStep2, "#0078D7");
                AppendLog($"   Source: {skuSource}", "#555");
                AppendLog($"   Target:  {target}", "#555");
                await RunCommandAsync($"xcopy \"{skuSource}\" \"{target}\" /E /I /Y /Q");
                string logCopySuccess = LocalizationService.Get("lblConvertSKU_LogCopySuccess") ?? "   ✅ Copy successful!\n";
                AppendLog(logCopySuccess, "Green");
                
                SetProgress(30);
                SetStepActive(iconStep1);

                string logStep3 = LocalizationService.Get("lblConvertSKU_LogStep3") ?? "⚙️ [Step 3] Executing SKU conversion...\n";
                AppendLog(logStep3, "#0078D7");
                
                var cmdsList = new System.Collections.Generic.List<(string key, string cmd)>();
                cmdsList.Add(("lblConvertSKU_CmdReinstall", "cscript.exe %windir%\\system32\\slmgr.vbs /rilc"));
                
                // Do not use /upk before /ipk to prevent license loss if it fails
                cmdsList.Add(("lblConvertSKU_CmdClearKms", "cscript.exe %windir%\\system32\\slmgr.vbs /ckms >nul 2>&1"));

                string productKeyToInstall = productKey;
                if (_windowsInfo != null && !string.IsNullOrWhiteSpace(_windowsInfo.OemKey))
                {
                    string oem = _windowsInfo.OemEdition?.ToLowerInvariant() ?? "";
                    string tgt = targetEdition.ToLowerInvariant();
                    bool isMatch = false;

                    if (oem.Contains("single language") && tgt.Contains("single language")) isMatch = true;
                    else if ((oem == "home" || oem == "core") && tgt == "home") isMatch = true;
                    else if (oem == "pro" && tgt == "pro") isMatch = true;
                    else if (oem == tgt) isMatch = true;

                    if (isMatch)
                    {
                        productKeyToInstall = _windowsInfo.OemKey;
                    }
                }

                cmdsList.Add(("lblConvertSKU_CmdInstallKey", $"cscript.exe %windir%\\system32\\slmgr.vbs /ipk {productKeyToInstall}"));
                cmdsList.Add(("lblConvertSKU_CmdClearKeyCache", "cscript.exe %windir%\\system32\\slmgr.vbs /cpky >nul 2>&1"));
                cmdsList.Add(("lblConvertSKU_CmdStartLicMgr", "sc config LicenseManager start= auto & net start LicenseManager"));
                cmdsList.Add(("lblConvertSKU_CmdStartWinUpd", "sc config wuauserv start= auto & net start wuauserv"));
                cmdsList.Add(("lblConvertSKU_CmdRunClipup", "clipup -v -o -altto c:\\"));

                var cmds = cmdsList.ToArray();

                for (int i = 0; i < cmds.Length; i++)
                {
                    string fallbackDesc = cmds[i].key switch {
                        "lblConvertSKU_CmdReinstall" => "Reinstall license",
                        "lblConvertSKU_CmdUninstallKey" => "Uninstall old product key",
                        "lblConvertSKU_CmdClearKms" => "Clear KMS server",
                        "lblConvertSKU_CmdClearKeyCache" => "Clear product key cache",
                        "lblConvertSKU_CmdInstallKey" => $"Install key {targetEdition}",
                        "lblConvertSKU_CmdStartLicMgr" => "Start License Manager",
                        "lblConvertSKU_CmdStartWinUpd" => "Start Windows Update",
                        "lblConvertSKU_CmdRunClipup" => "Run clipup",
                        _ => "Executing command"
                    };
                    string desc = LocalizationService.Get(cmds[i].key)?.Replace("{0}", targetEdition) ?? fallbackDesc;
                    


                    AppendLog($"   ▶ {desc}...", "#555");
                    var output = await RunCommandAsync(cmds[i].cmd);
                    if (!string.IsNullOrWhiteSpace(output))
                        foreach (var l in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                            if (!string.IsNullOrWhiteSpace(l.Trim()))
                                AppendLog($"   {l.Trim()}", "#777");
                    
                    string logCmdCompleted = LocalizationService.Get("lblConvertSKU_LogCmdCompleted") ?? "   ✓ Completed";
                    AppendLog(logCmdCompleted, "Green");
                    
                    int progressStep = 30 + (65 * (i + 1) / cmds.Length);
                    SetProgress(progressStep);
                    if (i == cmds.Length / 2) SetStepActive(iconStep2);
                    if (i == cmds.Length - 1) SetStepActive(iconStep3);
                }

                SetProgress(100);
                SetStepActive(iconStep4);

                AppendLog("\n══════════════════════════════════════", "#333");
                string logComplete = LocalizationService.Get("lblConvertSKU_LogComplete") ?? "🎉 SKU CONVERSION COMPLETE!";
                AppendLog(logComplete, "Green");
                string logRestart = LocalizationService.Get("lblConvertSKU_LogRestart") ?? "🔄 Please restart your computer.";
                AppendLog(logRestart, "#0078D7");
                AppendLog("══════════════════════════════════════", "#333");
                
                await Task.Delay(500); 
                ShowDone(true, targetWindows, targetEdition);
            }
            catch (Exception ex)
            {
                string logErrUnexpect = LocalizationService.Get("lblConvertSKU_LogErrUnexpect")?.Replace("{0}", ex.Message) ?? $"\n❌ Unexpected error: {ex.Message}";
                AppendLog(logErrUnexpect, "Red");
                ShowDone(false);
            }
        }

        private async Task<string> RunCommandAsync(string command)
        {
            return await Task.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                using var process = Process.Start(psi);
                if (process == null) return string.Empty;
                
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                process.WaitForExit(60000); 
                var output = outputTask.Result;
                var error = errorTask.Result;
                return string.IsNullOrWhiteSpace(output) ? error : output;
            });
        }

        private void AppendLog(string msg, string color = "#444")
        {
            Dispatcher.Invoke(() =>
            {
                LogMessages.Add(new LogMessage { Text = msg, Color = color });
                if (LogList.Items.Count > 0)
                {
                    LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
                }
            });
        }

        private void ShowDone(bool success, string targetWindows = "", string targetEdition = "")
        {
            Dispatcher.Invoke(() =>
            {
                OverlayProcessing.Visibility = Visibility.Collapsed;
                btnStartConversion.IsEnabled = true;
                if (success) 
                {
                    btnRestart.Visibility = Visibility.Visible;
                    btnStartConversion.Visibility = Visibility.Collapsed;
                    
                    txtSuccessTargetName.Text = $"{targetWindows} {targetEdition}";
                    txtSuccessTimeVal.Text = DateTime.Now.ToString("HH:mm dd/MM/yyyy");
                    
                    if (targetWindows.Contains("11"))
                    {
                        imgSuccessOS.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Images/windows11.png"));
                    }
                    else
                    {
                        imgSuccessOS.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Images/windows10.png"));
                    }
                    
                    OverlaySuccess.Visibility = Visibility.Visible;
                }
                _isConverting = false;
            });
        }

        private void LogScroller_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var listBox = sender as System.Windows.Controls.ListBox;
            if (listBox == null) return;
            var border = System.Windows.Media.VisualTreeHelper.GetChild(listBox, 0) as System.Windows.Controls.Decorator;
            var scv = border?.Child as System.Windows.Controls.ScrollViewer;
            if (scv == null) return;
            
            bool canScroll = false;
            if (e.Delta < 0 && scv.VerticalOffset < scv.ScrollableHeight) canScroll = true;
            if (e.Delta > 0 && scv.VerticalOffset > 0) canScroll = true;
            
            if (!canScroll && !e.Handled)
            {
                e.Handled = true;
                var eventArg = new System.Windows.Input.MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = System.Windows.UIElement.MouseWheelEvent,
                    Source = sender
                };
                var parent = ((System.Windows.FrameworkElement)sender).Parent as System.Windows.UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }
    }
}





