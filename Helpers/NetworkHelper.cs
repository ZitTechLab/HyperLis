using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace HyperLisUI.Helpers
{
    internal static class NetworkHelper
    {
        public class DnsProvider
        {
            public string Name { get; set; }
            public string[] DnsV4 { get; set; }
            public string[] DnsV6 { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        internal static List<DnsProvider> DnsProviders = new List<DnsProvider>
        {
            new DnsProvider { Name = "Cloudflare", DnsV4 = new string[] { "1.1.1.1", "1.0.0.1" }, DnsV6 = new string[] { "2606:4700:4700::1111", "2606:4700:4700::1001" } },
            new DnsProvider { Name = "Google", DnsV4 = new string[] { "8.8.8.8", "8.8.4.4" }, DnsV6 = new string[] { "2001:4860:4860::8888", "2001:4860:4860::8844" } },
            new DnsProvider { Name = "Quad9", DnsV4 = new string[] { "9.9.9.9", "149.112.112.112" }, DnsV6 = new string[] { "2620:fe::fe", "2620:fe::9" } },
            new DnsProvider { Name = "AdGuard", DnsV4 = new string[] { "94.140.14.14", "94.140.15.15" }, DnsV6 = new string[] { "2a10:50c0::ad1:ff", "2a10:50c0::ad2:ff" } },
            new DnsProvider { Name = "OpenDNS", DnsV4 = new string[] { "208.67.222.222", "208.67.220.220" }, DnsV6 = new string[] { "2620:0:ccc::2", "2620:0:ccd::2" } },
            new DnsProvider { Name = "CleanBrowsing", DnsV4 = new string[] { "185.228.168.9", "185.228.169.9" }, DnsV6 = new string[] { "2a0d:2a00:1::2", "2a0d:2a00:2::2" } },
            new DnsProvider { Name = "Alternate DNS", DnsV4 = new string[] { "76.76.19.19", "76.223.122.150" }, DnsV6 = new string[] { "2602:fc23:18::19", "2602:fc23:18::150" } },
            new DnsProvider { Name = "Automatic (DHCP)", DnsV4 = new string[] { }, DnsV6 = new string[] { } }
        };

        internal static void SetDNS(string adapterName, DnsProvider provider)
        {
            string nic = adapterName;
            string[] dnsv4 = provider.DnsV4;
            string[] dnsv6 = provider.DnsV6;
            
            // Allow quotes in adapter name if needed, but existing command handles quotes
            // Note: Warehouse logic puts quotes around {nic} in the command string: \"{nic}\"

            // Reset to DHCP if no static DNS provided
            if (dnsv4.Length == 0)
            {
                Utilities.RunCommand($"netsh interface ipv4 set dnsservers \"{nic}\" dhcp");
                // Assuming IsIPv6Enabled logic check is desired, for now we run it always or skip check if helper missing
                // Simplification based on warehouse logic:
                try { Utilities.RunCommand($"netsh interface ipv6 set dnsservers \"{nic}\" dhcp"); } catch { }
                return;
            }

            // Set IPv4
            string cmdv4Primary = $"netsh interface ipv4 set dnsservers \"{nic}\" static {dnsv4[0]} primary";
            Utilities.RunCommand(cmdv4Primary);
            if (dnsv4.Length == 2)
            {
                string cmdv4Alternate = $"netsh interface ipv4 add dnsservers \"{nic}\" {dnsv4[1]} index=2";
                Utilities.RunCommand(cmdv4Alternate);
            }

            // Set IPv6
            try
            {
                if (dnsv6.Length > 0)
                {
                    string cmdv6Primary = $"netsh interface ipv6 set dnsservers \"{nic}\" static {dnsv6[0]} primary";
                    Utilities.RunCommand(cmdv6Primary);
                    if (dnsv6.Length == 2)
                    {
                        string cmdv6Alternate = $"netsh interface ipv6 add dnsservers \"{nic}\" {dnsv6[1]} index=2";
                        Utilities.RunCommand(cmdv6Alternate);
                    }
                }
            }
            catch { }
        }

        internal static void FlushDNSCache()
        {
            Utilities.RunCommand("ipconfig /flushdns");
        }

        public static NetworkInterface[] GetActiveNetworkAdapters()
        {
             return NetworkInterface.GetAllNetworkInterfaces().Where(
                a => a.OperationalStatus == OperationalStatus.Up &&
                (a.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || a.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                a.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily.ToString() == "InterNetwork")).ToArray();
        }

        public static void OpenNetworkConnections()
        {
             try { Process.Start(new ProcessStartInfo("ncpa.cpl") { UseShellExecute = true }); } catch { }
        }

        internal static async Task<PingReply> PingHost(string host)
        {
            using (var pinger = new Ping())
            {
                 // Default timeout 3000ms
                 return await pinger.SendPingAsync(host, 3000);
            }
        }
        
        // Throttling Logic (Kept from previous implementation as it's part of the UI features)
        internal static void DisableNetworkThrottling()
        {
            try
            {
                Int32 tempInt = Convert.ToInt32("ffffffff", 16);
                Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex", tempInt, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Psched", "NonBestEffortLimit", 0, RegistryValueKind.DWord);
            }
            catch { }
        }

        internal static void EnableNetworkThrottling()
        {
            try
            {
                Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex", 10, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Psched", "NonBestEffortLimit", 20, RegistryValueKind.DWord);
            }
            catch { }
        }
        
        internal static bool IsNetworkThrottlingDisabled()
        {
             try
             {
                 var val = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex", 10);
                 if (val is int i && i == -1) return true; // ffffffff as signed int32 is -1
             }
             catch { }
             return false;
        }

        internal static bool IsWifiSenseDisabled()
        {
            try
            {
                var val = Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\PolicyManager\\default\\WiFi\\AllowAutoConnectToWiFiSenseHotspots", "Value", 1);
                if (val is int i && i == 0) return true;
            }
            catch { }
            return false;
        }

        internal static void DisableWifiSense()
        {
            try
            {
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\PolicyManager\default\WiFi\AllowAutoConnectToWiFiSenseHotspots", "Value", 0, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\PolicyManager\default\WiFi\AllowWiFiHotSpotReporting", "Value", 0, RegistryValueKind.DWord);
            }
            catch { }
        }

        internal static void EnableWifiSense()
        {
            try
            {
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\PolicyManager\default\WiFi\AllowAutoConnectToWiFiSenseHotspots", "Value", 1, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\PolicyManager\default\WiFi\AllowWiFiHotSpotReporting", "Value", 1, RegistryValueKind.DWord);
            }
            catch { }
        }

        internal static void ApplyWifiFix()
        {
            try
            {
                string key = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NlaSvc\Parameters\Internet";
                
                // DNS Probe (Google)
                Registry.SetValue(key, "ActiveDnsProbeContent", "8.8.8.8", RegistryValueKind.String);
                Registry.SetValue(key, "ActiveDnsProbeContentV6", "2001:4860:4860::8888", RegistryValueKind.String);
                Registry.SetValue(key, "ActiveDnsProbeHost", "dns.google", RegistryValueKind.String);
                Registry.SetValue(key, "ActiveDnsProbeHostV6", "dns.google", RegistryValueKind.String);

                // Web Probe (Microsoft NCSI)
                Registry.SetValue(key, "ActiveWebProbeContent", "Microsoft NCSI", RegistryValueKind.String);
                Registry.SetValue(key, "ActiveWebProbeContentV6", "Microsoft NCSI", RegistryValueKind.String);
                Registry.SetValue(key, "ActiveWebProbeHost", "www.msftncsi.com", RegistryValueKind.String);
                Registry.SetValue(key, "ActiveWebProbeHostV6", "www.msftncsi.com", RegistryValueKind.String);
                Registry.SetValue(key, "ActiveWebProbePath", "ncsi.txt", RegistryValueKind.String);
                Registry.SetValue(key, "ActiveWebProbePathV6", "ncsi.txt", RegistryValueKind.String);

                // Timers & Settings
                Registry.SetValue(key, "CaptivePortalTimer", 0, RegistryValueKind.DWord);
                Registry.SetValue(key, "CaptivePortalTimerBackOffIncrementsInSeconds", 5, RegistryValueKind.DWord);
                Registry.SetValue(key, "CaptivePortalTimerMaxInSeconds", 30, RegistryValueKind.DWord);
                Registry.SetValue(key, "EnableActiveProbing", 1, RegistryValueKind.DWord);
                Registry.SetValue(key, "PassivePollPeriod", 15, RegistryValueKind.DWord);
                Registry.SetValue(key, "StaleThreshold", 30, RegistryValueKind.DWord);
                Registry.SetValue(key, "WebTimeout", 35, RegistryValueKind.DWord);
            }
            catch { }
        }

        internal static void RevertWifiFix()
        {
            try
            {
                string key = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NlaSvc\Parameters\Internet";

                // Restore Microsoft Defaults
                Registry.SetValue(key, "ActiveDnsProbeContent", "131.107.255.255", RegistryValueKind.String);
                Registry.SetValue(key, "ActiveDnsProbeContentV6", "fd3e:4f5a:5b81::1", RegistryValueKind.String);
                Registry.SetValue(key, "ActiveDnsProbeHost", "dns.msftncsi.com", RegistryValueKind.String);
                Registry.SetValue(key, "ActiveDnsProbeHostV6", "dns.msftncsi.com", RegistryValueKind.String);

                // Rest are usually standard, but ensuring defaults for critical ones
                Registry.SetValue(key, "EnableActiveProbing", 1, RegistryValueKind.DWord);
            }
            catch { }
        }

        internal static bool IsWifiFixApplied()
        {
            try
            {
                var val = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NlaSvc\Parameters\Internet", "ActiveDnsProbeHost", "");
                if (val is string s && s == "dns.google") return true;
            }
            catch { }
            return false;
        }


        public static (string Band, string Signal, string Channel, string SSID, string Authentication) GetWlanInfo()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("netsh", "wlan show interfaces")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null) return ("N/A", "N/A", "N/A", "N/A", "N/A");

                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                string channel = "-";
                string signal = "-";
                string band = "N/A";
                string ssid = "N/A";
                string auth = "N/A";

                using (var reader = new System.IO.StringReader(output))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        // Parse key-value pairs separated by ':'
                        int colonIndex = line.IndexOf(':');
                        if (colonIndex > 0)
                        {
                            string key = line.Substring(0, colonIndex).Trim();
                            string value = line.Substring(colonIndex + 1).Trim();

                            if (key.Equals("Channel", StringComparison.OrdinalIgnoreCase))
                            {
                                channel = value;
                            }
                            else if (key.Equals("Signal", StringComparison.OrdinalIgnoreCase))
                            {
                                signal = value;
                            }
                            else if (key.Equals("SSID", StringComparison.OrdinalIgnoreCase))
                            {
                                ssid = value;
                            }
                            else if (key.Equals("Authentication", StringComparison.OrdinalIgnoreCase))
                            {
                                auth = value;
                            }
                        }
                    }
                }

                if (int.TryParse(channel, out int chNum))
                {
                    if (chNum >= 1 && chNum <= 14) band = "2.4 GHz";
                    else if (chNum > 14) band = "5 GHz";
                }

                return (band, signal, channel, ssid, auth);
            }
            catch
            {
                return ("N/A", "N/A", "N/A", "N/A", "N/A");
            }
        }
    }
}
