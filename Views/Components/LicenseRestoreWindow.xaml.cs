using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using HyperLisUI.Models;

namespace HyperLisUI.Views.Components
{
    public class BackupItemDisplay
    {
        public string DisplayName { get; set; }
        public string DateString { get; set; }
        public string BackupPath { get; set; }
    }

    public partial class LicenseRestoreWindow : Window
    {
        public string SelectedPath { get; private set; }
        private ObservableCollection<BackupItemDisplay> _backupItems;

        public LicenseRestoreWindow()
        {
            InitializeComponent();
            _backupItems = new ObservableCollection<BackupItemDisplay>();
            lvBackups.ItemsSource = _backupItems;
            lvBackups.SelectionChanged += LvBackups_SelectionChanged;

            ScanDefaultBackups();

            // Apply translations
            try {
                this.Title = Helpers.LocalizationService.Get("lblPopupRestoreTitle");
                if (lblPopupRestoreTitle != null) lblPopupRestoreTitle.Text = Helpers.LocalizationService.Get("lblPopupRestoreTitle");
                if (lblPopupRestoreSubTitle != null) lblPopupRestoreSubTitle.Text = Helpers.LocalizationService.Get("lblPopupRestoreSubTitle");
                if (lblPopupRestoreNotFound != null) lblPopupRestoreNotFound.Text = Helpers.LocalizationService.Get("lblPopupRestoreNotFound");
                if (lblPopupRestoreBrowse != null) lblPopupRestoreBrowse.Text = Helpers.LocalizationService.Get("lblPopupBrowse");
                if (btnRestore != null) btnRestore.Content = Helpers.LocalizationService.Get("btnLicRestore");
                if (btnCancel != null) btnCancel.Content = Helpers.LocalizationService.Get("btnLicCancel");
            } catch {}
        }

        private void ScanDefaultBackups()
        {
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string defaultDir = Path.Combine(docPath, "HyperLis_Backup");

            if (!Directory.Exists(defaultDir)) return;

            foreach (var dir in Directory.GetDirectories(defaultDir))
            {
                AddBackupItem(dir);
            }
            
            if (_backupItems.Count > 0)
            {
                lvBackups.SelectedIndex = 0;
            }
        }

        private void AddBackupItem(string dir)
        {
            try
            {
                string jsonPath = Path.Combine(dir, "HyperLis_BackupActivation.json");
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var info = JsonSerializer.Deserialize<BackupActivationInfo>(jsonContent);

                    if (info != null)
                    {
                        string osName = string.IsNullOrEmpty(info.ProductName) ? "Unknown OS" : info.ProductName;
                        string edition = string.IsNullOrEmpty(info.EditionID) ? "" : $" ({info.EditionID})";
                        
                        string dateStr = "Unknown Date";
                        if (!string.IsNullOrEmpty(info.Time) && long.TryParse(info.Time, System.Globalization.NumberStyles.HexNumber, null, out long fileTime))
                        {
                            try
                            {
                                dateStr = DateTime.FromFileTimeUtc(fileTime).ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
                            }
                            catch { }
                        }

                        _backupItems.Add(new BackupItemDisplay
                        {
                            DisplayName = osName + edition,
                            DateString = $"{Helpers.LocalizationService.Get("msgBackupSavedTo")}: {dateStr}",
                            BackupPath = dir
                        });
                    }
                }
            }
            catch
            {
                // Ignore invalid folders
            }
        }

        private void LvBackups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnRestore.IsEnabled = lvBackups.SelectedItem != null;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Chọn thư mục chứa file HyperLis_BackupActivation.json",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer)
            };

            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FolderName;
                string jsonPath = Path.Combine(path, "HyperLis_BackupActivation.json");
                
                if (!File.Exists(jsonPath))
                {
                    MessageBox.Show("Thư mục này không chứa bản sao lưu hợp lệ (không tìm thấy file HyperLis_BackupActivation.json).", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Try to parse and add to top of list, then select it
                try
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var info = JsonSerializer.Deserialize<BackupActivationInfo>(jsonContent);

                    if (info != null)
                    {
                        string osName = string.IsNullOrEmpty(info.ProductName) ? "Unknown OS" : info.ProductName;
                        string dateStr = Helpers.LocalizationService.Get("msgManualFolder");

                        var item = new BackupItemDisplay
                        {
                            DisplayName = $"{osName} ({Helpers.LocalizationService.Get("msgManual")})",
                            DateString = dateStr,
                            BackupPath = path
                        };
                        
                        _backupItems.Insert(0, item);
                        lvBackups.SelectedItem = item;
                    }
                }
                catch
                {
                    MessageBox.Show("File cấu hình sao lưu bị lỗi hoặc không thể đọc.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (lvBackups.SelectedItem is BackupItemDisplay item)
            {
                SelectedPath = item.BackupPath;
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
