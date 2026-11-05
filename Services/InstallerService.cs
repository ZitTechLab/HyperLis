using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using HyperLisUI.Helpers;
using HyperLisUI.Models;

namespace HyperLisUI.Services
{
    public class InstallerService
    {
        private WinPEService _winpeService = new WinPEService();
        private PartitionManager _partitionManager = new PartitionManager();
        private ConfigService _configService = new ConfigService();

        public Task StartInstallationAsync(InstallationOptions options, string isoPath, IProgress<(int Step, int Percentage, string Message)> legacyProgress)
        {
            var bridge = new Progress<InstallProgressReport>(r =>
            {
                legacyProgress?.Report((r.Step, r.OverallPercentage, r.Message));
            });
            return StartInstallationAsync(options, isoPath, bridge, CancellationToken.None);
        }

        public async Task StartInstallationAsync(
            InstallationOptions options, 
            string isoPath, 
            IProgress<InstallProgressReport> progressMonitor,
            CancellationToken cancellationToken = default)
        {
            try
            {
                progressMonitor?.Report(new InstallProgressReport { Step = 0, OverallPercentage = 5, Message = "Validating input..." });
                // Validate
                if (!File.Exists(isoPath)) throw new FileNotFoundException("Windows ISO file not found.");
                
                // 1. Run PowerShell Script to Prepare WinPE Environment
                progressMonitor?.Report(new InstallProgressReport { Step = 0, OverallPercentage = 15, Message = "Running WinPE preparation script..." });
                
                string currDir = AppDomain.CurrentDomain.BaseDirectory;
                // Look for script in Config folder relative to app (deployed/built structure)
                string scriptPath = Path.Combine(currDir, "Config", "ApplyWinPEtoHDD.ps1");
                
                // Fallback searching if running from VS/Debug and file not copied yet or looking for source
                if (!File.Exists(scriptPath))
                {
                     // Try up a few levels (Debug/net9.0/...) looking for source in Warehouse
                     string tryPath = Path.GetFullPath(Path.Combine(currDir, @"..\..\..\..\Warehouse\ApplyWinPEtoHDD.ps1"));
                     if (File.Exists(tryPath)) scriptPath = tryPath;
                }

                if (!File.Exists(scriptPath)) throw new FileNotFoundException($"Script not found at: {scriptPath}");

                // Run visible PowerShell
                var psi = new ProcessStartInfo("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"")
                {
                    UseShellExecute = true, // Show window
                    Verb = "runas" // Admin
                };
                
                using (var p = Process.Start(psi))
                {
                    if (p != null) await p.WaitForExitAsync(cancellationToken);
                }

                // 2. Extract Windows ISO Content to X:\WSAP
                string targetDrive = "X";
                string targetRoot = $"{targetDrive}:\\";
                string installSourcePath = Path.Combine(targetRoot, "WSAP");
                
                // Verify X: exists now
                if (!Directory.Exists(targetRoot))
                {
                    string logPath = Path.Combine(Path.GetDirectoryName(scriptPath) ?? currDir, "winpe_install.log");
                    string errorDetail = "";
                    if (File.Exists(logPath))
                    {
                        try
                        {
                            var lines = File.ReadAllLines(logPath);
                            var lastLines = lines.Length > 6 ? lines[^6..] : lines;
                            errorDetail = "\n\nChi tiết log:\n" + string.Join("\n", lastLines);
                        }
                        catch { }
                    }
                    throw new Exception($"Installation Failed: Drive X: was not created or detected.{errorDetail}\nInstallation aborted.");
                }

                // Ensure WSAP folder exists
                if (!Directory.Exists(installSourcePath)) Directory.CreateDirectory(installSourcePath);

                progressMonitor?.Report(new InstallProgressReport { Step = 0, OverallPercentage = 25, Message = "Mounting Windows ISO for extraction..." });
                string winIsoMountLetter = await MountIsoAsync(isoPath);
                if (string.IsNullOrEmpty(winIsoMountLetter)) throw new Exception("Failed to mount Windows ISO.");

                try
                {
                    // Copy all files from mounted ISO to WSAP folder with real-time file & % tracking (excluding boot.wim)
                    await CopyDirectoryWithProgressAsync(
                        $"{winIsoMountLetter}:\\",
                        installSourcePath,
                        step: 0,
                        taskName: "Copying Windows installation files to X:\\WSAP...",
                        progress: progressMonitor,
                        excludeFilter: f => Path.GetFileName(f).Equals("boot.wim", StringComparison.OrdinalIgnoreCase),
                        cancellationToken: cancellationToken
                    );
                }
                finally
                {
                    await DismountIsoAsync(isoPath);
                }

                // 3c. Dam bao startnet.cmd tuy bien duoc cap nhat vao X:\Windows\System32\startnet.cmd
                string targetStartnet = Path.Combine(targetRoot, "Windows", "System32", "startnet.cmd");
                string sourceStartnet = Path.Combine(currDir, "Config", "startnet.cmd");
                if (!File.Exists(sourceStartnet))
                {
                    string tryStartnet = Path.GetFullPath(Path.Combine(currDir, @"..\..\..\..\Warehouse\startnet.cmd"));
                    if (File.Exists(tryStartnet)) sourceStartnet = tryStartnet;
                }
                if (File.Exists(sourceStartnet))
                {
                    File.Copy(sourceStartnet, targetStartnet, true);
                }

                // 4. Generate and Save Config
                progressMonitor?.Report(new InstallProgressReport { Step = 0, OverallPercentage = 92, Message = "Generating configuration..." });
                
                // Helpers to get Disk and Partition Index for X:
                int xDiskIndex = -1;
                int xPartIndex = -1;

                // Simple WMI or DiskHelper call to find X:'s details
                // Ideally we should add a helper. For now, we might try to infer or re-scan.
                // Re-using DiskHelper from UI logic might be hard if not static.
                // Let's implement a quick WMI lookup here or assume User selected Disk is the one.
                // Actually the script uses "C:" -> Shrink -> Create X:.
                // So X: is likely on the SAME disk as C: or whatever IsSystem.
                // We'll trust the logic or try to find it.
                
                var xPart = await DiskHelper.GetPartitionInfoByDriveLetterAsync(targetDrive);
                if (xPart != null)
                {
                    xDiskIndex = xPart.DiskIndex;
                    xPartIndex = xPart.PartitionIndex;
                }
                else
                {
                     // Fallback: Use RecommendedDisk if we can't find X (weird)
                     xDiskIndex = options.RecommendedDisk;
                     // But we don't know partition. 
                     // IMPORTANT: ConfigService needs accurate info.
                     throw new Exception("Could not determine Disk/Partition info for drive X:");
                }

                // Detect install.wim, install.esd, or install.swm
                string fullWimPath = Path.Combine(installSourcePath, "sources", "install.wim");
                string fullEsdPath = Path.Combine(installSourcePath, "sources", "install.esd");
                string fullSwmPath = Path.Combine(installSourcePath, "sources", "install.swm");
                
                string finalSourcePath = "\\WSAP\\sources\\install.wim"; // Default
                string finalSourceType = "WIM"; // Default

                if (File.Exists(fullWimPath))
                {
                    finalSourcePath = "\\WSAP\\sources\\install.wim";
                    finalSourceType = "WIM";
                }
                else if (File.Exists(fullEsdPath))
                {
                    finalSourcePath = "\\WSAP\\sources\\install.esd";
                    finalSourceType = "ESD";
                }
                else if (File.Exists(fullSwmPath))
                {
                    finalSourcePath = "\\WSAP\\sources\\install.swm";
                    finalSourceType = "SWM";
                }

                // Update Options
                options.WinSource = finalSourceType; 
                options.WinSourceDisk = xDiskIndex;
                options.WinSourcePartition = xPartIndex;
                options.WinSourcePathNoDrive = finalSourcePath; 

                // Copy Unattend File if provided
                if (!string.IsNullOrEmpty(options.SourceUnattendPath) && File.Exists(options.SourceUnattendPath))
                {
                    progressMonitor?.Report(new InstallProgressReport { Step = 0, OverallPercentage = 96, Message = "Copying unattended file..." });
                    string fileName = Path.GetFileName(options.SourceUnattendPath);
                    string destPath = Path.Combine(targetRoot, fileName);
                    File.Copy(options.SourceUnattendPath, destPath, true);
                    
                    options.UnattendedFileDisk = xDiskIndex;
                    options.UnattendedFilePartition = xPartIndex;
                    options.UnattendedFilePathNoDrive = "\\" + fileName;
                }
                
                string configContent = _configService.GenerateConfig(options);
                // Keep config at Root for easy find
                string configPath = Path.Combine(targetRoot, "HyperLisWindowsConfig.txt");
                await File.WriteAllTextAsync(configPath, configContent, cancellationToken);
                
                progressMonitor?.Report(new InstallProgressReport 
                { 
                    Step = 0, 
                    OverallPercentage = 100, 
                    FilePercentage = 100,
                    Message = $"Setup Complete! File type: {finalSourceType}. Please restart to boot into WinPE." 
                });
            }
            catch (Exception ex)
            {
                 progressMonitor?.Report(new InstallProgressReport { Step = 0, OverallPercentage = 100, Message = $"Error: {ex.Message}" });
                 throw;
            }
        }

        private async Task CopyDirectoryWithProgressAsync(
            string sourceDir, 
            string destDir, 
            int step, 
            string taskName, 
            IProgress<InstallProgressReport> progress, 
            Func<string, bool> excludeFilter,
            CancellationToken cancellationToken)
        {
            var dirInfo = new DirectoryInfo(sourceDir);
            if (!dirInfo.Exists) throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

            // 1. Enumerate all files to copy
            var allFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories)
                                  .Where(f => excludeFilter == null || !excludeFilter(f.FullName))
                                  .ToList();

            long totalBytes = allFiles.Sum(f => f.Length);
            long totalCopiedBytes = 0;

            var stopwatch = Stopwatch.StartNew();
            long lastReportedTime = 0;
            long lastReportedBytes = 0;
            double currentSpeed = 0;

            // Buffer: 4MB for high performance
            byte[] buffer = new byte[4 * 1024 * 1024];

            foreach (var file in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string relativePath = Path.GetRelativePath(sourceDir, file.FullName);
                string destFilePath = Path.Combine(destDir, relativePath);
                string destFileDir = Path.GetDirectoryName(destFilePath)!;
                if (!Directory.Exists(destFileDir))
                {
                    Directory.CreateDirectory(destFileDir);
                }

                long fileLength = file.Length;

                // For small files (< 1MB), use File.Copy directly
                if (fileLength < 1024 * 1024)
                {
                    File.Copy(file.FullName, destFilePath, true);
                    totalCopiedBytes += fileLength;

                    int overallPct = totalBytes > 0 ? 25 + (int)((totalCopiedBytes * 60) / totalBytes) : 85;
                    
                    long elapsed = stopwatch.ElapsedMilliseconds;
                    if (elapsed - lastReportedTime >= 100)
                    {
                        long bytesDiff = totalCopiedBytes - lastReportedBytes;
                        long timeDiff = elapsed - lastReportedTime;
                        if (timeDiff > 0)
                        {
                            currentSpeed = (bytesDiff * 1000.0) / timeDiff;
                        }
                        lastReportedTime = elapsed;
                        lastReportedBytes = totalCopiedBytes;

                        progress?.Report(new InstallProgressReport
                        {
                            Step = step,
                            OverallPercentage = overallPct,
                            FilePercentage = 100,
                            Message = taskName,
                            CurrentFileName = relativePath,
                            TransferredSize = $"{FormatBytes(totalCopiedBytes)} / {FormatBytes(totalBytes)}",
                            Speed = $"{FormatBytes((long)currentSpeed)}/s",
                            IsDetailed = true
                        });
                    }
                    continue;
                }

                // For larger files (e.g. install.wim ~4GB - 6GB)
                using (var sourceStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, buffer.Length, FileOptions.SequentialScan))
                using (var destStream = new FileStream(destFilePath, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, FileOptions.SequentialScan))
                {
                    long fileCopied = 0;
                    int bytesRead;

                    while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await destStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        fileCopied += bytesRead;
                        totalCopiedBytes += bytesRead;

                        long elapsed = stopwatch.ElapsedMilliseconds;
                        if (elapsed - lastReportedTime >= 100 || fileCopied == fileLength)
                        {
                            long bytesDiff = totalCopiedBytes - lastReportedBytes;
                            long timeDiff = elapsed - lastReportedTime;
                            if (timeDiff > 0)
                            {
                                currentSpeed = (bytesDiff * 1000.0) / timeDiff;
                            }
                            lastReportedTime = elapsed;
                            lastReportedBytes = totalCopiedBytes;

                            int overallPct = totalBytes > 0 ? 25 + (int)((totalCopiedBytes * 60) / totalBytes) : 85;
                            int filePct = fileLength > 0 ? (int)((fileCopied * 100) / fileLength) : 100;

                            progress?.Report(new InstallProgressReport
                            {
                                Step = step,
                                OverallPercentage = overallPct,
                                FilePercentage = filePct,
                                Message = taskName,
                                CurrentFileName = relativePath,
                                TransferredSize = $"{FormatBytes(totalCopiedBytes)} / {FormatBytes(totalBytes)}",
                                Speed = $"{FormatBytes((long)currentSpeed)}/s",
                                IsDetailed = true
                            });
                        }
                    }
                    await destStream.FlushAsync(cancellationToken);
                }
            }

