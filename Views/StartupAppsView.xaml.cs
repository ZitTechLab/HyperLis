using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HyperLisUI.Helpers;
using HyperLisUI.Models;

namespace HyperLisUI.Views
{
    public partial class StartupAppsView : UserControl
    {
        public StartupAppsView()
        {
            InitializeComponent();
            Loaded += StartupAppsView_Loaded;

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

            lblStartupTitle.Text = Helpers.LocalizationService.Get("lblStartupTitle");
            colStartupName.Header = Helpers.LocalizationService.Get("lblStartupName");
            colStartupLocation.Header = Helpers.LocalizationService.Get("lblStartupLocation");


            btnStartupToggle.Content = Helpers.LocalizationService.Get("btnStartupToggle") ?? "Toggle Status";
            btnStartupRefresh.Content = Helpers.LocalizationService.Get("btnStartupRefresh");
            cbHideSystem.Content = Helpers.LocalizationService.Get("cbHideSystem") ?? "Hide system apps";
        }

        private List<StartupItem> _allStartupItems = new List<StartupItem>();

        private async void StartupAppsView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadItemsAsync();
        }

        private async Task LoadItemsAsync()
        {
            // Hiển thị trạng thái đang tải nếu cần (hoặc disable nút)
            btnStartupRefresh.IsEnabled = false;
            btnStartupToggle.IsEnabled = false;
            cbHideSystem.IsEnabled = false;

            _allStartupItems = await Task.Run(() => StartupHelper.GetStartupItems());

            ApplyFilter();

            btnStartupRefresh.IsEnabled = true;
            btnStartupToggle.IsEnabled = true;
            cbHideSystem.IsEnabled = true;
        }

        private void ApplyFilter()
        {
            var filteredItems = _allStartupItems.AsEnumerable();
            
            if (cbHideSystem.IsChecked == true)
            {
                filteredItems = filteredItems.Where(i => !IsSystemApp(i));
            }

            var items = filteredItems.Select(i => new StartupAppItemViewModel(i)).ToList();
            StartupList.ItemsSource = items;
        }

        private bool IsSystemApp(StartupItem item)
        {
            string location = item.FileLocation?.ToLowerInvariant() ?? "";
            string name = item.Name?.ToLowerInvariant() ?? "";

            if (location.Contains("\\windows\\system32") || location.Contains("\\windows\\syswow64") || location.Contains("%windir%") || location.Contains("%systemroot%"))
                return true;

            if (item is TaskStartupItem taskItem)
            {
                if (taskItem.TaskPathAndName.StartsWith("\\Microsoft\\Windows\\", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            if (name.Contains("securityhealth") || name.Contains("ctfmon"))
                return true;

            return false;
        }

        private void cbHideSystem_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (StartupList != null)
            {
                ApplyFilter();
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadItemsAsync();
        }

        private async void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (StartupList.ItemsSource is List<StartupAppItemViewModel> items)
            {
                var targets = items.Where(i => i.IsChecked).ToList();
                if (targets.Count == 0) return;

                var confirmWindow = new ModernConfirmationWindow(string.Format(Helpers.LocalizationService.Get("msgDeleteStartupConfirm") ?? "Are you sure you want to change status of {0} items?", targets.Count), Helpers.LocalizationService.Get("msgConfirmDelete") ?? "Confirm", "Yes", "No");
                confirmWindow.Owner = Window.GetWindow(this);
                if (confirmWindow.ShowDialog() == true)
                {
                    bool success = true;
                    foreach (var item in targets)
                    {
                        try
                        {
                            if (item.AppsItem.IsDisabled)
                                item.AppsItem.Enable();
                            else
                                item.AppsItem.Disable();
                        }
                        catch
                        {
                            success = false;
                        }
                    }

                    if (!success)
                    {
                        var warningWindow = new ModernConfirmationWindow(Helpers.LocalizationService.Get("msgDeleteStartupWarning") ?? "Some items could not be changed.", Helpers.LocalizationService.Get("msgWarning") ?? "Warning", "OK", null);
                        warningWindow.Owner = Window.GetWindow(this);
                        warningWindow.ShowDialog();
                    }

                    await LoadItemsAsync();
                }
            }
        }
    }

    public class StartupAppItemViewModel
    {
        public StartupItem AppsItem { get; }

        public StartupAppItemViewModel(StartupItem item)
        {
            AppsItem = item;
        }

        public bool IsChecked { get; set; }
        public string Name => AppsItem.Name;
        public bool IsDisabled => AppsItem.IsDisabled;
        public string Location => !string.IsNullOrEmpty(AppsItem.FileLocation) ? AppsItem.FileLocation : AppsItem.ToString();
    }
}
