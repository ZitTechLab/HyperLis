using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using HyperLisUI.Helpers;
using HyperLisUI.Services;

namespace HyperLisUI.Views.Components
{
    public partial class UpdateWindow : Window
    {
        private readonly UpdateInfo _updateInfo;
        private readonly UpdateService _updateService;

        public UpdateWindow(UpdateInfo updateInfo, UpdateService updateService)
        {
            InitializeComponent();
            _updateInfo = updateInfo;
            _updateService = updateService;

            if (Application.Current != null && Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                this.Owner = Application.Current.MainWindow;
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            LoadDataAndLocalization();
        }

        private void LoadDataAndLocalization()
        {
            VersionRun.Text = _updateInfo.Version;
            ChangelogVersionRun.Text = _updateInfo.Version;

            string msgIsAvailable = LocalizationService.Get("msgIsAvailable");
            if (msgIsAvailable == "msgIsAvailable") msgIsAvailable = " is available!";
            IsAvailableRun.Text = msgIsAvailable;

            string msgUpdateReady = LocalizationService.Get("msgUpdateReady");
            if (msgUpdateReady == "msgUpdateReady") msgUpdateReady = "A new update is ready to improve your experience.";
            UpdateReadyText.Text = msgUpdateReady;

            string msgReleased = LocalizationService.Get("msgReleased");
            if (msgReleased == "msgReleased") msgReleased = "Released:";
            string releaseDateStr = string.IsNullOrWhiteSpace(_updateInfo.ReleaseDate) ? DateTime.Now.ToString("dd/MM/yyyy") : _updateInfo.ReleaseDate;
            ReleaseDateText.Text = $"{msgReleased} {releaseDateStr}";

            string msgSize = LocalizationService.Get("lblSize");
            if (msgSize == "lblSize") msgSize = "Size:";
            string sizeStr = string.IsNullOrWhiteSpace(_updateInfo.Size) ? "Unknown" : _updateInfo.Size;
            SizeText.Text = $"{msgSize} {sizeStr}";

            string msgWhatsNew = LocalizationService.Get("lblWhatsNew");
            if (msgWhatsNew == "lblWhatsNew") msgWhatsNew = "What's new in v";
            WhatsNewText.Text = msgWhatsNew;

            string msgNew = LocalizationService.Get("lblNew");
            if (msgNew == "lblNew") msgNew = "NEW";
            NewBadgeText.Text = msgNew;

            string msgViewChangelog = LocalizationService.Get("lblViewChangelog");
            if (msgViewChangelog == "lblViewChangelog") msgViewChangelog = "View full changelog";
            ViewChangelogText.Text = msgViewChangelog;

            string chkText = LocalizationService.Get("chkDontShowAgain");
            if (chkText == "chkDontShowAgain") chkText = "Don't show again for this version";
            chkDontShowAgain.Content = chkText;

            string msgSkip = LocalizationService.Get("btnSkip");
            if (msgSkip == "btnSkip") msgSkip = "Skip";
            SkipButtonText.Text = msgSkip;

            string msgRemindLater = LocalizationService.Get("btnRemindLater");
            if (msgRemindLater == "btnRemindLater") msgRemindLater = "Remind me later";
            RemindLaterButtonText.Text = msgRemindLater;

            string msgUpdateNow = LocalizationService.Get("btnUpdateNow");
            if (msgUpdateNow == "btnUpdateNow") msgUpdateNow = "Update Now";
            UpdateNowButtonText.Text = msgUpdateNow;

            string progressText = LocalizationService.Get("msgUpdateDownloading");
            if (progressText == "msgUpdateDownloading") progressText = "Downloading update... {0}%";
            ProgressText.Text = string.Format(progressText, "0");

            RenderChangelog();
        }

        private void RenderChangelog()
        {
            ChangelogPanel.Children.Clear();
            if (_updateInfo.Content == null || _updateInfo.Content.Count == 0) return;

            foreach (var item in _updateInfo.Content)
            {
                var lowerItem = item.ToLower();
                string iconHex = "\uE946"; // Default Info icon
                SolidColorBrush iconColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C69FF")); // Blue
                bool hasBackgroundCircle = false;

                if (lowerItem.Contains("add") || lowerItem.Contains("bổ sung") || lowerItem.Contains("thêm"))
                {
                    iconHex = "\uE10B"; // Tick
                    iconColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")); // Green
                }
                else if (lowerItem.Contains("fix") || lowerItem.Contains("sửa"))
                {
                    iconHex = "\uEBE8"; // Bug
                    iconColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // Red
                }
                else if (lowerItem.Contains("improv") || lowerItem.Contains("cải thiện") || lowerItem.Contains("nâng cấp"))
                {
                    iconHex = "\uE945"; // LightningBolt
                    iconColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")); // Orange
                }
                else if (lowerItem.Contains("remov") || lowerItem.Contains("xóa") || lowerItem.Contains("bỏ"))
                {
                    iconHex = "\uE107"; // Delete/Trash
                    iconColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // Red
                }
                else if (lowerItem.Contains("revamp") || lowerItem.Contains("cập nhật") || lowerItem.Contains("đổi mới") || lowerItem.Contains("thay đổi"))
                {
                    iconHex = "\uE117"; // Sync/Update
                    iconColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C69FF")); // Blue
                }

                UIElement finalIconElement = new TextBlock
                {
                    Text = iconHex,
                    FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                    FontSize = 16,
                    Foreground = iconColor,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(3, 0, 16, 0)
                };

                var itemPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0) };

                var textBlock = new TextBlock
                {
                    Text = item,
                    FontSize = 14,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")),
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 600,
                    VerticalAlignment = VerticalAlignment.Center
                };

                itemPanel.Children.Add(finalIconElement);
                itemPanel.Children.Add(textBlock);

                bool isLast = item == _updateInfo.Content[_updateInfo.Content.Count - 1];
                var border = new Border
                {
                    BorderThickness = isLast ? new Thickness(0) : new Thickness(0, 0, 0, 1),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F4F8")),
                    Padding = isLast ? new Thickness(0) : new Thickness(0, 0, 0, 16),
                    Margin = isLast ? new Thickness(0) : new Thickness(0, 0, 0, 16),
                    Child = itemPanel
                };

                ChangelogPanel.Children.Add(border);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            HandleSkipOrClose();
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            HandleSkipOrClose();
        }

