using System.Windows.Controls;

namespace HyperLisUI.Views
{
    public partial class GeneralView : UserControl
    {
        private bool _suppressEvent = false;

        public GeneralView()
        {
            InitializeComponent();
            Loaded += GeneralView_Loaded;
            Helpers.LocalizationService.LanguageChanged += ApplyTranslations;
        }

        private void ApplyTranslations()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ApplyTranslations);
                return;
            }

            lblSystem.Text = Helpers.LocalizationService.Get("lblSystem");
            lblDiskDrives.Text = Helpers.LocalizationService.Get("lblDiskDrives");
            lblPrivacy.Text = Helpers.LocalizationService.Get("lblPrivacy");
            lblApps.Text = Helpers.LocalizationService.Get("lblApps");

            OptimizePerfCheck.Content = Helpers.LocalizationService.Get("chkGenOptimizePerf");
            DisableErrorReportingCheck.Content = Helpers.LocalizationService.Get("chkDisableErrorReporting");
            DisableSmartScreenCheck.Content = Helpers.LocalizationService.Get("chkDisableSmartScreen");
            DisableFSOCheck.Content = Helpers.LocalizationService.Get("chkDisableFSO");
            DisablePrintServiceCheck.Content = Helpers.LocalizationService.Get("chkDisablePrintService");
            DisableFaxServiceCheck.Content = Helpers.LocalizationService.Get("chkDisableFaxService");
            DisableStickyKeysCheck.Content = Helpers.LocalizationService.Get("chkDisableStickyKeys");
            DisplayPerformanceCheck.Content = Helpers.LocalizationService.Get("chkDisplayPerformance");

            DisableSuperfetchCheck.Content = Helpers.LocalizationService.Get("chkDisableSuperfetch");
            DisableNTFSTimeStampCheck.Content = Helpers.LocalizationService.Get("chkDisableNTFSTimeStamp");

            DisableTelemetryTasksCheck.Content = Helpers.LocalizationService.Get("chkDisableTelemetryTasks");
            DisableMediaPlayerSharingCheck.Content = Helpers.LocalizationService.Get("chkDisableMediaPlayerSharing");
            DisableHomeGroupCheck.Content = Helpers.LocalizationService.Get("chkDisableHomeGroup");
            DisableSMBCheck.Content = Helpers.LocalizationService.Get("chkDisableSMB");

            DisableOfficeTelemetryCheck.Content = Helpers.LocalizationService.Get("chkDisableOfficeTelemetry");
            DisableChromeTelemetryCheck.Content = Helpers.LocalizationService.Get("chkDisableChromeTelemetry");
            DisableNvidiaTelemetryCheck.Content = Helpers.LocalizationService.Get("chkDisableNvidiaTelemetry");
            DisableBraveTelemetryCheck.Content = Helpers.LocalizationService.Get("chkDisableBraveTelemetry");
            DisableEdgeTelemetryCheck.Content = Helpers.LocalizationService.Get("chkDisableEdgeTelemetry");
            DisableVisualStudioTelemetryCheck.Content = Helpers.LocalizationService.Get("chkDisableVisualStudioTelemetry");

            // --- TOOLTIP CONTENT ---
            ApplyTooltips();
        }

        private void ApplyTooltips()
        {
            SetTooltip(OptimizePerfCheck, "chkGenOptimizePerfToolTip");
            SetTooltip(DisableErrorReportingCheck, "chkDisableErrorReportingToolTip");
            SetTooltip(DisableSmartScreenCheck, "chkDisableSmartScreenToolTip");
            SetTooltip(DisableFSOCheck, "chkDisableFSOToolTip");
            SetTooltip(DisablePrintServiceCheck, "chkDisablePrintServiceToolTip");
            SetTooltip(DisableFaxServiceCheck, "chkDisableFaxServiceToolTip");
            SetTooltip(DisableStickyKeysCheck, "chkDisableStickyKeysToolTip");
            SetTooltip(DisplayPerformanceCheck, "chkDisplayPerformanceToolTip");
            SetTooltip(DisableSuperfetchCheck, "chkDisableSuperfetchToolTip");
            SetTooltip(DisableNTFSTimeStampCheck, "chkDisableNTFSTimeStampToolTip");
            SetTooltip(DisableTelemetryTasksCheck, "chkDisableTelemetryTasksToolTip");
            SetTooltip(DisableMediaPlayerSharingCheck, "chkDisableMediaPlayerSharingToolTip");
            SetTooltip(DisableHomeGroupCheck, "chkDisableHomeGroupToolTip");
            SetTooltip(DisableSMBCheck, "chkDisableSMBToolTip");
            SetTooltip(DisableOfficeTelemetryCheck, "chkDisableOfficeTelemetryToolTip");
            SetTooltip(DisableChromeTelemetryCheck, "chkDisableChromeTelemetryToolTip");
            SetTooltip(DisableNvidiaTelemetryCheck, "chkDisableNvidiaTelemetryToolTip");
            SetTooltip(DisableBraveTelemetryCheck, "chkDisableBraveTelemetryToolTip");
            SetTooltip(DisableEdgeTelemetryCheck, "chkDisableEdgeTelemetryToolTip");
            SetTooltip(DisableVisualStudioTelemetryCheck, "chkDisableVisualStudioTelemetryToolTip");
        }

        private static void SetTooltip(System.Windows.FrameworkElement element, string key)
        {
            string text = Helpers.LocalizationService.Get(key);
            if (!string.IsNullOrEmpty(text) && text != key)
            {
                element.ToolTip = new System.Windows.Controls.TextBlock
                {
                    Text = text,
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    MaxWidth = 300,
                    FontSize = 13,
                    LineHeight = 20
                };
                System.Windows.Controls.ToolTipService.SetInitialShowDelay(element, 600);
                System.Windows.Controls.ToolTipService.SetShowDuration(element, 8000);
                System.Windows.Controls.ToolTipService.SetBetweenShowDelay(element, 0);
            }
        }


        private void GeneralView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ApplyTranslations();
            // Scenario 3: Check Registry for Persistence State
            _suppressEvent = true;
            try
            {
                OptimizePerfCheck.IsChecked = Helpers.OptimizationHelper.IsOptimized();
                DisableErrorReportingCheck.IsChecked = Helpers.OptimizationHelper.IsErrorReportingDisabled();
                DisableSmartScreenCheck.IsChecked = Helpers.OptimizationHelper.IsSmartScreenDisabled();
                DisablePrintServiceCheck.IsChecked = Helpers.OptimizationHelper.IsPrintServiceDisabled();
                DisableFSOCheck.IsChecked = Helpers.OptimizationHelper.IsFSOptimizationsDisabled();
                DisableFaxServiceCheck.IsChecked = Helpers.OptimizationHelper.IsFaxServiceDisabled();
                DisableStickyKeysCheck.IsChecked = Helpers.OptimizationHelper.IsStickyKeysDisabled();
                DisplayPerformanceCheck.IsChecked = Helpers.OptimizationHelper.IsDisplayPerformanceOptimized();
                DisableSuperfetchCheck.IsChecked = Helpers.OptimizationHelper.IsSuperfetchDisabled();
                DisableNTFSTimeStampCheck.IsChecked = Helpers.OptimizationHelper.IsNTFSTimeStampDisabled();
                DisableTelemetryTasksCheck.IsChecked = Helpers.OptimizationHelper.IsTelemetryDisabled();
                DisableHomeGroupCheck.IsChecked = Helpers.OptimizationHelper.IsHomeGroupDisabled();
                DisableMediaPlayerSharingCheck.IsChecked = Helpers.OptimizationHelper.IsMediaPlayerSharingDisabled();
                DisableSMBCheck.IsChecked = Helpers.OptimizationHelper.IsSMBv1v2Disabled();
                DisableOfficeTelemetryCheck.IsChecked = Helpers.OptimizationHelper.IsOfficeTelemetryDisabled();
                DisableChromeTelemetryCheck.IsChecked = Helpers.OptimizationHelper.IsChromeTelemetryDisabled();
                DisableNvidiaTelemetryCheck.IsChecked = Helpers.OptimizationHelper.IsNvidiaTelemetryDisabled();
                DisableVisualStudioTelemetryCheck.IsChecked = Helpers.OptimizationHelper.IsVisualStudioTelemetryDisabled();
                DisableBraveTelemetryCheck.IsChecked = Helpers.OptimizationHelper.IsBraveTelemetryDisabled();
                DisableEdgeTelemetryCheck.IsChecked = Helpers.OptimizationHelper.IsEdgeTelemetryDisabled();
            }
            catch { }
            _suppressEvent = false;
        }

        private void OptimizePerfCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try 
            {
                Helpers.OptimizationHelper.EnablePerformanceTweaks();
                
                // Verify
                // Verify
                // string report = Helpers.OptimizationHelper.VerifyOptimization();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during optimization: {ex.Message}");
                
                // Revert toggle on error
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private void OptimizePerfCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try
            {
                 Helpers.OptimizationHelper.DisablePerformanceTweaks();
            }
             catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling optimization: {ex.Message}");
            }
        }
        private void DisableErrorReportingCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try
            {
                Helpers.OptimizationHelper.DisableErrorReporting();

                // Verify
                // Verify
                // string report = Helpers.OptimizationHelper.VerifyErrorReporting();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling error reporting: {ex.Message}");

                // Revert toggle on error
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private void DisableErrorReportingCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try
            {
                Helpers.OptimizationHelper.EnableErrorReporting();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling error reporting: {ex.Message}");
                
                // Revert toggle on error
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
            }

        private async void DisableFSOCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableFSOptimizations());
                // Verify
                // string report = Helpers.OptimizationHelper.VerifyFSOptimizations();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling FSO: {ex.Message}");

                // Revert toggle on error
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableFSOCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableFSOptimizations());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling FSO: {ex.Message}");
                
                // Revert toggle on error
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private void DisableSmartScreenCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try
            {
                Helpers.OptimizationHelper.DisableSmartScreen();

                // Verify
                // Verify
                // string report = Helpers.OptimizationHelper.VerifySmartScreen();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling SmartScreen: {ex.Message}");

                // Revert toggle on error
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private void DisableSmartScreenCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try
            {
                Helpers.OptimizationHelper.EnableSmartScreen();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling SmartScreen: {ex.Message}");
                
                // Revert toggle on error
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private void DisablePrintServiceCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try
            {
                Helpers.OptimizationHelper.DisablePrintService();

                // Verify
                // Verify
                // string report = Helpers.OptimizationHelper.VerifyPrintService();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Print Service: {ex.Message}");

                // Revert toggle on error
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private void DisablePrintServiceCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try
            {
                Helpers.OptimizationHelper.EnablePrintService();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Print Service: {ex.Message}");
                
                // Revert toggle on error
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private void DisableFaxServiceCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try
            {
                Helpers.OptimizationHelper.DisableFaxService();

                // Verify
                // Verify
                // string report = Helpers.OptimizationHelper.VerifyFaxService();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Fax Service: {ex.Message}");

                // Revert toggle on error
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private void DisableFaxServiceCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try
            {
                Helpers.OptimizationHelper.EnableFaxService();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Fax Service: {ex.Message}");
                
                // Revert toggle on error
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private void DisableStickyKeysCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try
            {
                Helpers.OptimizationHelper.DisableStickyKeys();

                // Verify
                // Verify
                // string report = Helpers.OptimizationHelper.VerifyStickyKeys();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Sticky Keys: {ex.Message}");

                // Revert toggle on error
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private void DisableStickyKeysCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try
            {
                Helpers.OptimizationHelper.EnableStickyKeys();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Sticky Keys: {ex.Message}");
                
                // Revert toggle on error
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private void DisplayPerformanceCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try
            {
                Helpers.OptimizationHelper.EnableDisplayPerformanceTweaks();

                // Verify
                // Verify
                // string report = Helpers.OptimizationHelper.VerifyDisplayPerformance();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Display Performance: {ex.Message}");

                // Revert toggle on error
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private void DisplayPerformanceCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;

            try
            {
                Helpers.OptimizationHelper.DisableDisplayPerformanceTweaks();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Display Performance: {ex.Message}");

                // Revert toggle on error
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private void DisableSuperfetchCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                Helpers.OptimizationHelper.DisableSuperfetch();
                Helpers.OptimizationHelper.DisableSuperfetch();
                // string report = Helpers.OptimizationHelper.VerifySuperfetch();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Superfetch: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private void DisableSuperfetchCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                 Helpers.OptimizationHelper.EnableSuperfetch();
            }
             catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Superfetch: {ex.Message}");
                 _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }

        private void DisableNTFSTimeStampCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                Helpers.OptimizationHelper.DisableNTFSTimeStamp();
                // We run Verify via string method
                Helpers.OptimizationHelper.DisableNTFSTimeStamp();
                // We run Verify via string method
                // string report = Helpers.OptimizationHelper.VerifyNTFSTimeStamp();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling NTFS Timestamp: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private void DisableNTFSTimeStampCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                 Helpers.OptimizationHelper.EnableNTFSTimeStamp();
            }
             catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling NTFS TimeStamp: {ex.Message}");
                 _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private async void DisableTelemetryTasksCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                // Run heavy task on background thread to prevent UI freeze
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableTelemetry());
                
                
                // string report = Helpers.OptimizationHelper.VerifyTelemetryTasks();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Telemetry Tasks: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableTelemetryTasksCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                 // Run heavy task on background thread via Task.Run
                 await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableTelemetry());
            }
             catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Telemetry Tasks: {ex.Message}");
                 _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private async void DisableHomeGroupCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableHomeGroup());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableHomeGroup());
                // string report = Helpers.OptimizationHelper.VerifyHomeGroup();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling HomeGroup: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableHomeGroupCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableHomeGroup());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableHomeGroup());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling HomeGroup: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private async void DisableMediaPlayerSharingCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableMediaPlayerSharing());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableMediaPlayerSharing());
                // string report = Helpers.OptimizationHelper.VerifyMediaPlayerSharing();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Media Player Sharing: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableMediaPlayerSharingCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableMediaPlayerSharing());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableMediaPlayerSharing());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Media Player Sharing: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private async void DisableSMBCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableSMBv1v2());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableSMBv1v2());
                // string report = Helpers.OptimizationHelper.VerifySMBv1v2();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling SMB v1/v2: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableSMBCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableSMBv1v2());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableSMBv1v2());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling SMB v1/v2: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private async void DisableOfficeTelemetryCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableOfficeTelemetry());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableOfficeTelemetry());
                // string report = Helpers.OptimizationHelper.VerifyOfficeTelemetry();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Office Telemetry: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableOfficeTelemetryCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableOfficeTelemetry());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableOfficeTelemetry());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Office Telemetry: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private async void DisableChromeTelemetryCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableChromeTelemetry());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableChromeTelemetry());
                // string report = Helpers.OptimizationHelper.VerifyChromeTelemetry();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Chrome Telemetry: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableChromeTelemetryCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableChromeTelemetry());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableChromeTelemetry());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Chrome Telemetry: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private async void DisableNvidiaTelemetryCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableNvidiaTelemetry());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableNvidiaTelemetry());
                // string report = Helpers.OptimizationHelper.VerifyNvidiaTelemetry();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling NVIDIA Telemetry: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableNvidiaTelemetryCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableNvidiaTelemetry());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableNvidiaTelemetry());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling NVIDIA Telemetry: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private async void DisableVisualStudioTelemetryCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableVisualStudioTelemetry());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableVisualStudioTelemetry());
                // string report = Helpers.OptimizationHelper.VerifyVisualStudioTelemetry();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling VS Telemetry: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableVisualStudioTelemetryCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableVisualStudioTelemetry());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableVisualStudioTelemetry());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling VS Telemetry: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private async void DisableBraveTelemetryCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableBraveTelemetry());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableBraveTelemetry());
                // string report = Helpers.OptimizationHelper.VerifyBraveTelemetry();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Brave Telemetry: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableBraveTelemetryCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableBraveTelemetry());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableBraveTelemetry());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Brave Telemetry: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
        private async void DisableEdgeTelemetryCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableEdgeTelemetry());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.DisableEdgeTelemetry());
                // string report = Helpers.OptimizationHelper.VerifyEdgeTelemetry();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling Edge Telemetry: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = false;
                _suppressEvent = false;
            }
        }

        private async void DisableEdgeTelemetryCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            try
            {
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableEdgeTelemetry());
                await System.Threading.Tasks.Task.Run(() => Helpers.OptimizationHelper.EnableEdgeTelemetry());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling Edge Telemetry: {ex.Message}");
                _suppressEvent = true;
                if (sender is CheckBox chk) chk.IsChecked = true;
                _suppressEvent = false;
            }
        }
    }
}
