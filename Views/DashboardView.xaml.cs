using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Input;
using System.Windows.Threading;
using HyperLisUI.Helpers;

namespace HyperLisUI.Views
{
    public partial class DashboardView : UserControl, INotifyPropertyChanged
    {
        private DispatcherTimer? _cpuRamTimer;
        private DispatcherTimer? _gpuTimer;
        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _ramCounter;

        private static bool _hasInitialized = false;
        private int _cpuRamUpdateInProgress = 0;
        private int _gpuUpdateInProgress = 0;
        private DispatcherTimer? _scrollDebounceTimer;
        private bool _isScrolling = false;
        
        private class GpuInfo { public string Name { get; set; } = ""; public int Index { get; set; } public ulong DedicatedRam { get; set; } public ulong SharedRam { get; set; } public ulong AdapterRam => IsIntegrated ? SharedRam : DedicatedRam; public string Luid { get; set; } = ""; public bool IsIntegrated { get; set; } }
        private System.Collections.Generic.List<GpuInfo> _detectedGpus = new System.Collections.Generic.List<GpuInfo>();
        private int _selectedGpuIndex = 0;
        private string _currentChangelogVersion = "";

        // Events for navigation
        public event EventHandler<string>? NavigateRequested;

#pragma warning disable CS0169, CS0649
        // Backing stubs for removed UI elements to preserve code-behind compatibility
        private readonly TextBlock? lblGreetingTime = null;
        private readonly TextBlock? lblWelcome = null;
        private readonly TextBlock? lblSystemHealth = null;
        private readonly TextBlock? lblScanNow = null;
        private readonly TextBlock? iconScanNow = null;
        private readonly TextBlock? lblJunkFiles = null;
        private readonly TextBlock? lblMinidumps = null;
        private readonly TextBlock? lblRecycleBin = null;
        private readonly TextBlock? lblWindowsUpdates = null;
        private readonly TextBlock? lblUpToDate = null;
        private readonly TextBlock? lblQuickSettings = null;
        private readonly TextBlock? lblApplyAll = null;
        private readonly Button? btnScanNow = null;
        private readonly Button? btnCleanJunk = null;
        private readonly Button? btnCleanMinidumps = null;
        private readonly Button? btnCleanRecycleBin = null;
        private readonly Button? btnCheckUpdates = null;
        private readonly RotateTransform? scanIconRotation = null;
        private readonly System.Windows.Shapes.Ellipse? dotUpdateStatus = null;
        private readonly CheckBox? chkOptimizePerf = null;
        private readonly CheckBox? chkOptimizeNetwork = null;
        private readonly CheckBox? chkNoErrorReports = null;
        private readonly CheckBox? chkDisablePrintFax = null;
        private readonly CheckBox? chkNoStickyKeys = null;
        private readonly CheckBox? chkNoSmartScreen = null;
#pragma warning restore CS0169, CS0649

        public DashboardView()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += DashboardView_Loaded;
            Unloaded += DashboardView_Unloaded;
            LocalizationService.LanguageChanged += ApplyDashboardTranslations;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private DispatcherTimer? _loadingDotsTimer;
        private string _loadingDots = "";
        
        public string LoadingDots
        {
            get => _loadingDots;
            set { _loadingDots = value; OnPropertyChanged(nameof(LoadingDots)); }
        }

        private bool _isLoadingHardware;
        public bool IsLoadingHardware
        {
            get => _isLoadingHardware;
            set { _isLoadingHardware = value; OnPropertyChanged(nameof(IsLoadingHardware)); }
        }

        // System Health Properties
        private string _junkFilesSizeText = "Scanning...";
        public string JunkFilesSizeText
        {
            get => _junkFilesSizeText;
            set { _junkFilesSizeText = value; OnPropertyChanged(nameof(JunkFilesSizeText)); }
        }

        private string _minidumpsCountText = "Scanning...";
        public string MinidumpsCountText
        {
            get => _minidumpsCountText;
            set { _minidumpsCountText = value; OnPropertyChanged(nameof(MinidumpsCountText)); }
        }

        private string _recycleBinSizeText = "Scanning...";
        public string RecycleBinSizeText
        {
            get => _recycleBinSizeText;
            set { _recycleBinSizeText = value; OnPropertyChanged(nameof(RecycleBinSizeText)); }
        }

        // Storage & Drives
        public class DriveDisplayItem : INotifyPropertyChanged
        {
            private string _name = "";
            public string Name
            {
                get => _name;
                set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
            }

