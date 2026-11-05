using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperLisUI.Helpers
{
    public class UWPAppInfo
    {
        public string Name { get; set; } = string.Empty;
        public string PackageFullName { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string InstallLocation { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string LogoPath { get; set; } = string.Empty;
    }

    public static class UWPHelper
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _logoCache = new(StringComparer.OrdinalIgnoreCase);

        public static string ResolveLogo(string installLocation)
        {
            if (string.IsNullOrWhiteSpace(installLocation) || !Directory.Exists(installLocation))
                return string.Empty;

            if (_logoCache.TryGetValue(installLocation, out var cached))
                return cached;

            string foundLogo = ResolveLogoCore(installLocation);
            _logoCache[installLocation] = foundLogo;
            return foundLogo;
        }

        private static string ResolveLogoCore(string installLocation)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(installLocation) || !Directory.Exists(installLocation))
                    return string.Empty;

                // Bỏ qua các package hệ thống không có giao diện thực (tránh asset wireframe 'X' như CBS)
                if (installLocation.IndexOf("Windows.Client.CBS", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    installLocation.IndexOf("ShellExperienceHost", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return string.Empty;
                }

                var assetsDir = Path.Combine(installLocation, "Assets");
                if (Directory.Exists(assetsDir))
                {
                    var priorityPrefixes = new[]
                    {
                        "Square44x44Logo",
                        "AppList",
                        "SmallTile",
                        "MedTile",
                        "Square150x150Logo",
                        "Logo",
                        "StoreLogo"
                    };

                    var imageFiles = Directory.EnumerateFiles(assetsDir, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(f => {
                            var ext = Path.GetExtension(f);
                            if (!ext.Equals(".png", StringComparison.OrdinalIgnoreCase) &&
                                !ext.Equals(".ico", StringComparison.OrdinalIgnoreCase) &&
                                !ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
                                return false;

                            var fileName = Path.GetFileNameWithoutExtension(f);
                            if (fileName.Contains("altform-unplated", StringComparison.OrdinalIgnoreCase) ||
                                fileName.Contains("contrast-white", StringComparison.OrdinalIgnoreCase) ||
                                fileName.Contains("contrast-black", StringComparison.OrdinalIgnoreCase))
                                return false;

                            return true;
                        })
                        .ToList();

                    if (imageFiles.Count > 0)
                    {
                        foreach (var prefix in priorityPrefixes)
                        {
                            var match = imageFiles.FirstOrDefault(f => {
                                var fileName = Path.GetFileNameWithoutExtension(f);
                                return fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                                       fileName.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0;
                            });

                            if (match != null)
                                return match;
                        }

                        var anyPng = imageFiles.FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
                        if (anyPng != null)
                            return anyPng;
                    }
                }
            }
            catch { }

            return string.Empty;
        }

        public static async Task<List<UWPAppInfo>> GetInstalledAppsAsync()
        {
            // Ensure necessary services are running (optimization might have stopped them)
            // AppXSvc, StateRepository, and LicenseManager/ClipSVC are required for Get-AppxPackage to work properly.
            try
            {
                await Task.Run(() => {
                    var services = new[] { "AppXSvc", "StateRepository", "Appinfo", "AppReadiness", "ClipSVC", "LicenseManager" };
                    foreach (var svc in services)
                    {
                        // Ensure service is not disabled, otherwise StartService fails
                        Utilities.RunCommand($"sc config {svc} start= demand");
                        Utilities.StartService(svc);
                    }
                });
            }
            catch { }

            var apps = new List<UWPAppInfo>();
            // Script to get apps. 
            // We select Name, PackageFullName, Publisher, InstallLocation. 
            // We perform minimal processing in PS to output clear text or CSV.
            // Using CSV format for easy parsing.
            string script = "Get-AppxPackage | Select-Object Name, PackageFullName, Publisher, InstallLocation | ConvertTo-Csv -NoTypeInformation";

            string output = await RunPowerShellAsync(script);

            // Parse CSV output
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 1) // Header + Data
            {
                // Skip header
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var line = lines[i];
                        // CSV parsing (handling quotes)
                        var parts = ParseCsvLine(line);
                        if (parts.Length >= 4)
                        {
                            var app = new UWPAppInfo
                            {
                                Name = parts[0],
                                PackageFullName = parts[1],
                                Publisher = parts[2],
                                InstallLocation = parts[3]
                            };

                            // Calculate size and resolve logo if location exists
                            if (!string.IsNullOrEmpty(app.InstallLocation) && Directory.Exists(app.InstallLocation))
                            {
                                app.Size = FormatSize(GetDirectorySize(app.InstallLocation));
                                app.LogoPath = ResolveLogo(app.InstallLocation);
                            }
                            else
                            {
                                app.Size = "Unknown";
                            }
                            
                            // Cleanup Publisher (usually CN=Microsoft Corporation, ...)
                            if (app.Publisher.Contains("CN="))
                            {
                                var cn = app.Publisher.Split(',').FirstOrDefault(s => s.Trim().StartsWith("CN="));
                                if (cn != null)
                                {
                                    app.Publisher = cn.Replace("CN=", "").Trim();
                                }
                            }

                            apps.Add(app);
                        }
                    }
                    catch { }
                }
            }

            return apps.OrderBy(a => a.Name).ToList();
        }

        public static async Task UninstallAppAsync(string packageFullName)
        {
            string script = $"Remove-AppxPackage -Package \"{packageFullName}\"";
            await RunPowerShellAsync(script);
        }

        public static async Task RestoreAllAppsAsync()
        {
             string script = "Get-AppxPackage -AllUsers| Foreach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\"}";
             await RunPowerShellAsync(script);
        }

        private static async Task<string> RunPowerShellAsync(string script)
        {
             return await Task.Run(() =>
             {
                 var process = new Process
                 {
                     StartInfo = new ProcessStartInfo
                     {
                         FileName = "powershell.exe",
                         Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                         RedirectStandardOutput = true,
                         RedirectStandardError = true,
                         UseShellExecute = false,
                         CreateNoWindow = true,
                         StandardOutputEncoding = Encoding.UTF8
                     }
                 };

                 try
                 {
                     process.Start();
                     string output = process.StandardOutput.ReadToEnd();
                     string error = process.StandardError.ReadToEnd();
                     process.WaitForExit();
                     
                     if (!string.IsNullOrEmpty(error) && string.IsNullOrEmpty(output))
                     {
                         Debug.WriteLine("PowerShell Error: " + error);
                         return string.Empty; 
                     }
                     return output;
                 }
                 catch
                 {
                    return string.Empty;
                 }
             });
        }

        private static string[] ParseCsvLine(string line)
        {
            // Simple split by comma, ignoring quotes
            // NOTE: A proper CSV parser is better, but PS ConvertTo-Csv usually quotes everything.
            // We'll strip the wrapping quotes and split by ","
            
            // "Name","PackageFullName","Publisher","InstallLocation"
            // We can split by "\",\""
            
            if (line.StartsWith("\"") && line.EndsWith("\""))
            {
                line = line.Substring(1, line.Length - 2);
            }
            
            return line.Split(new[] { "\",\"" }, StringSplitOptions.None);
        }

        private static long GetDirectorySize(string path)
        {
            try
            {
                var dir = new DirectoryInfo(path);
                return dir.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
            }
            catch
            {
                return 0;
            }
        }

        private static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.00} {sizes[order]}";
        }
    }
}
