using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HyperLisUI.Helpers
{
    public static class IsoHelper
    {
        public static async Task<List<string>> GetWindowsEditionsAsync(string isoPath)
        {
            return await Task.Run(() =>
            {
                var editions = new List<string>();
                string driveLetter = null;

                try
                {
                    // 1. Mount ISO
                    driveLetter = MountIso(isoPath);
                    if (string.IsNullOrEmpty(driveLetter)) return editions;

                    // 2. Find install.wim or install.esd
                    string sourcesPath = Path.Combine(driveLetter + ":\\", "sources");
                    string wimPath = Path.Combine(sourcesPath, "install.wim");
                    if (!File.Exists(wimPath))
                    {
                        wimPath = Path.Combine(sourcesPath, "install.esd");
                        if (!File.Exists(wimPath)) return editions; 
                    }

                    // 3. Get Editions using DISM via PowerShell (more reliable parsing)
                    // We use PowerShell's Get-WindowsImage because parsing raw dism output is messy, 
                    // but calling dism directly is often faster/less overhead if we just regex it. 
                    // Let's use basic DISM command first as it is standard.
                    
                    // Command: dism /Get-WimInfo /WimFile:"..."
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = $"/Get-WimInfo /WimFile:\"{wimPath}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        Verb = "runas"
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            // Parse Output
                            // Name : Windows 10 Pro
                            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                if (line.TrimStart().StartsWith("Name :"))
                                {
                                    string name = line.Substring(line.IndexOf(':') + 1).Trim();
                                    editions.Add(name);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting editions: {ex.Message}");
                }
                finally
                {
                    // 4. Dismount ISO
                    if (!string.IsNullOrEmpty(driveLetter))
                    {
                        DismountIso(isoPath);
                    }
                }

                return editions;
            });
        }

        private static string MountIso(string isoPath)
        {
            try
            {
                // Use PowerShell to mount and extract drive letter
                // Get-Volume | Get-Partition | Get-DiskImage doesn't work directly easiest is Mount-DiskImage -PassThru
                string command = $"powershell -Command \"Mount-DiskImage -ImagePath '{isoPath}' -StorageType ISO -PassThru | Get-Volume | Select-Object -ExpandProperty DriveLetter\"";
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    
                    string letter = output.Trim();
                    if (!string.IsNullOrEmpty(letter) && letter.Length == 1)
                    {
                        return letter;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error mounting ISO: {ex.Message}");
            }
            return null;
        }

        private static void DismountIso(string isoPath)
        {
            try
            {
                string command = $"powershell -Command \"Dismount-DiskImage -ImagePath '{isoPath}'\"";
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(startInfo)?.WaitForExit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error dismounting ISO: {ex.Message}");
            }
        }
    }
}