        private void HandleSkipOrClose()
        {
            if (chkDontShowAgain.IsChecked == true)
            {
                AppConfigManager.Config.SkippedVersion = _updateInfo.Version;
                AppConfigManager.SaveConfig();
            }
            this.Close();
        }

        private void LaterButton_Click(object sender, RoutedEventArgs e)
        {
            AppConfigManager.Config.RemindDate = DateTime.Now.AddDays(3);
            AppConfigManager.SaveConfig();
            this.Close();
        }

        private void ViewChangelogLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/ZitTechLab/HyperLis/releases",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonPanel.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;
            
            var progress = new Progress<double>(percent =>
            {
                DownloadProgress.Value = percent;
                var textFormat = LocalizationService.Get("msgUpdateDownloading") ?? "Downloading update... {0}%";
                ProgressText.Text = string.Format(textFormat, percent.ToString("0.0"));
            });

            try
            {
                var url = _updateInfo.DownloadUrlSetup?.Trim();
                if (string.IsNullOrWhiteSpace(url))
                {
                    throw new Exception("Download URL is missing.");
                }

                await _updateService.DownloadAndRunInstallerAsync(url, progress);
            }
            catch (Exception ex)
            {
                var failFormat = LocalizationService.Get("msgUpdateFailed") ?? "Update failed: {0}";
                CustomMessageBoxWindow.Show(string.Format(failFormat, ex.Message), LocalizationService.Get("msgError") ?? "Error");
                
                ButtonPanel.Visibility = Visibility.Visible;
                ProgressPanel.Visibility = Visibility.Collapsed;
            }
        }
    }
}
