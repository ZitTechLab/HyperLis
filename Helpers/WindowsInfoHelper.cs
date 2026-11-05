using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace HyperLisUI.Helpers
{
    public class WindowsInfo
    {
        public string OsName { get; set; } = LocalizationService.Get("lblConvertSKU_Loading") ?? "Đang tải...";
        public string Version { get; set; } = LocalizationService.Get("lblConvertSKU_Loading") ?? "Đang tải...";
        public string Build { get; set; } = LocalizationService.Get("lblConvertSKU_Loading") ?? "Đang tải...";
        public string Architecture { get; set; } = LocalizationService.Get("lblConvertSKU_Loading") ?? "Đang tải...";
        public string ProductId { get; set; } = LocalizationService.Get("lblConvertSKU_Loading") ?? "Đang tải...";
        public string InstallDate { get; set; } = LocalizationService.Get("lblConvertSKU_Loading") ?? "Đang tải...";
        public string LastBoot { get; set; } = LocalizationService.Get("lblConvertSKU_Loading") ?? "Đang tải...";
        public string LicenseStatus { get; set; } = LocalizationService.Get("lblConvertSKU_Loading") ?? "Đang tải...";
        public string LicenseChannel { get; set; } = LocalizationService.Get("lblConvertSKU_Loading") ?? "Đang tải...";
        public bool IsActivated { get; set; } = false;
        public string InstalledKey { get; set; } = "";
        public string PartialKey { get; set; } = "";
        public string OemKey { get; set; } = "";
        public string OemEdition { get; set; } = "";

        // MAS Detection & OEM Matching fields
        public bool IsMasDetected { get; set; } = false;
        public string MasMethod { get; set; } = "";
        public string MasWarningMessage { get; set; } = "";
        public bool IsOemOriginalMatch { get; set; } = false;
        public bool IsOemMismatch { get; set; } = false;
        public bool IsKms38 { get; set; } = false;
        public bool IsOnlineKms { get; set; } = false;
        public string KmsHost { get; set; } = "";
        public bool HasOhook { get; set; } = false;

        public string GetStatusTitle()
        {
            if (IsActivated)
            {
                if (IsMasDetected)
                {
                    return LocalizationService.Get("txtStatusTitleMasWarning") ?? "Cảnh báo bản quyền MAS / Không khớp OEM";
                }
                return LocalizationService.Get("txtStatusTitleActivated") ?? "Windows đã được kích hoạt";
            }
            return LocalizationService.Get("txtStatusTitleNotActivated") ?? "Windows chưa được kích hoạt";
        }

        public string GetMasWarningMessage()
        {
            if (IsOemMismatch)
            {
                string currentCleanName = OsName?.Replace("Microsoft ", "").Trim() ?? "";
                string fmt = LocalizationService.Get("txtStatusDescMasMismatch") ?? "Phát hiện bản quyền không khớp với OEM gốc đi kèm máy. Khóa gốc BIOS là Windows {0}, nhưng thiết bị hiện đang chạy {1} (kích hoạt bằng {2}).";
                return string.Format(fmt, OemEdition, currentCleanName, MasMethod);
            }
            if (IsKms38)
            {
                return LocalizationService.Get("txtStatusDescMasKms38") ?? "Thiết bị đang được kích hoạt bằng phương thức KMS38 (Hạn sử dụng đến năm 2038).";
            }
            if (IsOnlineKms)
            {
                string fmt = LocalizationService.Get("txtStatusDescMasOnlineKms") ?? "Thiết bị đang kích hoạt qua máy chủ Online KMS ngoài ({0}).";
                return string.Format(fmt, KmsHost);
            }
            return LocalizationService.Get("txtStatusDescMasGeneric") ?? "Thiết bị đang sử dụng bản quyền kích hoạt qua MAS.";
        }

        public string GetActivationDescription()
        {
            if (!IsActivated)
            {
                return LocalizationService.Get("txtStatusDescNotActivated") ?? "Thiết bị chưa có bản quyền hợp lệ.";
            }
            if (IsMasDetected)
            {
                return GetMasWarningMessage();
            }
            if (IsOemOriginalMatch)
            {
                string fmt = LocalizationService.Get("txtStatusDescOemMatch") ?? "Thiết bị đang sử dụng đúng bản quyền OEM gốc đi kèm máy ({0}).";
                return string.Format(fmt, OemEdition);
            }
            return LocalizationService.Get("txtStatusDescActivated") ?? "Thiết bị này đã được kích hoạt bằng bản quyền số.";
        }

        public string GetStatusTagText()
        {
            if (IsActivated)
            {
                if (IsMasDetected)
                {
                    return MasMethod;
                }
                if (IsOemOriginalMatch)
                {
                    return LocalizationService.Get("txtTagOemOriginal") ?? "OEM License (Gốc theo máy)";
                }
                return $"{LicenseChannel} License";
            }
            return LocalizationService.Get("msgInvalid") ?? "Not Activated";
        }

        public string GetActivationStatusText()
        {
            return LocalizationService.Get(IsActivated ? "lblLicStatusActivated" : "lblLicStatusNotActivated") 
                   ?? (IsActivated ? "Đã kích hoạt" : "Chưa kích hoạt");
        }
    }

    public static class WindowsInfoHelper
    {
        public static WindowsInfo GetCurrentWindowsInfo()
        {
            var info = new WindowsInfo();

            try
            {
                // 1. Get basic OS info from Win32_OperatingSystem
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption, Version, BuildNumber, OSArchitecture, SerialNumber, InstallDate, LastBootUpTime FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        info.OsName = obj["Caption"]?.ToString().Trim() ?? "Unknown";
                        info.Build = obj["BuildNumber"]?.ToString() ?? "Unknown";
                        info.Architecture = obj["OSArchitecture"]?.ToString() ?? "Unknown";
                        info.ProductId = obj["SerialNumber"]?.ToString() ?? "Unknown";

                        // Format dates
                        string rawInstall = obj["InstallDate"]?.ToString();
                        if (!string.IsNullOrEmpty(rawInstall) && rawInstall.Length >= 8)
                        {
                            info.InstallDate = $"{rawInstall.Substring(6, 2)}/{rawInstall.Substring(4, 2)}/{rawInstall.Substring(0, 4)}";
                        }

                        string rawBoot = obj["LastBootUpTime"]?.ToString();
                        if (!string.IsNullOrEmpty(rawBoot) && rawBoot.Length >= 12)
                        {
                            info.LastBoot = $"{rawBoot.Substring(6, 2)}/{rawBoot.Substring(4, 2)}/{rawBoot.Substring(0, 4)} {rawBoot.Substring(8, 2)}:{rawBoot.Substring(10, 2)}";
                        }
                        
                        break; // Only need the first one
                    }
                }

                // 2. Adjust OS Name if it's Windows 11 (WMI often reports Windows 10 for Windows 11)
                if (info.OsName.Contains("Windows 10"))
                {
                    if (int.TryParse(info.Build, out int buildNum) && buildNum >= 22000)
                    {
                        info.OsName = info.OsName.Replace("Windows 10", "Windows 11");
                    }
                }

                // 3. Get Release Version (e.g. 24H2) from Registry
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    {
                        if (key != null)
                        {
                            object displayVersion = key.GetValue("DisplayVersion");
                            object releaseId = key.GetValue("ReleaseId");
                            if (displayVersion != null)
                                info.Version = displayVersion.ToString();
                            else if (releaseId != null)
                                info.Version = releaseId.ToString();
                            else
                                info.Version = "Unknown";
                                
                            // Add minor build number (UBR)
                            object ubr = key.GetValue("UBR");
                            if (ubr != null && info.Build != "Unknown")
                            {
                                info.Build = $"{info.Build}.{ubr}";
                            }
                        }
                    }
                }
                catch { }

                // 4. Get Activation Status & License Channel via SoftwareLicensingProduct
                info.LicenseStatus = LocalizationService.Get("lblConvertSKU_NotActivated") ?? "Chưa kích hoạt";
                info.LicenseChannel = "Retail";
                info.IsActivated = false;

                try
                {
                    // Filter specifically for Windows Application ID: 55c92734-d682-4d71-983e-d6ec3f16059f
                    // and where PartialProductKey is not null
                    string wmiQuery = "SELECT Name, Description, LicenseStatus, PartialProductKey, ProductKeyChannel " +
                                      "FROM SoftwareLicensingProduct " +
                                      "WHERE ApplicationId = '55c92734-d682-4d71-983e-d6ec3f16059f' AND PartialProductKey IS NOT NULL";

                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery))
                    {
                        ManagementObject? activatedObj = null;
                        ManagementObject? fallbackObj = null;

                        foreach (ManagementObject obj in searcher.Get())
                        {
                            fallbackObj ??= obj;

                            int status = 0;
                            if (obj["LicenseStatus"] != null)
                            {
                                int.TryParse(obj["LicenseStatus"].ToString(), out status);
                            }

                            if (status == 1) // 1 = Licensed (Activated)
                            {
                                activatedObj = obj;
                                break;
                            }
                        }

                        var targetObj = activatedObj ?? fallbackObj;
                        if (targetObj != null)
                        {
                            string description = targetObj["Description"]?.ToString() ?? "";
                            string productKeyChannel = targetObj["ProductKeyChannel"]?.ToString() ?? "";
                            string partialKey = targetObj["PartialProductKey"]?.ToString() ?? "";

                            if (!string.IsNullOrEmpty(partialKey))
                            {
                                info.PartialKey = partialKey;
                            }

                            // Determine License Channel
                            if (description.Contains("VOLUME", StringComparison.OrdinalIgnoreCase) || 
                                productKeyChannel.Contains("Volume", StringComparison.OrdinalIgnoreCase))
                            {
                                info.LicenseChannel = "Volume";
                            }
                            else if (description.Contains("OEM", StringComparison.OrdinalIgnoreCase) || 
                                     productKeyChannel.Contains("OEM", StringComparison.OrdinalIgnoreCase))
                            {
                                info.LicenseChannel = "OEM";
                            }
                            else
                            {
                                info.LicenseChannel = "Retail";
                            }

                            int finalStatus = 0;
                            if (targetObj["LicenseStatus"] != null)
                            {
                                int.TryParse(targetObj["LicenseStatus"].ToString(), out finalStatus);
                            }

                            if (finalStatus == 1)
                            {
                                info.LicenseStatus = LocalizationService.Get("lblConvertSKU_Activated") ?? "Đã kích hoạt";
                                info.IsActivated = true;
                            }
                            else
                            {
                                info.LicenseStatus = LocalizationService.Get("lblConvertSKU_NotActivated") ?? "Chưa kích hoạt";
                                info.IsActivated = false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("WMI SoftwareLicensingProduct query error: " + ex.Message);
                }

                // Fallback check: If WMI didn't detect activation (e.g. WMI query failure or un-enumerated Digital License)
                if (!info.IsActivated)
                {
                    CheckActivationFallback(info);
                }

                // 5. Get Installed Key
                try
                {
                    using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform"))
                    {
                        if (key != null)
                        {
                            object backupKey = key.GetValue("BackupProductKeyDefault");
                            if (backupKey != null)
                            {
                                info.InstalledKey = backupKey.ToString();
                            }
                        }
                    }
                }
                catch { }

                // 6. Get OEM Key & Edition
                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT OA3xOriginalProductKey, OA3xOriginalProductKeyDescription FROM SoftwareLicensingService"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            info.OemKey = obj["OA3xOriginalProductKey"]?.ToString() ?? "";
                            string rawOem = obj["OA3xOriginalProductKeyDescription"]?.ToString() ?? "";
                            info.OemEdition = ParseOemEdition(rawOem);
                            break;
                        }
                    }
                }
                catch { }

                // 7. Detect MAS Activation & Match OEM Status
                DetectMasAndOemStatus(info);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error getting Windows info: " + ex.Message);
            }
            return info;
        }

        private static void DetectMasAndOemStatus(WindowsInfo info)
        {
            try
            {
                // 1. Check Ohook (Office)
                CheckOhookPresence(info);

                // 2. Check KMS38 / Online KMS
                CheckKmsMasStatus(info);

                // 3. Match current edition against OEM edition in BIOS
                if (!string.IsNullOrEmpty(info.OemKey) && !string.IsNullOrEmpty(info.OemEdition))
                {
                    bool isMatch = IsEditionMatch(info.OsName, info.OemEdition);

                    if (isMatch)
                    {
                        info.IsOemOriginalMatch = true;
                        info.IsOemMismatch = false;
                        // If edition matches OEM and not using KMS38/Online KMS, it's valid OEM original license
                        if (!info.IsKms38 && !info.IsOnlineKms)
                        {
                            info.IsMasDetected = false;
                        }
                    }
                    else
                    {
                        info.IsOemOriginalMatch = false;
                        // Machine has OEM for another edition (e.g. Home SL) but is currently running and activated on another edition (e.g. Pro)
                        if (info.IsActivated)
                        {
                            info.IsMasDetected = true;
                            info.IsOemMismatch = true;
                            if (string.IsNullOrEmpty(info.MasMethod))
                            {
                                info.MasMethod = "MAS HWID License";
                            }
                            info.MasWarningMessage = info.GetMasWarningMessage();
                        }
                    }
                }
                else
                {
                    // No OEM key in BIOS
                    info.IsOemOriginalMatch = false;
                    info.IsOemMismatch = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DetectMasAndOemStatus error: " + ex.Message);
            }
        }

        private static bool IsEditionMatch(string currentOsName, string oemEdition)
        {
            if (string.IsNullOrWhiteSpace(currentOsName) || string.IsNullOrWhiteSpace(oemEdition))
                return false;

            string current = currentOsName.ToLowerInvariant();
            string oem = oemEdition.ToLowerInvariant();

            if (oem.Contains("single language"))
            {
                return current.Contains("single language");
            }
            if (oem.Contains("china") || oem.Contains("countryspecific"))
            {
                return current.Contains("china") || current.Contains("countryspecific");
            }
            if (oem == "home" || oem.Contains("core"))
            {
                return current.Contains("home") && !current.Contains("single language") && !current.Contains("china");
            }
            if (oem == "pro" || oem.Contains("professional"))
            {
                if (oem.Contains("education"))
                    return current.Contains("pro education") || current.Contains("professional education");
                if (oem.Contains("workstation"))
                    return current.Contains("workstation");
                return (current.Contains("pro") || current.Contains("professional")) &&
                       !current.Contains("education") && !current.Contains("workstation");
            }
            if (oem.Contains("education"))
            {
                return current.Contains("education");
            }
            if (oem.Contains("enterprise"))
            {
                return current.Contains("enterprise");
            }

            return current.Contains(oem);
        }

        private static void CheckOhookPresence(WindowsInfo info)
        {
            try
            {
                string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                string commonFiles = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
                string commonFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);

                string[] ohookDllPaths = new[]
                {
                    Path.Combine(progFiles, @"Microsoft Office\root\vfs\System\sppc.dll"),
                    Path.Combine(progFilesX86, @"Microsoft Office\root\vfs\System\sppc.dll"),
                    Path.Combine(commonFiles, @"microsoft shared\ClickToRun\sppc.dll"),
                    Path.Combine(commonFilesX86, @"microsoft shared\ClickToRun\sppc.dll"),
                    Path.Combine(progFiles, @"Microsoft Office\root\Office16\version.dll"),
                    Path.Combine(progFilesX86, @"Microsoft Office\root\Office16\version.dll")
                };

                foreach (var path in ohookDllPaths)
                {
                    if (File.Exists(path))
                    {
                        info.HasOhook = true;
                        break;
                    }
                }
            }
            catch { }
        }

        private static void CheckKmsMasStatus(WindowsInfo info)
        {
            try
            {
                // Check KMS Host server
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform"))
                {
                    if (key != null)
                    {
                        object? kmsHost = key.GetValue("KeyManagementServiceMachine");
                        if (kmsHost != null && !string.IsNullOrWhiteSpace(kmsHost.ToString()))
                        {
                            string hostStr = kmsHost.ToString()!.Trim();
                            if (!hostStr.Equals("localhost", StringComparison.OrdinalIgnoreCase) && 
                                !hostStr.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                            {
                                info.IsMasDetected = true;
                                info.IsOnlineKms = true;
                                info.KmsHost = hostStr;
                                info.MasMethod = "Online KMS (MAS)";
                                info.MasWarningMessage = info.GetMasWarningMessage();
                            }
                        }
                    }
                }

                // Check KMS38 via SoftwareLicensingProduct GracePeriodRemaining or slmgr check
                if (info.LicenseChannel.Equals("Volume", StringComparison.OrdinalIgnoreCase) && info.IsActivated)
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                        "SELECT GracePeriodRemaining FROM SoftwareLicensingProduct WHERE ApplicationId = '55c92734-d682-4d71-983e-d6ec3f16059f' AND LicenseStatus = 1"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            if (obj["GracePeriodRemaining"] != null &&
                                uint.TryParse(obj["GracePeriodRemaining"].ToString(), out uint graceMins))
                            {
                                // Regular KMS is 180 days (~259200 mins). KMS38 is until year 2038 (~ > 3,000,000 mins)
                                if (graceMins > 1000000)
                                {
                                    info.IsMasDetected = true;
                                    info.IsKms38 = true;
                                    info.MasMethod = "KMS38 (MAS)";
                                    info.MasWarningMessage = info.GetMasWarningMessage();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static void CheckActivationFallback(WindowsInfo info)
        {
            try
            {
                string slmgrPath = Path.Combine(Environment.SystemDirectory, "slmgr.vbs");
                if (!File.Exists(slmgrPath)) return;

                var psi = new ProcessStartInfo
                {
                    FileName = "cscript.exe",
                    Arguments = $"//nologo \"{slmgrPath}\" /dli",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);

                    if (!string.IsNullOrEmpty(output))
                    {
                        if (output.Contains("Licensed", StringComparison.OrdinalIgnoreCase) ||
                            output.Contains("Được cấp phép", StringComparison.OrdinalIgnoreCase) ||
                            output.Contains("permanently activated", StringComparison.OrdinalIgnoreCase) ||
                            output.Contains("kích hoạt vĩnh viễn", StringComparison.OrdinalIgnoreCase))
                        {
                            info.IsActivated = true;
                            info.LicenseStatus = LocalizationService.Get("lblConvertSKU_Activated") ?? "Đã kích hoạt";

                            if (output.Contains("VOLUME", StringComparison.OrdinalIgnoreCase))
                                info.LicenseChannel = "Volume";
                            else if (output.Contains("OEM", StringComparison.OrdinalIgnoreCase))
                                info.LicenseChannel = "OEM";
                            else if (output.Contains("RETAIL", StringComparison.OrdinalIgnoreCase))
                                info.LicenseChannel = "Retail";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fallback slmgr check error: " + ex.Message);
            }
        }

        private static string ParseOemEdition(string rawDescription)
        {
            if (string.IsNullOrEmpty(rawDescription)) return rawDescription;
            
            string edition = rawDescription;
            
            // Lọc tiền tố như [4.0]
            int bracketEnd = edition.IndexOf(']');
            if (bracketEnd >= 0 && bracketEnd < edition.Length - 1)
            {
                edition = edition.Substring(bracketEnd + 1).Trim();
            }
            
            // Lọc hậu tố như OEM:DM hoặc OEM:NONSLP
            int spaceIndex = edition.IndexOf(' ');
            if (spaceIndex > 0)
            {
                edition = edition.Substring(0, spaceIndex).Trim();
            }

            // Map qua tên hiển thị
            return edition.ToLowerInvariant() switch
            {
                "core" => "Home",
                "coresinglelanguage" => "Home Single Language",
                "corecountryspecific" => "Home China",
                "professional" => "Pro",
                "professionaleducation" => "Pro Education",
                "professionalworkstation" => "Pro for Workstations",
                "education" => "Education",
                "enterprise" => "Enterprise",
                "enterprises" => "Enterprise LTSC",
                "iotenterprise" => "IoT Enterprise",
                "iotenterprises" => "IoT Enterprise LTSC",
                _ => edition 
            };
        }

        public static string GetOemKey()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT OA3xOriginalProductKey FROM SoftwareLicensingService"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string key = obj["OA3xOriginalProductKey"]?.ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(key)) return key.Trim();
                    }
                }
            }
            catch { }
            return "";
        }
    }
}

