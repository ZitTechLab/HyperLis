namespace HyperLisUI.Models
{
    public class DiskInfo
    {
        public int Index { get; set; }
        public string Model { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string Style { get; set; } = string.Empty; // GPT/MBR
        public string DisplayName => Index < 0 ? Model : $"Disk {Index}: {Model} ({Size})";

        // Helper to format size
        public string Size => FormatBytes(SizeBytes);

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n0} {1}", number, suffixes[counter]);
        }
        public List<PartitionInfo> Partitions { get; set; } = new List<PartitionInfo>();
    }

    public class PartitionInfo
    {
        public int DiskIndex { get; set; }
        public int PartitionIndex { get; set; }
        public string Type { get; set; } = string.Empty; // System, Reserved, Primary, Recovery
        public long SizeBytes { get; set; }
        public string Style { get; set; } = string.Empty; // GPT/MBR (Inherited from Disk)
        public string FileSystem { get; set; } = string.Empty;
        public string DriveLetter { get; set; } = string.Empty; // e.g., "C"
        public string Label { get; set; } = string.Empty; // Volume Label
        public string Info { get; set; } = string.Empty; // System, Boot, Hidden

        // UI Display Properties
        public string Name => $"Partition {PartitionIndex}";
        public string Size => FormatBytes(SizeBytes);
        
        // Formatted Drive Letter for UI (e.g., "C")
        public string Drive => string.IsNullOrEmpty(DriveLetter) ? "" : DriveLetter.TrimEnd(':');

        public string ComboBoxDisplay => $"Partition {PartitionIndex}: {Type} {(string.IsNullOrEmpty(DriveLetter) ? "" : $"({DriveLetter})")}";

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            // Detail logic can be improved, e.g. 100 MB, 465 GB
            return string.Format("{0:n0} {1}", number, suffixes[counter]); 
        }
    }


    public class InstallationOptions
    {
        // Source
        public string WinSource { get; set; } = "WIM"; // ISO/WIM/ESD
        public int WinSourceDisk { get; set; }
        public int WinSourcePartition { get; set; }
        public string WinSourcePathNoDrive { get; set; } = string.Empty;
        public int WimIndex { get; set; }
        public string WimName { get; set; } = string.Empty;

        // System
        public string FirmwareType { get; set; } = "UEFI"; // Detect or set

        // Target
        public int RecommendedDisk { get; set; }
        public int RecommendedEFIPart { get; set; }
        public int RecommendedOSPart { get; set; }
        public bool AutoFormat { get; set; } = true;

        // Drivers
        public int? DriversDisk { get; set; }
        public int? DriversPartition { get; set; }
        public string DriversPathNoDrive { get; set; } = string.Empty;

        // Unattend
        public int? UnattendedFileDisk { get; set; }
        public int? UnattendedFilePartition { get; set; }
        public string UnattendedFilePathNoDrive { get; set; } = string.Empty;
        public string SourceUnattendPath { get; set; } = string.Empty;

        // Bypasses
        public bool AutoBypassWindows11 { get; set; } = true;
        public bool AutoBypassNRO { get; set; } = true;
        
        // Boot Menus
        public bool AutoOtherWindowsBootMenu { get; set; } = true;
        public bool AutoWinpeBootMenu { get; set; } = true;

        // Boot WIM (WinPE location)
        public int BootWimDisk { get; set; }
        public int BootWimPartition { get; set; }
        public string BootWimPathNoDrive { get; set; } = string.Empty; // e.g. \sources\boot.wim
        public string BootSdiPathNoDrive { get; set; } = string.Empty; // e.g. \boot\boot.sdi

        // Misc
        public bool AutoRestart { get; set; } = true;
    }

    public class InstallProgressReport
    {
        public int Step { get; set; } = 1;
        public int OverallPercentage { get; set; } = 0;
        public int FilePercentage { get; set; } = 0;
        public string Message { get; set; } = string.Empty;
        public string CurrentFileName { get; set; } = string.Empty;
        public string TransferredSize { get; set; } = string.Empty;
        public string Speed { get; set; } = string.Empty;
        public bool IsDetailed { get; set; } = false;
    }
}
