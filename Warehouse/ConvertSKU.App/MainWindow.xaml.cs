using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace ConvertSKU.App;

public partial class MainWindow : Window
{
    private string _currentSku = "";
    private string? _selectedWindows;
    private string? _selectedEdition;
    private bool _isConverting;

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
    };

    public MainWindow()
    {
        InitializeComponent();
        DetectCurrentWindows();
        UpdateSummary();
    }

    #region Detect System Info

    private void DetectCurrentWindows()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key is not null)
            {
                var productName = key.GetValue("ProductName") as string ?? "Windows";
                var displayVersion = key.GetValue("DisplayVersion") as string ?? "";
                var buildNumber = key.GetValue("CurrentBuildNumber") as string ?? "";
                var ubr = key.GetValue("UBR");
                var arch = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

                // Fix: Registry ProductName on Win 11 often shows "Windows 10"
                if (int.TryParse(buildNumber, out int build) && build >= 22000)
                    productName = productName.Replace("Windows 10", "Windows 11");

                _currentSku = productName;
                CurrentVersionText.Text = productName;
                VersionValue.Text = displayVersion;
                BuildValue.Text = ubr is not null ? $"{buildNumber}.{ubr}" : buildNumber;
                ArchValue.Text = arch;
                DetectLicenseStatus();
            }
        }
        catch
        {
            _currentSku = "Windows (không xác định)";
            CurrentVersionText.Text = _currentSku;
        }
    }

    private void DetectLicenseStatus()
    {
        try
        {
            var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT LicenseStatus FROM SoftwareLicensingProduct WHERE ApplicationID='55c92734-d682-4d71-983e-d6ec3f16059f' AND LicenseStatus=1");
            bool activated = false;
            foreach (var obj in searcher.Get()) { activated = true; break; }
            LicenseValue.Text = activated ? "✅ Activated" : "❌ Not Activated";
            LicenseValue.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(activated ? "#047857" : "#DC2626"));
        }
        catch { LicenseValue.Text = "Không xác định"; }
    }

    #endregion

    #region UI Event Handlers

    private void WindowsOption_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string ver)
        { _selectedWindows = ver; UpdateSummary(); }
    }

    private void EditionOption_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Content is string ed)
        { _selectedEdition = ed; UpdateSummary(); }
    }

    private void UpdateSummary()
    {
        if (SummaryText is null) return;
        SummaryText.Text = string.IsNullOrEmpty(_selectedWindows) || string.IsNullOrEmpty(_selectedEdition)
            ? "Vui lòng chọn phiên bản Windows và Edition muốn chuyển."
            : $"Từ {_currentSku} sang {_selectedWindows} {_selectedEdition}.";
    }

    private void ConvertButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isConverting) return;
        if (string.IsNullOrEmpty(_selectedWindows) || string.IsNullOrEmpty(_selectedEdition))
        {
            MessageBox.Show("Vui lòng chọn phiên bản Windows và Edition muốn chuyển.",
                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var msg = $"Bạn có chắc muốn chuyển từ:\n\n" +
                  $"   {_currentSku}\n   → {_selectedWindows} {_selectedEdition}\n\n" +
                  $"Sau khi hoàn tất, bạn cần khởi động lại máy tính.";
        if (MessageBox.Show(msg, "Xác nhận chuyển đổi SKU",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        _isConverting = true;
        ProgressOverlay.Visibility = Visibility.Visible;
        LogText.Text = "";
        RestartButton.Visibility = Visibility.Collapsed;
        CloseLogButton.Visibility = Visibility.Collapsed;
        _ = RunConversionAsync();
    }

    private void CloseLog_Click(object sender, RoutedEventArgs e)
    {
        ProgressOverlay.Visibility = Visibility.Collapsed;
        _isConverting = false;
    }

    private void RestartPC_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Khởi động lại máy tính ngay bây giờ?", "Xác nhận",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Process.Start("shutdown", "/r /t 5 /c \"Khởi động lại để áp dụng chuyển đổi SKU\"");
            Application.Current.Shutdown();
        }
    }

    #endregion

    #region Conversion Logic

    private async Task RunConversionAsync()
    {
        try
        {
            var winVer = _selectedWindows!.Contains("10") ? "10" : "11";
            var edition = _selectedEdition!;
            var productKey = EditionToKey[edition];

            AppendLog($"🔄 Bắt đầu: {_currentSku} → {_selectedWindows} {edition}");
            AppendLog($"🔑 Product Key: {productKey}\n");

            // Step 1: Find SKU path
            var skuBase = FindSkuBasePath();
            if (skuBase == null) { AppendLog("❌ Không tìm thấy thư mục SKU files!"); ShowDone(false); return; }

            var skuSource = Path.Combine(skuBase, $"SKUs-Windows-{winVer}");
            if (!Directory.Exists(skuSource)) { AppendLog($"❌ Không tìm thấy: {skuSource}"); ShowDone(false); return; }

            // Step 2: Take ownership of target directory
            var target = @"C:\Windows\System32\spp\tokens\skus";
            AppendLog($"🔐 [Bước 1] Lấy quyền sở hữu thư mục đích...");
            await RunCommandAsync($"takeown /F \"{target}\" /R /D Y");
            AppendLog("   ✓ Takeown hoàn thành");
            await RunCommandAsync($"icacls \"{target}\" /grant Administrators:F /T /Q");
            AppendLog("   ✓ Cấp quyền hoàn thành\n");

            // Step 3: Copy SKU files
            AppendLog($"📁 [Bước 2] Sao chép SKU files...");
            AppendLog($"   Nguồn: {skuSource}");
            AppendLog($"   Đích:  {target}");
            await RunCommandAsync($"xcopy \"{skuSource}\" \"{target}\" /E /I /Y /Q");
            AppendLog("   ✅ Sao chép thành công!\n");

            // Step 4: Run commands
            AppendLog("⚙️ [Bước 3] Thực hiện chuyển đổi SKU...\n");
            var cmds = new (string desc, string cmd)[]
            {
                ("Cài đặt lại license", "cscript.exe %windir%\\system32\\slmgr.vbs /rilc"),
                ("Gỡ product key cũ", "cscript.exe %windir%\\system32\\slmgr.vbs /upk >nul 2>&1"),
                ("Xóa KMS server", "cscript.exe %windir%\\system32\\slmgr.vbs /ckms >nul 2>&1"),
                ("Xóa product key cache", "cscript.exe %windir%\\system32\\slmgr.vbs /cpky >nul 2>&1"),
                ($"Nạp key {edition}", $"cscript.exe %windir%\\system32\\slmgr.vbs /ipk {productKey}"),
                ("Khởi động License Manager", "sc config LicenseManager start= auto & net start LicenseManager"),
                ("Khởi động Windows Update", "sc config wuauserv start= auto & net start wuauserv"),
                ("Chạy clipup", "clipup -v -o -altto c:\\"),
            };

            for (int i = 0; i < cmds.Length; i++)
            {
                AppendLog($"   ▶ {cmds[i].desc}...");
                var output = await RunCommandAsync(cmds[i].cmd);
                if (!string.IsNullOrWhiteSpace(output))
                    foreach (var l in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        if (!string.IsNullOrWhiteSpace(l.Trim()))
                            AppendLog($"   {l.Trim()}");
                AppendLog($"   ✓ Hoàn thành");
            }

            AppendLog("\n══════════════════════════════════════");
            AppendLog("🎉 CHUYỂN ĐỔI SKU HOÀN TẤT!");
            AppendLog("🔄 Vui lòng khởi động lại máy tính.");
            AppendLog("══════════════════════════════════════");
            ShowDone(true);
        }
        catch (Exception ex)
        {
            AppendLog($"\n❌ Lỗi không mong muốn: {ex.Message}");
            ShowDone(false);
        }
    }

    private string? FindSkuBasePath()
    {
        // Check Warehouse folder next to exe (build output)
        var p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Warehouse");
        if (Directory.Exists(p1)) return p1;
        // Fallback: SKU Win10_Win11 next to exe
        var p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SKU Win10_Win11");
        if (Directory.Exists(p2)) return p2;
        return null;
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
            using var process = Process.Start(psi)!;
            // Read both streams async to avoid deadlock
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit(60000); // timeout 60s
            var output = outputTask.Result;
            var error = errorTask.Result;
            return string.IsNullOrWhiteSpace(output) ? error : output;
        });
    }

    private void AppendLog(string msg)
    {
        Dispatcher.Invoke(() =>
        {
            LogText.Text += msg + "\n";
            LogScroller.ScrollToEnd();
        });
    }

    private void ShowDone(bool success)
    {
        Dispatcher.Invoke(() =>
        {
            if (success) RestartButton.Visibility = Visibility.Visible;
            CloseLogButton.Visibility = Visibility.Visible;
            _isConverting = false;
        });
    }

    #endregion
}