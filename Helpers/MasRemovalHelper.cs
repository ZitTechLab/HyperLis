using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace HyperLisUI.Helpers
{
    public static class MasRemovalHelper
    {
        private static readonly string WinDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        private static readonly string System32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        private static readonly string ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        private static readonly string ProgramFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        private static readonly string CommonProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
        private static readonly string CommonProgramFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
        private static readonly string ProgramData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        #region Public MAS Removal Methods

        /// <summary>
        /// 1. Gỡ bỏ kích hoạt số (HWID) của Windows.
        /// Dọn dẹp key, xóa cache/token ClipSVC. Nếu máy có OEM Key phần cứng trong BIOS, tự động cài lại để về bản quyền gốc.
        /// </summary>
        public static async Task<bool> RemoveWindowsHwidAsync(Action<int, int, string>? progress = null)
        {
            try
            {
                progress?.Invoke(0, 15, LocalizationService.Get("msgRemovingWinKey") ?? "Đang gỡ bỏ mã khóa sản phẩm Windows hiện tại...");
                await RunCommandAsync("cscript", $"//nologo \"{Path.Combine(System32, "slmgr.vbs")}\" /upk");
                await RunCommandAsync("cscript", $"//nologo \"{Path.Combine(System32, "slmgr.vbs")}\" /cpky");

                progress?.Invoke(0, 40, LocalizationService.Get("msgCleaningClipSvc") ?? "Đang dọn dẹp bộ nhớ đệm và chứng chỉ số ClipSVC...");
                await RunCommandAsync("net", "stop ClipSVC /y");
                await Task.Delay(500);

                await RunCommandAsync("rundll32", "clipc.dll,ClipCleanUpState");

                // Xóa registry volatile và IdentityCRL
                DeleteRegistryKeySafe(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ClipSVC\Volatile\PersistedSystemState");
                DeleteRegistryKeySafe(RegistryHive.Users, @"S-1-5-19\SOFTWARE\Microsoft\IdentityCRL");

                progress?.Invoke(0, 65, LocalizationService.Get("msgRestartingLicServices") ?? "Đang khởi động lại các dịch vụ cấp phép bản quyền...");
                await RunCommandAsync("net", "start ClipSVC");
                await RunCommandAsync("powershell", "-NoProfile -Command \"Start-Job { Restart-Service wlidsvc, LicenseManager -Force -ErrorAction SilentlyContinue } | Wait-Job -Timeout 15 | Out-Null\"");

                // Kiểm tra xem máy có OEM Key gốc đi kèm BIOS (MSDM) hay không
                progress?.Invoke(0, 85, LocalizationService.Get("msgCheckingOemKey") ?? "Đang kiểm tra khóa bản quyền OEM gốc đi kèm máy...");
                string oemKey = WindowsInfoHelper.GetOemKey();
                if (!string.IsNullOrWhiteSpace(oemKey) && oemKey.Length == 29 && oemKey != "N/A")
                {
                    progress?.Invoke(0, 95, string.Format(LocalizationService.Get("msgRestoringOemKey") ?? "Đang khôi phục bản quyền OEM gốc ({0})...", oemKey));
                    await RunCommandAsync("cscript", $"//nologo \"{Path.Combine(System32, "slmgr.vbs")}\" /ipk {oemKey}");
                    await RunCommandAsync("cscript", $"//nologo \"{Path.Combine(System32, "slmgr.vbs")}\" /ato");
                }

                progress?.Invoke(0, 100, LocalizationService.Get("msgRemoveHwidCompleted") ?? "Hoàn tất dọn dẹp bản quyền HWID!");
                await Task.Delay(500);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MasRemovalHelper] RemoveWindowsHwidAsync error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 2. Gỡ bỏ Online KMS kích hoạt Windows.
        /// Xóa Scheduled Tasks, file/thư mục KMS của MAS, registry KMS cache, và gọi slmgr /ckms.
        /// </summary>
        public static async Task<bool> RemoveWindowsKmsAsync(Action<int, int, string>? progress = null)
        {
            try
            {
                progress?.Invoke(0, 20, LocalizationService.Get("msgDeletingKmsTasks") ?? "Đang xóa các tác vụ tự động gia hạn KMS...");
                string[] tasks = { "Activation-Renewal", "Activation-Run_Once", "Online_KMS_Activation_Script-Renewal", "Online_KMS_Activation_Script-Run_Once" };
                foreach (var task in tasks)
                {
                    await RunCommandAsync("schtasks", $"/delete /tn \"{task}\" /f");
                }

                progress?.Invoke(0, 45, LocalizationService.Get("msgDeletingKmsFiles") ?? "Đang xóa các tệp và thư mục cấu hình KMS...");
                DeleteDirectorySafe(Path.Combine(ProgramFiles, "Activation-Renewal"));
                DeleteDirectorySafe(Path.Combine(ProgramData, "Activation-Renewal"));
                DeleteDirectorySafe(Path.Combine(ProgramData, "Online_KMS_Activation"));
                DeleteDirectorySafe(Path.Combine(WinDir, "Online_KMS_Activation_Script"));
                DeleteFileSafe(Path.Combine(ProgramData, "Online_KMS_Activation.cmd"));
                DeleteRegistryKeySafe(RegistryHive.ClassesRoot, @"DesktopBackground\shell\Activate Windows - Office");

                progress?.Invoke(0, 70, LocalizationService.Get("msgClearingKmsRegistry") ?? "Đang xóa cấu hình máy chủ KMS trong Registry...");
                string spp = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform";
                DeleteRegistryValueSafe(RegistryHive.LocalMachine, spp, "KeyManagementServiceName", RegistryView.Registry64);
                DeleteRegistryValueSafe(RegistryHive.LocalMachine, spp, "KeyManagementServiceName", RegistryView.Registry32);
                DeleteRegistryValueSafe(RegistryHive.LocalMachine, spp, "KeyManagementServicePort", RegistryView.Registry64);
                DeleteRegistryValueSafe(RegistryHive.LocalMachine, spp, "KeyManagementServicePort", RegistryView.Registry32);
                DeleteRegistryValueSafe(RegistryHive.LocalMachine, spp, "DisableDnsPublishing", RegistryView.Registry64);
                DeleteRegistryValueSafe(RegistryHive.LocalMachine, spp, "DisableKeyManagementServiceHostCaching", RegistryView.Registry64);

                DeleteRegistryKeySafe(RegistryHive.LocalMachine, $@"{spp}\55c92734-d682-4d71-983e-d6ec3f16059f", RegistryView.Registry64);
                DeleteRegistryKeySafe(RegistryHive.LocalMachine, $@"{spp}\55c92734-d682-4d71-983e-d6ec3f16059f", RegistryView.Registry32);
                DeleteRegistryKeySafe(RegistryHive.Users, $@"S-1-5-20\{spp}\55c92734-d682-4d71-983e-d6ec3f16059f");

                progress?.Invoke(0, 85, LocalizationService.Get("msgClearingKmsServer") ?? "Đang xóa bỏ liên kết máy chủ KMS khỏi hệ thống...");
                await RunCommandAsync("cscript", $"//nologo \"{Path.Combine(System32, "slmgr.vbs")}\" /ckms");
                await RunCommandAsync("cscript", $"//nologo \"{Path.Combine(System32, "slmgr.vbs")}\" /upk");
                await RunCommandAsync("cscript", $"//nologo \"{Path.Combine(System32, "slmgr.vbs")}\" /cpky");

                progress?.Invoke(0, 95, LocalizationService.Get("msgRestartingSppsvc") ?? "Đang khởi động lại dịch vụ bảo vệ bản quyền Software Protection...");
                await RunCommandAsync("net", "stop sppsvc /y");
                await RunCommandAsync("net", "start sppsvc");

                progress?.Invoke(0, 100, LocalizationService.Get("msgRemoveWinKmsCompleted") ?? "Hoàn tất gỡ bỏ Online KMS cho Windows!");
                await Task.Delay(500);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MasRemovalHelper] RemoveWindowsKmsAsync error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 3. Gỡ bỏ TSforce (TSforge) kích hoạt Windows.
        /// Gỡ bỏ Product key TSforce, xóa registry override / KMS38 lock và rearm hệ thống.
        /// </summary>
        public static async Task<bool> RemoveWindowsTsforgeAsync(Action<int, int, string>? progress = null)
        {
            try
            {
                progress?.Invoke(0, 25, LocalizationService.Get("msgRemovingWinKey") ?? "Đang dỡ bỏ mã khóa sản phẩm Windows TSforce...");
                await RunCommandAsync("cscript", $"//nologo \"{Path.Combine(System32, "slmgr.vbs")}\" /upk");
                await RunCommandAsync("cscript", $"//nologo \"{Path.Combine(System32, "slmgr.vbs")}\" /cpky");

                progress?.Invoke(0, 55, LocalizationService.Get("msgClearingTsforgeRegistry") ?? "Đang dọn dẹp khóa phân phối bản quyền TSforge trong Registry...");
                string spp = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform";
                DeleteRegistryKeySafe(RegistryHive.LocalMachine, $@"{spp}\55c92734-d682-4d71-983e-d6ec3f16059f", RegistryView.Registry64);
                DeleteRegistryKeySafe(RegistryHive.LocalMachine, $@"{spp}\55c92734-d682-4d71-983e-d6ec3f16059f", RegistryView.Registry32);
                DeleteRegistryKeySafe(RegistryHive.Users, $@"S-1-5-20\{spp}\55c92734-d682-4d71-983e-d6ec3f16059f");

                progress?.Invoke(0, 80, LocalizationService.Get("msgRestartingSppsvc") ?? "Đang khởi động lại dịch vụ Software Protection...");
                await RunCommandAsync("net", "stop sppsvc /y");
                await RunCommandAsync("net", "start sppsvc");

                progress?.Invoke(0, 95, LocalizationService.Get("msgRearmWindows") ?? "Đang đặt lại bộ đếm kích hoạt Windows (Rearm)...");
                await RunCommandAsync("cscript", $"//nologo \"{Path.Combine(System32, "slmgr.vbs")}\" /rearm");

                progress?.Invoke(0, 100, LocalizationService.Get("msgRemoveWinTsforceCompleted") ?? "Hoàn tất gỡ bỏ TSforce cho Windows!");
                await Task.Delay(500);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MasRemovalHelper] RemoveWindowsTsforgeAsync error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 4. Gỡ bỏ Ohook kích hoạt Office.
        /// Quét và xóa các DLL chèn hook (sppc.dll, sppc64.dll), phục hồi OSPPC.DLL gốc và xóa registry bỏ qua kiểm tra bản quyền.
        /// </summary>
        public static async Task<bool> RemoveOfficeOhookAsync(Action<int, int, string>? progress = null)
        {
            try
            {
                progress?.Invoke(0, 15, LocalizationService.Get("msgStoppingOfficeServices") ?? "Đang dừng các ứng dụng Office và dịch vụ bảo vệ bản quyền...");
                string[] officeProcs = { "winword", "excel", "powerpnt", "outlook", "msaccess", "onenote", "lync" };
                foreach (var proc in officeProcs)
                {
                    KillProcessesByNameSafe(proc);
                }
                await RunCommandAsync("net", "stop osppsvc /y");
                await RunCommandAsync("net", "stop sppsvc /y");

                progress?.Invoke(0, 45, LocalizationService.Get("msgRemovingOhookDlls") ?? "Đang quét và xóa các tệp DLL chèn Ohook (sppc.dll)...");
                
                // Các thư mục chứa sppc*.dll của Office
                var targetDirs = new List<string>
                {
                    Path.Combine(ProgramFiles, @"Microsoft Office\root\vfs\System"),
                    Path.Combine(ProgramFiles, @"Microsoft Office\root\vfs\SystemX86"),
                    Path.Combine(ProgramFilesX86, @"Microsoft Office\root\vfs\System"),
                    Path.Combine(ProgramFilesX86, @"Microsoft Office\root\vfs\SystemX86"),
                    Path.Combine(ProgramFiles, @"Microsoft Office\Office14"),
                    Path.Combine(ProgramFiles, @"Microsoft Office\Office15"),
                    Path.Combine(ProgramFiles, @"Microsoft Office\Office16"),
                    Path.Combine(ProgramFilesX86, @"Microsoft Office\Office14"),
                    Path.Combine(ProgramFilesX86, @"Microsoft Office\Office15"),
                    Path.Combine(ProgramFilesX86, @"Microsoft Office\Office16")
                };

                foreach (var dir in targetDirs)
                {
                    if (Directory.Exists(dir))
                    {
                        var dlls = Directory.GetFiles(dir, "sppc*.dll", SearchOption.TopDirectoryOnly);
                        foreach (var dll in dlls)
                        {
                            DeleteFileSafe(dll);
                        }
                    }
                }

                progress?.Invoke(0, 65, LocalizationService.Get("msgRestoringOsppcDll") ?? "Đang khôi phục tệp OSPPC.DLL gốc của Office...");
                var sharedDirs = new List<string>
                {
                    Path.Combine(CommonProgramFiles, @"Microsoft Shared\OfficeSoftwareProtectionPlatform"),
                    Path.Combine(CommonProgramFilesX86, @"Microsoft Shared\OfficeSoftwareProtectionPlatform")
                };

                foreach (var sharedDir in sharedDirs)
                {
                    if (Directory.Exists(sharedDir))
                    {
                        string osppc = Path.Combine(sharedDir, "OSPPC.DLL");
                        string sppcs = Path.Combine(sharedDir, "sppcs.dll");

                        // Nếu OSPPC.DLL nhỏ hơn 100KB -> là file shim hook -> xóa
                        if (File.Exists(osppc))
                        {
                            var fi = new FileInfo(osppc);
                            if (fi.Length < 100000)
                            {
                                DeleteFileSafe(osppc);
                            }
                        }

                        // Nếu tồn tại sppcs.dll (backup gốc >= 100KB) -> đổi tên lại thành OSPPC.DLL
                        if (File.Exists(sppcs))
                        {
                            var fiBackup = new FileInfo(sppcs);
                            if (fiBackup.Length >= 100000)
                            {
                                try
                                {
                                    if (File.Exists(osppc)) DeleteFileSafe(osppc);
                                    File.Move(sppcs, osppc);
                                }
                                catch { }
                            }
                            else
                            {
                                DeleteFileSafe(sppcs);
                            }
                        }
                    }
                }

                progress?.Invoke(0, 85, LocalizationService.Get("msgClearingOhookRegistry") ?? "Đang dọn dẹp các khóa Registry liên quan đến Ohook...");
                DeleteRegistryKeySafe(RegistryHive.CurrentUser, @"Software\Microsoft\Office\16.0\Common\Licensing\Resiliency");
                DeleteRegistryKeySafe(RegistryHive.Users, @".DEFAULT\Software\Microsoft\Office\16.0\Common\Licensing\Resiliency");

                // Xóa cho tất cả user profiles
                try
                {
                    using (var users = Registry.Users)
                    {
                        foreach (var sub in users.GetSubKeyNames())
                        {
                            if (!sub.EndsWith("_Classes", StringComparison.OrdinalIgnoreCase))
                            {
                                DeleteRegistryKeySafe(RegistryHive.Users, $@"{sub}\Software\Microsoft\Office\16.0\Common\Licensing\Resiliency");
                            }
                        }
                    }
                }
                catch { }

                // Xóa KMS banner prevention key cho Office
                string sppOfficeApp = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform\0ff1ce15-a989-479d-af46-f275c6370663";
                DeleteRegistryKeySafe(RegistryHive.LocalMachine, sppOfficeApp, RegistryView.Registry64);
                DeleteRegistryKeySafe(RegistryHive.LocalMachine, sppOfficeApp, RegistryView.Registry32);

                progress?.Invoke(0, 95, LocalizationService.Get("msgRestartingOfficeServices") ?? "Đang khởi động lại dịch vụ bảo vệ bản quyền Office...");
                await RunCommandAsync("net", "start sppsvc");
                await RunCommandAsync("net", "start osppsvc");

                progress?.Invoke(0, 100, LocalizationService.Get("msgRemoveOhookCompleted") ?? "Hoàn tất gỡ bỏ Ohook cho Office!");
                await Task.Delay(500);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MasRemovalHelper] RemoveOfficeOhookAsync error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 5. Gỡ bỏ TSforce (TSforge) kích hoạt Office.
        /// Gỡ bỏ các Product Key giả lập của Office qua ospp.vbs và xóa các registry cấu hình liên quan.
        /// </summary>
        public static async Task<bool> RemoveOfficeTsforgeAsync(Action<int, int, string>? progress = null)
        {
            try
            {
                progress?.Invoke(0, 20, LocalizationService.Get("msgScanningOfficeKeys") ?? "Đang quét danh sách mã khóa Office được cài đặt...");
                var osppPaths = FindOsppVbsPaths();

                if (osppPaths.Count > 0)
                {
                    progress?.Invoke(0, 50, LocalizationService.Get("msgUninstallingOfficeKeys") ?? "Đang gỡ bỏ các mã khóa sản phẩm Office TSforce...");
                    foreach (var ospp in osppPaths)
                    {
                        var partialKeys = await GetOfficePartialProductKeysAsync(ospp);
                        foreach (var key in partialKeys)
                        {
                            await RunCommandAsync("cscript", $"//nologo \"{ospp}\" /unpkey:{key}");
                        }
                    }
                }

                progress?.Invoke(0, 75, LocalizationService.Get("msgClearingOfficeTsforgeRegistry") ?? "Đang dọn dẹp khóa Registry Office Software Protection Platform...");
                DeleteRegistryKeySafe(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\OfficeSoftwareProtectionPlatform\0ff1ce15-a989-479d-af46-f275c6370663", RegistryView.Registry64);
                DeleteRegistryKeySafe(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\OfficeSoftwareProtectionPlatform\0ff1ce15-a989-479d-af46-f275c6370663", RegistryView.Registry32);
                DeleteRegistryKeySafe(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\OfficeSoftwareProtectionPlatform\59a52881-a989-479d-af46-f275c6370663", RegistryView.Registry64);
                DeleteRegistryKeySafe(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\OfficeSoftwareProtectionPlatform\59a52881-a989-479d-af46-f275c6370663", RegistryView.Registry32);

                string sppOfficeApp = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform\0ff1ce15-a989-479d-af46-f275c6370663";
                DeleteRegistryKeySafe(RegistryHive.LocalMachine, sppOfficeApp, RegistryView.Registry64);
                DeleteRegistryKeySafe(RegistryHive.LocalMachine, sppOfficeApp, RegistryView.Registry32);

                progress?.Invoke(0, 95, LocalizationService.Get("msgRestartingOfficeServices") ?? "Đang khởi động lại dịch vụ Office...");
                await RunCommandAsync("net", "stop osppsvc /y");
                await RunCommandAsync("net", "start osppsvc");

                progress?.Invoke(0, 100, LocalizationService.Get("msgRemoveOffTsforceCompleted") ?? "Hoàn tất gỡ bỏ TSforce cho Office!");
                await Task.Delay(500);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MasRemovalHelper] RemoveOfficeTsforgeAsync error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 6. Gỡ bỏ Online KMS kích hoạt Office.
        /// Xóa máy chủ KMS của Office qua ospp.vbs /remhst, gỡ key KMS client và dọn dẹp Registry.
        /// </summary>
        public static async Task<bool> RemoveOfficeKmsAsync(Action<int, int, string>? progress = null)
        {
            try
            {
                progress?.Invoke(0, 20, LocalizationService.Get("msgRemovingOfficeKmsHost") ?? "Đang gỡ bỏ cấu hình máy chủ KMS của Office (remhst)...");
                var osppPaths = FindOsppVbsPaths();

                if (osppPaths.Count > 0)
                {
                    foreach (var ospp in osppPaths)
                    {
                        await RunCommandAsync("cscript", $"//nologo \"{ospp}\" /remhst");

                        progress?.Invoke(0, 50, LocalizationService.Get("msgUninstallingOfficeKeys") ?? "Đang gỡ bỏ các mã khóa sản phẩm KMS của Office...");
                        var partialKeys = await GetOfficePartialProductKeysAsync(ospp);
                        foreach (var key in partialKeys)
                        {
                            await RunCommandAsync("cscript", $"//nologo \"{ospp}\" /unpkey:{key}");
                        }
                    }
                }

                progress?.Invoke(0, 75, LocalizationService.Get("msgClearingOfficeKmsRegistry") ?? "Đang dọn dẹp cấu hình KMS Registry của Office...");
                string oppk = @"SOFTWARE\Microsoft\OfficeSoftwareProtectionPlatform";
                DeleteRegistryValueSafe(RegistryHive.LocalMachine, oppk, "KeyManagementServiceName", RegistryView.Registry64);
                DeleteRegistryValueSafe(RegistryHive.LocalMachine, oppk, "KeyManagementServiceName", RegistryView.Registry32);
                DeleteRegistryValueSafe(RegistryHive.LocalMachine, oppk, "KeyManagementServicePort", RegistryView.Registry64);
                DeleteRegistryValueSafe(RegistryHive.LocalMachine, oppk, "KeyManagementServicePort", RegistryView.Registry32);
                DeleteRegistryValueSafe(RegistryHive.LocalMachine, oppk, "DisableDnsPublishing", RegistryView.Registry64);
                DeleteRegistryValueSafe(RegistryHive.LocalMachine, oppk, "DisableKeyManagementServiceHostCaching", RegistryView.Registry64);

                string sppOfficeApp = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform\0ff1ce15-a989-479d-af46-f275c6370663";
                DeleteRegistryKeySafe(RegistryHive.LocalMachine, sppOfficeApp, RegistryView.Registry64);
                DeleteRegistryKeySafe(RegistryHive.LocalMachine, sppOfficeApp, RegistryView.Registry32);
                DeleteRegistryKeySafe(RegistryHive.Users, $@"S-1-5-20\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform\0ff1ce15-a989-479d-af46-f275c6370663");

                progress?.Invoke(0, 95, LocalizationService.Get("msgRestartingOfficeServices") ?? "Đang khởi động lại dịch vụ Office...");
                await RunCommandAsync("net", "stop osppsvc /y");
                await RunCommandAsync("net", "start osppsvc");

                progress?.Invoke(0, 100, LocalizationService.Get("msgRemoveOffKmsCompleted") ?? "Hoàn tất gỡ bỏ Online KMS cho Office!");
                await Task.Delay(500);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MasRemovalHelper] RemoveOfficeKmsAsync error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Subroutines

        /// <summary>
        /// Tìm tất cả các đường dẫn tồn tại của ospp.vbs trong hệ thống.
        /// </summary>
        public static List<string> FindOsppVbsPaths()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var candidatePaths = new List<string>
            {
                Path.Combine(ProgramFiles, @"Microsoft Office\Office16\ospp.vbs"),
                Path.Combine(ProgramFilesX86, @"Microsoft Office\Office16\ospp.vbs"),
                Path.Combine(ProgramFiles, @"Microsoft Office\root\Office16\ospp.vbs"),
                Path.Combine(ProgramFilesX86, @"Microsoft Office\root\Office16\ospp.vbs"),
                Path.Combine(ProgramFiles, @"Microsoft Office\Office15\ospp.vbs"),
                Path.Combine(ProgramFilesX86, @"Microsoft Office\Office15\ospp.vbs"),
                Path.Combine(ProgramFiles, @"Microsoft Office\Office14\ospp.vbs"),
                Path.Combine(ProgramFilesX86, @"Microsoft Office\Office14\ospp.vbs"),
            };

            // Tìm qua registry ClickToRun InstallPath nếu có
            try
            {
                using (var c2rKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun"))
                {
                    string? installPath = c2rKey?.GetValue("InstallPath")?.ToString();
                    if (!string.IsNullOrEmpty(installPath))
                    {
                        candidatePaths.Add(Path.Combine(installPath, @"root\Office16\ospp.vbs"));
                    }
                }
            }
            catch { }

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    result.Add(path);
                }
            }

            return result.ToList();
        }

        /// <summary>
        /// Đọc danh sách 5 ký tự cuối (partial product keys) của các bản quyền Office cài đặt.
        /// </summary>
        private static async Task<List<string>> GetOfficePartialProductKeysAsync(string osppPath)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var result = await RunCommandAsync("cscript", $"//nologo \"{osppPath}\" /dstatus");
                if (!string.IsNullOrEmpty(result.Output))
                {
                    var matches = Regex.Matches(result.Output, @"Last 5 characters of installed product key:\s*([A-Z0-9]{5})", RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            keys.Add(match.Groups[1].Value.Trim().ToUpperInvariant());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MasRemovalHelper] GetOfficePartialProductKeysAsync error: {ex.Message}");
            }
            return keys.ToList();
        }

        public static async Task<(int ExitCode, string Output)> RunCommandAsync(string fileName, string arguments, int timeoutMs = 30000)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                if (await Task.WhenAny(process.WaitForExitAsync(), Task.Delay(timeoutMs)) == process.WaitForExitAsync())
                {
                    string output = await outputTask;
                    string error = await errorTask;
                    return (process.ExitCode, string.IsNullOrWhiteSpace(output) ? error : output);
                }
                else
                {
                    try { process.Kill(); } catch { }
                    return (-1, "Command timed out");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MasRemovalHelper] RunCommandAsync failed ({fileName} {arguments}): {ex.Message}");
                return (-1, ex.Message);
            }
        }

        private static void DeleteRegistryKeySafe(RegistryHive hive, string subKeyPath, RegistryView view = RegistryView.Default)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                baseKey.DeleteSubKeyTree(subKeyPath, false);
            }
            catch { }
        }

        private static void DeleteRegistryValueSafe(RegistryHive hive, string subKeyPath, string valueName, RegistryView view = RegistryView.Default)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var key = baseKey.OpenSubKey(subKeyPath, true);
                key?.DeleteValue(valueName, false);
            }
            catch { }
        }

        private static void DeleteDirectorySafe(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch { }
        }

        private static void DeleteFileSafe(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                }
            }
            catch { }
        }

        private static void KillProcessesByNameSafe(string procName)
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(procName))
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(2000);
                    }
                    catch { }
                }
            }
            catch { }
        }

        #endregion
    }
}
