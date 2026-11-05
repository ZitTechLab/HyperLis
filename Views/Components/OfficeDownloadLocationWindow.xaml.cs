using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace HyperLisUI.Views.Components
{
    public partial class OfficeDownloadLocationWindow : Window
    {
        private const string PlaceholderText = "Hãy chọn 1 đường dẫn";
        private const string DownloadFolderName = "OfficeDownload";

        private static readonly Brush PlaceholderBrush = new SolidColorBrush(Color.FromRgb(156, 163, 175));
        private static readonly Brush NormalTextBrush = new SolidColorBrush(Color.FromRgb(55, 65, 81));

        private readonly string _browseInitialFolder;
        private string _lastBaseFolder;
        private string _selectedFolderPath = string.Empty;

        public string SelectedFolderPath => _selectedFolderPath;

        public OfficeDownloadLocationWindow(string defaultFolder)
        {
            InitializeComponent();

            string initialFolder = defaultFolder;
            if (string.IsNullOrWhiteSpace(initialFolder) || !Directory.Exists(initialFolder))
            {
                initialFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            }

            if (string.IsNullOrWhiteSpace(initialFolder) || !Directory.Exists(initialFolder))
            {
                initialFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            _browseInitialFolder = initialFolder;
            _lastBaseFolder = initialFolder;

            ShowPlaceholderPath();
            UpdateFreeSpaceLabel();
        }

        private void FolderPath_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateFreeSpaceLabel();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Chon vi tri luu tai xuong",
                Multiselect = false
            };

            string initialDirectory = _lastBaseFolder;
            if (string.IsNullOrWhiteSpace(initialDirectory) || !Directory.Exists(initialDirectory))
            {
                initialDirectory = _browseInitialFolder;
            }

            if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }

            if (dialog.ShowDialog() == true)
            {
                string selectedBaseFolder = dialog.FolderName?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(selectedBaseFolder))
                {
                    return;
                }

                _lastBaseFolder = selectedBaseFolder;
                _selectedFolderPath = BuildDownloadFolderPath(selectedBaseFolder);
                ShowSelectedPath();
                UpdateFreeSpaceLabel();
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SelectedFolderPath))
            {
                CustomMessageBoxWindow.Show("Vui long chon thu muc luu.", "Thong bao");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void UpdateFreeSpaceLabel()
        {
            try
            {
                string path = SelectedFolderPath;
                if (string.IsNullOrWhiteSpace(path))
                {
                    txtFreeSpace.Text = "Dung lượng trống: --";
                    return;
                }

                string resolvedPath = path;
                if (!Directory.Exists(resolvedPath))
                {
                    string? root = Path.GetPathRoot(resolvedPath);
                    if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                    {
                        txtFreeSpace.Text = "Dung lượng trống: --";
                        return;
                    }

                    resolvedPath = root;
                }

                var driveInfo = new DriveInfo(Path.GetPathRoot(resolvedPath)!);
                double freeGiB = driveInfo.AvailableFreeSpace / (1024d * 1024d * 1024d);
                txtFreeSpace.Text = $"Dung lượng trống: {freeGiB:N1} GB";
            }
            catch
            {
                txtFreeSpace.Text = "Dung lượng trống: --";
            }
        }

        private void ShowPlaceholderPath()
        {
            txtFolderPath.Foreground = PlaceholderBrush;
            txtFolderPath.Text = PlaceholderText;
        }

        private void ShowSelectedPath()
        {
            txtFolderPath.Foreground = NormalTextBrush;
            txtFolderPath.Text = _selectedFolderPath;
        }

        private static string BuildDownloadFolderPath(string baseFolder)
        {
            string normalized = baseFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(Path.GetFileName(normalized), DownloadFolderName, StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            return Path.Combine(normalized, DownloadFolderName);
        }
    }
}
