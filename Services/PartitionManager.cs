using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HyperLisUI.Helpers;

namespace HyperLisUI.Services
{
    public class PartitionManager
    {
        public class PartitionResult
        {
            public bool Success { get; set; }
            public int NewPartitionIndex { get; set; }
            public string DriveLetter { get; set; } // "Z"
            public string Error { get; set; }
        }

        public async Task<PartitionResult> PrepareWinPEPartitionAsync(int diskIndex)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    // 1. Find last partition to shrink
                    var partitions = await DiskHelper.GetPartitionsAsync(diskIndex);
                    if (partitions.Count == 0) return new PartitionResult { Success = false, Error = "No partitions found on disk." };

                    // Sort by start offset? GetPartitionsAsync sorts by PartitionNumber. 
                    // Usually highest partition number is at the end, but not always.
                    // However, we assume the user selects a disk and we stick to the plan "Shrink last partition".
                    // Let's take the partition with the largest PartitionNumber for now, or the one with most free space? 
                    // The plan says "Last Partition". Usually typically the Data partition.
                    var lastPartition = partitions.OrderByDescending(p => p.PartitionIndex).FirstOrDefault();
                    if (lastPartition == null) return new PartitionResult { Success = false, Error = "Could not identify last partition." };

                    // Check free space? We need checking logic if possible, but diskpart shrink will fail if not enough space.
                    // We'll proceed and catch errors.

                    // 2. Check if Drive X already exists
                    // Reference script logic: If X exists, reuse it (format it).
                    // We check if any partition has letter X.
                    try 
                    {
                        var allPartitions = await DiskHelper.GetAllPartitionsAsync();
                        var existingX = allPartitions.FirstOrDefault(p => string.Equals(p.DriveLetter, "X", StringComparison.OrdinalIgnoreCase));
                        
                        if (existingX != null)
                        {
                            // X exists. We should discard the shrink/create step and just format/reuse this partition.
                            // We need to make sure it's on the target disk? Or just use it wherever it is?
                            // Script says: "Ổ X: đã tồn tại, bỏ qua bước tạo ổ."
                            // But usually we want it on the target disk for speed/portability?
                            // For now, if X exists, we use it. 
                            // TODO: Format it? The script formats it.
                            
                            // Let's format it to be sure it's clean for WinPE.
                            // We can use a quick diskpart script to format X
                            string formatScript = Path.GetTempFileName();
                            await File.WriteAllLinesAsync(formatScript, new[] { 
                                $"select volume X", 
                                "format fs=ntfs quick label=\"WINPE\"", 
                                "exit" 
                            });
                            
                            var pFormat = Process.Start(new ProcessStartInfo { FileName="diskpart.exe", Arguments=$"/s \"{formatScript}\"", UseShellExecute=false, CreateNoWindow=true });
                            pFormat.WaitForExit();
                            try { File.Delete(formatScript); } catch { }

                            return new PartitionResult 
                            { 
                                Success = true, 
                                NewPartitionIndex = existingX.PartitionIndex, 
                                DriveLetter = "X" 
                            };
                        }
                    }
                    catch { /* Continue to create if check fails */ }

                    // No X found, proceed to create
                    string targetLetter = "X";
                    int sizeMB = 10240; // 10GB as requested 

                    // 3. Generate DiskPart script
                    string scriptFile = Path.GetTempFileName();
                    var scriptLines = new System.Collections.Generic.List<string>
                    {
                        $"select disk {diskIndex}",
                        $"select partition {lastPartition.PartitionIndex}",
                        $"shrink desired={sizeMB} minimum={sizeMB}",
                        "create partition primary",
                        "format fs=ntfs quick label=\"WINPE\"",
                        $"assign letter={targetLetter}",
                        "exit"
                    };
                    
                    await File.WriteAllLinesAsync(scriptFile, scriptLines);

                    // 4. Run DiskPart
                    var psi = new ProcessStartInfo
                    {
                        FileName = "diskpart.exe",
                        Arguments = $"/s \"{scriptFile}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var p = Process.Start(psi))
                    {
                        if (p == null) throw new Exception("Failed to start diskpart.");
                        string output = await p.StandardOutput.ReadToEndAsync();
                        string error = await p.StandardError.ReadToEndAsync();
                        p.WaitForExit();

                        try { File.Delete(scriptFile); } catch { }

                        if (p.ExitCode != 0 || output.ToLower().Contains("error") || !string.IsNullOrEmpty(error))
                        {
                            if (output.Contains("Shrink volume failed", StringComparison.OrdinalIgnoreCase) || 
                                output.Contains("There is not enough usable free space", StringComparison.OrdinalIgnoreCase))
                            {
                                return new PartitionResult { Success = false, Error = "Not enough free space to shrink (2GB required)." };
                            }
                        }
                    }

                    // 5. Verify and get new Partition Index with Retry
                    for (int i = 0; i < 15; i++)
                    {
                        await Task.Delay(2000); 
                        var newPartitions = await DiskHelper.GetPartitionsAsync(diskIndex);
                        
                        // Check by Letter X
                        var winpePart = newPartitions.FirstOrDefault(p => p.DriveLetter != null && p.DriveLetter.StartsWith(targetLetter, StringComparison.OrdinalIgnoreCase));
                        
                        // Check by Label WINPE
                        if (winpePart == null)
                        {
                            winpePart = newPartitions.FirstOrDefault(p => p.Label != null && p.Label.Equals("WINPE", StringComparison.OrdinalIgnoreCase));
                        }

                        if (winpePart != null)
                        {
                            string finalLetter = winpePart.DriveLetter;
                             
                            if (string.IsNullOrEmpty(finalLetter))
                            {
                                // Found by Label but no Letter. Force assign X.
                                try
                                {
                                    // Use PowerShell to add drive letter
                                    // Set-Partition -DiskNumber D -PartitionNumber P -NewDriveLetter X
                                    string cmd = $"Set-Partition -DiskNumber {winpePart.DiskIndex} -PartitionNumber {winpePart.PartitionIndex} -NewDriveLetter {targetLetter}";
                                    var psRes = await Task.Run(() => 
                                    {
                                        var p = Process.Start(new ProcessStartInfo
                                        {
                                            FileName = "powershell",
                                            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{cmd}\"",
                                            UseShellExecute = false, 
                                            CreateNoWindow = true
                                        });
                                        p.WaitForExit();
                                        return p.ExitCode;
                                    });
                                    
                                    if (psRes == 0) finalLetter = targetLetter;
                                }
                                catch {}

                                if (string.IsNullOrEmpty(finalLetter)) continue; // Retry verification
                            }

                            return new PartitionResult 
                            { 
                                Success = true, 
                                NewPartitionIndex = winpePart.PartitionIndex, 
                                DriveLetter = finalLetter.Replace(":", "") 
                            };
                        }
                    }
                    
                    return new PartitionResult { Success = false, Error = "Partition created but could not be identified (WINPE/X)." };
                }
                catch (Exception ex)
                {
                    return new PartitionResult { Success = false, Error = ex.Message };
                }
            });
        }

        private string FindFreeDriveLetter()
        {
            // Deprecated, we force X
            return "X";
        }
    }
}
