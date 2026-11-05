using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;
using HyperLisUI.Models;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Linq;

namespace HyperLisUI.Helpers
{
    public static class LicenseBackupHelper
    {
        private const string SppStorePath = @"C:\Windows\System32\spp\store\2.0";
        private const string RegWindowsNT = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        public static async Task StopServicesAsync()
        {
            await RunCommandAsync("net", "stop sppsvc /y");
            await RunCommandAsync("net", "stop osppsvc /y");
        }

        public static async Task StartServicesAsync()
        {
            await RunCommandAsync("net", "start sppsvc");
            await RunCommandAsync("net", "start osppsvc");
        }

        public static async Task<bool> BackupLicenseAsync(string backupFolderPath, Action<int, int, string> progress = null)
        {
            try
            {
                // Ensure backup directory exists
                Directory.CreateDirectory(backupFolderPath);

                // 1. Stop services
                progress?.Invoke(0, 10, "Đang dừng các dịch vụ bản quyền...");
                await StopServicesAsync();

                // 2. Copy SPP Store
                progress?.Invoke(0, 40, "Đang sao chép mã thông báo bản quyền...");
                string storeBackupPath = Path.Combine(backupFolderPath, "2.0");
                CopyDirectory(SppStorePath, storeBackupPath);

                // 3. Read Registry
                progress?.Invoke(0, 70, "Đang trích xuất khóa Registry...");
                var backupInfo = new BackupActivationInfo
                {
                    Time = DateTime.Now.ToFileTimeUtc().ToString("X"),
                    Description = ""
                };

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegWindowsNT))
                {
                    if (key != null)
                    {
                        backupInfo.ProductName = key.GetValue("ProductName")?.ToString() ?? "";
                        backupInfo.EditionID = key.GetValue("EditionID")?.ToString() ?? "";
                        backupInfo.CurrentVersion = key.GetValue("CurrentVersion")?.ToString() ?? "";
                        backupInfo.ProductId = key.GetValue("ProductId")?.ToString() ?? "";

                        byte[] digitalProductId = key.GetValue("DigitalProductId") as byte[];
                        if (digitalProductId != null)
                        {
                            backupInfo.DigitalProductId = ByteArrayToHexString(digitalProductId);
                        }

                        byte[] digitalProductId4 = key.GetValue("DigitalProductId4") as byte[];
                        if (digitalProductId4 != null)
                        {
                            backupInfo.DigitalProductId4 = ByteArrayToHexString(digitalProductId4);
                        }
                    }
                }

                // 4. Create HyperLis_BackupActivation.json
                string jsonPath = Path.Combine(backupFolderPath, "HyperLis_BackupActivation.json");
                string jsonString = JsonSerializer.Serialize(backupInfo, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(jsonPath, jsonString);

                // 5. Start services
                progress?.Invoke(0, 90, "Đang khởi động lại dịch vụ...");
                await StartServicesAsync();

                progress?.Invoke(0, 100, "Hoàn tất sao lưu!");
                await Task.Delay(500); // Give user time to see 100%
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Backup error: {ex.Message}");
                // Try to start services even if it fails
                await StartServicesAsync();
                return false;
            }
        }

        public static async Task<bool> RestoreLicenseAsync(string backupFolderPath, Action<int, int, string> progress = null)
        {
            try
            {
                string jsonPath = Path.Combine(backupFolderPath, "HyperLis_BackupActivation.json");
                string storeBackupPath = Path.Combine(backupFolderPath, "2.0");

                if (!File.Exists(jsonPath) || !Directory.Exists(storeBackupPath))
                {
                    return false;
                }

                // 1. Read JSON
                string jsonString = File.ReadAllText(jsonPath);
                var backupInfo = JsonSerializer.Deserialize<BackupActivationInfo>(jsonString);

                if (backupInfo == null) return false;

                // 2. Stop services
                progress?.Invoke(0, 10, "Đang dừng các dịch vụ bản quyền...");
                await StopServicesAsync();

                // 3. Restore Registry
                progress?.Invoke(0, 40, "Đang khôi phục khóa Registry...");
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegWindowsNT, writable: true))
                {
                    if (key != null)
                    {
                        if (!string.IsNullOrEmpty(backupInfo.DigitalProductId))
                        {
                            byte[] digitalProductId = HexStringToByteArray(backupInfo.DigitalProductId);
                            key.SetValue("DigitalProductId", digitalProductId, RegistryValueKind.Binary);
                        }

                        if (!string.IsNullOrEmpty(backupInfo.DigitalProductId4))
                        {
                            byte[] digitalProductId4 = HexStringToByteArray(backupInfo.DigitalProductId4);
                            key.SetValue("DigitalProductId4", digitalProductId4, RegistryValueKind.Binary);
                        }
                    }
                }

                // 4. Restore SPP Store
                progress?.Invoke(0, 70, "Đang khôi phục mã thông báo bản quyền...");
                // Note: Deleting everything inside spp store requires careful handling
                EmptyDirectory(SppStorePath);
                CopyDirectory(storeBackupPath, SppStorePath);
                
                // Set permissions for Network Service if needed
                await RunCommandAsync("icacls", $"\"{SppStorePath}\" /grant \"NETWORK SERVICE\":(OI)(CI)(F) /T");

                // 5. Start services
                progress?.Invoke(0, 90, "Đang khởi động lại dịch vụ...");
                await StartServicesAsync();

                progress?.Invoke(0, 100, "Hoàn tất khôi phục!");
                await Task.Delay(500); // Give user time to see 100%
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Restore error: {ex.Message}");
                await StartServicesAsync();
                return false;
            }
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFile, true);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                string targetSubDirectory = Path.Combine(targetDir, Path.GetFileName(directory));
                CopyDirectory(directory, targetSubDirectory);
            }
        }

        private static void EmptyDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                foreach (string file in Directory.GetFiles(directoryPath))
                {
                    try { File.Delete(file); } catch { }
                }
                foreach (string dir in Directory.GetDirectories(directoryPath))
                {
                    try { Directory.Delete(dir, true); } catch { }
                }
            }
        }

        private static string ByteArrayToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        private static byte[] HexStringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        private static async Task RunCommandAsync(string fileName, string arguments)
        {
            await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Verb = "runas" // Request elevation
                    };
                    using (Process process = Process.Start(psi))
                    {
                        process?.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error running {fileName} {arguments}: {ex.Message}");
                }
            });
        }
    }
}
