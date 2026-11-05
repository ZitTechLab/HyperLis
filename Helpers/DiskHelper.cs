using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HyperLisUI.Models;

namespace HyperLisUI.Helpers
{
    public static class DiskHelper
    {
        public static async Task<List<DiskInfo>> GetPhysicalDisksAsync()
        {
            return await Task.Run(() =>
            {
                var disks = new List<DiskInfo>();
                try
                {
                    // Use simple Get-PhysicalDisk command like USB Boot tab for maximum compatibility
                    // Format: DeviceID, FriendlyName, Model, MediaType, Size
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-PhysicalDisk | Sort-Object DeviceID | Select-Object DeviceID, FriendlyName, Model, MediaType, Size | ConvertTo-Csv -NoTypeInformation\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(psi))
                    {
                        if (process != null)
                        {
                            var output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();

                            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                            {
                                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                // Skip header
                                for (int i = 1; i < lines.Length; i++)
                                {
                                    var parts = ParseCsvLine(lines[i]);
                                    // Expected: DeviceID, FriendlyName, Model, MediaType, Size
                                    if (parts.Length >= 5)
                                    {
                                        if (int.TryParse(parts[0], out int number) && long.TryParse(parts[4], out long size))
                                        {
                                            string name = Sanitize(parts[1]);
                                            string model = Sanitize(parts[2]);
                                            if (string.IsNullOrEmpty(name)) name = model;
                                            if (string.IsNullOrEmpty(name)) name = "Unknown Disk";

                                            disks.Add(new DiskInfo
                                            {
                                                Index = number,
                                                Model = name,
                                                SizeBytes = size,
                                                Style = "Unknown" // Get-PhysicalDisk doesn't provide Style, will be fetched later if needed
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting disks: {ex.Message}");
                }
                return disks;
            });
        }

        private static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            // Remove control characters and quotes
            string cleaned = Regex.Replace(input, @"[\p{C}]", "");
            return cleaned.Trim().Trim('"').Trim();
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string current = "";
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            result.Add(current);
            // Sanitize each part during parse or after
            for(int i=0; i<result.Count; i++) result[i] = Sanitize(result[i]);
            return result.ToArray();
        }

        public static async Task<Dictionary<int, string>> GetAllDiskStylesAsync()
        {
            return await Task.Run(() =>
            {
                var styles = new Dictionary<int, string>();
                try
                {
                    string command = "powershell -NoProfile -ExecutionPolicy Bypass -Command \"Get-Disk -ErrorAction SilentlyContinue | Select-Object Number, PartitionStyle | ConvertTo-Csv -NoTypeInformation\"";
                    var result = Utilities.RunCommandWithOutput(command);

                    if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
                    {
                        var lines = result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 1; i < lines.Length; i++)
                        {
                            var parts = ParseCsvLine(lines[i]);
                            if (parts.Length >= 2 && int.TryParse(parts[0], out int number))
                            {
                                styles[number] = parts[1];
                            }
                        }
                    }
                }
                catch { }
                return styles;
            });
        }

        public static async Task<List<PartitionInfo>> GetAllPartitionsAsync()
        {
            return await Task.Run(() =>
            {
                var partitions = new List<PartitionInfo>();
                try
                {
                    // Fetch ALL partitions at once
                    string command = "powershell -NoProfile -ExecutionPolicy Bypass -Command \"Get-Partition -ErrorAction SilentlyContinue | ForEach-Object { $vol = $_ | Get-Volume -ErrorAction SilentlyContinue; $_ | Select-Object DiskNumber, PartitionNumber, Size, Type, DriveLetter, @{Name='FileSystem';Expression={$vol.FileSystem}}, @{Name='Label';Expression={$vol.FileSystemLabel}} } | Sort-Object DiskNumber, PartitionNumber | ConvertTo-Csv -NoTypeInformation\"";
                    
                    var result = Utilities.RunCommandWithOutput(command);

                    if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
                    {
                        var lines = result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 1; i < lines.Length; i++)
                        {
                            var parts = ParseCsvLine(lines[i]);
                            // Expected: DiskNumber, PartitionNumber, Size, Type, DriveLetter, FileSystem
                            if (parts.Length >= 6) 
                            {
                                if (int.TryParse(parts[0], out int diskNum) && int.TryParse(parts[1], out int partNum))
                                {
                                    long.TryParse(parts[2], out long size);
                                    string originalType = parts[3];
                                    string letter = parts[4];
                                    string fs = parts[5];
                                    string label = parts.Length > 6 ? parts[6] : "";
                                    
                                    if (letter == "0" || letter == "\0") letter = "";

                                    var pInfo = new PartitionInfo
                                    {
                                        DiskIndex = diskNum,
                                        PartitionIndex = partNum,
                                        SizeBytes = size,
                                        DriveLetter = letter,

                                        FileSystem = fs,
                                        Label = label
                                    };
                                    
                                    pInfo.Type = MapPartitionType(originalType);
                                    pInfo.Info = MapPartitionInfo(pInfo.Type, pInfo.DriveLetter, pInfo.FileSystem);

                                    partitions.Add(pInfo);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting all partitions: {ex.Message}");
                }
                return partitions;
            });
        }

        public static async Task<List<PartitionInfo>> GetPartitionsAsync(int diskIndex)
        {
            // Keep for fallback if needed, or redirect to global filter? 
            // Better to keep independent implementation for now
            return await Task.Run(() =>
            {
                var partitions = new List<PartitionInfo>();
                try
                {
                    string command = $"powershell -NoProfile -ExecutionPolicy Bypass -Command \"Get-Partition -DiskNumber {diskIndex} | ForEach-Object {{ $vol = $_ | Get-Volume -ErrorAction SilentlyContinue; $_ | Select-Object PartitionNumber, Size, Type, DriveLetter, @{{Name='FileSystem';Expression={{$vol.FileSystem}} }}, @{{Name='Label';Expression={{$vol.FileSystemLabel}} }} }} | Sort-Object PartitionNumber | ConvertTo-Csv -NoTypeInformation\"";
                    
                    var result = Utilities.RunCommandWithOutput(command);
                    // ... parsing logic same as before ...
                    // Simulating the existing parsing to save token space in this replace
                    if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
                    {
                         var lines = result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 1; i < lines.Length; i++)
                        {
                            var parts = ParseCsvLine(lines[i]);
                            if (parts.Length >= 5 && int.TryParse(parts[0], out int partNum))
                            {
                                long.TryParse(parts[1], out long size);
                                var pInfo = new PartitionInfo
                                {
                                    DiskIndex = diskIndex,
                                    PartitionIndex = partNum,
                                    SizeBytes = size,
                                    DriveLetter = parts[3] == "0" ? "" : parts[3],
                                    FileSystem = parts[4],
                                    Label = parts.Length > 5 ? parts[5] : "",
                                    Type = MapPartitionType(parts[2])
                                };
                                pInfo.Info = MapPartitionInfo(pInfo.Type, pInfo.DriveLetter, pInfo.FileSystem);
                                partitions.Add(pInfo);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting partitions for disk {diskIndex}: {ex.Message}");
                }
                return partitions;
            });
        }

        private static string MapPartitionType(string originalType)
        {
            if (string.IsNullOrEmpty(originalType)) return "Unknown";
            
            // Handle Type
            switch (originalType)
            {
                case "System": return "System";
                case "Reserved": return "Reserved";
                case "Basic": return "Primary";
                case "Recovery": return "Recovery";
                case "Dynamic": return "Dynamic";
                default: return originalType;
            }
        }

        private static string MapPartitionInfo(string type, string driveLetter, string fileSystem)
        {
            var infos = new List<string>();

            if (type == "System" || (fileSystem == "FAT32" && string.IsNullOrEmpty(driveLetter) && type != "Recovery"))
            {
                infos.Add("System"); 
            }

            if (type == "Recovery")
            {
                infos.Add("Hidden");
            }
            
            if (!string.IsNullOrEmpty(driveLetter) && driveLetter.StartsWith("C", StringComparison.OrdinalIgnoreCase))
            {
                infos.Add("Boot");
            }

            return string.Join(", ", infos);
        }
        public static async Task<List<PartitionInfo>> GetPartitionsUsingDiskPartAsync(int diskIndex)
        {
            return await Task.Run(() =>
            {
                var partitions = new List<PartitionInfo>();
                try
                {
                    // Use diskpart script
                    string scriptFile = System.IO.Path.GetTempFileName();
                    System.IO.File.WriteAllLines(scriptFile, new[] { $"select disk {diskIndex}", "detail disk", "exit" });

                    var psi = new ProcessStartInfo
                    {
                        FileName = "diskpart.exe",
                        Arguments = $"/s \"{scriptFile}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(psi))
                    {
                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();
                            
                            // Cleanup
                            try { System.IO.File.Delete(scriptFile); } catch { }

                            if (process.ExitCode == 0)
                            {
                                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                bool inTable = false;
                                
                                foreach (var line in lines)
                                {
                                    string trimLine = line.Trim();
                                    if (trimLine.StartsWith("Volume ###"))
                                    {
                                        inTable = true;
                                        continue;
                                    }
                                    if (trimLine.StartsWith("-----")) continue;
                                    
                                    if (inTable && trimLine.StartsWith("Volume"))
                                    {
                                        // Parse line: Volume 1     C   Label    NTFS   Partition   100 MB  Healthy  System
                                        // Use Regex to handle variable spacing
                                        // Parts: [Volume] [Number] [Ltr] [Label] [Fs] [Type] [Size] [Unit] [Status] [Info...]
                                        
                                        // Simplified parsing logic based on assuming standard columns
                                        // Ideally use Regex
                                        try 
                                        {
                                            var parts = Regex.Split(trimLine, @"\s{2,}"); 
                                            // Split by 2+ spaces to avoid splitting single space labels (though labels can be problematic)
                                            
                                            if (parts.Length >= 6)
                                            {
                                                // Identify index
                                                // parts[0] is "Volume 1" or similar. Split space.
                                                string[] volParts = parts[0].Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);
                                                if (volParts.Length > 1 && int.TryParse(volParts[1], out int pIndex))
                                                {
                                                    // Map columns roughly logic
                                                    // Col 1: Ltr
                                                    string letter = parts[1].Trim();
                                                    // Col 2: Label (might be empty? regex split might skip it if used \s+)
                                                    // DiskPart spacing is tricky. Let's try a safer regex for extracting specific columns if possible.
                                                    // Alternatively, if parts[1] is single char, it's letter. 

                                                    // Let's use a simpler heuristic:
                                                    // Look for size (number + MB/GB) and work backwards/forwards
                                                    
                                                    // Re-parsing carefully
                                                    // Volume 0     E                       NTFS   Partition    428 GB  Healthy
                                                    // Volume 1     C                NTFS   Partition    237 GB  Healthy    Boot
                                                    
                                                    // Use pattern matching
                                                    var match = Regex.Match(trimLine, @"Volume\s+(\d+)\s+([A-Z]?)\s+(.*?)\s+(FAT32|NTFS|REFS|EXFAT|UDF)\s+(.*?)\s+(\d+)\s+(MB|GB|TB|KB)\s+(.*?)$", RegexOptions.IgnoreCase);
                                                    
                                                    if (match.Success)
                                                    {
                                                        int partIndex = int.Parse(match.Groups[1].Value);
                                                        string drvLetter = match.Groups[2].Value.Trim();
                                                        string fs = match.Groups[4].Value.Trim();
                                                        string type = match.Groups[5].Value.Trim(); // Partition/Simple...
                                                        long sizeVal = long.Parse(match.Groups[6].Value);
                                                        string sizeUnit = match.Groups[7].Value.ToUpper();
                                                        string info = match.Groups[8].Value.Trim(); // Info and Status mixed? match group 8 is "Healthy    Boot" usually

                                                        // Convert size to bytes
                                                        long sizeBytes = sizeVal;
                                                        if (sizeUnit == "KB") sizeBytes *= 1024;
                                                        if (sizeUnit == "MB") sizeBytes *= 1024 * 1024;
                                                        if (sizeUnit == "GB") sizeBytes *= 1024 * 1024 * 1024;
                                                        if (sizeUnit == "TB") sizeBytes *= 1024L * 1024L * 1024L * 1024L;

                                                        var pInfo = new PartitionInfo
                                                        {
                                                            DiskIndex = diskIndex,
                                                            PartitionIndex = partIndex,
                                                            DriveLetter = drvLetter,
                                                            FileSystem = fs,
                                                            SizeBytes = sizeBytes,
                                                            Type = MapPartitionType(type) // Map "Simple" etc
                                                        };

                                                        // Fix Info
                                                        var infoList = new List<string>();
                                                        if (info.Contains("System", StringComparison.OrdinalIgnoreCase)) infoList.Add("System");
                                                        if (info.Contains("Boot", StringComparison.OrdinalIgnoreCase)) infoList.Add("Boot");
                                                        if (info.Contains("Hidden", StringComparison.OrdinalIgnoreCase)) infoList.Add("Hidden");
                                                        
                                                        // Fallback info mapping
                                                        string mappedInfo = MapPartitionInfo(pInfo.Type, pInfo.DriveLetter, pInfo.FileSystem);
                                                        if (!string.IsNullOrEmpty(mappedInfo) && mappedInfo != "System, Boot" && infoList.Count == 0)
                                                        {
                                                            // Logic merge
                                                            pInfo.Info = mappedInfo;
                                                        }
                                                        else if (infoList.Count > 0)
                                                        {
                                                            pInfo.Info = string.Join(", ", infoList);
                                                        }

                                                        partitions.Add(pInfo);
                                                    }
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting diskpart partitions for disk {diskIndex}: {ex.Message}");
                }
                return partitions;
            });
        }
        public static async Task<PartitionInfo?> GetPartitionInfoByDriveLetterAsync(string driveLetter)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string letter = driveLetter.TrimEnd(':');
                    // PowerShell to get partition info by DriveLetter
                    string command = $"powershell -NoProfile -ExecutionPolicy Bypass -Command \"Get-Partition -DriveLetter {letter} -ErrorAction SilentlyContinue | Select-Object DiskNumber, PartitionNumber | ConvertTo-Csv -NoTypeInformation\"";
                    
                    var result = Utilities.RunCommandWithOutput(command);

                    if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
                    {
                        var lines = result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        // Skip header, read first data line
                        for (int i = 1; i < lines.Length; i++)
                        {
                            var parts = ParseCsvLine(lines[i]);
                            // Expected: DiskNumber, PartitionNumber
                            if (parts.Length >= 2) 
                            {
                                if (int.TryParse(parts[0], out int diskNum) && int.TryParse(parts[1], out int partNum))
                                {
                                    return new PartitionInfo
                                    {
                                        DiskIndex = diskNum,
                                        PartitionIndex = partNum,
                                        DriveLetter = letter
                                    };
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting partition for drive {driveLetter}: {ex.Message}");
                }
                return null;
            });
        }

        public static async Task<SecurityCheckResult> CheckSecurityPreconditionsAsync()
        {
            return await Task.Run(() =>
            {
                var result = new SecurityCheckResult();

                // 1. Check BitLocker Status across all drives
                try
                {
                    var mbdeResult = Utilities.RunCommandWithOutput("manage-bde.exe -status");
                    if (mbdeResult.ExitCode == 0 && !string.IsNullOrWhiteSpace(mbdeResult.Output))
                    {
                        var lines = mbdeResult.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        string currentDrive = "";
                        foreach (var rawLine in lines)
                        {
                            string line = rawLine.Trim();
                            var match = System.Text.RegularExpressions.Regex.Match(line, @"(?:Volume|Disk volume)\s+([A-Za-z]:)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                currentDrive = match.Groups[1].Value.ToUpper();
                            }

                            bool isProtectionOn = line.IndexOf("Protection On", StringComparison.OrdinalIgnoreCase) >= 0;
                            bool isEncrypted = (line.IndexOf("Conversion Status:", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                                line.IndexOf("Fully Decrypted", StringComparison.OrdinalIgnoreCase) < 0) ||
                                               line.IndexOf("Encryption in Progress", StringComparison.OrdinalIgnoreCase) >= 0;

                            if (isProtectionOn || isEncrypted)
                            {
                                if (!string.IsNullOrEmpty(currentDrive) && !result.BitLockerActiveDrives.Contains(currentDrive))
                                {
                                    result.BitLockerActiveDrives.Add(currentDrive);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Fallback: PowerShell Get-BitLockerVolume
                        string psCmd = "powershell -NoProfile -ExecutionPolicy Bypass -Command \"Get-BitLockerVolume -ErrorAction SilentlyContinue | Where-Object { $_.ProtectionStatus -eq 'On' -or $_.VolumeStatus -ne 'FullyDecrypted' } | Select-Object -ExpandProperty MountPoint\"";
                        var psResult = Utilities.RunCommandWithOutput(psCmd);
                        if (psResult.ExitCode == 0 && !string.IsNullOrWhiteSpace(psResult.Output))
                        {
                            var mounts = psResult.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var m in mounts)
                            {
                                string tr = m.Trim().ToUpper();
                                if (!string.IsNullOrEmpty(tr) && !result.BitLockerActiveDrives.Contains(tr))
                                {
                                    result.BitLockerActiveDrives.Add(tr);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error checking BitLocker: {ex.Message}");
                }

                result.IsBitLockerDisabled = (result.BitLockerActiveDrives.Count == 0);

                // 2. Check Secure Boot Status
                try
                {
                    bool isSecureBootOn = false;
                    // Check Registry
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State"))
                    {
                        if (key != null)
                        {
                            var val = key.GetValue("UEFISecureBootEnabled");
                            if (val is int intVal && intVal == 1)
                            {
                                isSecureBootOn = true;
                            }
                        }
                    }

                    // Fallback check with PowerShell Confirm-SecureBootUEFI
                    if (!isSecureBootOn)
                    {
                        string psCmd = "powershell -NoProfile -ExecutionPolicy Bypass -Command \"try { Confirm-SecureBootUEFI } catch { $false }\"";
                        var psResult = Utilities.RunCommandWithOutput(psCmd);
                        if (psResult.ExitCode == 0 && bool.TryParse(psResult.Output.Trim(), out bool isEnabled))
                        {
                            isSecureBootOn = isEnabled;
                        }
                    }

                    result.IsSecureBootDisabled = !isSecureBootOn;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error checking Secure Boot: {ex.Message}");
                    result.IsSecureBootDisabled = true;
                }

                return result;
            });
        }
    }

    public class SecurityCheckResult
    {
        public bool IsBitLockerDisabled { get; set; } = true;
        public bool IsSecureBootDisabled { get; set; } = true;
        public bool IsPassed => IsBitLockerDisabled && IsSecureBootDisabled;
        public List<string> BitLockerActiveDrives { get; set; } = new();
    }
}
