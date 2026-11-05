using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Management;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.Collections.Generic;

namespace HyperLisUI.Views
{
    public partial class HardwareInfoView : UserControl
    {
        private string _fullReport = "";
        private bool _isDetailed = false;

        public HardwareInfoView()
        {
            InitializeComponent();
            Loaded += HardwareInfoView_Loaded;
        }

        private async void HardwareInfoView_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTranslations();
            Helpers.LocalizationService.LanguageChanged += ApplyTranslations;
            // Initial load (Simple mode by default)
            await LoadHardwareInfoAsync(_isDetailed);
        }

        private void ApplyTranslations()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ApplyTranslations);
                return;
            }

            DetailedViewToggle.Content = Helpers.LocalizationService.Get("lblHardwareDetailedView");
            BtnSaveTxt.Content = Helpers.LocalizationService.Get("btnHardwareSaveTxt");
            BtnCopyAll.Content = Helpers.LocalizationService.Get("btnHardwareCopyAll");

            lblHardwareOS.Text = Helpers.LocalizationService.Get("lblHardwareOS");
            lblHardwareProcessors.Text = Helpers.LocalizationService.Get("lblHardwareProcessors");
            lblHardwareMemory.Text = Helpers.LocalizationService.Get("lblHardwareMemory");
            lblHardwareMotherboards.Text = Helpers.LocalizationService.Get("lblHardwareMotherboards");
            lblHardwareGraphics.Text = Helpers.LocalizationService.Get("lblHardwareGraphics");
            lblHardwareDiskDrives.Text = Helpers.LocalizationService.Get("lblHardwareDiskDrives");
            lblHardwareNetworkAdapters.Text = Helpers.LocalizationService.Get("lblHardwareNetworkAdapters");
            lblHardwareBIOS.Text = Helpers.LocalizationService.Get("lblHardwareBIOS");
            
            ExpanderGeneralInfo.Header = Helpers.LocalizationService.Get("lblHardwareGeneralInfo");
            ExpanderHealthTools.Header = Helpers.LocalizationService.Get("lblHardwareHealthTools");
            lblDiskHealth.Text = Helpers.LocalizationService.Get("lblDiskHealthStatus");
            lblBatteryHealth.Text = Helpers.LocalizationService.Get("lblBatteryHealthStatus");
            BtnCheckDisk.Content = Helpers.LocalizationService.Get("btnHardwareCheckNow");
            BtnCheckBattery.Content = Helpers.LocalizationService.Get("btnHardwareCheckNow");
            TextDiskHealthStatus.Text = Helpers.LocalizationService.Get("msgHardwareReadyToScan");
            TextBatteryHealthStatus.Text = Helpers.LocalizationService.Get("msgHardwareReadyToScan");
        }

        private async void DetailedView_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chk)
            {
                _isDetailed = chk.IsChecked == true;
                await LoadHardwareInfoAsync(_isDetailed);
            }
        }

        private async Task LoadHardwareInfoAsync(bool detailed)
        {
            // Show loading indicators
            string loadingText = Helpers.LocalizationService.Get("msgLoading");
            TextOS.Text = loadingText;
            TextCPU.Text = loadingText;
            TextRAM.Text = loadingText;
            TextMotherboard.Text = loadingText;
            TextGPU.Text = loadingText;
            TextDisks.Text = loadingText;
            TextNetwork.Text = loadingText;
            TextBIOS.Text = loadingText;

            string os = "", cpu = "", ram = "", mobo = "", gpu = "", disks = "", network = "", bios = "";

            await Task.Run(() =>
            {
                os = GetOSInfo(detailed);
                cpu = GetCpuInfo(detailed);
                ram = GetRamInfo(detailed);
                mobo = GetMotherboardInfo(detailed);
                gpu = GetGpuInfo(detailed);
                disks = GetDiskInfo(detailed);
                network = GetNetworkInfo(detailed);
                bios = GetBiosInfo(detailed);
            });

            // Update UI
            TextOS.Text = os;
            TextCPU.Text = cpu;
            TextRAM.Text = ram;
            TextMotherboard.Text = mobo;
            TextGPU.Text = gpu;
            TextDisks.Text = disks;
            TextNetwork.Text = network;
            TextBIOS.Text = bios;

            BuildFullReport(os, cpu, ram, mobo, gpu, disks, network, bios);
        }

        private void BuildFullReport(string os, string cpu, string ram, string mobo, string gpu, string disks, string network, string bios)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Helpers.LocalizationService.Get("msgHardwareReportTitle"));
            sb.AppendLine("========================");
            sb.AppendLine();
            sb.AppendLine(Helpers.LocalizationService.Get("lblHardwareOS"));
            sb.AppendLine(os);
            sb.AppendLine();
            sb.AppendLine(Helpers.LocalizationService.Get("lblHardwareProcessors"));
            sb.AppendLine(cpu);
            sb.AppendLine();
            sb.AppendLine(Helpers.LocalizationService.Get("lblHardwareMemory"));
            sb.AppendLine(ram);
            sb.AppendLine();
            sb.AppendLine(Helpers.LocalizationService.Get("lblHardwareMotherboards"));
            sb.AppendLine(mobo);
            sb.AppendLine();
            sb.AppendLine(Helpers.LocalizationService.Get("lblHardwareGraphics"));
            sb.AppendLine(gpu);
            sb.AppendLine();
            sb.AppendLine(Helpers.LocalizationService.Get("lblHardwareDiskDrives"));
            sb.AppendLine(disks);
            sb.AppendLine();
            sb.AppendLine(Helpers.LocalizationService.Get("lblHardwareNetworkAdapters"));
            sb.AppendLine(network);
            sb.AppendLine();
            sb.AppendLine(Helpers.LocalizationService.Get("lblHardwareBIOS"));
            sb.AppendLine(bios);

            _fullReport = sb.ToString();
        }

        #region WMI Helpers

        private string GetInfo(string query, string prop1, string? prop2 = null, string format = "{0}")
        {
            try
            {
                var sb = new StringBuilder();
                using (var searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var val1 = obj[prop1]?.ToString();
                        var val2 = prop2 != null ? obj[prop2]?.ToString() : null;

                        if (!string.IsNullOrWhiteSpace(val1))
                        {
                            if (prop2 != null)
                                sb.AppendLine(string.Format(format, val1, val2));
                            else
                                sb.AppendLine(string.Format(format, val1));
                        }
                    }
                }
                return sb.ToString().Trim();
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        private string GetOSInfo(bool detailed)
        {
             try
            {
                var sb = new StringBuilder();
                using (var searcher = new ManagementObjectSearcher("SELECT Caption, OSArchitecture, CSName, Version, BuildNumber FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        sb.AppendLine($"{obj["Caption"]} ({obj["OSArchitecture"]})");
                        sb.AppendLine($"Laptop ({obj["CSName"]})");
                        if (detailed)
                        {
                            sb.AppendLine($"\tVersion: {obj["Version"]}");
                            sb.AppendLine($"\tBuild: {obj["BuildNumber"]}");
                        }
                    }
                }
                return sb.ToString().Trim();
            }
            catch { }
            return "Windows 11 (Unknown)";
        }

        private string GetCpuInfo(bool detailed)
        {
             try
            {
                var sb = new StringBuilder();
                using (var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors, L2CacheSize, L3CacheSize, VirtualizationFirmwareEnabled, Stepping, Description FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (detailed)
                        {
                            sb.AppendLine($"{obj["Name"]}");
                            sb.AppendLine($"\tCores: {obj["NumberOfCores"]}");
                            sb.AppendLine($"\tThreads: {obj["NumberOfLogicalProcessors"]}");
                            sb.AppendLine($"\tVirtualization: {obj["VirtualizationFirmwareEnabled"]}");
                            // Cache size is in KB usually
                            double l2 = Convert.ToDouble(obj["L2CacheSize"]) / 1024.0;
                            double l3 = Convert.ToDouble(obj["L3CacheSize"]) / 1024.0;
                            sb.AppendLine($"\tL2 Cache: {l2:F2} MB");
                            sb.AppendLine($"\tL3 Cache: {l3:F2} MB");
                            sb.AppendLine($"\tStepping: {obj["Description"]}"); // Often contains stepping info
                        }
                        else
                        {
                            sb.AppendLine($"{obj["Name"]} ({obj["NumberOfCores"]} Cores, {obj["NumberOfLogicalProcessors"]} Threads)");
                        }
                    }
                }
                return sb.ToString().Trim();
            }
            catch { return "Unknown CPU"; }
        }

        private string GetRamInfo(bool detailed)
        {
            try
            {
                if (!detailed)
                {
                    long totalCapacity = 0;
                    string speed = "";
                    using (var searcher = new ManagementObjectSearcher("SELECT Capacity, Speed FROM Win32_PhysicalMemory"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            if (long.TryParse(obj["Capacity"]?.ToString(), out long cap))
                                totalCapacity += cap;
                            
                            if (string.IsNullOrEmpty(speed) && obj["Speed"] != null)
                                speed = obj["Speed"].ToString();
                        }
                    }
                    double gb = Math.Round(totalCapacity / (1024.0 * 1024.0 * 1024.0), 2);
                    return $"{gb} GiB Unknown @ {speed} MHz";
                }
                else
                {
                    var sb = new StringBuilder();
                    int i = 0;
                    using (var searcher = new ManagementObjectSearcher("SELECT Capacity, Speed, Manufacturer, MemoryType, FormFactor FROM Win32_PhysicalMemory"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            sb.AppendLine($"Module {i++}");
                            sb.AppendLine($"\tManufacturer: {obj["Manufacturer"]}");
                            if (long.TryParse(obj["Capacity"]?.ToString(), out long cap))
                                sb.AppendLine($"\tSize: {Math.Round(cap / (1024.0 * 1024.0 * 1024.0), 2)} GiB");
                            sb.AppendLine($"\tSpeed: {obj["Speed"]} MHz");
                            // Determine form factor roughly if needed, 12=SODIMM usually
                            sb.AppendLine($"\tForm Factor: {(obj["FormFactor"]?.ToString() == "12" ? "SODIMM" : "DIMM")}"); 
                        }
                    }
                    
                    // Virtual Memory (PageFile)
                    using (var searcher = new ManagementObjectSearcher("SELECT AllocatedBaseSize, CurrentUsage FROM Win32_PageFileUsage"))
                    {
                         foreach (ManagementObject obj in searcher.Get())
                         {
                             sb.AppendLine("Virtual Memory");
                             sb.AppendLine($"\tTotal: {obj["AllocatedBaseSize"]} MB");
                             sb.AppendLine($"\tUsed: {obj["CurrentUsage"]} MB");
                         }
                    }
                    return sb.ToString().Trim();
                }

            }
            catch { return "Unknown RAM"; }
        }

        private string GetMotherboardInfo(bool detailed)
        {
            if (!detailed)
                return GetInfo("SELECT Manufacturer, Product FROM Win32_BaseBoard", "Manufacturer", "Product", "{0} ({1})");
            
            try 
            {
                 var sb = new StringBuilder();
                 using (var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Product, Version FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        sb.AppendLine($"{obj["Manufacturer"]}");
                        sb.AppendLine($"\tProduct: {obj["Product"]}");
                        sb.AppendLine($"\tVersion: {obj["Version"]}");
                    }
                }
                return sb.ToString().Trim();
            } 
            catch { return "Unknown Mainboard";}
        }

        private string GetGpuInfo(bool detailed)
        {
            if (!detailed)
                 return GetInfo("SELECT Name, AdapterRAM FROM Win32_VideoController", "Name", "AdapterRAM", "{0}");
            
             try
            {
                var sb = new StringBuilder();
                using (var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM, VideoProcessor, DriverVersion, CurrentHorizontalResolution, CurrentVerticalResolution, CurrentRefreshRate, VideoModeDescription FROM Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        sb.AppendLine($"{obj["Name"]}");
                        if (long.TryParse(obj["AdapterRAM"]?.ToString(), out long ram))
                             sb.AppendLine($"\tVideo Memory: {Math.Round(ram / (1024.0 * 1024.0 * 1024.0), 2)} GiB");
                        
                        sb.AppendLine($"\tDriver Version: {obj["DriverVersion"]}");
                         if (obj["CurrentHorizontalResolution"] != null)
                             sb.AppendLine($"\tResolution: {obj["CurrentHorizontalResolution"]} x {obj["CurrentVerticalResolution"]} @ {obj["CurrentRefreshRate"]} Hz");
                    }
                }
                return sb.ToString().Trim();
            }
            catch { return "Unknown GPU"; }
        }

        private string GetDiskInfo(bool detailed)
        {
             try
            {
                var sb = new StringBuilder();
                using (var searcher = new ManagementObjectSearcher("SELECT Model, Size, FirmwareRevision, MediaType, BytesPerSector FROM Win32_DiskDrive"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (long.TryParse(obj["Size"]?.ToString(), out long size))
                        {
                            double gb = Math.Round(size / (1024.0 * 1024.0 * 1024.0), 2);
                            sb.AppendLine(detailed ? $"{obj["Model"]}" : $"{obj["Model"]} ({gb} GB)");
                            
                            if(detailed)
                            {
                                sb.AppendLine($"\tSize: {gb} GB");
                                sb.AppendLine($"\tFirmware: {obj["FirmwareRevision"]}");
                                sb.AppendLine($"\tMedia Type: {obj["MediaType"]}");
                                sb.AppendLine($"\tBytes/Sector: {obj["BytesPerSector"]}");
                            }
                        }
                    }
                }
                return sb.ToString().Trim();
            }
            catch { return "Unknown Disk"; }
        }

        private string GetNetworkInfo(bool detailed)
        {
            if (!detailed)
                 return GetInfo("SELECT Name FROM Win32_NetworkAdapter WHERE PhysicalAdapter=True", "Name");
            
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Physical Adapters");
                 using (var searcher = new ManagementObjectSearcher("SELECT Name, Manufacturer, AdapterType, MACAddress, ServiceName FROM Win32_NetworkAdapter WHERE PhysicalAdapter=True"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                         sb.AppendLine($"\t{obj["Name"]}");
                         sb.AppendLine($"\t\tManufacturer: {obj["Manufacturer"]}");
                         sb.AppendLine($"\t\tMAC: {obj["MACAddress"]}");
                    }
                }
                
                sb.AppendLine("Virtual Adapters");
                using (var searcher = new ManagementObjectSearcher("SELECT Name, Manufacturer, MACAddress FROM Win32_NetworkAdapter WHERE PhysicalAdapter=False"))
                {
                     foreach (ManagementObject obj in searcher.Get())
                    {
                        // Filter out empty ones to reduce noise if needed, but keeping consistent with Detail.txt
                        if (obj["Name"] != null)
                        {
                            sb.AppendLine($"\t{obj["Name"]}");
                             if(obj["MACAddress"] != null) sb.AppendLine($"\t\tMAC: {obj["MACAddress"]}");
                        }
                    }
                }

                return sb.ToString().Trim();
            }
            catch { return "Unknown Network"; }
        }

        private string GetBiosInfo(bool detailed)
        {
             if (!detailed)
                return GetInfo("SELECT SMBIOSBIOSVersion, Manufacturer FROM Win32_BIOS", "Manufacturer", "SMBIOSBIOSVersion", "{0} {1}");
             
             try
             {
                 var sb = new StringBuilder();
                 using (var searcher = new ManagementObjectSearcher("SELECT SMBIOSBIOSVersion, Manufacturer, SerialNumber, Version FROM Win32_BIOS"))
                 {
                     foreach (ManagementObject obj in searcher.Get())
                     {
                         sb.AppendLine($"{obj["Manufacturer"]}");
                         sb.AppendLine($"\tVersion: {obj["SMBIOSBIOSVersion"]}"); // Usually the version user sees
                         sb.AppendLine($"\tBIOS Version: {obj["Version"]}");
                         sb.AppendLine($"\tSerial: {obj["SerialNumber"]}");
                     }
                 }
                 return sb.ToString().Trim();
             }
             catch {return "Unknown BIOS";}
        }

        #endregion

        private async void BtnCheckBattery_Click(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.UpdateProgress(0, 10, Helpers.LocalizationService.Get("msgCheckingBattery"));
            LoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                await Task.Delay(1000); 
                
                bool isLaptop = false;
                bool hasBattery = false;

                await Task.Run(() => {
                    try {
                        using (var searcher = new ManagementObjectSearcher("SELECT ChassisTypes FROM Win32_SystemEnclosure"))
                        {
                            foreach (ManagementObject obj in searcher.Get())
                            {
                                if (obj["ChassisTypes"] is ushort[] types && types.Length > 0)
                                {
                                    var type = types[0];
                                    if (type == 8 || type == 9 || type == 10 || type == 11 || type == 14 || type == 30 || type == 31 || type == 32)
                                        isLaptop = true;
                                }
                            }
                        }
                    } catch {}

                    try {
                        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery"))
                        {
                            foreach (ManagementObject obj in searcher.Get())
                            {
                                hasBattery = true;
                                break;
                            }
                        }
                    } catch {}
                });

                if (!isLaptop && !hasBattery)
                {
                    var popup = new InfoPopupWindow(Helpers.LocalizationService.Get("msgNoBatteryDesk"), Helpers.LocalizationService.Get("titleHealth"), Helpers.LocalizationService.Get("lblDesktop"));
                    popup.Owner = Window.GetWindow(this);
                    popup.ShowDialog();
                    return;
                }

                if (isLaptop && !hasBattery)
                {
                     var msg = Helpers.LocalizationService.Get("msgNoBatteryLaptop");
                     var popupInfo = new InfoPopupWindow(msg, Helpers.LocalizationService.Get("titleHealth"), Helpers.LocalizationService.Get("lblWarningNoBattery"));
                     popupInfo.Owner = Window.GetWindow(this);
                     popupInfo.ShowDialog();
                     return;
                }

                LoadingOverlay.UpdateProgress(0, 50, Helpers.LocalizationService.Get("msgGenBatReport"));
                await Task.Delay(1000);
                
                string reportText = "";
                await Task.Run(() => {
                    string psScript = $@"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$reportfile = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), 'batteryreport.xml')
powercfg /batteryreport /xml /output ""$reportfile"" | Out-Null
if (-not (Test-Path ""$reportfile"")) {{
    Write-Output '{Helpers.LocalizationService.Get("msgFailBatReport").Replace("\n", "`n")}'
    exit
}}

$xml = [xml](Get-Content ""$reportfile"")
$battery = $xml.BatteryReport.Batteries.Battery
$wmi = Get-WmiObject Win32_Battery -ErrorAction SilentlyContinue
$remaining = 'N/A'
if ($wmi) {{ $remaining = $wmi.EstimatedChargeRemaining }}

if ($battery) {{
    if ($battery -is [array]) {{ $battery = $battery[0] }}
    $design = [int]$battery.DesignCapacity
    $full = [int]$battery.FullChargeCapacity
    
    $wear = 0
    $health = 0
    if ($design -gt 0) {{
        $wear = [math]::Round((($design - $full) / $design) * 100, 2)
        $health = [math]::Round(($full / $design) * 100, 2)
    }}
    
    $res = ""{Helpers.LocalizationService.Get("lblRemainingBat")} $($remaining)%`n""
    $res += ""{Helpers.LocalizationService.Get("lblDesignBat")} $design mWh`n""
    $res += ""{Helpers.LocalizationService.Get("lblFullBat")} $full mWh`n""
    $res += ""{Helpers.LocalizationService.Get("lblWearBat")} $($wear)%`n""
    $res += ""{Helpers.LocalizationService.Get("lblHealthLeftBat")} $($health)%`n""
    
    if ($health -lt 20 -or $wear -gt 90) {{
        $res += ""`n{Helpers.LocalizationService.Get("lblWarnOldBat")}""
    }} elseif ($health -lt 50) {{
        $res += ""`n{Helpers.LocalizationService.Get("lblNoteBatDec")}""
    }}
    
    Write-Output $res
}} else {{
    Write-Output '{Helpers.LocalizationService.Get("lblWarnCantRead").Replace("\n", "`n")}'
}}
Remove-Item -Path ""$reportfile"" -ErrorAction SilentlyContinue
";
                    string tempScript = Path.Combine(Path.GetTempPath(), "_chkbat.ps1");
                    File.WriteAllText(tempScript, psScript, Encoding.UTF8);
                    
                    var proc = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScript}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            StandardOutputEncoding = Encoding.UTF8
                        }
                    };
                    proc.Start();
                    reportText = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                });
                LoadingOverlay.UpdateProgress(0, 100, Helpers.LocalizationService.Get("msgAnalyzingData"));
                await Task.Delay(500);

                if (string.IsNullOrWhiteSpace(reportText))
                    reportText = Helpers.LocalizationService.Get("lblWarnCantRead").Replace("\n", " ");
                    
                var resPopup = new InfoPopupWindow(reportText.Trim(), Helpers.LocalizationService.Get("titleHealth"), Helpers.LocalizationService.Get("lblLaptop"));
                resPopup.Owner = Window.GetWindow(this);
                resPopup.ShowDialog();
                
                TextBatteryHealthStatus.Text = Helpers.LocalizationService.Get("btnHardwareCheckNow");
            }
            catch (Exception ex)
            {
                 var popupEx = new InfoPopupWindow(Helpers.LocalizationService.Get("lblError") + ": " + ex.Message, Helpers.LocalizationService.Get("titleHealth"), Helpers.LocalizationService.Get("lblException"));
                 popupEx.Owner = Window.GetWindow(this);
                 popupEx.ShowDialog();
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnCheckDisk_Click(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.UpdateProgress(0, 20, Helpers.LocalizationService.Get("msgScanningDisk"));
            LoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                await Task.Delay(1000);
                LoadingOverlay.UpdateProgress(0, 60, Helpers.LocalizationService.Get("msgGatheringSmart"));
                
                string psScript = $@"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$disks = Get-PhysicalDisk
$resStr = ''
foreach ($d in $disks) {{
    if (!$d) {{ continue }}
    $model = $d.FriendlyName
    $size = [math]::Round($d.Size / 1GB, 1)
    $mediaType = $d.MediaType
    $busType = $d.BusType
    $firmware = $d.FirmwareVersion
    $serial = $d.SerialNumber
    $health = $d.HealthStatus
    $opStatus = if ($d.OperationalStatus) {{ $d.OperationalStatus -join ', ' }} else {{ 'Unknown' }}
    
    $predictFailure = '{Helpers.LocalizationService.Get("lblDiskFailureNo")}'
    $temp = 'N/A'
    $diskNumber = $d.DeviceID
    
    try {{
        $wmiFailPredict = Get-WmiObject -Namespace 'root\wmi' -Class 'MSStorageDriver_FailurePredictStatus' -ErrorAction SilentlyContinue | Where-Object {{ $_.InstanceName -match 'PhysicalDrive' + $diskNumber }}
        if ($wmiFailPredict) {{ $predictFailure = if ($wmiFailPredict.PredictFailure) {{ '{Helpers.LocalizationService.Get("lblDiskFailureYes")}' }} else {{ '{Helpers.LocalizationService.Get("lblDiskFailureNo")}' }} }}
        
        $wmiTemp = Get-WmiObject -Namespace 'root\wmi' -Class 'MSStorageDriver_ATAPISmartData' -ErrorAction SilentlyContinue | Where-Object {{ $_.InstanceName -match 'PhysicalDrive' + $diskNumber }}
        if ($wmiTemp -and $wmiTemp.VendorSpecific) {{
            $vd = $wmiTemp.VendorSpecific
            for ($i = 2; $i -lt $vd.Length - 12; $i += 12) {{
                $attrId = $vd[$i]
                if ($attrId -eq 194 -or $attrId -eq 190) {{
                    $tv = $vd[$i + 5]; if ($tv -gt 0 -and $tv -lt 100) {{ $temp = ""$tv°C""; break }}
                }}
            }}
        }}
    }} catch {{}}
    
    $storageStats = $null
    if ($temp -eq 'N/A') {{
        try {{
            $storageStats = Get-StorageReliabilityCounter -PhysicalDisk $d -ErrorAction SilentlyContinue
            if ($storageStats -and $storageStats.Temperature -ne $null) {{
                $temp = [string]$storageStats.Temperature + '°C'
            }}
        }} catch {{}}
    }}
    
    $resStr += ""[$model]`n""
    $resStr += ""• {Helpers.LocalizationService.Get("lblDiskCapacity")} $size GB`n""
    $resStr += ""• {Helpers.LocalizationService.Get("lblDiskStructure")} $mediaType | {Helpers.LocalizationService.Get("lblDiskInterface")} $busType`n""
    $resStr += ""• Firmware: $firmware | Serial: $serial`n""
    $resStr += ""`n{Helpers.LocalizationService.Get("lblTitleSmart")}`n""
    $resStr += ""• {Helpers.LocalizationService.Get("lblDiskFailurePredict")} $predictFailure`n""
    $resStr += ""• {Helpers.LocalizationService.Get("lblDiskTemp")} $temp`n""
    $resStr += ""• {Helpers.LocalizationService.Get("lblDiskOpStatus")} $opStatus`n""
    
    $healthPct = 100
    $danhGia = '{Helpers.LocalizationService.Get("lblDiskHealthGreat")}'
    if ($health -eq 'Warning') {{ $healthPct = 70; $danhGia = '{Helpers.LocalizationService.Get("lblDiskHealthWarn")}' }} 
    elseif ($health -ne 'Healthy') {{ $healthPct = 30; $danhGia = '{Helpers.LocalizationService.Get("lblDiskHealthDanger")}' }}
    
    try {{
        if (-not $storageStats) {{ $storageStats = Get-StorageReliabilityCounter -PhysicalDisk $d -ErrorAction SilentlyContinue }}
        if ($storageStats -and $storageStats.Wear -ne $null) {{
            if ($storageStats.Wear -ge 0 -and $storageStats.Wear -le 100) {{
                $healthPct = 100 - $storageStats.Wear
                if ($healthPct -ge 80) {{ $danhGia = '{Helpers.LocalizationService.Get("lblDiskHealthGreat")}' }}
                elseif ($healthPct -ge 50) {{ $danhGia = '{Helpers.LocalizationService.Get("lblDiskHealthWarn")}' }}
                else {{ $danhGia = '{Helpers.LocalizationService.Get("lblDiskHealthDanger")}' }}
            }}
        }}
    }} catch {{}}

    $resStr += ""`n{Helpers.LocalizationService.Get("lblTitleHealth")}`n""
    $resStr += ""• {Helpers.LocalizationService.Get("lblDiskHealth")} $healthPct% ($danhGia)`n""
    
    try {{
        if ($storageStats) {{
             $resStr += ""`n{Helpers.LocalizationService.Get("lblTitleStorageDetail")}`n""
             if ($storageStats.PowerOnHours -ne $null) {{
                 $days = [math]::Floor($storageStats.PowerOnHours / 24)
                 $resStr += ""• {Helpers.LocalizationService.Get("lblHoursOn")} $($storageStats.PowerOnHours) h (~$days {Helpers.LocalizationService.Get("lblDays")})`n""
             }}
             if ($storageStats.ReadLatencyMax -ne $null) {{ $resStr += ""• {Helpers.LocalizationService.Get("lblReadLatency")} $($storageStats.ReadLatencyMax) ms`n"" }}
             if ($storageStats.WriteLatencyMax -ne $null) {{ $resStr += ""• {Helpers.LocalizationService.Get("lblWriteLatency")} $($storageStats.WriteLatencyMax) ms`n"" }}
        }}
    }} catch {{}}
    
    $resStr += ""`n""
}}
Write-Output $resStr.Trim()
";
                string reportText = "";
                await Task.Run(() => {
                    string tempScript = Path.Combine(Path.GetTempPath(), "_chkdisk.ps1");
                    File.WriteAllText(tempScript, psScript, Encoding.UTF8);
                    
                    var proc = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScript}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            StandardOutputEncoding = Encoding.UTF8
                        }
                    };
                    proc.Start();
                    reportText = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                });
                
                LoadingOverlay.UpdateProgress(0, 100, Helpers.LocalizationService.Get("msgAnalyzingData"));
                await Task.Delay(500);

                if (string.IsNullOrWhiteSpace(reportText)) { reportText = Helpers.LocalizationService.Get("msgFailDiskReport"); }
                
                var popupDisk = new InfoPopupWindow(reportText.Trim(), Helpers.LocalizationService.Get("lblDiskHealthStatus"));
                popupDisk.Owner = Window.GetWindow(this);
                popupDisk.ShowDialog();
                
                TextDiskHealthStatus.Text = Helpers.LocalizationService.Get("btnHardwareCheckNow");
            }
            catch (Exception ex)
            {
                 var popupEx = new InfoPopupWindow(Helpers.LocalizationService.Get("lblError") + ": " + ex.Message, Helpers.LocalizationService.Get("lblDiskHealthStatus"), Helpers.LocalizationService.Get("lblException"));
                 popupEx.Owner = Window.GetWindow(this);
                 popupEx.ShowDialog();
            }
            finally {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt";
            saveFileDialog.FileName = "SystemInfo.txt";
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveFileDialog.FileName, _fullReport, System.Text.Encoding.UTF8);
            }
        }



        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_fullReport);
            MessageBox.Show(Helpers.LocalizationService.Get("msgHardwareCopied"), Helpers.LocalizationService.Get("titleCopied"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
