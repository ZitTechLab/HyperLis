using System;
using System.Text;
using HyperLisUI.Models;

namespace HyperLisUI.Services
{
    public class ConfigService
    {
        public string GenerateConfig(InstallationOptions options)
        {
            var sb = new StringBuilder();

            // Source
            sb.AppendLine($"WinSource={options.WinSource}");
            sb.AppendLine($"WinSourceDisk={options.WinSourceDisk}");
            sb.AppendLine($"WinSourcePartition={options.WinSourcePartition}");
            // If WinSource is HDD, PathNoDrive might act as root or specific folder
            sb.AppendLine($"WinSourcePathNoDrive={options.WinSourcePathNoDrive}");
            sb.AppendLine($"WimIndex={options.WimIndex}");
            sb.AppendLine($"WimName={options.WimName}");

            // System
            sb.AppendLine($"FirmwareType={options.FirmwareType}");

            // Target
            sb.AppendLine($"RecommendedDisk={options.RecommendedDisk}");
            sb.AppendLine($"RecommendedEFIPart={options.RecommendedEFIPart}");
            sb.AppendLine($"RecommendedOSPart={options.RecommendedOSPart}");
            sb.AppendLine($"AutoFormat={(options.AutoFormat ? "true" : "false")}");

            // Drivers
            if (options.DriversDisk.HasValue)
            {
                sb.AppendLine($"DriversDisk={options.DriversDisk}");
                sb.AppendLine($"DriversPartition={options.DriversPartition}");
                sb.AppendLine($"DriversPathNoDrive={options.DriversPathNoDrive}");
            }
            else
            {
                sb.AppendLine("DriversDisk=");
                sb.AppendLine("DriversPartition=");
                sb.AppendLine("DriversPathNoDrive=");
            }

            // Unattend
            if (options.UnattendedFileDisk.HasValue)
            {
                sb.AppendLine($"UnattendedFileDisk={options.UnattendedFileDisk}");
                sb.AppendLine($"UnattendedFilePartition={options.UnattendedFilePartition}");
                sb.AppendLine($"UnattendedFilePathNoDrive={options.UnattendedFilePathNoDrive}");
            }
            else
            {
                sb.AppendLine("UnattendedFileDisk=");
                sb.AppendLine("UnattendedFilePartition=");
                sb.AppendLine("UnattendedFilePathNoDrive=");
            }

            // Bypasses
            sb.AppendLine($"AutoBypassWindows11={(options.AutoBypassWindows11 ? "true" : "false")}");
            sb.AppendLine($"AutoBypassNRO={(options.AutoBypassNRO ? "true" : "false")}");

            // Boot Menus
            sb.AppendLine($"AutoOtherWindowsBootMenu={(options.AutoOtherWindowsBootMenu ? "true" : "false")}");
            sb.AppendLine($"AutoWinpeBootMenu={(options.AutoWinpeBootMenu ? "true" : "false")}");

            // Boot WIM (WinPE)
            sb.AppendLine($"BootWimDisk={options.BootWimDisk}");
            sb.AppendLine($"BootWimPartition={options.BootWimPartition}");
            sb.AppendLine($"BootWimPathNoDrive={options.BootWimPathNoDrive}");
            sb.AppendLine($"BootSdiPathNoDrive={options.BootSdiPathNoDrive}");

            // Misc
            sb.AppendLine($"AutoRestart={(options.AutoRestart ? "true" : "false")}");

            return sb.ToString();
        }
    }
}