            private string _usageText = "";
            public string UsageText
            {
                get => _usageText;
                set { _usageText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UsageText))); }
            }

            private string _percentText = "";
            public string PercentText
            {
                get => _percentText;
                set { _percentText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PercentText))); }
            }

            private double _percentValue;
            public double PercentValue
            {
                get => _percentValue;
                set { _percentValue = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PercentValue))); }
            }

            private string _formatAndType = "";
            public string FormatAndType
            {
                get => _formatAndType;
                set { _formatAndType = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FormatAndType))); }
            }

            public bool IsSystemDrive { get; set; }
            public bool IsNonSystemDrive => !IsSystemDrive;

            public event PropertyChangedEventHandler? PropertyChanged;
        }

        public ObservableCollection<DriveDisplayItem> DrivesList { get; set; } = new ObservableCollection<DriveDisplayItem>();

        // System Overview Properties
        private string _osName = "Windows 11 Pro 23H2";
        public string OsName
        {
            get => _osName;
            set { _osName = value; OnPropertyChanged(nameof(OsName)); }
        }

        private string _machineHostname = Environment.MachineName;
        public string MachineHostname
        {
            get => _machineHostname;
            set { _machineHostname = value; OnPropertyChanged(nameof(MachineHostname)); }
        }

        private string _systemUptime = "2h 34m";
        public string SystemUptime
        {
            get => _systemUptime;
            set { _systemUptime = value; OnPropertyChanged(nameof(SystemUptime)); }
        }

        private string _ipAddress = "172.16.40.18";
        public string IpAddress
        {
            get => _ipAddress;
            set { _ipAddress = value; OnPropertyChanged(nameof(IpAddress)); }
        }

        private string _architecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
        public string Architecture
        {
            get => _architecture;
            set { _architecture = value; OnPropertyChanged(nameof(Architecture)); }
        }

        private string _win11VersionName = "Windows 11 Pro";
        public string Win11VersionName
        {
            get => _win11VersionName;
            set { _win11VersionName = value; OnPropertyChanged(nameof(Win11VersionName)); }
        }

        private string _win11BuildNumber = "23H2 (22631.5257)";
        public string Win11BuildNumber
        {
            get => _win11BuildNumber;
            set { _win11BuildNumber = value; OnPropertyChanged(nameof(Win11BuildNumber)); }
        }

        private string _osLogoSource = "/Images/windows11.png";
        public string OsLogoSource
        {
            get => _osLogoSource;
            set { _osLogoSource = value; OnPropertyChanged(nameof(OsLogoSource)); }
        }

        // Changelog Classes
        public class ChangelogData
        {
            public string? title { get; set; }
            public string? summary_title { get; set; }
            public System.Collections.Generic.List<string>? content { get; set; }
            public string? version_label { get; set; }
            public string? version_value { get; set; }
        }

        public class ChangelogDisplayItem
        {
            public string? title { get; set; }
            public string? summary_title { get; set; }
            public System.Collections.Generic.List<string> content { get; set; } = new System.Collections.Generic.List<string>();
            public string? version_label { get; set; }
            public string? version_value { get; set; }
            public string? date { get; set; }
            public SolidColorBrush? DotBrush { get; set; }
        }

        public ObservableCollection<ChangelogDisplayItem> ChangelogItems { get; set; } = new ObservableCollection<ChangelogDisplayItem>();

        private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateGreeting();
            RefreshToggleStates();
            LoadDrivesInfo();
            LoadSystemOverview();
            _ = ScanSystemHealthAsync();
            LoadChangelog(LocalizationService.CurrentLanguage);
            ApplyDashboardTranslations();

            // Trigger Hardware Scan
            if (!_hasInitialized)
            {
                _hasInitialized = true;
                IsLoadingHardware = true;
                
                // Start Dots Animation
                _loadingDotsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                _loadingDotsTimer.Tick += (s, args) => 
                {
                    if (LoadingDots.Length >= 3) LoadingDots = "";
                    else LoadingDots += ".";
                };
                _loadingDotsTimer.Start();

                // Start fetching static hardware info in background
                var hardwareTask = UpdateHardwareInfoAsync();

                // Initialize Counters in Background
                await Task.Run(() => 
                {
                    try 
                    {
                        if (_cpuCounter == null) 
                        {
                            try 
                            {
                                _cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
                                _cpuCounter.NextValue(); 
                            }
                            catch 
                            {
                                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                                _cpuCounter.NextValue();
                            }
                        }
                        if (_ramCounter == null) _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                        _ramCounter.NextValue();
                    } 
                    catch { }
                });
                
                await hardwareTask;
                await UpdateCpuRamStatsAsync();
                await UpdateGpuStatsAsync();

                _loadingDotsTimer?.Stop();
                IsLoadingHardware = false;
            }
            else if (_cpuCounter == null)
            {
               await Task.Run(() => 
               {
                    try 
                    {
                        if (_cpuCounter == null) 
                        {
                            try 
                            {
                                _cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
                            }
                            catch 
                            {
                                 _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                            }
                            _cpuCounter.NextValue();
                        }
                        if (_ramCounter == null) _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                        _ramCounter.NextValue();
                    } catch {}
               });
            }

            if (_cpuRamTimer == null)
            {
                _cpuRamTimer = new DispatcherTimer();
                _cpuRamTimer.Interval = TimeSpan.FromSeconds(2);
                _cpuRamTimer.Tick += CpuRamTimer_Tick;
            }
            if (_gpuTimer == null)
            {
                _gpuTimer = new DispatcherTimer();
                _gpuTimer.Interval = TimeSpan.FromSeconds(5);
                _gpuTimer.Tick += GpuTimer_Tick;
            }
            _cpuRamTimer.Start();
            _gpuTimer.Start();
        }

        private void DashboardView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_cpuRamTimer != null)
            {
                _cpuRamTimer.Stop();
                _cpuRamTimer = null;
            }
            if (_gpuTimer != null)
            {
                _gpuTimer.Stop();
                _gpuTimer = null;
            }
            _loadingDotsTimer?.Stop();
            _loadingDotsTimer = null;
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();
            _cpuPerformanceCounter?.Dispose();
            _gpuVramCounter?.Dispose();
            _gpuVramCounter = null;
            DisposeGpuCounters();
        }

        private async void CpuRamTimer_Tick(object? sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _cpuRamUpdateInProgress, 1) == 1) return;
            try
            {
                await UpdateCpuRamStatsAsync();
            }
            finally
            {
                Interlocked.Exchange(ref _cpuRamUpdateInProgress, 0);
            }
        }

        private async void GpuTimer_Tick(object? sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _gpuUpdateInProgress, 1) == 1) return;
            try
            {
                await UpdateGpuStatsAsync();
            }
            finally
            {
                Interlocked.Exchange(ref _gpuUpdateInProgress, 0);
            }
        }

        private async Task UpdateCpuRamStatsAsync()
        {
            float cpu = 0;
            float ramUsagePercent = 0;
            float totalRamGB = _totalPhysicalMemory / (1024f * 1024f * 1024f);
            float ramAvailable = 0;
            float cpuPerf = 100;

            await Task.Run(() =>
            {
                try
                {
                    if (_cpuCounter != null)
                    {
                        float rawCpu = _cpuCounter.NextValue();
                        cpu = Math.Min(rawCpu, 100);
                    }
                    
                    if (_ramCounter != null)
                    {
                        ramAvailable = _ramCounter.NextValue();
                        float totalRamMB = _totalPhysicalMemory / (1024f * 1024f); 
                        ramUsagePercent = 0;
                        if (totalRamMB > 0)
                        {
                            ramUsagePercent = ((totalRamMB - ramAvailable) / totalRamMB) * 100f;
                        }
                    }

                    try
                    {
                        if (_cpuPerformanceCounter == null)
                        {
                            _cpuPerformanceCounter = new PerformanceCounter("Processor Information", "% Processor Performance", "_Total");
                        }
                        cpuPerf = _cpuPerformanceCounter.NextValue();
                    }
                    catch { }
                }
                catch { }
            });

            await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                CpuProgress.Value = cpu;
                CpuProgress.Text = $"{Math.Round(cpu)}%";
                if (_maxCpuClock > 0)
                {
                    double currentMhz = _maxCpuClock * (cpuPerf / 100.0);
                    CpuProgress.Subtext = $"{currentMhz / 1000.0:0.0} GHz";
                }
                else
                {
                    CpuProgress.Subtext = "Unknown GHz";
                }

                RamProgress.Value = ramUsagePercent;
                RamProgress.Text = $"{Math.Round(ramUsagePercent)}%";
                float usedGB = totalRamGB - (ramAvailable / 1024f);
                RamProgress.Subtext = $"{usedGB:0.0}/{totalRamGB:0.0} GB";
            }));
        }

        #region DXGI Interop for Hardware GPU Enumeration
        [DllImport("dxgi.dll")]
        private static extern int CreateDXGIFactory1(ref Guid riid, out IntPtr ppFactory);

        [ComImport]
        [Guid("770aae78-f26f-4dba-a829-253c83d1b387")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDXGIFactory1
        {
            int SetPrivateData();
            int SetPrivateDataInterface();
            int GetPrivateData();
            int GetParent();
            int EnumAdapters();
            int MakeWindowAssociation();
            int GetWindowAssociation();
            int CreateSwapChain();
            int CreateSoftwareAdapter();
            int EnumAdapters1(uint Adapter, out IntPtr ppAdapter);
            int IsCurrent();
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DXGI_ADAPTER_DESC1
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string Description;
            public uint VendorId;
            public uint DeviceId;
            public uint SubSysId;
            public uint Revision;
            public UIntPtr DedicatedVideoMemory;
            public UIntPtr DedicatedSystemMemory;
            public UIntPtr SharedSystemMemory;
            public uint LuidLow;
            public int LuidHigh;
            public uint Flags;
        }

        [ComImport]
        [Guid("29038f61-3839-4626-91fd-086879011a05")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDXGIAdapter1
        {
            int SetPrivateData();
            int SetPrivateDataInterface();
            int GetPrivateData();
            int GetParent();
            int EnumOutputs();
            int GetDesc();
            int CheckInterfaceSupport();
            int GetDesc1(out DXGI_ADAPTER_DESC1 pDesc);
        }

        private static System.Collections.Generic.List<GpuInfo> DetectGpus()
        {
            var gpus = new System.Collections.Generic.List<GpuInfo>();
            var seenHw = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. DXGI enumeration (matches Windows Task Manager directly)
            try
            {
                Guid factoryGuid = new Guid("770aae78-f26f-4dba-a829-253c83d1b387");
                if (CreateDXGIFactory1(ref factoryGuid, out IntPtr pFactory) == 0 && pFactory != IntPtr.Zero)
                {
                    var factory = (IDXGIFactory1)Marshal.GetObjectForIUnknown(pFactory);
                    uint adapterIndex = 0;
                    int gpuOrdinal = 0;

                    while (factory.EnumAdapters1(adapterIndex, out IntPtr pAdapter) == 0)
                    {
                        if (pAdapter != IntPtr.Zero)
                        {
                            try
                            {
                                var adapter = (IDXGIAdapter1)Marshal.GetObjectForIUnknown(pAdapter);
                                if (adapter.GetDesc1(out DXGI_ADAPTER_DESC1 desc) == 0)
                                {
                                    // Filter out software / Basic Render Driver (Flags == 2 or Microsoft VendorId 0x1414)
                                    bool isSoftware = (desc.Flags & 2) != 0 || desc.VendorId == 0x1414;
                                    string hwKey = $"{desc.VendorId:X4}_{desc.DeviceId:X4}";

                                    if (!isSoftware && !seenHw.Contains(hwKey))
                                    {
                                        seenHw.Add(hwKey);
                                        string luid = $"0x{desc.LuidHigh:x8}_0x{desc.LuidLow:x8}";
                                        ulong ded = (ulong)desc.DedicatedVideoMemory;
                                        ulong shared = (ulong)desc.SharedSystemMemory;
                                        bool isIntegrated = ded < 1024UL * 1024UL * 1024UL;

                                        gpus.Add(new GpuInfo
                                        {
                                            Name = desc.Description?.Trim() ?? "GPU",
                                            Index = gpuOrdinal++,
                                            DedicatedRam = ded,
                                            SharedRam = shared,
                                            Luid = luid,
                                            IsIntegrated = isIntegrated
                                        });
                                    }
                                }
                            }
                            finally
                            {
                                Marshal.Release(pAdapter);
                            }
                        }
                        adapterIndex++;
                    }
                    Marshal.Release(pFactory);
                }
            }
            catch { }

            // 2. Query WMI to enrich VRAM (e.g. Intel iGPU has small dedicated VRAM reported by DXGI but larger AdapterRAM in WMI)
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, DeviceID, AdapterRAM, PNPDeviceID FROM Win32_VideoController");
                var wmiList = new System.Collections.Generic.List<(string Name, ulong Vram, string Pnp)>();
                foreach (ManagementObject obj in searcher.Get())
                {
                    string pnp = obj["PNPDeviceID"]?.ToString() ?? "";
                    // Filter out non-PCI virtual display adapters (e.g. StarDesk ROOT\\DISPLAY\\0000)
                    if (!pnp.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase)) continue;

                    string name = obj["Name"]?.ToString() ?? "";
                    ulong vram = 0;
                    if (obj["AdapterRAM"] != null) ulong.TryParse(obj["AdapterRAM"]?.ToString(), out vram);
                    wmiList.Add((name, vram, pnp));
                }

                if (gpus.Count > 0)
                {
                    foreach (var g in gpus)
                    {
                        var match = wmiList.Find(w => w.Name.IndexOf(g.Name, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                      g.Name.IndexOf(w.Name, StringComparison.OrdinalIgnoreCase) >= 0);
                        if (!g.IsIntegrated && match.Vram > g.DedicatedRam)
                        {
                            g.DedicatedRam = match.Vram;
                        }
                    }
                }
                else
                {
                    // Fallback to physical PCI devices from WMI if DXGI was completely unavailable
                    int fallbackIdx = 0;
                    foreach (var w in wmiList)
                    {
                        bool isIntelOrAmdApu = w.Name.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
                                               w.Name.Contains("Radeon(TM) Graphics", StringComparison.OrdinalIgnoreCase);
                        gpus.Add(new GpuInfo
                        {
                            Name = w.Name,
                            Index = fallbackIdx++,
                            DedicatedRam = isIntelOrAmdApu ? 0 : w.Vram,
                            SharedRam = 0,
                            Luid = "",
                            IsIntegrated = isIntelOrAmdApu
                        });
                    }
                }
            }
            catch { }

            return gpus;
        }
        #endregion

        private async Task UpdateGpuStatsAsync()
        {
            float gpuUsage = 0;
            string vramSubtext = "0.0/0.0 GB";
            await Task.Run(() =>
            {
                try
                {
                    var currentGpu = _detectedGpus.Find(g => g.Index == _selectedGpuIndex);
                    string luid = currentGpu?.Luid ?? "";

                    gpuUsage = GetGpuUsage(_selectedGpuIndex, luid);
                    vramSubtext = GetGpuVramDisplay(currentGpu);
                }
                catch { }
            });

            await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                GpuProgress.Value = gpuUsage;
                GpuProgress.Text = $"{Math.Round(gpuUsage)}%";
                GpuProgress.Subtext = vramSubtext;
            }));
        }

        private System.Collections.Generic.Dictionary<string, PerformanceCounter> _gpuCounters = new System.Collections.Generic.Dictionary<string, PerformanceCounter>();
        private int _lastGpuIndexScanned = -1;
        private DateTime _lastGpuInstanceRefresh = DateTime.MinValue;
        private static readonly TimeSpan GpuInstanceRefreshInterval = TimeSpan.FromSeconds(20);

        private void DisposeGpuCounters()
        {
            foreach (var counter in _gpuCounters.Values)
            {
                try { counter.Dispose(); } catch { }
            }
            _gpuCounters.Clear();
        }

        private void RefreshGpuCounters(int gpuIndex, string luid)
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var instanceNames = category.GetInstanceNames();
            var currentInstanceNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string filter = !string.IsNullOrEmpty(luid) ? luid.ToLowerInvariant() : $"phys_{gpuIndex}";

            foreach (var name in instanceNames)
            {
                if (!name.ToLowerInvariant().Contains(filter)) continue;
                currentInstanceNames.Add(name);
                if (_gpuCounters.ContainsKey(name)) continue;

                try
                {
                    var pc = new PerformanceCounter("GPU Engine", "Utilization Percentage", name);
                    pc.NextValue();
                    _gpuCounters.Add(name, pc);
                }
                catch { }
            }

            var toRemove = new System.Collections.Generic.List<string>();
            foreach (var key in _gpuCounters.Keys)
            {
                if (!key.ToLowerInvariant().Contains(filter) || !currentInstanceNames.Contains(key))
                {
                    toRemove.Add(key);
                }
            }

            foreach (var key in toRemove)
            {
                _gpuCounters[key].Dispose();
                _gpuCounters.Remove(key);
            }
        }

        private float GetGpuUsage(int gpuIndex, string luid)
        {
            try
            {
                bool shouldRefresh = _lastGpuIndexScanned != gpuIndex ||
                    (DateTime.UtcNow - _lastGpuInstanceRefresh) >= GpuInstanceRefreshInterval;
                if (shouldRefresh)
                {
                    RefreshGpuCounters(gpuIndex, luid);
                    _lastGpuInstanceRefresh = DateTime.UtcNow;
                    _lastGpuIndexScanned = gpuIndex;
                }

                var engineTotals = new System.Collections.Generic.Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

                foreach (var kvp in _gpuCounters)
                {
                    try
                    {
                        float val = kvp.Value.NextValue();
                        string key = kvp.Key;
                        int idx = key.IndexOf("eng_", StringComparison.OrdinalIgnoreCase);
                        string engineId = idx >= 0 ? key.Substring(idx) : key;
                        if (!engineTotals.ContainsKey(engineId)) engineTotals[engineId] = 0;
                        engineTotals[engineId] += val;
                    }
                    catch { }
                }

                float maxLoad = 0;
                foreach (var val in engineTotals.Values)
                {
                    if (val > maxLoad) maxLoad = val;
                }

                return Math.Min(maxLoad, 100);
            }
            catch 
            {
                return 0;
            }
        }

        private PerformanceCounter? _gpuVramCounter;
        private int _lastGpuVramIndex = -1;
        private bool _isCurrentCounterShared = false;

        private string GetGpuVramDisplay(GpuInfo? gpu)
        {
            if (gpu == null) return "0.0/0.0 GB";

            try
            {
                bool useShared = gpu.IsIntegrated;
                string counterName = useShared ? "Shared Usage" : "Dedicated Usage";

                if (_lastGpuVramIndex != gpu.Index || _gpuVramCounter == null || _isCurrentCounterShared != useShared)
                {
                    _lastGpuVramIndex = gpu.Index;
                    _isCurrentCounterShared = useShared;
                    _gpuVramCounter?.Dispose();
                    _gpuVramCounter = null;

                    var category = new PerformanceCounterCategory("GPU Adapter Memory");
                    var instanceNames = category.GetInstanceNames();
                    string filter = !string.IsNullOrEmpty(gpu.Luid) ? gpu.Luid.ToLowerInvariant() : $"phys_{gpu.Index}";

                    foreach (var name in instanceNames)
                    {
                        if (name.ToLowerInvariant().Contains(filter))
                        {
                            _gpuVramCounter = new PerformanceCounter("GPU Adapter Memory", counterName, name);
                            break;
                        }
                    }
                }

                float usedBytes = 0;
                if (_gpuVramCounter != null)
                {
                    usedBytes = _gpuVramCounter.NextValue();
                }

                ulong totalBytes = gpu.IsIntegrated ? gpu.SharedRam : gpu.DedicatedRam;
                if (totalBytes == 0)
                {
                    totalBytes = gpu.IsIntegrated ? (_totalPhysicalMemory / 2) : (4UL * 1024UL * 1024UL * 1024UL);
                }

                double usedGB = usedBytes / (1024.0 * 1024.0 * 1024.0);
                double totalGB = totalBytes / (1024.0 * 1024.0 * 1024.0);

                return $"{usedGB:0.0}/{totalGB:0.0} GB";
            }
            catch
            {
                return "0.0/0.0 GB";
            }
        }
        
        private ulong _totalPhysicalMemory = 0;
        private uint _maxCpuClock = 0;
        private PerformanceCounter? _cpuPerformanceCounter;

        private async Task UpdateHardwareInfoAsync()
        {
             await Task.Run(() =>
            {
                string cpuName = "Unknown CPU";
                string ramInfo = "Unknown RAM";
                
                try
                {
                    // CPU Name
                    using (var searcher = new ManagementObjectSearcher("SELECT Name, MaxClockSpeed FROM Win32_Processor"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            cpuName = obj["Name"]?.ToString()?.Replace("Intel(R) Core(TM) ", "")?.Replace("CPU @", "")?.Trim() ?? "CPU";
                            if (obj["MaxClockSpeed"] != null && uint.TryParse(obj["MaxClockSpeed"].ToString(), out uint clock))
                            {
                                _maxCpuClock = clock;
                            }
                            break; 
                        }
                    }

                    // Total RAM
                    using (var searcher = new ManagementObjectSearcher("SELECT Capacity, Speed, Manufacturer, SMBIOSMemoryType, MemoryType FROM Win32_PhysicalMemory"))
                    {
                        ulong total = 0;
                        string speed = "";
                        string manufacturer = "";
                        int smbiosType = 0;
                        int memType = 0;
                        
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            if (ulong.TryParse(obj["Capacity"]?.ToString(), out ulong cap))
                                total += cap;
                            
                            if (string.IsNullOrEmpty(speed) && obj["Speed"] != null) speed = obj["Speed"]?.ToString() ?? "";
                            if (string.IsNullOrEmpty(manufacturer) && obj["Manufacturer"] != null) manufacturer = obj["Manufacturer"]?.ToString() ?? "";
                            
                            if (smbiosType == 0 && obj["SMBIOSMemoryType"] != null && int.TryParse(obj["SMBIOSMemoryType"]?.ToString(), out int smType))
                                smbiosType = smType;
                                
                            if (memType == 0 && obj["MemoryType"] != null && int.TryParse(obj["MemoryType"]?.ToString(), out int mType))
                                memType = mType;
                        }
                        _totalPhysicalMemory = total;
                        
                        string ddrType = "RAM";
                        if (smbiosType == 20 || memType == 20) ddrType = "DDR";
                        else if (smbiosType == 21 || memType == 21) ddrType = "DDR2";
                        else if (smbiosType == 22 || memType == 22) ddrType = "DDR2 FB-DIMM";
                        else if (smbiosType == 24 || memType == 24) ddrType = "DDR3";
                        else if (smbiosType == 26 || memType == 26) ddrType = "DDR4";
                        else if (smbiosType == 27 || memType == 27) ddrType = "DDR5";
                        else if (smbiosType == 28) ddrType = "LPDDR";
                        else if (smbiosType == 29) ddrType = "LPDDR2";
                        else if (smbiosType == 30) ddrType = "LPDDR3";
                        else if (smbiosType == 31) ddrType = "LPDDR4";
                        else if (smbiosType == 34) ddrType = "LPDDR5";
                        else if (smbiosType == 35) ddrType = "LPDDR5X";
                        else if (!string.IsNullOrEmpty(speed) && int.TryParse(speed, out int spd))
                        {
                            if (spd >= 4800) ddrType = "DDR5";
                            else if (spd >= 2133) ddrType = "DDR4";
                            else if (spd >= 800) ddrType = "DDR3";
                            else ddrType = "DDR";
                        }
                        
                        ramInfo = $"{manufacturer} {ddrType}".Trim(); 
                    }
                    
                    // GPU (DXGI hardware enumeration matching Windows Task Manager)
                    _detectedGpus = DetectGpus();
                    
                    Dispatcher.Invoke(() =>
                    {
                        CpuNameText.Text = cpuName;
                        RamDetailsText.Text = ramInfo;
                        
                        GpuSelector.Items.Clear();
                        foreach(var g in _detectedGpus)
                        {
                            GpuSelector.Items.Add($"GPU {g.Index}");
                        }
                        
                        if (GpuSelector.Items.Count > 0) 
                        {
                            GpuSelector.SelectedIndex = 0;
                            string fullName = _detectedGpus[0].Name;
                            GpuNameText.Text = CleanGpuDisplayName(fullName);
                            GpuSelector.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            GpuSelector.Visibility = Visibility.Collapsed;
                            GpuNameText.Text = "No GPU Detected";
                        }
                    });
                }
                catch { }
            });
        }

        // Quick Settings Properties
        public bool IsOptimizePerformanceEnabled
        {
            get => Helpers.OptimizationHelper.IsOptimized();
            set 
            {
                 if (value) Helpers.OptimizationHelper.EnablePerformanceTweaks();
                 else Helpers.OptimizationHelper.DisablePerformanceTweaks();
                 OnPropertyChanged(nameof(IsOptimizePerformanceEnabled));
            }
        }

        public bool IsDisablePrintFaxEnabled
        {
            get => Helpers.OptimizationHelper.IsPrintServiceDisabled() && Helpers.OptimizationHelper.IsFaxServiceDisabled();
            set 
            {
                if (value) 
                {
                    Helpers.OptimizationHelper.DisablePrintService();
                    Helpers.OptimizationHelper.DisableFaxService();
                }
                else 
                {
                    Helpers.OptimizationHelper.EnablePrintService();
                    Helpers.OptimizationHelper.EnableFaxService();
                }
                OnPropertyChanged(nameof(IsDisablePrintFaxEnabled));
            }
        }

        public bool IsOptimizeNetworkEnabled
        {
            get => Helpers.NetworkHelper.IsNetworkThrottlingDisabled();
            set 
            {
                if (value) Helpers.NetworkHelper.DisableNetworkThrottling();
                else Helpers.NetworkHelper.EnableNetworkThrottling();
                OnPropertyChanged(nameof(IsOptimizeNetworkEnabled));
            }
        }

        public bool IsDisableStickyKeysEnabled
        {
            get => Helpers.OptimizationHelper.IsStickyKeysDisabled();
            set 
            {
                if (value) Helpers.OptimizationHelper.DisableStickyKeys();
                else Helpers.OptimizationHelper.EnableStickyKeys();
                OnPropertyChanged(nameof(IsDisableStickyKeysEnabled));
            }
        }

        public bool IsDisableErrorReportingEnabled
        {
            get => Helpers.OptimizationHelper.IsErrorReportingDisabled();
            set 
            {
                if (value) Helpers.OptimizationHelper.DisableErrorReporting();
                else Helpers.OptimizationHelper.EnableErrorReporting();
                OnPropertyChanged(nameof(IsDisableErrorReportingEnabled));
            }
        }

        public bool IsDisableSmartScreenEnabled
        {
            get => Helpers.OptimizationHelper.IsSmartScreenDisabled();
            set 
            {
                if (value) Helpers.OptimizationHelper.DisableSmartScreen();
                else Helpers.OptimizationHelper.EnableSmartScreen();
                OnPropertyChanged(nameof(IsDisableSmartScreenEnabled));
            }
        }

        public void LoadDrivesInfo()
        {
            try
            {
                DrivesList.Clear();
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType != DriveType.Fixed || !drive.IsReady) continue;

                    double totalGB = drive.TotalSize / (1024.0 * 1024 * 1024);
                    double freeGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                    double usedGB = totalGB - freeGB;
                    double percent = (totalGB > 0) ? (usedGB / totalGB) * 100.0 : 0;

                    string driveLabel = string.IsNullOrEmpty(drive.VolumeLabel) 
                        ? (drive.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase) ? "Local Disk" : "Data") 
                        : drive.VolumeLabel;
                    string driveLetter = drive.Name.TrimEnd('\\');
                    bool isSys = drive.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase);

                    DrivesList.Add(new DriveDisplayItem
                    {
                        Name = $"{driveLabel} ({driveLetter})",
                        UsageText = $"{Math.Round(usedGB)} GB / {Math.Round(totalGB)} GB",
                        PercentText = $"{Math.Round(percent)}%",
                        PercentValue = percent,
                        FormatAndType = $"{drive.DriveFormat} • {(isSys ? "NVMe SSD" : "SSD")}",
                        IsSystemDrive = isSys
                    });
                }
            }
            catch { }
        }

        private void LoadSystemOverview()
        {
            try
            {
                MachineHostname = Environment.MachineName;
                Architecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

                var uptimeSpan = TimeSpan.FromMilliseconds(Environment.TickCount64);
                SystemUptime = $"{(int)uptimeSpan.TotalHours}h {uptimeSpan.Minutes}m";

                string ip = "127.0.0.1";
                try
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var addr in host.AddressList)
                    {
                        if (addr.AddressFamily == AddressFamily.InterNetwork && !addr.ToString().StartsWith("127."))
                        {
                            ip = addr.ToString();
                            break;
                        }
                    }
                }
                catch { }
                IpAddress = ip;

                string productName = "Windows 11 Pro";
                string displayVersion = "23H2";
                string currentBuild = "22631";
                string ubr = "5257";
                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                    if (key != null)
                    {
                        productName = key.GetValue("ProductName")?.ToString() ?? "Windows 11 Pro";
                        displayVersion = key.GetValue("DisplayVersion")?.ToString() ?? "23H2";
                        currentBuild = key.GetValue("CurrentBuild")?.ToString() ?? "22631";
                        var ubrVal = key.GetValue("UBR");
                        if (ubrVal != null) ubr = ubrVal.ToString()!;
                    }
                }
                catch { }

                if (int.TryParse(currentBuild, out int bNum) && bNum >= 22000 && productName.Contains("Windows 10"))
                {
                    productName = productName.Replace("Windows 10", "Windows 11");
                }

                // Shorten overly long Windows edition names to avoid clipping
                string shortenedName = productName
                    .Replace("Home Single Language", "HomeSL")
                    .Replace("Single Language", "SL")
                    .Replace("Pro for Workstations", "Pro Workstations")
                    .Replace("Pro Education", "Pro Edu")
                    .Trim();

                OsName = $"{shortenedName} {displayVersion}".Trim();
                Win11VersionName = shortenedName;
                Win11BuildNumber = $"{displayVersion} ({currentBuild}.{ubr})";
                OsLogoSource = productName.Contains("Windows 10") ? "/Images/windows10.png" : "/Images/windows11.png";
            }
            catch { }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        private static long GetRecycleBinSize()
        {
            long totalSize = 0;
            try
            {
                var info = new SHQUERYRBINFO();
                info.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));
                int hr = SHQueryRecycleBin(null, ref info);
                if (hr == 0)
                {
                    return info.i64Size;
                }

                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                    {
                        var dInfo = new SHQUERYRBINFO();
                        dInfo.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));
                        if (SHQueryRecycleBin(drive.Name, ref dInfo) == 0)
                        {
                            totalSize += dInfo.i64Size;
                        }
                    }
                }
            }
            catch { }
            return totalSize;
        }

        private static string FormatBytesToReadable(long bytes)
        {
            if (bytes <= 0) return "0 MB";
            double mb = bytes / (1024.0 * 1024.0);
            if (mb >= 1024.0)
                return $"{mb / 1024.0:0.0} GB";
            if (mb >= 1.0)
                return $"{Math.Round(mb)} MB";
            return $"{mb:0.1} MB";
        }

        private static long CalculateFolderSizeSafe(string dirPath, int maxItems = 20000, int maxMilliseconds = 1500)
        {
            long size = 0;
            if (!Directory.Exists(dirPath)) return 0;

            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var stack = new System.Collections.Generic.Stack<string>();
                stack.Push(dirPath);
                int itemsCounted = 0;

                while (stack.Count > 0 && itemsCounted < maxItems && sw.ElapsedMilliseconds < maxMilliseconds)
                {
                    string currentDir = stack.Pop();

                    try
                    {
                        var dirInfo = new DirectoryInfo(currentDir);
                        if ((dirInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                            continue;

                        foreach (var file in dirInfo.EnumerateFiles())
                        {
                            try
                            {
                                size += file.Length;
                                itemsCounted++;
                            }
                            catch { }
                        }

                        foreach (var subDir in dirInfo.EnumerateDirectories())
                        {
                            try
                            {
                                if ((subDir.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                                {
                                    stack.Push(subDir.FullName);
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return size;
        }

        private static void CleanFolderSafe(string dirPath)
        {
            if (!Directory.Exists(dirPath)) return;
            try
            {
                var dirInfo = new DirectoryInfo(dirPath);
                foreach (var file in dirInfo.EnumerateFiles())
                {
                    try { file.Delete(); } catch { }
                }
                foreach (var subDir in dirInfo.EnumerateDirectories())
                {
                    try
                    {
                        if ((subDir.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                        {
                            subDir.Delete(true);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void CheckWindowsUpdateStatus()
        {
            try
            {
                bool rebootPending = false;
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired"))
                {
                    if (key != null) rebootPending = true;
                }

                if (!rebootPending)
                {
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending"))
                    {
                        if (key != null) rebootPending = true;
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    if (rebootPending)
                    {
                        if (lblUpToDate != null) lblUpToDate.Text = "Restart required";
                        if (dotUpdateStatus != null) dotUpdateStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                    }
                    else
                    {
                        var upToDate = LocalizationService.Get("lblUpToDate");
                        if (lblUpToDate != null) lblUpToDate.Text = (!string.IsNullOrEmpty(upToDate) && upToDate != "lblUpToDate") ? upToDate : "Up to date";
                        if (dotUpdateStatus != null) dotUpdateStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                    }
                });
            }
            catch { }
        }

        private async Task ScanSystemHealthAsync()
        {
            await Task.Run(() =>
            {
                long junkBytes = 0;
                int minidumps = 0;

                try
                {
                    junkBytes += CalculateFolderSizeSafe(Path.GetTempPath());
                }
                catch { }

                try
                {
                    junkBytes += CalculateFolderSizeSafe(@"C:\Windows\Temp");
                }
                catch { }

                try
                {
                    string miniDir = @"C:\Windows\Minidump";
                    if (Directory.Exists(miniDir))
                    {
                        minidumps = Directory.GetFiles(miniDir, "*.dmp").Length;
                    }
                }
                catch { }

                long recycleBinBytes = GetRecycleBinSize();

                Dispatcher.Invoke(() =>
                {
                    JunkFilesSizeText = FormatBytesToReadable(junkBytes);
                    MinidumpsCountText = $"{minidumps} files";
                    RecycleBinSizeText = FormatBytesToReadable(recycleBinBytes);
                });

                CheckWindowsUpdateStatus();
            });
        }

        private async Task RunCleanActionAsync(Button? btn, Action<string> setSizeText, Func<Task<string>> cleanTask)
        {
            if (btn == null) return;

            string originalText = btn.Content?.ToString() ?? "Clean";
            bool isVi = LocalizationService.CurrentLanguage == "vi";
            string cleaningLabel = isVi ? "Đang dọn..." : "Cleaning...";
            string cleanedLabel = isVi ? "✓ Đã dọn" : "✓ Cleaned";

            btn.IsEnabled = false;
            btn.Content = cleaningLabel;
            setSizeText(cleaningLabel);

            string newSize = "0 MB";
            try
            {
                newSize = await cleanTask();
            }
            catch { }

            setSizeText(newSize);

            // Completed feedback state
            btn.Content = cleanedLabel;
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
            btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
            btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));

            await Task.Delay(1500);

            // Restore normal state
            btn.Content = originalText;
            btn.ClearValue(Button.BackgroundProperty);
            btn.ClearValue(Button.BorderBrushProperty);
            btn.ClearValue(Button.ForegroundProperty);
            btn.IsEnabled = true;
        }

        private async void CleanRecycleBin_Click(object sender, RoutedEventArgs e)
        {
            await RunCleanActionAsync(btnCleanRecycleBin,
                s => RecycleBinSizeText = s,
                async () =>
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            SHEmptyRecycleBin(IntPtr.Zero, null, 7);
                        }
                        catch { }
                    });

                    long remaining = GetRecycleBinSize();
                    return FormatBytesToReadable(remaining);
                });
        }

        private async void CleanJunk_Click(object sender, RoutedEventArgs e)
        {
            await RunCleanActionAsync(btnCleanJunk,
                s => JunkFilesSizeText = s,
                async () =>
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            CleanFolderSafe(Path.GetTempPath());
                            CleanFolderSafe(@"C:\Windows\Temp");
                        }
                        catch { }
                    });

                    long remaining = CalculateFolderSizeSafe(Path.GetTempPath()) + CalculateFolderSizeSafe(@"C:\Windows\Temp");
                    return FormatBytesToReadable(remaining);
                });
        }

        private async void CleanMinidumps_Click(object sender, RoutedEventArgs e)
        {
            await RunCleanActionAsync(btnCleanMinidumps,
                s => MinidumpsCountText = s,
                async () =>
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            string mini = @"C:\Windows\Minidump";
                            if (Directory.Exists(mini))
                            {
                                foreach (var file in Directory.GetFiles(mini, "*.dmp"))
                                {
                                    try { File.Delete(file); } catch { }
                                }
                            }
                        }
                        catch { }
                    });

                    int remaining = 0;
                    try
                    {
                        if (Directory.Exists(@"C:\Windows\Minidump"))
                            remaining = Directory.GetFiles(@"C:\Windows\Minidump", "*.dmp").Length;
                    }
                    catch { }
                    return $"{remaining} files";
                });
        }

        private void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:windowsupdate") { UseShellExecute = true });
            }
            catch
            {
                try
                {
                    Process.Start(new ProcessStartInfo("control", "/name Microsoft.WindowsUpdate") { UseShellExecute = true });
                }
                catch { }
            }
        }

        private async void ScanNow_Click(object sender, RoutedEventArgs e)
        {
            if (btnScanNow == null || !btnScanNow.IsEnabled) return;

            bool isVi = LocalizationService.CurrentLanguage == "vi";
            btnScanNow.IsEnabled = false;

            // Start rotating scan icon
            try
            {
                var rotateAnim = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromSeconds(1),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                scanIconRotation?.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
            }
            catch { }

            if (lblScanNow != null) lblScanNow.Text = isVi ? "Đang quét..." : "Scanning...";

            string scanningLabel = isVi ? "Đang quét..." : "Scanning...";

            // Step 1: Scan Junk Files
            JunkFilesSizeText = scanningLabel;
            long junkBytes = await Task.Run(() =>
            {
                long bytes = 0;
                try { bytes += CalculateFolderSizeSafe(Path.GetTempPath()); } catch { }
                try { bytes += CalculateFolderSizeSafe(@"C:\Windows\Temp"); } catch { }
                return bytes;
            });
            await Task.Delay(350);
            JunkFilesSizeText = FormatBytesToReadable(junkBytes);

            // Step 2: Scan Minidumps
            MinidumpsCountText = scanningLabel;
            int minidumps = await Task.Run(() =>
            {
                int count = 0;
                try
                {
                    string miniDir = @"C:\Windows\Minidump";
                    if (Directory.Exists(miniDir))
                    {
                        count = Directory.GetFiles(miniDir, "*.dmp").Length;
                    }
                }
                catch { }
                return count;
            });
            await Task.Delay(350);
            MinidumpsCountText = $"{minidumps} files";

            // Step 3: Scan Recycle Bin
            RecycleBinSizeText = scanningLabel;
            long recycleBinBytes = await Task.Run(() => GetRecycleBinSize());
            await Task.Delay(350);
            RecycleBinSizeText = FormatBytesToReadable(recycleBinBytes);

            // Step 4: Scan Windows Updates
            if (lblUpToDate != null) lblUpToDate.Text = isVi ? "Đang kiểm tra..." : "Checking...";
            if (dotUpdateStatus != null) dotUpdateStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));
            await Task.Delay(350);
            await Task.Run(() => CheckWindowsUpdateStatus());

            // Stop rotation
            try
            {
                scanIconRotation?.BeginAnimation(RotateTransform.AngleProperty, null);
                if (scanIconRotation != null) scanIconRotation.Angle = 0;
            }
            catch { }

            // Step 5: Completed feedback state
            btnScanNow.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
            btnScanNow.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
            if (lblScanNow != null)
            {
                lblScanNow.Text = isVi ? "✓ Hoàn tất" : "✓ Complete";
                lblScanNow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
            }
            if (iconScanNow != null)
            {
                iconScanNow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
            }

            await Task.Delay(1500);

            // Restore normal state
            btnScanNow.ClearValue(Button.BackgroundProperty);
            btnScanNow.ClearValue(Button.BorderBrushProperty);
            var scanNow = LocalizationService.Get("lblScanNow");
            if (lblScanNow != null)
            {
                lblScanNow.Text = (!string.IsNullOrEmpty(scanNow) && scanNow != "lblScanNow") ? scanNow : "Scan Now";
                lblScanNow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));
            }
            if (iconScanNow != null)
            {
                iconScanNow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));
            }
            btnScanNow.IsEnabled = true;
        }


        private void MoreDrives_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("dfrgui.exe") { UseShellExecute = true });
            }
            catch
            {
                try
                {
                    Process.Start(new ProcessStartInfo("ms-settings:storagesense") { UseShellExecute = true });
                }
                catch { }
            }
        }

        private void FacebookCard_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://www.facebook.com/ZitTechLab") { UseShellExecute = true });
            }
            catch { }
        }

        private void GithubCard_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://github.com/ZitTechLab/HyperLis") { UseShellExecute = true });
            }
            catch { }
        }

        private void WebsiteCard_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://zittechlab.com/") { UseShellExecute = true });
            }
            catch { }
        }

        private void ApplyAllQuickSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsOptimizePerformanceEnabled = true;
                IsOptimizeNetworkEnabled = true;
                IsDisableErrorReportingEnabled = true;
                IsDisablePrintFaxEnabled = true;
                IsDisableStickyKeysEnabled = true;
                IsDisableSmartScreenEnabled = true;
                RefreshToggleStates();
            }
            catch { }
        }

        private void BtnViewAllChangelog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var changelogWindow = new HyperLisUI.Views.Components.ChangelogWindow();
                if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
                {
                    changelogWindow.Owner = Application.Current.MainWindow;
                    changelogWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                changelogWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DashboardView] Error opening ChangelogWindow: {ex.Message}");
            }
        }

        private void UpdateGreeting()
        {
            try
            {
                int hour = DateTime.Now.Hour;
                string greeting = (hour >= 5 && hour < 12) ? "GOOD MORNING ☀️" :
                                  (hour >= 12 && hour < 18) ? "GOOD AFTERNOON ☀️" : "GOOD EVENING ☀️";
                if (lblGreetingTime != null) lblGreetingTime.Text = greeting;
            }
            catch { }
        }

        private void LoadChangelog(string langCode)
        {
            try 
            {
                string fileName = $"{langCode.ToLower()}.json";
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Changelog", fileName);

                if (!File.Exists(path))
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Changelog", "en.json");
                }

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var allVersions = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, ChangelogData>>(json);
                    
                    if (allVersions != null && allVersions.Count > 0)
                    {
                        var sortedKeys = allVersions.Keys.OrderByDescending(k => k).ToList();
                        
                        ChangelogItems.Clear();

                        var dateMap = new System.Collections.Generic.Dictionary<string, string>
                        {
                            { "v1.0.7", "2026-11-05" },
                            { "v1.0.6", "2026-10-20" },
                            { "v1.0.5", "2026-09-12" },
                            { "v1.0.4", "2026-08-03" },
                            { "v1.0.3", "2026-07-15" },
                            { "v1.0.2", "2026-06-20" },
                            { "v1.0.1", "2026-05-10" },
                            { "v1.0.0", "2026-04-01" }
                        };

                        var brushMap = new SolidColorBrush[]
                        {
                            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4")),
                            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                        };

                        int count = 0;
                        foreach (var key in sortedKeys)
                        {
                            if (count >= 2) break;
                            var data = allVersions[key];

                            string dateStr = "2026-10-20";
                            foreach (var kvp in dateMap)
                            {
                                if (key.Contains(kvp.Key))
                                {
                                    dateStr = kvp.Value;
                                    break;
                                }
                            }

                            var fullContent = data.content ?? new System.Collections.Generic.List<string>();
                            var displayContent = fullContent.Take(5).ToList();

                            var displayItem = new ChangelogDisplayItem
                            {
                                title = data.title,
                                summary_title = data.summary_title,
                                content = displayContent,
                                version_label = data.version_label,
                                version_value = data.version_value,
                                date = dateStr,
                                DotBrush = brushMap[count % brushMap.Length]
                            };

                            ChangelogItems.Add(displayItem);
                            count++;
                        }
                    }
                }
            }
            catch { }
        }

        private void GpuSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GpuSelector.SelectedIndex >= 0 && GpuSelector.SelectedIndex < _detectedGpus.Count)
            {
                _selectedGpuIndex = _detectedGpus[GpuSelector.SelectedIndex].Index;
                string fullName = _detectedGpus[GpuSelector.SelectedIndex].Name;
                GpuNameText.Text = CleanGpuDisplayName(fullName);
                _lastGpuIndexScanned = -1;
                _lastGpuInstanceRefresh = DateTime.MinValue;
                _gpuVramCounter?.Dispose();
                _gpuVramCounter = null;
            }
        }

        private static string CleanGpuDisplayName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "Unknown GPU";
            return fullName.Replace("NVIDIA ", "")?.Replace("GeForce ", "")?.Trim() ?? fullName;
        }

        private void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Math.Abs(e.VerticalChange) < 0.1) return;

            if (!_isScrolling)
            {
                _isScrolling = true;
                _cpuRamTimer?.Stop();
                _gpuTimer?.Stop();
            }

            if (_scrollDebounceTimer == null)
            {
                _scrollDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
                _scrollDebounceTimer.Tick += (s, args) =>
                {
                    _scrollDebounceTimer.Stop();
                    _isScrolling = false;
                    _cpuRamTimer?.Start();
                    _gpuTimer?.Start();
                };
            }

            _scrollDebounceTimer.Stop();
            _scrollDebounceTimer.Start();
        }

        private void ChangelogScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scv)
            {
                if ((e.Delta > 0 && scv.VerticalOffset == 0) ||
                    (e.Delta < 0 && scv.VerticalOffset >= scv.ScrollableHeight))
                {
                    e.Handled = true;
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                    {
                        RoutedEvent = UIElement.MouseWheelEvent,
                        Source = sender
                    };
                    MainScrollViewer?.RaiseEvent(eventArg);
                }
            }
        }

        private void RefreshToggleStates()
        {
            OnPropertyChanged(nameof(IsOptimizePerformanceEnabled));
            OnPropertyChanged(nameof(IsDisablePrintFaxEnabled));
            OnPropertyChanged(nameof(IsOptimizeNetworkEnabled));
            OnPropertyChanged(nameof(IsDisableStickyKeysEnabled));
            OnPropertyChanged(nameof(IsDisableErrorReportingEnabled));
            OnPropertyChanged(nameof(IsDisableSmartScreenEnabled));
        }

        private void ApplyDashboardTranslations()
        {
            Dispatcher.Invoke(() =>
            {
                UpdateGreeting();
                string userName = Environment.UserName;
                if (string.IsNullOrWhiteSpace(userName)) userName = "User";

                var welcome = LocalizationService.Get("lblWelcome");
                if (!string.IsNullOrEmpty(welcome) && welcome != "lblWelcome")
                {
                    welcome = welcome.Replace("{0}", userName).Replace("User", userName);
                }
                else
                {
                    welcome = $"Welcome back, {userName}!";
                }

                if (lblWelcome != null) lblWelcome.Text = welcome;

                var fbTitle = LocalizationService.Get("lblFacebookTitle");
                if (lblFacebookTitle != null) lblFacebookTitle.Text = (!string.IsNullOrEmpty(fbTitle) && fbTitle != "lblFacebookTitle") ? fbTitle : "Facebook";
                var fbDesc = LocalizationService.Get("lblFacebookDesc");
                if (lblFacebookDesc != null) lblFacebookDesc.Text = (!string.IsNullOrEmpty(fbDesc) && fbDesc != "lblFacebookDesc") ? fbDesc : "Zịt's Tech Lab Community";

                var ghTitle = LocalizationService.Get("lblGithubTitle");
                if (lblGithubTitle != null) lblGithubTitle.Text = (!string.IsNullOrEmpty(ghTitle) && ghTitle != "lblGithubTitle") ? ghTitle : "GitHub Repository";
                var ghDesc = LocalizationService.Get("lblGithubDesc");
                if (lblGithubDesc != null) lblGithubDesc.Text = (!string.IsNullOrEmpty(ghDesc) && ghDesc != "lblGithubDesc") ? ghDesc : "Open source & contribute";

                var wsTitle = LocalizationService.Get("lblWebsiteTitle");
                if (lblWebsiteTitle != null) lblWebsiteTitle.Text = (!string.IsNullOrEmpty(wsTitle) && wsTitle != "lblWebsiteTitle") ? wsTitle : "Website";
                var wsDesc = LocalizationService.Get("lblWebsiteDesc");
                if (lblWebsiteDesc != null) lblWebsiteDesc.Text = (!string.IsNullOrEmpty(wsDesc) && wsDesc != "lblWebsiteDesc") ? wsDesc : "Official HyperLis homepage";

                var perfTracker = LocalizationService.Get("lblSystemPerformance");
                if (lblPerfTracker != null) lblPerfTracker.Text = (!string.IsNullOrEmpty(perfTracker) && perfTracker != "lblSystemPerformance") ? perfTracker : "System Performance";

                var sysHealth = LocalizationService.Get("lblSystemHealth");
                if (lblSystemHealth != null) lblSystemHealth.Text = (!string.IsNullOrEmpty(sysHealth) && sysHealth != "lblSystemHealth") ? sysHealth : "System Health";

                var scanNow = LocalizationService.Get("lblScanNow");
                if (lblScanNow != null) lblScanNow.Text = (!string.IsNullOrEmpty(scanNow) && scanNow != "lblScanNow") ? scanNow : "Scan Now";

                var junk = LocalizationService.Get("lblJunkFiles");
                if (lblJunkFiles != null) lblJunkFiles.Text = (!string.IsNullOrEmpty(junk) && junk != "lblJunkFiles") ? junk : "Junk Files";

                var mini = LocalizationService.Get("lblMinidumps");
                if (lblMinidumps != null) lblMinidumps.Text = (!string.IsNullOrEmpty(mini) && mini != "lblMinidumps") ? mini : "Minidumps";

                var rec = LocalizationService.Get("lblRecycleBin");
                if (lblRecycleBin != null) lblRecycleBin.Text = (!string.IsNullOrEmpty(rec) && rec != "lblRecycleBin") ? rec : "Recycle Bin";

                var winUp = LocalizationService.Get("lblWindowsUpdates");
                if (lblWindowsUpdates != null) lblWindowsUpdates.Text = (!string.IsNullOrEmpty(winUp) && winUp != "lblWindowsUpdates") ? winUp : "Windows Update";

                var clean = LocalizationService.Get("btnClean");
                if (btnCleanJunk != null) btnCleanJunk.Content = (!string.IsNullOrEmpty(clean) && clean != "btnClean") ? clean : "Clean";
                if (btnCleanMinidumps != null) btnCleanMinidumps.Content = (!string.IsNullOrEmpty(clean) && clean != "btnClean") ? clean : "Clean";
                if (btnCleanRecycleBin != null) btnCleanRecycleBin.Content = (!string.IsNullOrEmpty(clean) && clean != "btnClean") ? clean : "Clean";

                var check = LocalizationService.Get("btnCheck");
                if (btnCheckUpdates != null) btnCheckUpdates.Content = (!string.IsNullOrEmpty(check) && check != "btnCheck") ? check : "Check";

                var upToDate = LocalizationService.Get("lblUpToDate");
                if (lblUpToDate != null) lblUpToDate.Text = (!string.IsNullOrEmpty(upToDate) && upToDate != "lblUpToDate") ? upToDate : "Up to date";

                var storageDrives = LocalizationService.Get("lblStorageDrives");
                if (lblStorageDrives != null) lblStorageDrives.Text = (!string.IsNullOrEmpty(storageDrives) && storageDrives != "lblStorageDrives") ? storageDrives : "Storage & Drives";

                var opt = LocalizationService.Get("btnOptimize");
                if (lblMore != null) lblMore.Text = (!string.IsNullOrEmpty(opt) && opt != "btnOptimize") ? opt : "Optimize";

                var quickSettings = LocalizationService.Get("lblQuickSettings");
                if (lblQuickSettings != null) lblQuickSettings.Text = (!string.IsNullOrEmpty(quickSettings) && quickSettings != "lblQuickSettings") ? quickSettings : "Quick Settings";

                var applyAll = LocalizationService.Get("btnApplyAll");
                if (lblApplyAll != null) lblApplyAll.Text = (!string.IsNullOrEmpty(applyAll) && applyAll != "btnApplyAll") ? applyAll : "Apply All";

                var changelogTitle = LocalizationService.Get("lblChangelog");
                if (ChangelogHeaderTitle != null) ChangelogHeaderTitle.Text = (!string.IsNullOrEmpty(changelogTitle) && changelogTitle != "lblChangelog") ? changelogTitle : "Changelog";

                if (chkOptimizePerf != null && chkOptimizePerf.Content != null) chkOptimizePerf.Content = LocalizationService.Get("chkOptimizePerf");
                if (chkOptimizeNetwork != null && chkOptimizeNetwork.Content != null) chkOptimizeNetwork.Content = LocalizationService.Get("chkNetThrottling");
                if (chkNoErrorReports != null && chkNoErrorReports.Content != null) chkNoErrorReports.Content = LocalizationService.Get("chkNoErrorReports");
                if (chkDisablePrintFax != null && chkDisablePrintFax.Content != null) chkDisablePrintFax.Content = LocalizationService.Get("chkDisablePrintFax");
                if (chkNoStickyKeys != null && chkNoStickyKeys.Content != null) chkNoStickyKeys.Content = LocalizationService.Get("chkNoStickyKeys");
                if (chkNoSmartScreen != null && chkNoSmartScreen.Content != null) chkNoSmartScreen.Content = LocalizationService.Get("chkNoSmartScreen");

                if (lblCPU != null) lblCPU.Text = LocalizationService.Get("lblCPU") ?? "CPU";
                if (lblRAM != null) lblRAM.Text = LocalizationService.Get("lblRAM") ?? "RAM";
                if (lblGPU != null) lblGPU.Text = LocalizationService.Get("lblGPU") ?? "GPU";

                LoadChangelog(LocalizationService.CurrentLanguage);
            });
        }
    }
}