            // Final report for copy completion (85% overall)
            progress?.Report(new InstallProgressReport
            {
                Step = step,
                OverallPercentage = 85,
                FilePercentage = 100,
                Message = taskName,
                CurrentFileName = "Completed",
                TransferredSize = $"{FormatBytes(totalBytes)} / {FormatBytes(totalBytes)}",
                Speed = "0 MB/s",
                IsDetailed = true
            });
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)Math.Max(0, bytes);
            while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:0.0} {suffixes[counter]}";
        }

        private async Task<string> MountIsoAsync(string isoPath)
        {
            // Use PowerShell Mount-DiskImage
            string cmd = $"Mount-DiskImage -ImagePath \"{isoPath}\" -PassThru | Get-Volume | Select-Object -ExpandProperty DriveLetter";
            var res = await Utils.RunPowershellAsync(cmd); // Need helper
            if (res.Success && !string.IsNullOrWhiteSpace(res.Output))
            {
                return res.Output.Trim(); // Returns letter "E"
            }
            return null;
        }

        private async Task DismountIsoAsync(string isoPath)
        {
            string cmd = $"Dismount-DiskImage -ImagePath \"{isoPath}\"";
            await Utils.RunPowershellAsync(cmd);
        }

        // Helper wrapper for Utilities and Process
        private static class Utils
        {
            public class CmdResult { public int ExitCode; public string Output; public bool Success => ExitCode == 0; }
            
             public static async Task<CmdResult> RunCommandAsync(string exe, string args, bool admin)
            {
                // Reusing Utilities logic or Process.Start
                return await Task.Run(() => {
                    var psi = new ProcessStartInfo(exe, args)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        Verb = admin ? "runas" : ""
                    };
                    using (var p = Process.Start(psi))
                    {
                        if (p==null) return new CmdResult { ExitCode=-1, Output="Failed start" };
                        string outT = p.StandardOutput.ReadToEnd();
                        string err = p.StandardError.ReadToEnd();
                        p.WaitForExit();
                        return new CmdResult { ExitCode = p.ExitCode, Output = outT + "\n" + err };
                    }
                });
            }

            public static async Task<CmdResult> RunPowershellAsync(string cmd)
            {
                 return await RunCommandAsync("powershell", $"-NoProfile -ExecutionPolicy Bypass -Command \"{cmd}\"", true);
            }
        }
    }
}
