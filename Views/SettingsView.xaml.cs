using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using HyperLisUI.Helpers;

namespace HyperLisUI.Views
{
    public partial class SettingsView : UserControl
    {
        private bool _isInitializing = true;

        public SettingsView()
        {
            InitializeComponent();
            Loaded += SettingsView_Loaded;
            LocalizationService.LanguageChanged += ApplyTranslations;
        }

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            // Set ComboBox to match current language (without triggering change event)
            _isInitializing = true;
            string currentLang = LocalizationService.CurrentLanguage;
            for (int i = 0; i < LanguageComboBox.Items.Count; i++)
            {
                if (LanguageComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == currentLang)
                {
                    LanguageComboBox.SelectedIndex = i;
                    break;
                }
            }
            _isInitializing = false;

            ApplyTranslations();
            LoadCurrentVersion();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (LanguageComboBox.SelectedItem is ComboBoxItem selected && selected.Tag is string langCode)
            {
                LocalizationService.SaveLanguagePreference(langCode);
                LocalizationService.LoadLanguage(langCode);
            }
        }

        private void ApplyTranslations()
        {
            // Use the visual tree translator for named elements
            LocalizationService.TranslateUI(this);
            
            // Explicitly set dynamic/code-behind text if needed
            lblCheckUpdateTitle.Text = LocalizationService.Get("lblCheckUpdateTitle") ?? "Check for updates";
            btnCheckUpdate.Content = LocalizationService.Get("btnCheckUpdateText") ?? "Check Update";
            LoadCurrentVersion(); // Refresh version string translation
        }

        private void LeMinhDuc_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://www.facebook.com/why.not.Zit");
        }

        private void TuanHai_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://www.facebook.com/tuanhaii26");
        }

        private void ZitTechLab_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://www.facebook.com/ZitTechLab");
        }

        private void FacebookLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenUrl("https://www.facebook.com/ZitTechLab");
        }

        private void GithubLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenUrl("https://github.com/ZitTechLab/HyperLis");
        }

        private void LoadCurrentVersion()
        {
            try
            {
                var lang = (LocalizationService.CurrentLanguage ?? "en").ToLower();
                string path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Changelog", $"{lang}.json");
                
                // Fallback to en.json if language file doesn't exist
                if (!System.IO.File.Exists(path))
                {
                    path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Changelog", "en.json");
                }

                if (System.IO.File.Exists(path))
                {
                    string json = System.IO.File.ReadAllText(path);
                    var allVersions = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, DashboardView.ChangelogData>>(json);
                    if (allVersions != null && allVersions.Count > 0)
                    {
                        var sortedKeys = System.Linq.Enumerable.ToList(System.Linq.Enumerable.OrderByDescending(allVersions.Keys, k => k));
                        var latestData = allVersions[sortedKeys[0]];
                        lblCurrentVersion.Text = $"{LocalizationService.Get("lblVersionInfo") ?? "Current Version:"} {latestData.version_value}";
                    }
                }
            }
            catch { }
        }

        private async void btnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnCheckUpdate.IsEnabled = false;
                var updateService = new HyperLisUI.Services.UpdateService();
                var updateInfo = await updateService.CheckForUpdateAsync();

                if (updateInfo != null)
                {
                    var updateWindow = new HyperLisUI.Views.Components.UpdateWindow(updateInfo, updateService);
                    updateWindow.ShowDialog();
                }
                else
                {
                    HyperLisUI.Views.Components.CustomMessageBoxWindow.Show(
                        LocalizationService.Get("msgUpdateLatest") ?? "You're on the latest version.",
                        "Check Update"
                    );
                }
            }
            finally
            {
                btnCheckUpdate.IsEnabled = true;
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
