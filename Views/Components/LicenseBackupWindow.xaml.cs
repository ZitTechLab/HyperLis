using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HyperLisUI.Views.Components
{
    public partial class LicenseBackupWindow : Window
    {
        public string SelectedPath { get; private set; }

        private string _defaultDir;

        public LicenseBackupWindow()
        {
            InitializeComponent();
            
            // Setup default path
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _defaultDir = Path.Combine(docPath, "HyperLis_Backup");
            txtDefaultPath.Text = $"Đường dẫn: {_defaultDir}";
            
            // Set default file name for custom path
            string defaultFolderName = $"ActiveBackup_{DateTime.Now:yyyyMMdd_HHmmss}";
            tbCustomPath.Text = Path.Combine(_defaultDir, defaultFolderName);

            // Apply translations
            try {
                this.Title = Helpers.LocalizationService.Get("lblPopupBackupTitle");
                if (lblPopupBackupTitle != null) lblPopupBackupTitle.Text = Helpers.LocalizationService.Get("lblPopupBackupTitle");
                if (lblPopupBackupSubTitle != null) lblPopupBackupSubTitle.Text = Helpers.LocalizationService.Get("lblPopupBackupSubTitle");
                if (rbDefaultPath != null) rbDefaultPath.Content = Helpers.LocalizationService.Get("rbDefaultPath");
                if (rbCustomPath != null) rbCustomPath.Content = Helpers.LocalizationService.Get("rbCustomPath");
                if (lblPopupBackupBrowse != null) lblPopupBackupBrowse.Text = Helpers.LocalizationService.Get("lblPopupBrowse");
                if (btnBackup != null) btnBackup.Content = Helpers.LocalizationService.Get("btnLicBackup");
                if (btnCancel != null) btnCancel.Content = Helpers.LocalizationService.Get("btnLicCancel");
                if (txtDefaultPath != null) txtDefaultPath.Text = $"{Helpers.LocalizationService.Get("lblPopupPath")}: {_defaultDir}";
            } catch {}
        }

        private void Radio_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            if (rbDefaultPath.IsChecked == true)
            {
                rbDefaultPath.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));
                rbCustomPath.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                
                if (tbCustomPath != null)
                {
                    tbCustomPath.IsEnabled = false;
                    btnBrowse.IsEnabled = false;
                    tbCustomPath.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
                }
            }
            else
            {
                rbDefaultPath.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                rbCustomPath.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));
                
                if (tbCustomPath != null)
                {
                    tbCustomPath.IsEnabled = true;
                    btnBrowse.IsEnabled = true;
                    tbCustomPath.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                }
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Chọn thư mục lưu bản sao lưu",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() == true)
            {
                // Create a subfolder inside the selected path for the backup
                string defaultFolderName = $"ActiveBackup_{DateTime.Now:yyyyMMdd_HHmmss}";
                tbCustomPath.Text = Path.Combine(dialog.FolderName, defaultFolderName);
            }
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            if (rbDefaultPath.IsChecked == true)
            {
                // Ensure directory exists
                if (!Directory.Exists(_defaultDir))
                {
                    Directory.CreateDirectory(_defaultDir);
                }
                SelectedPath = Path.Combine(_defaultDir, $"ActiveBackup_{DateTime.Now:yyyyMMdd_HHmmss}");
            }
            else
            {
                string customPath = tbCustomPath.Text.Trim();
                if (string.IsNullOrEmpty(customPath))
                {
                    MessageBox.Show("Vui lòng chọn đường dẫn hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                try
                {
                    if (!Directory.Exists(customPath))
                    {
                        Directory.CreateDirectory(customPath);
                    }
                }
                catch
                {
                    MessageBox.Show("Đường dẫn không hợp lệ hoặc không có quyền truy cập.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                SelectedPath = customPath;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
