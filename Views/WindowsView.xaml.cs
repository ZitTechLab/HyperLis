using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace HyperLisUI.Views
{
    public partial class WindowsView : UserControl
    {
        private bool _suppressEvent = false;

        public WindowsView()
        {
            InitializeComponent();
            this.Loaded += WindowsView_Loaded;
            Helpers.LocalizationService.LanguageChanged += ApplyTranslations;
        }

        private void ApplyTranslations()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ApplyTranslations);
                return;
            }

            lblWinSystem.Text = Helpers.LocalizationService.Get("lblWinSystem");
            lblWinPrivacy.Text = Helpers.LocalizationService.Get("lblWinPrivacy");
            lblWinGaming.Text = Helpers.LocalizationService.Get("lblWinGaming");
            lblWinUpdate.Text = Helpers.LocalizationService.Get("lblWinUpdate");
            lblWinTaskbar.Text = Helpers.LocalizationService.Get("lblWinTaskbar");
            lblWinExtras.Text = Helpers.LocalizationService.Get("lblWinExtras");
            lblWinAdvanced.Text = Helpers.LocalizationService.Get("lblWinAdvanced");

            // System
            DisableMyPeopleCheck.Content = Helpers.LocalizationService.Get("chkDisableMyPeople");
            DisableTPMCheckCheck.Content = Helpers.LocalizationService.Get("chkDisableTPMCheck");
            EnableLongPathsCheck.Content = Helpers.LocalizationService.Get("chkEnableLongPaths");
            DisableSensorServicesCheck.Content = Helpers.LocalizationService.Get("chkDisableSensorServices");
            
            // Dynamic Content for VBS / Volume Mixer
            if (Helpers.Utilities.IsWindows11)
            {
                VBSOrVolumeMixerCheck.Content = Helpers.LocalizationService.Get("chkVBSOrVolumeMixer");
            }
            else
            {
                VBSOrVolumeMixerCheck.Content = Helpers.LocalizationService.Get("chkClassicVolumeMixer");
            }

            // Privacy
            DisableTelemetryCheck.Content = Helpers.LocalizationService.Get("chkDisableTelemetry");
            DisableCortanaCheck.Content = Helpers.LocalizationService.Get("chkDisableCortana");
            DisableCopilotCheck.Content = Helpers.LocalizationService.Get("chkDisableCopilot");
            EnhancePrivacyCheck.Content = Helpers.LocalizationService.Get("chkEnhancePrivacy");
            DisableStartMenuAdsCheck.Content = Helpers.LocalizationService.Get("chkDisableStartMenuAds");

            // Browser AI
            lblBrowserAI.Text = Helpers.LocalizationService.Get("lblBrowserAI");
            optAllBrowsers.Content = Helpers.LocalizationService.Get("optAllBrowsers");
            btnRemoveAI.Content = Helpers.LocalizationService.Get("btnRemoveAI");
            btnRestoreAI.Content = Helpers.LocalizationService.Get("btnRestoreAI");

            // Gaming
            EnableGamingModeCheck.Content = Helpers.LocalizationService.Get("chkEnableGamingMode");
            DisableGameBarCheck.Content = Helpers.LocalizationService.Get("chkDisableGameBar");
            DisableXboxLiveCheck.Content = Helpers.LocalizationService.Get("chkDisableXboxLive");
            DisableMouseAccelerationCheck.Content = Helpers.LocalizationService.Get("chkDisableMouseAcceleration");

            // Power Plan
            lblWinPowerPlan.Text = Helpers.LocalizationService.Get("lblWinPowerPlan");
            EnableHighPerformanceCheck.Content = Helpers.LocalizationService.Get("chkEnableHighPerformance");
            EnableUltimatePerformanceCheck.Content = Helpers.LocalizationService.Get("chkEnableUltimatePerformance");

            // Update
            PauseUpdatesCheck.Content = Helpers.LocalizationService.Get("chkPauseUpdates");
            DisableMSStoreUpdatesCheck.Content = Helpers.LocalizationService.Get("chkDisableMSStoreUpdates");
            DisableInsiderServiceCheck.Content = Helpers.LocalizationService.Get("chkDisableInsiderService");
            ExcludeDriversCheck.Content = Helpers.LocalizationService.Get("chkExcludeDrivers");
            DisableWindowsUpdateCheck.Content = Helpers.LocalizationService.Get("btnDisableWinUpdate");

            // Taskbar
            AlignTaskbarLeftCheck.Content = Helpers.LocalizationService.Get("chkAlignTaskbarLeft");
            DisableWidgetsCheck.Content = Helpers.LocalizationService.Get("chkDisableWidgets");
            EnableSmallTaskbarIconsCheck.Content = Helpers.LocalizationService.Get("chkEnableSmallTaskbarIcons");
            DisableChatCheck.Content = Helpers.LocalizationService.Get("chkDisableChat");
            DisableStickersCheck.Content = Helpers.LocalizationService.Get("chkDisableStickers");
            FixPinToTaskbarCheck.Content = Helpers.LocalizationService.Get("chkFixPinToTaskbar");
            DisableNotificationCenterCheck.Content = Helpers.LocalizationService.Get("chkDisableNotificationCenter");

            // Extras
            DisableSnapAssistCheck.Content = Helpers.LocalizationService.Get("chkDisableSnapAssist");
            EnableClassicRightClickCheck.Content = Helpers.LocalizationService.Get("chkEnableClassicRightClick");
            ShowFileExtensionsCheck.Content = Helpers.LocalizationService.Get("chkShowFileExtensions");
            EnableCompactModeCheck.Content = Helpers.LocalizationService.Get("chkEnableCompactMode");
            EnableNumLockCheck.Content = Helpers.LocalizationService.Get("chkEnableNumLock");
            ShowHiddenFilesCheck.Content = Helpers.LocalizationService.Get("chkShowHiddenFiles");

            // Advanced
            ForWorking.Content = Helpers.LocalizationService.Get("chkForWorking");
            ResetToDefault.Content = Helpers.LocalizationService.Get("chkResetToDefault");
            ForExtremeGaming.Content = Helpers.LocalizationService.Get("chkForExtremeGaming");

            // --- TOOLTIP CONTENT ---
            ApplyTooltips();
        }

        private void ApplyTooltips()
        {
            // System
            SetTooltip(DisableMyPeopleCheck, "chkDisableMyPeopleToolTip");
            SetTooltip(EnableLongPathsCheck, "chkEnableLongPathsToolTip");
            SetTooltip(DisableSensorServicesCheck, "chkDisableSensorServicesToolTip");
            SetTooltip(VBSOrVolumeMixerCheck, Helpers.Utilities.IsWindows11 ? "chkVBSOrVolumeMixerToolTip" : "chkClassicVolumeMixerToolTip");

            // Privacy
            SetTooltip(DisableTelemetryCheck, "chkDisableTelemetryToolTip");
            SetTooltip(DisableCortanaCheck, "chkDisableCortanaToolTip");
            SetTooltip(DisableCopilotCheck, "chkDisableCopilotToolTip");
            SetTooltip(EnhancePrivacyCheck, "chkEnhancePrivacyToolTip");
            SetTooltip(DisableStartMenuAdsCheck, "chkDisableStartMenuAdsToolTip");

            // Gaming
            SetTooltip(EnableGamingModeCheck, "chkEnableGamingModeToolTip");
            SetTooltip(DisableGameBarCheck, "chkDisableGameBarToolTip");
            SetTooltip(DisableXboxLiveCheck, "chkDisableXboxLiveToolTip");
            SetTooltip(DisableMouseAccelerationCheck, "chkDisableMouseAccelerationToolTip");

            // Power Plan
            SetTooltip(EnableHighPerformanceCheck, "chkEnableHighPerformanceToolTip");
            SetTooltip(EnableUltimatePerformanceCheck, "chkEnableUltimatePerformanceToolTip");

            // Update
            SetTooltip(PauseUpdatesCheck, "chkPauseUpdatesToolTip");
            SetTooltip(DisableMSStoreUpdatesCheck, "chkDisableMSStoreUpdatesToolTip");
            SetTooltip(DisableInsiderServiceCheck, "chkDisableInsiderServiceToolTip");
            SetTooltip(ExcludeDriversCheck, "chkExcludeDriversToolTip");
            SetTooltip(DisableWindowsUpdateCheck, "chkDisableWindowsUpdateToolTip");

            // Taskbar
            SetTooltip(AlignTaskbarLeftCheck, "chkAlignTaskbarLeftToolTip");
            SetTooltip(DisableWidgetsCheck, "chkDisableWidgetsToolTip");
            SetTooltip(EnableSmallTaskbarIconsCheck, "chkEnableSmallTaskbarIconsToolTip");
            SetTooltip(DisableChatCheck, "chkDisableChatToolTip");
            SetTooltip(DisableStickersCheck, "chkDisableStickersToolTip");
            SetTooltip(FixPinToTaskbarCheck, "chkFixPinToTaskbarToolTip");
            SetTooltip(DisableNotificationCenterCheck, "chkDisableNotificationCenterToolTip");

            // Extras
            SetTooltip(DisableSnapAssistCheck, "chkDisableSnapAssistToolTip");
            SetTooltip(EnableClassicRightClickCheck, "chkEnableClassicRightClickToolTip");
            SetTooltip(ShowFileExtensionsCheck, "chkShowFileExtensionsToolTip");
            SetTooltip(EnableCompactModeCheck, "chkEnableCompactModeToolTip");
            SetTooltip(EnableNumLockCheck, "chkEnableNumLockToolTip");
            SetTooltip(ShowHiddenFilesCheck, "chkShowHiddenFilesToolTip");

            // Advanced
            SetTooltip(ForWorking, "chkForWorkingToolTip");
            SetTooltip(ResetToDefault, "chkResetToDefaultToolTip");
            SetTooltip(ForExtremeGaming, "chkForExtremeGamingToolTip");
        }

        private static void SetTooltip(System.Windows.FrameworkElement element, string key)
        {
            string text = Helpers.LocalizationService.Get(key);
            if (!string.IsNullOrEmpty(text) && text != key)
            {
                element.ToolTip = new System.Windows.Controls.TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 300,
                    FontSize = 13,
                    LineHeight = 20
                };
                ToolTipService.SetInitialShowDelay(element, 600);
                ToolTipService.SetShowDuration(element, 8000);
                ToolTipService.SetBetweenShowDelay(element, 0);
            }
        }


        private void WindowsView_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTranslations();
            RefreshUIState();
        }




        private async void DisableMyPeopleCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableMyPeople());
                // string report = Helpers.OptimizationHelper.VerifyMyPeople();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling My People: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableMyPeopleCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableMyPeople());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling My People: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void DisableTPMCheckCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableTPMCheck());
                // string report = Helpers.OptimizationHelper.VerifyTPMCheck();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling TPM Check: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableTPMCheckCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableTPMCheck());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling TPM Check: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private async void DisableTelemetryCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableTelemetry());
                // string report = Helpers.OptimizationHelper.VerifyTelemetryTasks();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Telemetry: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableTelemetryCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableTelemetry());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Telemetry: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void DisableSensorServicesCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableSensorServices());
                // string report = Helpers.OptimizationHelper.VerifySensorServices();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Sensor Services: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableSensorServicesCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableSensorServices());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Sensor Services: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void EnableLongPathsCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableLongPaths());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Long Paths: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void EnableLongPathsCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableLongPaths());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Long Paths: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void VBSOrVolumeMixerCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                if (Helpers.Utilities.IsWindows11)
                {
                     await Task.Run(() => Helpers.OptimizationHelper.DisableVirtualizationBasedSecurity());
                }
                else
                {
                     await Task.Run(() => Helpers.OptimizationHelper.EnableClassicVolumeMixer());
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing VBS/VolumeMixer Checked: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void VBSOrVolumeMixerCheck_Unchecked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
            try
            {
                if (Helpers.Utilities.IsWindows11)
                {
                     await Task.Run(() => Helpers.OptimizationHelper.EnableVirtualizationBasedSecurity());
                }
                else
                {
                     await Task.Run(() => Helpers.OptimizationHelper.DisableClassicVolumeMixer());
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing VBS/VolumeMixer Unchecked: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void EnhancePrivacyCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnhancePrivacy());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error Enhancing Privacy: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void EnhancePrivacyCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.CompromisePrivacy());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error Reverting Privacy Upgrade: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void EnableGamingModeCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableGamingMode());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Gaming Mode: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void EnableGamingModeCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableGamingMode());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Gaming Mode: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void DisableGameBarCheck_Checked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableGameBar());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Game Bar: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableGameBarCheck_Unchecked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableGameBar());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Game Bar: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void DisableXboxLiveCheck_Checked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableXboxLive());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Xbox Live: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableXboxLiveCheck_Unchecked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableXboxLive());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Xbox Live: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void DisableMSStoreUpdatesCheck_Checked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableStoreUpdates());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling MS Store Updates: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableMSStoreUpdatesCheck_Unchecked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableStoreUpdates());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling MS Store Updates: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void DisableInsiderServiceCheck_Checked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableInsiderService());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Insider Service: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableInsiderServiceCheck_Unchecked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableInsiderService());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Insider Service: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void ExcludeDriversCheck_Checked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.ExcludeDriversFromUpdates());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error excluding drivers: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void ExcludeDriversCheck_Unchecked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.IncludeDriversInUpdates());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error including drivers: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void EnableNumLockCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableNumLockOnStartup());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling NumLock: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void EnableNumLockCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableNumLockOnStartup());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling NumLock: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void AlignTaskbarLeftCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableAlignTaskbarLeft());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Align Taskbar Left: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void AlignTaskbarLeftCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableAlignTaskbarLeft());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Align Taskbar Left: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void DisableWidgetsCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableWidgets());
            }
            catch (System.UnauthorizedAccessException)
            {
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;

                var wnd = new CleaningCompletedWindow(Helpers.LocalizationService.Get("msgWidgetDisabledError"), "msgPermissionErrorTitle");
                wnd.Owner = Window.GetWindow(this);
                wnd.ShowDialog();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Widgets: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableWidgetsCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableWidgets());
            }
            catch (System.UnauthorizedAccessException)
            {
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;

                var wnd = new CleaningCompletedWindow(Helpers.LocalizationService.Get("msgWidgetEnabledError"), "msgPermissionErrorTitle");
                wnd.Owner = Window.GetWindow(this);
                wnd.ShowDialog();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Widgets: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void DisableChatCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableChat());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Chat: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableChatCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableChat());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Chat: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void DisableStickersCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableStickers());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Stickers: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableStickersCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableStickers());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Stickers: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void FixPinToTaskbarCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.FixPinToTaskbar());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fixing Pin to Taskbar: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void FixPinToTaskbarCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.UnfixPinToTaskbar());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error unfixing Pin to Taskbar: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void DisableNotificationCenterCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableNotificationCenter());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Notification Center: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableNotificationCenterCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableNotificationCenter());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Notification Center: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void DisableSnapAssistCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableSnapAssist());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Snap Assist: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableSnapAssistCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableSnapAssist());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Snap Assist: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void EnableSmallTaskbarIconsCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableSmallTaskbarIcons());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Small Taskbar Icons: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void EnableSmallTaskbarIconsCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableSmallTaskbarIcons());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Small Taskbar Icons: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void EnableClassicRightClickCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableClassicRightClickMenu());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Classic Right-Click Menu: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void EnableClassicRightClickCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableClassicRightClickMenu());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Classic Right-Click Menu: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void EnableCompactModeCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableCompactMode());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Compact Mode: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void EnableCompactModeCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableCompactMode());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Compact Mode: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void PauseUpdatesCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.PauseUpdatesUntil2036());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error pausing updates: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void PauseUpdatesCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.RestoreUpdatesPauseConfig());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error restoring updates config: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void DisableStartMenuAdsCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableStartMenuAds());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Start Menu Ads: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableStartMenuAdsCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableStartMenuAds());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Start Menu Ads: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void DisableCortanaCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableCortana());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Cortana: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableCortanaCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableCortana());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Cortana: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void DisableCopilotCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableCoPilotAI());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Copilot: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableCopilotCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableCoPilotAI());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Copilot: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private async void ShowFileExtensionsCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableFileExtensions());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling File Extensions: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void ShowFileExtensionsCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableFileExtensions());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling File Extensions: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private async void ShowHiddenFilesCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableHiddenFiles());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Hidden Files: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void ShowHiddenFilesCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableHiddenFiles());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Hidden Files: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private async void DisableMouseAccelerationCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.DisableMouseAcceleration());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Mouse Acceleration: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableMouseAccelerationCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableMouseAcceleration());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Mouse Acceleration: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        // --- Advanced Tweaking Handlers ---

        private async void ForWorking_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                // Uncheck conflicting profiles
                _suppressEvent = true;
                if (ResetToDefault.IsChecked == true) ResetToDefault.IsChecked = false;
                _suppressEvent = false;

                SetLoading(true, "Optimizing for Working...", 0);
                
                var progress = new Progress<(int percent, string message)>(v => 
                {
                    SetLoading(true, v.message, v.percent);
                });

                await Helpers.OptimizationHelper.OptimizeForWorking(progress);
                
                // Force 100% UI Update
                SetLoading(true, "Finalizing...", 100);
                await Task.Delay(100);

                RefreshUIState();
                
                // Completed Popup
                new InfoPopupWindow("Optimization completed.", "Info", "Please Restart to apply changes").ShowDialog();
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void ForWorking_Unchecked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
             // Unchecking -> Undo?
             SetLoading(true, "Reverting Working Profile...");
             await Helpers.OptimizationHelper.UndoProfileAsync("Minimal");
             SetLoading(false);
             RefreshUIState();
        }

        private async void ForExtremeGaming_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            // 1. Flag: Bắt buộc tính năng Restore Point phải được bật trước khi chạy
            bool isRestoreEnabled = await Helpers.OptimizationHelper.IsSystemProtectionEnabledAsync();
            if (!isRestoreEnabled)
            {
                _suppressEvent = true;
                ForExtremeGaming.IsChecked = false;
                _suppressEvent = false;

                string warnMsg = Helpers.LocalizationService.Get("msgRestorePointRequiredBody");
                string warnTitle = Helpers.LocalizationService.Get("msgRestorePointRequiredTitle");

                Components.CustomMessageBoxWindow.Show(warnMsg, warnTitle);

                // Mở cửa sổ cấu hình System Protection để người dùng bật và tạo Restore Point
                try
                {
                    Process.Start(new ProcessStartInfo("SystemPropertiesProtection.exe") { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to open SystemPropertiesProtection.exe: {ex.Message}");
                }
                return;
            }

            string confirmMsg = Helpers.LocalizationService.Get("msgExtremeGamingConfirmBody");
            string confirmTitle = Helpers.LocalizationService.Get("msgExtremeGamingConfirmTitle");
            string btnYes = Helpers.LocalizationService.Get("btnYes");
            string btnNo = Helpers.LocalizationService.Get("btnNo");

            var confirmWindow = new ModernConfirmationWindow(confirmMsg, confirmTitle, btnYes, btnNo);
            confirmWindow.Owner = Window.GetWindow(this);
            
            if (confirmWindow.ShowDialog() != true)
            {
                _suppressEvent = true;
                ForExtremeGaming.IsChecked = false;
                _suppressEvent = false;
                return;
            }

            try 
            {
                // Uncheck conflicting
                _suppressEvent = true;
                if (ForWorking.IsChecked == true) ForWorking.IsChecked = false;
                ResetToDefault.IsChecked = false;
                _suppressEvent = false;
                
                SetLoading(true, "Initializing Script...", 0);
                
                var progress = new Progress<(int percent, string message)>(v => 
                {
                    SetLoading(true, v.message, v.percent);
                });

                await Helpers.OptimizationHelper.OptimizeForExtremeGaming(progress);
                
                SetLoading(true, "Finalizing...", 100);
                await Task.Delay(500);

                RefreshUIState();
                
                string finishedMsg = Helpers.LocalizationService.Get("msgExtremeGamingFinished");
                string infoTitle = Helpers.LocalizationService.Get("msgInfo");
                new InfoPopupWindow(finishedMsg, infoTitle).ShowDialog();
            }
            catch (Exception ex)
            {
                 MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void ForExtremeGaming_Unchecked(object sender, RoutedEventArgs e)
        {
             // Logic removed as per request
        }

        private async void ResetToDefault_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                // Starting Popup (Optional for Reset? User asked for "apply complete" missing msgbox. Assuming for Restore too)
                new InfoPopupWindow("Starting reset...").ShowDialog();

                SetLoading(true, "Resetting to Default...", 0);
                
                var progress = new Progress<(int percent, string message)>(v => 
                {
                    SetLoading(true, v.message, v.percent);
                });

                await Helpers.OptimizationHelper.ResetToDefault(progress);
                
                // Force 100% UI Update
                SetLoading(true, "Finalizing...", 100);
                await Task.Delay(100);

                // Uncheck others
                _suppressEvent = true;
                 if (ForWorking.IsChecked == true) ForWorking.IsChecked = false;
                _suppressEvent = false;

                RefreshUIState();
                
                 // Completed Popup
                new InfoPopupWindow("Reset completed.").ShowDialog();
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void ResetToDefault_Unchecked(object sender, RoutedEventArgs e)
        {
            // Do nothing
        }

        // --- Helpers ---

        private void RefreshUIState()
        {
            _suppressEvent = true;
            try
            {
                // Refresh existing checks
                DisableTelemetryCheck.IsChecked = Helpers.OptimizationHelper.IsTelemetryDisabled();
                DisableSensorServicesCheck.IsChecked = Helpers.OptimizationHelper.IsSensorServicesDisabled();
                EnableLongPathsCheck.IsChecked = Helpers.OptimizationHelper.IsLongPathsEnabled();
                if (Helpers.Utilities.IsWindows11)
                {
                    DisableTPMCheckCheck.Visibility = Visibility.Visible; // Ensure visibility for Win11
                    DisableTPMCheckCheck.IsChecked = Helpers.OptimizationHelper.IsTPMCheckDisabled();
                    
                    VBSOrVolumeMixerCheck.Content = Helpers.LocalizationService.Get("chkVBSOrVolumeMixer");
                    VBSOrVolumeMixerCheck.IsChecked = Helpers.OptimizationHelper.IsVBSDisabled();
                }
                else
                {
                    DisableTPMCheckCheck.Visibility = Visibility.Collapsed; // Hide TPM for Win10
                    
                    DisableMyPeopleCheck.IsChecked = Helpers.OptimizationHelper.IsMyPeopleDisabled();
                    
                    VBSOrVolumeMixerCheck.Content = Helpers.LocalizationService.Get("chkClassicVolumeMixer");
                    VBSOrVolumeMixerCheck.IsChecked = Helpers.OptimizationHelper.IsClassicVolumeMixerEnabled();
                }
                EnhancePrivacyCheck.IsChecked = Helpers.OptimizationHelper.IsPrivacyEnhanced();
                EnableGamingModeCheck.IsChecked = Helpers.OptimizationHelper.IsGamingModeEnabled();
                DisableGameBarCheck.IsChecked = Helpers.OptimizationHelper.IsGameBarDisabled();
                DisableMouseAccelerationCheck.IsChecked = Helpers.OptimizationHelper.IsMouseAccelerationDisabled();
                DisableXboxLiveCheck.IsChecked = Helpers.OptimizationHelper.IsXboxLiveDisabled();
                DisableMSStoreUpdatesCheck.IsChecked = Helpers.OptimizationHelper.IsStoreUpdatesDisabled();
                DisableInsiderServiceCheck.IsChecked = Helpers.OptimizationHelper.IsInsiderServiceDisabled();
                ExcludeDriversCheck.IsChecked = Helpers.OptimizationHelper.AreDriversExcluded();
                EnableNumLockCheck.IsChecked = Helpers.OptimizationHelper.IsNumLockOnStartupEnabled();
                AlignTaskbarLeftCheck.IsChecked = Helpers.OptimizationHelper.IsAlignTaskbarLeftEnabled();
                DisableWidgetsCheck.IsChecked = Helpers.OptimizationHelper.IsWidgetsDisabled();
                DisableChatCheck.IsChecked = Helpers.OptimizationHelper.IsChatDisabled();
                DisableStickersCheck.IsChecked = Helpers.OptimizationHelper.IsStickersDisabled();
                DisableSnapAssistCheck.IsChecked = Helpers.OptimizationHelper.IsSnapAssistDisabled();
                EnableSmallTaskbarIconsCheck.IsChecked = Helpers.OptimizationHelper.IsSmallTaskbarIconsEnabled();
                EnableClassicRightClickCheck.IsChecked = Helpers.OptimizationHelper.IsClassicRightClickMenuEnabled();
                EnableCompactModeCheck.IsChecked = Helpers.OptimizationHelper.IsCompactModeEnabled();
                ShowFileExtensionsCheck.IsChecked = Helpers.OptimizationHelper.IsFileExtensionsEnabled();
                ShowHiddenFilesCheck.IsChecked = Helpers.OptimizationHelper.IsHiddenFilesEnabled();
                PauseUpdatesCheck.IsChecked = Helpers.OptimizationHelper.IsUpdatesPausedUntil2036();
                FixPinToTaskbarCheck.IsChecked = Helpers.OptimizationHelper.IsPinToTaskbarFixed();
                DisableNotificationCenterCheck.IsChecked = Helpers.OptimizationHelper.IsNotificationCenterDisabled();
                
                // New Checks
                DisableWindowsUpdateCheck.IsChecked = Helpers.OptimizationHelper.IsWindowsUpdateDisabled();


                // Refresh Advanced Profile States (Heuristics)
                bool isGamingMode = EnableGamingModeCheck.IsChecked == true;
                bool isMouseDisabled = DisableMouseAccelerationCheck.IsChecked == true;
                bool isTelemetryDisabled = DisableTelemetryCheck.IsChecked == true;

                DisableStartMenuAdsCheck.IsChecked = Helpers.OptimizationHelper.IsStartMenuAdsDisabled();
                
                // Dynamic UI Labels & Checks
                DisableCortanaCheck.IsChecked = Helpers.OptimizationHelper.IsCortanaDisabled();
                DisableCopilotCheck.IsChecked = Helpers.OptimizationHelper.IsCoPilotAIDisabled();

                // Taskbar Settings Visibility Logic
                if (Helpers.Utilities.IsWindows11)
                {
                    AlignTaskbarLeftCheck.Visibility = Visibility.Visible;
                    DisableWidgetsCheck.Visibility = Visibility.Visible;
                    DisableChatCheck.Visibility = Visibility.Visible;
                    DisableStickersCheck.Visibility = Visibility.Visible;
                    FixPinToTaskbarCheck.Visibility = Visibility.Visible;
                }
                else
                {
                    AlignTaskbarLeftCheck.Visibility = Visibility.Collapsed;
                    DisableWidgetsCheck.Visibility = Visibility.Collapsed;
                    DisableChatCheck.Visibility = Visibility.Collapsed;
                    DisableStickersCheck.Visibility = Visibility.Collapsed;
                    FixPinToTaskbarCheck.Visibility = Visibility.Collapsed;
                }

                if (Helpers.OptimizationHelper.IsExtremeGamingOptimized())
                {
                    ForExtremeGaming.IsChecked = true;
                    ForWorking.IsChecked = false;
                    ResetToDefault.IsChecked = false;
                }
                else if (isTelemetryDisabled)
                {
                    ForWorking.IsChecked = true;
                    ForExtremeGaming.IsChecked = false;
                     ResetToDefault.IsChecked = false;
                }
                else
                {
                    ForWorking.IsChecked = false;
                    ForExtremeGaming.IsChecked = false;
                     ResetToDefault.IsChecked = true; // Default state
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing UI: {ex.Message}");
            }
            finally
            {
                _suppressEvent = false;
            }
        }

        private void SetLoading(bool isLoading, string message = "", int percentage = 0)
        {
             var mainWindow = Window.GetWindow(this) as MainWindow; 
             if (mainWindow == null) return;

             if (isLoading)
             {
                 int step = 1;
                 int localPercent = 0;

                 if (percentage <= 33)
                 {
                     step = 1;
                     localPercent = (int)(percentage * 3.03); // Map 0-33 to 0-100
                 }
                 else if (percentage <= 66)
                 {
                     step = 2;
                     localPercent = (int)((percentage - 33) * 3.03); // Map 33-66 to 0-100
                 }
                 else
                 {
                     step = 3;
                     localPercent = (int)((percentage - 66) * 2.94); // Map 66-100 to 0-100
                 }

                 if (localPercent > 100) localPercent = 100;
                 if (localPercent < 0) localPercent = 0;
                 if (percentage >= 100) { step = 3; localPercent = 100; }

                 mainWindow.ShowLoading(message, step, localPercent);
             }
             else
             {
                 mainWindow.HideLoading();
             }
        }
        private void ProfileWorking_Checked_Old(object sender, RoutedEventArgs e) { } // Placeholder cleanup

        private async void btnRemoveAI_Click(object sender, RoutedEventArgs e)
        {
            string browser = "all";
            switch (ComboBrowserAI.SelectedIndex)
            {
                case 1: browser = "chrome"; break;
                case 2: browser = "edge"; break;
                case 3: browser = "firefox"; break;
            }

            SetLoading(true, $"Removing AI from {browser}...", 50);
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.RemoveAIFromBrowser(browser));
                
                SetLoading(true, $"Removed AI from {browser}.", 100);
                await Task.Delay(300);
                
                var completedWindow = new CleaningCompletedWindow(Helpers.LocalizationService.Get("msgSuccess"));
                completedWindow.Owner = Window.GetWindow(this);
                completedWindow.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void btnRestoreAI_Click(object sender, RoutedEventArgs e)
        {
            string browser = "all";
            switch (ComboBrowserAI.SelectedIndex)
            {
                case 1: browser = "chrome"; break;
                case 2: browser = "edge"; break;
                case 3: browser = "firefox"; break;
            }

            SetLoading(true, $"Restoring AI in {browser}...", 50);
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.RestoreAIInBrowser(browser));
                
                SetLoading(true, $"Restored AI in {browser}.", 100);
                await Task.Delay(300);

                var completedWindow = new CleaningCompletedWindow(Helpers.LocalizationService.Get("msgSuccess"));
                completedWindow.Owner = Window.GetWindow(this);
                completedWindow.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void DisableWindowsUpdateCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            var confirmWindow = new ModernConfirmationWindow(
                Helpers.LocalizationService.Get("msgWinUpdateWarningBody"),
                Helpers.LocalizationService.Get("msgWinUpdateWarningTitle"),
                Helpers.LocalizationService.Get("btnYes"),
                Helpers.LocalizationService.Get("btnNo"));

            if (Window.GetWindow(this) != null) confirmWindow.Owner = Window.GetWindow(this);
            confirmWindow.ShowDialog();

            if (confirmWindow.DialogResult == true)
            {
                try
                {
                    await Task.Run(() => Helpers.OptimizationHelper.DisableWindowsUpdate());

                    var doneWindow = new ModernConfirmationWindow(
                        Helpers.LocalizationService.Get("msgRestartToApply"),
                        "Info", "Got it", null);
                    if (Window.GetWindow(this) != null) doneWindow.Owner = Window.GetWindow(this);
                    doneWindow.ShowDialog();
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disabling Windows Update: {ex.Message}");
                    _suppressEvent = true;
                    if (sender is CheckBox chk) chk.IsChecked = false;
                    _suppressEvent = false;
                }
            }
            else
            {
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableWindowsUpdateCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await Task.Run(() => Helpers.OptimizationHelper.EnableWindowsUpdate());

                var doneWindow = new ModernConfirmationWindow(
                    Helpers.LocalizationService.Get("msgRestartToApply"),
                    "Info", "Got it", null);
                if (Window.GetWindow(this) != null) doneWindow.Owner = Window.GetWindow(this);
                doneWindow.ShowDialog();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Windows Update: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void EnableHighPerformanceCheck_Checked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
            try
            {
                await Task.Run(() => 
                {
                    Helpers.OptimizationHelper.RunProcess("powercfg.exe", "-duplicatescheme 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                    Helpers.OptimizationHelper.RunProcess("powercfg.exe", "-setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                });
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling High Performance: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void EnableHighPerformanceCheck_Unchecked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
            try
            {
                await Task.Run(() => 
                {
                    Helpers.OptimizationHelper.RunProcess("powercfg.exe", "-setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
                });
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reverting High Performance: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private async void EnableUltimatePerformanceCheck_Checked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
            try
            {
                await Task.Run(() => 
                {
                    Helpers.OptimizationHelper.RunProcess("powercfg.exe", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
                    Helpers.OptimizationHelper.RunProcess("powercfg.exe", "-setactive e9a42b02-d5df-448d-aa00-03f14749eb61");
                });
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Ultimate Performance: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void EnableUltimatePerformanceCheck_Unchecked(object sender, RoutedEventArgs e)
        {
             if (_suppressEvent) return;
            try
            {
                await Task.Run(() => 
                {
                    Helpers.OptimizationHelper.RunProcess("powercfg.exe", "-setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
                });
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reverting Ultimate Performance: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
    }
}
