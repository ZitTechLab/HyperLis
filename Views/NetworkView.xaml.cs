using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HyperLisUI.Helpers;

namespace HyperLisUI.Views
{
    public partial class NetworkView : UserControl
    {
        private System.Windows.Threading.DispatcherTimer _refreshTimer;
        private bool _isFetchingWifi = false;

        public NetworkView()
        {
            InitializeComponent();
            Loaded += NetworkView_Loaded;
            Unloaded += NetworkView_Unloaded;

            Helpers.LocalizationService.LanguageChanged += ApplyTranslations;
            Loaded += (s, e) => ApplyTranslations();

            _refreshTimer = new System.Windows.Threading.DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(1);
            _refreshTimer.Tick += RefreshTimer_Tick;
        }

        private void ApplyTranslations()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ApplyTranslations);
                return;
            }

            // Headers
            lblNetSelectAdapter.Text = Helpers.LocalizationService.Get("lblNetSelectAdapter");
            lblNetPingTool.Text = Helpers.LocalizationService.Get("lblNetPingTool");
            lblNetPingResult.Text = Helpers.LocalizationService.Get("lblNetPingResult");
            lblNetInfoTitle.Text = Helpers.LocalizationService.Get("lblNetInfoTitle");

            lblNetDNSv4.Text = Helpers.LocalizationService.Get("lblNetDNSv4");
            lblNetDNSv6.Text = Helpers.LocalizationService.Get("lblNetDNSv6");

            // Buttons
            btnNetSetDNS.Content = Helpers.LocalizationService.Get("btnNetSetDNS");
            btnNetFlushDNS.Content = Helpers.LocalizationService.Get("btnNetFlushDNS");
            btnNetOpenSettings.Content = Helpers.LocalizationService.Get("btnNetOpenSettings");
            btnNetPing.Content = Helpers.LocalizationService.Get("btnNetPing");
            btnNetExport.Content = Helpers.LocalizationService.Get("btnNetExport");

            // Checkboxes
            CheckThrottling.Content = Helpers.LocalizationService.Get("chkNetThrottling");
            CheckWifiSense.Content = Helpers.LocalizationService.Get("chkNetWifiSense");
            CheckPaidWifi.Content = Helpers.LocalizationService.Get("chkNetPaidWifi");

            // Labels
            lblNetStatus.Text = Helpers.LocalizationService.Get("lblNetStatus");
            lblNetInternet.Text = Helpers.LocalizationService.Get("lblNetInternet");
            lblNetSSID.Text = Helpers.LocalizationService.Get("lblNetSSID");
            lblNetSignal.Text = Helpers.LocalizationService.Get("lblNetSignal");
            lblNetSecurity.Text = Helpers.LocalizationService.Get("lblNetSecurity");
            lblNetType.Text = Helpers.LocalizationService.Get("lblNetType");
            lblNetBand.Text = Helpers.LocalizationService.Get("lblNetBand");
            lblNetSpeed.Text = Helpers.LocalizationService.Get("lblNetSpeed");
            lblNetDataUsage.Text = Helpers.LocalizationService.Get("lblNetDataUsage");
            lblNetIPv4.Text = Helpers.LocalizationService.Get("lblNetIPv4");
            lblNetSubnet.Text = Helpers.LocalizationService.Get("lblNetSubnet");
            lblNetIPv6.Text = Helpers.LocalizationService.Get("lblNetIPv6");
            lblNetMAC.Text = Helpers.LocalizationService.Get("lblNetMAC");
            lblNetGateway.Text = Helpers.LocalizationService.Get("lblNetGateway");
            lblNetDHCP.Text = Helpers.LocalizationService.Get("lblNetDHCP");
            lblNetDNSServer.Text = Helpers.LocalizationService.Get("lblNetDNSServer");

            // Search Placeholder
            if (PingInput.Text == "Type the IP or Domain name..." || PingInput.Text == "Nhập IP hoặc tên miền..." || string.IsNullOrWhiteSpace(PingInput.Text))
            {
                 PingInput.Text = Helpers.LocalizationService.Get("txtPingPlaceholder");
            }
        }

        private void NetworkView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDNS(); // Must load DNS list before adapters, as UpdateNetworkInfo uses it
            LoadAdapters();
            // Load Throttling State
            if (CheckThrottling != null)
                CheckThrottling.IsChecked = NetworkHelper.IsNetworkThrottlingDisabled();
            if (CheckWifiSense != null)
                CheckWifiSense.IsChecked = NetworkHelper.IsWifiSenseDisabled();
            if (CheckPaidWifi != null)
                CheckPaidWifi.IsChecked = NetworkHelper.IsWifiFixApplied();

            _refreshTimer.Start();
        }

        private void NetworkView_Unloaded(object sender, RoutedEventArgs e)
        {
            _refreshTimer.Stop();
        }

        private async void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (ComboAdapters.SelectedItem is NetworkInterface adapter)
            {
                 // 1. Fast Updates (Sync)
                 try
                 {
                     // Speed
                     long speed = adapter.Speed;
                     TxtSpeed.Text = $"{speed / 1000000} Mbps";

                     // Data Usage
                     var stats = adapter.GetIPStatistics();
                     double sentMB = stats.BytesSent / (1024.0 * 1024.0);
                     double recvMB = stats.BytesReceived / (1024.0 * 1024.0);
                     
                     string sentStr = sentMB > 1024 ? $"{sentMB/1024.0:F2} GB" : $"{sentMB:F0} MB";
                     string recvStr = recvMB > 1024 ? $"{recvMB/1024.0:F2} GB" : $"{recvMB:F0} MB";

                     TxtDataDown.Text = $"⬇ {recvStr}";
                     TxtDataUp.Text = $"⬆ {sentStr}";
                 }
                 catch { }

                 // 2. Wifi Info (Async - Avoid overlap)
                 if (adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && !_isFetchingWifi)
                 {
                     // Only fetch if tab is actually visible/loaded? Timer stops on Unloaded so it's fine.
                     _isFetchingWifi = true;
                     try
                     {
                         // Run netsh in background
                         var wlanInfo = await Task.Run(() => NetworkHelper.GetWlanInfo());
                         
                         // Update UI
                         TxtSignal.Text = wlanInfo.Signal;
                         TxtBand.Text = wlanInfo.Band;
                         
                         if (!string.IsNullOrEmpty(wlanInfo.SSID) && wlanInfo.SSID != "N/A")
                            TxtSSID.Text = wlanInfo.SSID;
                         
                         // Optional: Auth might change too
                         TxtSecurity.Text = wlanInfo.Authentication;
                     }
                     catch { }
                     finally
                     {
                         _isFetchingWifi = false;
                     }
                 }
            }
        }

        private void LoadAdapters()
        {
            if (ComboAdapters == null) return;
            
            try
            {
                var adapters = NetworkHelper.GetActiveNetworkAdapters();
                ComboAdapters.ItemsSource = adapters;
                if (adapters != null && adapters.Length > 0)
                {
                    ComboAdapters.SelectedIndex = 0;
                    // Trigger update manually for first item
                    UpdateNetworkInfo(adapters[0]);
                }
            }
            catch { }
        }

        private void LoadDNS()
        {
             if (ComboDNS == null) return;
             // Create a new list with a placeholder at the top
             var providers = new System.Collections.Generic.List<NetworkHelper.DnsProvider>();
             providers.Add(new NetworkHelper.DnsProvider { Name = "Select one DNS", DnsV4 = new string[]{}, DnsV6 = new string[]{} });
             providers.AddRange(NetworkHelper.DnsProviders);
             
             ComboDNS.ItemsSource = providers;
             ComboDNS.SelectedIndex = 0; 
        }

        private void ComboAdapters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboAdapters.SelectedItem is NetworkInterface adapter)
            {
                UpdateNetworkInfo(adapter);
            }
        }

        private void UpdateNetworkInfo(NetworkInterface adapter)
        {
            if (adapter == null) return;
            
            try
            {
                var props = adapter.GetIPProperties();
                
                // Status
                TxtStatus.Text = adapter.OperationalStatus.ToString();
                TxtStatus.Foreground = adapter.OperationalStatus == OperationalStatus.Up ? Brushes.Green : Brushes.Red;

                // Internet Access (Approximation)
                TxtInternet.Text = adapter.OperationalStatus == OperationalStatus.Up ? "Available" : "Disconnected";

                // SSID (Cannot get via standard .NET without WlanAPI, use Description or Name as proxy)
                // Default fallback if not wifi or checks fail
                TxtSSID.Text = adapter.Name; 
                
                // Signal & Band & SSID & Security - Initial Load
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                     // We let the timer handle the continuous updates, but initial load needs one call
                     // Use Task.Run to avoid freezing initial UI load
                     Task.Run(() => 
                     {
                         var wlanInfo = NetworkHelper.GetWlanInfo();
                         Dispatcher.Invoke(() => 
                         {
                             TxtSignal.Text = wlanInfo.Signal;
                             TxtBand.Text = wlanInfo.Band;
                             
                             if (!string.IsNullOrEmpty(wlanInfo.SSID) && wlanInfo.SSID != "N/A")
                                TxtSSID.Text = wlanInfo.SSID;
                             
                             TxtSecurity.Text = wlanInfo.Authentication;
                         });
                     });
                }
                else
                {
                     TxtSignal.Text = "N/A (Wired)";
                     TxtBand.Text = "N/A";
                     TxtSecurity.Text = "N/A";
                }

                // Connection Type
                TxtType.Text = adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? "Wi-Fi" : 
                               (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? "Ethernet" : adapter.NetworkInterfaceType.ToString());

                // Speed
                long speed = adapter.Speed;
                TxtSpeed.Text = $"{speed / 1000000} Mbps";

                // Data Usage
                var stats = adapter.GetIPStatistics();
                double sentMB = stats.BytesSent / (1024.0 * 1024.0);
                double recvMB = stats.BytesReceived / (1024.0 * 1024.0);
                
                // Format nicely (GB if > 1024)
                string sentStr = sentMB > 1024 ? $"{sentMB/1024.0:F2} GB" : $"{sentMB:F0} MB";
                string recvStr = recvMB > 1024 ? $"{recvMB/1024.0:F2} GB" : $"{recvMB:F0} MB";

                TxtDataDown.Text = $"⬇ {recvStr}";
                TxtDataUp.Text = $"⬆ {sentStr}";

                // IP Info
                var ipv4 = props.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                TxtIPv4.Text = ipv4?.Address.ToString() ?? "-";
                TxtSubnet.Text = ipv4?.IPv4Mask.ToString() ?? "-";

                var ipv6 = props.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
                TxtIPv6.Text = ipv6?.Address.ToString() ?? "-";

                // MAC
                TxtMAC.Text = string.Join(":", adapter.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));

                // Gateway
                var gateway = props.GatewayAddresses.FirstOrDefault();
                TxtGateway.Text = gateway?.Address.ToString() ?? "-";

                // DHCP
                var dhcp = props.DhcpServerAddresses.FirstOrDefault();
                TxtDHCP.Text = dhcp?.ToString() ?? "-";
                
                // DNS (Right Panel) - Show IPv4 Only
                var dns = props.DnsAddresses;
                var dnsOnlyV4 = dns.Where(d => d.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                TxtDNSServer.Text = dnsOnlyV4.Any() ? string.Join(", ", dnsOnlyV4) : "-";
                
                // DNS (Left Panel - Settings Display)
                // Filter IPv4 and IPv6
                var dnsV4 = dns.Where(d => d.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToList();
                var dnsV6 = dns.Where(d => d.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6).ToList();

                TxtDnsV4_1.Text = dnsV4.Count > 0 ? dnsV4[0].ToString() : "-";
                TxtDnsV4_2.Text = dnsV4.Count > 1 ? dnsV4[1].ToString() : "-";

                TxtDnsV6_1.Text = dnsV6.Count > 0 ? dnsV6[0].ToString() : "-";
                TxtDnsV6_2.Text = dnsV6.Count > 1 ? dnsV6[1].ToString() : "-";

                // Sync DNS ComboBox
                ComboDNS.SelectedIndex = 0; // Default to "Select one DNS"
                if (dnsV4.Count > 0)
                {
                    string currentPrimary = dnsV4[0].ToString();
                    foreach (var item in ComboDNS.ItemsSource)
                    {
                        if (item is NetworkHelper.DnsProvider provider && provider.DnsV4 != null && provider.DnsV4.Length > 0)
                        {
                            if (provider.DnsV4[0] == currentPrimary)
                            {
                                ComboDNS.SelectedItem = provider;
                                break;
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                // Fail silently
            }
        }

        private async void SetDNS_Click(object sender, RoutedEventArgs e)
        {
            if (ComboDNS.SelectedItem is NetworkHelper.DnsProvider dns)
            {
                if (dns.Name == "Select one DNS")
                {
                    HyperLisUI.Views.Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgDNSValidProvider"), Helpers.LocalizationService.Get("msgWarning"));
                    return;
                }
                
                try
                {
                    if (ComboAdapters.SelectedItem is NetworkInterface adapter)
                    {
                        NetworkHelper.SetDNS(adapter.Name, dns);
                        HyperLisUI.Views.Components.CustomMessageBoxWindow.Show(string.Format(Helpers.LocalizationService.Get("msgDNSSetSuccess"), dns.Name, adapter.Name), Helpers.LocalizationService.Get("msgSuccess"));
                    }
                    else
                    {
                        HyperLisUI.Views.Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgDNSSelectAdapter"), Helpers.LocalizationService.Get("msgWarning"));
                    }
                    
                    // Refresh Display
                    // Wait a bit for netsh to apply
                    await Task.Delay(1000); 
                    
                    // Refresh Adapter List to get new props or just re-get info for current adapter
                    if (ComboAdapters.SelectedItem is NetworkInterface currentAdapter)
                    {
                        // Re-fetch interface object to get updated properties
                         var refreshedAdapter = NetworkHelper.GetActiveNetworkAdapters().FirstOrDefault(a => a.Id == currentAdapter.Id);
                         if (refreshedAdapter != null)
                         {
                             UpdateNetworkInfo(refreshedAdapter);
                         }
                    }
                }
                catch (Exception ex)
                {
                    HyperLisUI.Views.Components.CustomMessageBoxWindow.Show(string.Format(Helpers.LocalizationService.Get("msgDNSFailed"), ex.Message), Helpers.LocalizationService.Get("msgError"));
                }
            }
            else
            {
                HyperLisUI.Views.Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgDNSValidProvider"), Helpers.LocalizationService.Get("msgWarning"));
            }
        }

        private async void FlushDNS_Click(object sender, RoutedEventArgs e)
        {
             try
             {
                 NetworkHelper.FlushDNSCache();
                 HyperLisUI.Views.Components.CustomMessageBoxWindow.Show(Helpers.LocalizationService.Get("msgDNSFlushed"), Helpers.LocalizationService.Get("msgSuccess"));
                 
                 // Refresh DNS display just in case
                 await Task.Delay(500);
                 if (ComboAdapters.SelectedItem is NetworkInterface currentAdapter)
                 {
                      var refreshedAdapter = NetworkHelper.GetActiveNetworkAdapters().FirstOrDefault(a => a.Id == currentAdapter.Id);
                      if (refreshedAdapter != null) UpdateNetworkInfo(refreshedAdapter);
                 }
             }
             catch (Exception ex)
             {
                 HyperLisUI.Views.Components.CustomMessageBoxWindow.Show(string.Format(Helpers.LocalizationService.Get("msgFlushFailed"), ex.Message), Helpers.LocalizationService.Get("msgError"));
             }
        }
        
        private void OpenNetwork_Click(object sender, RoutedEventArgs e)
        {
            NetworkHelper.OpenNetworkConnections();
        }

        private async void Ping_Click(object sender, RoutedEventArgs e)
        {
            if (PingInput == null) return;
            
            string host = PingInput.Text.Trim();
            if (string.IsNullOrEmpty(host) || host == "Type the IP or Domain name...") return;
            
            // Populate PingResultPanel dynamically
            if (PingResultPanel != null)
            {
                PingResultPanel.Children.Clear();
                PingResultPanel.Children.Add(new TextBlock { Text = string.Format(Helpers.LocalizationService.Get("msgPinging"), host), Margin = new Thickness(0,0,0,15), Foreground = Brushes.Black });
                
                try 
                {
                    // Ping 4 times for a standard feel
                    long totalTime = 0;
                    long min = long.MaxValue;
                    long max = 0;
                    int successCount = 0;

                    for(int i=0; i<8; i++)
                    {
                        var reply = await NetworkHelper.PingHost(host);
                        
                        string status = reply.Status == IPStatus.Success ? Helpers.LocalizationService.Get("msgPingSuccess") : Helpers.LocalizationService.Get("msgPingFailed");
                        string line = $"{status} - LATENCY: {reply.RoundtripTime} ms - TTL: {reply.Options?.Ttl ?? 0}";
                        PingResultPanel.Children.Add(new TextBlock { Text = line, Foreground = Brushes.Black });
                        
                        if (reply.Status == IPStatus.Success)
                        {
                            successCount++;
                            totalTime += reply.RoundtripTime;
                            if (reply.RoundtripTime < min) min = reply.RoundtripTime;
                            if (reply.RoundtripTime > max) max = reply.RoundtripTime;
                        }
                        await Task.Delay(500);
                    }
                    
                    PingResultPanel.Children.Add(new TextBlock { Text = "", Margin = new Thickness(0,10,0,0) });
                    if (successCount > 0)
                        PingResultPanel.Children.Add(new TextBlock { Text = $"Min = {min}, Max = {max}, Average = {totalTime/successCount}", FontWeight = FontWeights.Bold, Foreground = Brushes.Black });
                    else
                        PingResultPanel.Children.Add(new TextBlock { Text = Helpers.LocalizationService.Get("msgPingTimeout"), FontWeight = FontWeights.Bold, Foreground = Brushes.Red });

                }
                catch (Exception ex)
                {
                     PingResultPanel.Children.Add(new TextBlock { Text = string.Format(Helpers.LocalizationService.Get("msgPingError"), ex.Message), Foreground = Brushes.Red });
                }
            }
        }

        private void CheckThrottling_Checked(object sender, RoutedEventArgs e)
        {
            NetworkHelper.DisableNetworkThrottling();
        }

        private void CheckThrottling_Unchecked(object sender, RoutedEventArgs e)
        {
            NetworkHelper.EnableNetworkThrottling();
        }

        private void CheckWifiSense_Checked(object sender, RoutedEventArgs e)
        {
            NetworkHelper.DisableWifiSense();
        }

        private void CheckWifiSense_Unchecked(object sender, RoutedEventArgs e)
        {
            NetworkHelper.EnableWifiSense();
        }

        private void CheckPaidWifi_Checked(object sender, RoutedEventArgs e)
        {
            NetworkHelper.ApplyWifiFix();
        }

        private void CheckPaidWifi_Unchecked(object sender, RoutedEventArgs e)
        {
            NetworkHelper.RevertWifiFix();
        }

        private void PingInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (PingInput.Text == Helpers.LocalizationService.Get("txtPingPlaceholder"))
            {
                PingInput.Text = "";
                PingInput.Foreground = Brushes.Black;
            }
        }

        private void PingInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PingInput.Text))
            {
                PingInput.Text = Helpers.LocalizationService.Get("txtPingPlaceholder");
                PingInput.Foreground = Brushes.Gray;
            }
        }

        private void ExportPing_Click(object sender, RoutedEventArgs e)
        {
            if (PingResultPanel == null || PingResultPanel.Children.Count == 0)
            {
                HyperLisUI.Views.Components.CustomMessageBoxWindow.Show(
                    Helpers.LocalizationService.Get("msgExportNoPingData") ?? "No ping data to export. Please run a ping first.",
                    Helpers.LocalizationService.Get("msgWarning") ?? "Warning");
                return;
            }

            // Collect all text from PingResultPanel
            var lines = new System.Collections.Generic.List<string>();
            lines.Add($"═══════════════════════════════════════════");
            lines.Add($"  HyperLis - Ping Export");
            lines.Add($"  Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            lines.Add($"═══════════════════════════════════════════");
            lines.Add("");

            foreach (var child in PingResultPanel.Children)
            {
                if (child is TextBlock tb)
                {
                    lines.Add(tb.Text);
                }
            }

            lines.Add("");
            lines.Add($"═══════════════════════════════════════════");

            // Show Save File Dialog
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Ping Results",
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = $"PingResult_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                DefaultExt = ".txt"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    System.IO.File.WriteAllLines(dlg.FileName, lines);
                    HyperLisUI.Views.Components.CustomMessageBoxWindow.Show(
                        string.Format(Helpers.LocalizationService.Get("msgExportSuccess") ?? "Exported successfully to:\n{0}", dlg.FileName),
                        Helpers.LocalizationService.Get("msgSuccess") ?? "Success");
                }
                catch (Exception ex)
                {
                    HyperLisUI.Views.Components.CustomMessageBoxWindow.Show(
                        string.Format(Helpers.LocalizationService.Get("msgExportFailed") ?? "Export failed: {0}", ex.Message),
                        Helpers.LocalizationService.Get("msgError") ?? "Error");
                }
            }
        }
    }
}
