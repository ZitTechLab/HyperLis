using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HyperLisUI.Helpers;
using HyperLisUI.Models;

namespace HyperLisUI.Views
{
    public partial class DownloadView : UserControl
    {
        public List<CategoryGroup> Categories { get; set; }
        private List<CategoryGroup> _allCategories;

        public DownloadView()
        {
            InitializeComponent();
            Helpers.LocalizationService.LanguageChanged += ApplyTranslations;
            // Data loading moved to Loaded event
        }

        private void ApplyTranslations()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ApplyTranslations);
                return;
            }

            btnInstallSelected.Content = Helpers.LocalizationService.Get("btnInstallSelected");
            btnUninstallSelected.Content = Helpers.LocalizationService.Get("btnUninstallSelected");
            btnClearSelection.Content = Helpers.LocalizationService.Get("btnClearSelection");
            btnUpdateAll.Content = Helpers.LocalizationService.Get("btnUpdateAll");

            UpdateAppStatusTexts();
        }

        private void UpdateAppStatusTexts()
        {
            var source = _allCategories ?? Categories;
            if (source == null) return;

            string installedText = Helpers.LocalizationService.Get("txtInstalled");

            foreach (var group in source)
            {
                if (group.Apps == null) continue;
                foreach (var app in group.Apps)
                {
                    if (app.IsInstalled)
                    {
                        app.StatusText = installedText;
                    }
                }
            }
        }

        private async void DownloadView_Loaded(object sender, RoutedEventArgs e)
        {
             ApplyTranslations();
             await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                if (!await DownloadHelper.IsChocoInstalled())
                {
                    var infoWindow = new ModernConfirmationWindow(
                        Helpers.LocalizationService.Get("msgChocoInstalling"),
                        Helpers.LocalizationService.Get("msgSettingUp"),
                        "OK",
                        null);

                    if (Window.GetWindow(this) != null) infoWindow.Owner = Window.GetWindow(this);

                    infoWindow.Show();

                    try
                    {
                        await DownloadHelper.InstallChoco();
                    }
                    finally
                    {
                        infoWindow.Close();
                    }
                }

                bool isSystemWinget = await DownloadHelper.DetermineWingetPathAsync();
                if (!isSystemWinget)
                {
                    var notifyWindow = new ModernConfirmationWindow(
                        Helpers.LocalizationService.Get("msgWingetInstallingBackground"),
                        Helpers.LocalizationService.Get("msgSettingUp"),
                        "OK",
                        null);

                    if (Window.GetWindow(this) != null) notifyWindow.Owner = Window.GetWindow(this);
                    notifyWindow.Show();

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await DownloadHelper.InstallWinget();
                            Logger.LogInfo("DOWNLOAD", "Background Winget installation completed.");
                        }
                        catch (System.Exception ex)
                        {
                            Logger.LogCrash(ex, "BackgroundInstallWinget");
                        }
                        finally
                        {
                            Dispatcher.Invoke(() =>
                            {
                                try { notifyWindow.Close(); } catch { }
                            });
                        }
                    });
                }

                Categories = await DownloadHelper.LoadAppsAsync();
                _allCategories = Categories;
                CategoriesList.ItemsSource = Categories;

                await DownloadHelper.UpdateInstalledStatusAsync(_allCategories ?? Categories);
                UpdateAppStatusTexts();
            }
            catch (System.Exception ex)
            {
                Logger.LogCrash(ex, nameof(DownloadView) + ".LoadData");
            }
        }

        private async void InstallSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedApps = GetSelectedApps();
            if (!selectedApps.Any()) return;

            List<PackageOperationResult> operationResults = new List<PackageOperationResult>();
            SetLoading(true, Helpers.LocalizationService.Get("msgInstallingApps"), 0);

            try
            {
                operationResults = await DownloadHelper.InstallPackages(selectedApps, p =>
                {
                    Dispatcher.Invoke(() => SetLoading(true, Helpers.LocalizationService.Get("msgInstallingApps"), (int)p));
                });
            }
            catch (System.Exception ex)
            {
                Logger.LogCrash(ex, nameof(DownloadView) + ".InstallSelected_Click");
            }
            finally
            {
                SetLoading(false);
            }

            LogOperationSummary("Install", operationResults);
            _ = RefreshStatusesAfterOperationAsync(selectedApps, true, "install");
        }

        private async void UninstallSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedApps = GetSelectedApps();
            if (!selectedApps.Any()) return;

            List<PackageOperationResult> operationResults = new List<PackageOperationResult>();
            SetLoading(true, Helpers.LocalizationService.Get("msgUninstallingApps"), 0);

            try
            {
                operationResults = await DownloadHelper.UninstallPackages(selectedApps, p =>
                {
                    Dispatcher.Invoke(() => SetLoading(true, Helpers.LocalizationService.Get("msgUninstallingApps"), (int)p));
                });
            }
            catch (System.Exception ex)
            {
                Logger.LogCrash(ex, nameof(DownloadView) + ".UninstallSelected_Click");
            }
            finally
            {
                SetLoading(false);
            }

            LogOperationSummary("Uninstall", operationResults);
            _ = RefreshStatusesAfterOperationAsync(selectedApps, false, "uninstall");
        }

        private async void UpdateAll_Click(object sender, RoutedEventArgs e)
        {
            SetLoading(true, Helpers.LocalizationService.Get("msgUpdatingApps"), 0);
            try
            {
                await DownloadHelper.UpdateAll();
            }
            catch (System.Exception ex)
            {
                Logger.LogCrash(ex, nameof(DownloadView) + ".UpdateAll_Click");
            }
            finally
            {
                SetLoading(false);
            }

            _ = RefreshAllStatusesAsync("update-all");
        }

        private async Task RefreshStatusesAfterOperationAsync(List<AppItem> apps, bool expectedInstalled, string operationName)
        {
            if (apps == null || apps.Count == 0)
            {
                return;
            }

            try
            {
                var verificationResults = await DownloadHelper.VerifyInstalledAppsAsync(apps, expectedInstalled);
                ApplyVerificationResults(verificationResults);

                if (verificationResults.Any(result => result.Status == PackageOperationStatus.VerificationDeferred))
                {
                    Logger.LogInfo("DOWNLOAD", $"{operationName} verification deferred. Scheduling full refresh.");
                    _ = RefreshAllStatusesAsync(operationName + "-deferred");
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogCrash(ex, nameof(DownloadView) + ".RefreshStatusesAfterOperationAsync");
                _ = RefreshAllStatusesAsync(operationName + "-fallback");
            }
        }

        private async Task RefreshAllStatusesAsync(string reason)
        {
            try
            {
                var source = _allCategories ?? Categories;
                if (source == null)
                {
                    return;
                }

                Logger.LogInfo("DOWNLOAD", $"Refreshing all download statuses. Reason={reason}.");
                await DownloadHelper.UpdateInstalledStatusAsync(source);
                UpdateAppStatusTexts();
            }
            catch (System.Exception ex)
            {
                Logger.LogCrash(ex, nameof(DownloadView) + ".RefreshAllStatusesAsync");
            }
        }

        private void ApplyVerificationResults(IEnumerable<PackageOperationResult> results)
        {
            if (results == null)
            {
                return;
            }

            foreach (var result in results)
            {
                if (result.App == null)
                {
                    continue;
                }

                if (result.DetectedInstalledState.HasValue)
                {
                    result.App.IsInstalled = result.DetectedInstalledState.Value;
                }

                if (result.Status == PackageOperationStatus.VerificationDeferred)
                {
                    Logger.LogInfo("DOWNLOAD", $"Verification deferred for {result.App.Name}.");
                }
                else if (result.Status == PackageOperationStatus.Failed)
                {
                    Logger.LogInfo("DOWNLOAD", $"Verification mismatch for {result.App.Name}: {result.Message}");
                }
            }

            UpdateAppStatusTexts();
        }

        private void LogOperationSummary(string operationName, IEnumerable<PackageOperationResult> results)
        {
            if (results == null)
            {
                return;
            }

            int succeeded = results.Count(r => r.Status == PackageOperationStatus.Succeeded);
            int failed = results.Count(r => r.Status == PackageOperationStatus.Failed);
            int timedOut = results.Count(r => r.Status == PackageOperationStatus.TimedOut);
            int deferred = results.Count(r => r.Status == PackageOperationStatus.VerificationDeferred);

            Logger.LogInfo(
                "DOWNLOAD",
                $"{operationName} summary: Succeeded={succeeded}; Failed={failed}; TimedOut={timedOut}; Deferred={deferred}");
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            var source = _allCategories ?? Categories;
            if (source == null) return;
            foreach (var group in source)
            {
                if (group.Apps == null) continue;
                foreach (var app in group.Apps)
                {
                    app.IsSelected = false;
                }
            }
        }

        private List<AppItem> GetSelectedApps()
        {
            var selected = new List<AppItem>();
            var source = _allCategories ?? Categories;
            if (source == null) return selected;

            foreach (var group in source)
            {
                if (group.Apps == null) continue;
                selected.AddRange(group.Apps.Where(a => a.IsSelected));
            }
            return selected;
        }

        private void SetLoading(bool isLoading, string message = "", int percentage = 0)
        {
             var mainWindow = Window.GetWindow(this) as MainWindow;
             if (mainWindow == null) return;

             if (isLoading)
             {
                 int step = 1;
                 int localPercent = 0;

                 if (percentage < 33)
                 {
                     step = 1;
                     localPercent = (int)(percentage * 3.03);
                 }
                 else if (percentage < 66)
                 {
                     step = 2;
                     localPercent = (int)((percentage - 33) * 3.03);
                 }
                 else
                 {
                     step = 3;
                     localPercent = (int)((percentage - 66) * 3.03);
                 }

                 mainWindow.ShowLoading(message, step, localPercent);
             }
             else
             {
                 mainWindow.HideLoading();
             }
        }

        private void InnerScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var parentScrollViewer = GetScrollViewer(CategoriesList);
            if (parentScrollViewer != null)
            {
                if (e.Delta < 0)
                {
                    parentScrollViewer.LineDown();
                    parentScrollViewer.LineDown();
                    parentScrollViewer.LineDown();
                }
                else
                {
                    parentScrollViewer.LineUp();
                    parentScrollViewer.LineUp();
                    parentScrollViewer.LineUp();
                }
                e.Handled = true;
            }
        }

        private void AppItem_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is FrameworkElement element && element.DataContext is AppItem app)
            {
                app.IsSelected = !app.IsSelected;
                e.Handled = true;
            }
        }

        private static ScrollViewer? GetScrollViewer(DependencyObject o)
        {
            if (o is ScrollViewer sv) return sv;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }
        public void FilterApps(string searchText)
        {
            if (_allCategories == null) return;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                Categories = _allCategories;
            }
            else
            {
                var filteredGroups = new List<CategoryGroup>();
                foreach (var group in _allCategories)
                {
                    if (group.Apps == null) continue;

                    var matchingApps = group.Apps
                        .Where(app => app.Name != null && app.Name.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();

                    if (matchingApps.Any())
                    {
                        filteredGroups.Add(new CategoryGroup
                        {
                            CategoryName = group.CategoryName,
                            Apps = matchingApps
                        });
                    }
                }
                Categories = filteredGroups;
            }
            CategoriesList.ItemsSource = Categories;
        }
    }
}
