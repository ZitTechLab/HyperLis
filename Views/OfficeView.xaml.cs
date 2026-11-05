using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using HyperLisUI.Helpers;

namespace HyperLisUI.Views
{
    public partial class OfficeView : UserControl
    {
        public class OfficeProductInfo
        {
            public string name { get; set; }
            public string productid { get; set; }
            public string channel { get; set; }
            public string pidkey { get; set; }
        }

        private Dictionary<string, OfficeProductInfo> _allProducts = new();
        private bool _isLoaded = false;
        private string _currentXml = "";
        private static readonly string[] OfficeFilesRootCandidates = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files"),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Warehouse\MSEDU\msoffice-main\Files")),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Warehouse\MSEDU\msoffice-main\Files")),
            @"E:\GGAI\Design\Warehouse\MSEDU\msoffice-main\Files"
        };

        public OfficeView()
        {
            InitializeComponent();
            Loaded += OfficeView_Loaded;
        }

        private async void OfficeView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded) return;
            _isLoaded = true;

            ApplyTranslations();
            LocalizationService.LanguageChanged += ApplyTranslations;

            await LoadOfficeProductsAsync();
            UpdateSubVersions();
        }

        private void ApplyTranslations()
        {
            lblOfficeTabTitle.Text = LocalizationService.Get("lblOfficeTabTitle") ?? "Office Settings";
            lblOfficeSection1.Text = LocalizationService.Get("lblOfficeSection1") ?? "1. Microsoft 365 / Office";
            lblOfficeSection2.Text = LocalizationService.Get("lblOfficeSection2") ?? "2. Detailed Version";
            lblOfficeSection3.Text = LocalizationService.Get("lblOfficeSection3") ?? "3. Architecture";
            lblOfficeSection4.Text = LocalizationService.Get("lblOfficeSection4") ?? "4. Update Channel";
            lblOfficeSection5.Text = LocalizationService.Get("lblOfficeSection5") ?? "5. Applications";
            lblOfficeSection6.Text = LocalizationService.Get("lblOfficeSection6") ?? "6. Language";
            lblOfficeSection7.Text = LocalizationService.Get("lblOfficeSection7") ?? "7. License Type";
            lblOfficeSection8.Text = LocalizationService.Get("lblOfficeSection8") ?? "8. Advanced Options";
            
            lblQuickSelect.Text = LocalizationService.Get("lblQuickSelect") ?? "Quick Select:";
            btnSelectMin.Content = LocalizationService.Get("btnSelectMin") ?? "Minimum";
            btnSelectRecommended.Content = LocalizationService.Get("btnSelectRecommended") ?? "Recommended";
            btnSelectFull.Content = LocalizationService.Get("btnSelectFull") ?? "Full";
            btnSelectNone.Content = LocalizationService.Get("btnSelectNone") ?? "Unselect All";

            lblOfficeSection9.Text = LocalizationService.Get("lblOfficeSection9") ?? "9. Configuration XML Preview";
            txtCopyXml.Text = LocalizationService.Get("txtCopyXml") ?? "Copy";

            lblOfficeSection10.Text = LocalizationService.Get("lblOfficeSection10") ?? "10. Configuration Overview";
            lblOverviewTitleVersion.Text = LocalizationService.Get("lblOverviewTitleVersion") ?? "Version:";
            lblOverviewTitleArch.Text = LocalizationService.Get("lblOverviewTitleArch") ?? "Architecture:";
            lblOverviewTitleChannel.Text = LocalizationService.Get("lblOverviewTitleChannel") ?? "Channel:";
            lblOverviewTitleLang.Text = LocalizationService.Get("lblOverviewTitleLang") ?? "Language:";
            lblOverviewTitleApps.Text = LocalizationService.Get("lblOverviewTitleApps") ?? "Apps:";
            lblOverviewTitleOpts.Text = LocalizationService.Get("lblOverviewTitleOpts") ?? "Options:";

            lblOfficeSection11.Text = LocalizationService.Get("lblOfficeSection11") ?? "11. Actions";
            txtDownloadOffice.Text = LocalizationService.Get("txtDownloadOffice") ?? "Download";
            txtInstallOffice.Text = LocalizationService.Get("btnInstallOffice") ?? "Install";
            txtExportXml.Text = LocalizationService.Get("btnExportXml") ?? "Export XML";
            lblOfficeSection12.Text = GetLocalizedText("lblOfficeSection12", "12. Uninstall Office");
            txtUninstallWarning.Text = GetLocalizedText("txtUninstallWarning", "Remove Microsoft Office, Visio, and Project currently installed on this machine.");
            chkConfirmUninstall.Content = GetLocalizedText("chkConfirmUninstall", "I understand this action will uninstall all Office products");
            txtUninstallOffice.Text = GetLocalizedText("btnUninstallOffice", "Uninstall Office");
            
            lblInternetReq.Text = LocalizationService.Get("lblInternetReq") ?? "* Internet connection required to download and install.";

            chkRemoveOffice.Content = LocalizationService.Get("chkRemoveOffice") ?? "Remove existing Office";
            chkCreateDesktopShortcut.Content = LocalizationService.Get("chkCreateDesktopShortcut") ?? "Create Desktop Shortcut";

            // Cáº­p nháº­t tooltip theo ngÃ´n ngá»¯ má»›i náº¿u Ä‘ang á»Ÿ cháº¿ Ä‘á»™ 365
            if (rdoLicenseVolume.IsEnabled == false)
            {
                rdoLicenseVolume.ToolTip = LocalizationService.Get("ttNoVolumeFor365");
            }

            // LÃ m má»›i giÃ¡ trá»‹ Ä‘á»™ng trong Overview khi Ä‘á»•i ngÃ´n ngá»¯
            GenerateXmlPreview();
            UpdateUninstallButtonState();
        }

        private static string GetLocalizedText(string key, string fallback)
        {
            string? localizedValue = LocalizationService.Get(key);
            if (string.IsNullOrWhiteSpace(localizedValue) || string.Equals(localizedValue, key, StringComparison.OrdinalIgnoreCase))
            {
                return fallback;
            }

            return localizedValue;
        }

        private static string FormatLocalizedText(string key, string fallback, params object[] args)
        {
            string template = GetLocalizedText(key, fallback);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        private static string FormatExitCode(int exitCode)
        {
            return $"{exitCode} (0x{unchecked((uint)exitCode):X8})";
        }

        private async System.Threading.Tasks.Task LoadOfficeProductsAsync()
        {
            _allProducts.Clear();
            
            void AddProduct(string key, string name, string id, string channel = "Current")
            {
                _allProducts[key] = new OfficeProductInfo { name = name, productid = id, channel = channel, pidkey = "" };
            }

            // Microsoft 365
            AddProduct("365_Home", "Microsoft 365 Home", "O365HomePremRetail");
            AddProduct("365_Business", "Microsoft 365 Apps for Business", "O365BusinessRetail");
            AddProduct("365_Enterprise", "Microsoft 365 Apps for Enterprise", "O365ProPlusRetail");

            // Office 2024
            AddProduct("2024_Pro", "Office LTSC Pro Plus 2024", "ProPlus2024Volume");
            AddProduct("2024_Std", "Office LTSC Standard 2024", "Standard2024Volume");
            AddProduct("2024_HomeBusiness", "Office HomeBusiness 2024", "HomeBusiness2024Retail");
            AddProduct("2024_HomeStudent", "Office HomeStudent LTSC 2024", "HomeStudent2024Retail");

            // Office 2021
            AddProduct("2021_Pro", "Office LTSC Pro Plus 2021", "ProPlus2021Volume");
            AddProduct("2021_Std", "Office LTSC Standard 2021", "Standard2021Volume");
            AddProduct("2021_HomeBusiness", "Office HomeBusiness 2021", "HomeBusiness2021Retail");
            AddProduct("2021_HomeStudent", "Office HomeStudent LTSC 2021", "HomeStudent2021Retail");

            // Office 2019
            AddProduct("2019_Pro", "Office 2019 Professional Plus", "ProPlus2019Volume");
            AddProduct("2019_Std", "Office 2019 Standard", "Standard2019Volume");
            AddProduct("2019_HomeBusiness", "Office HomeBusiness 2019", "HomeBusiness2019Retail");
            AddProduct("2019_HomeStudent", "Office HomeStudent 2019", "HomeStudent2019Retail");

            // Office 2016
            AddProduct("2016_Pro", "Office 2016 Professional Plus", "ProfessionalRetail");
            AddProduct("2016_Std", "Office 2016 Standard", "StandardRetail");

            // Office 2013
            AddProduct("2013_Pro", "Office 2013 Professional Plus", "ProfessionalRetail");
            AddProduct("2013_Std", "Office 2013 Standard", "StandardRetail");

            await System.Threading.Tasks.Task.CompletedTask;
        }

        private void OfficeVersion_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            UpdateSubVersions();
        }

        private void UpdateSubVersions()
        {
            PanelOfficeSubVersion.Children.Clear();

            string keyword = "";
            if (rbOffice365.IsChecked == true) keyword = "365";
            else if (rbOffice2024.IsChecked == true) keyword = "2024";
            else if (rbOffice2021.IsChecked == true) keyword = "2021";
            else if (rbOffice2019.IsChecked == true) keyword = "2019";
            else if (rbOffice2016.IsChecked == true) keyword = "2016";
            else if (rbOffice2013.IsChecked == true) keyword = "2013";

            var filteredProducts = _allProducts.Values
                .Where(p => p.name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();

            bool isFirst = true;
            foreach (var product in filteredProducts)
            {
                var rb = new RadioButton
                {
                    Content = product.name,
                    Tag = product,
                    Margin = new Thickness(0, 0, 10, 10),
                    IsChecked = isFirst,
                    Style = (Style)Application.Current.FindResource("ModernRadioButtonStyle")
                };
                rb.Checked += Config_Changed;
                PanelOfficeSubVersion.Children.Add(rb);
                isFirst = false;
            }

            // Xá»­ lÃ½ vÃ´ hiá»‡u hÃ³a Volume Ä‘á»‘i vá»›i Microsoft 365 (Chá»‰ há»— trá»£ Retail)
            if (keyword == "365")
            {
                rdoLicenseRetail.IsChecked = true;
                rdoLicenseVolume.IsEnabled = false;
                rdoLicenseVolume.ToolTip = LocalizationService.Get("ttNoVolumeFor365");
            }
            else
            {
                rdoLicenseVolume.IsEnabled = true;
                rdoLicenseVolume.ToolTip = null;
            }

            UpdateChannels();
            GenerateXmlPreview();
        }

        private void UpdateChannels()
        {
            // Update channel list based on selected architecture or product if needed.
            // For now, statically add some basic channels or dynamically from the product.
            var selectedProduct = GetSelectedProduct();
            if (selectedProduct == null) return;

            cboUpdateChannel.Items.Clear();
            
            // Just add the channel recommended by the product
            var cboItem = new ComboBoxItem { Content = selectedProduct.channel, Tag = selectedProduct.channel, IsSelected = true };
            cboUpdateChannel.Items.Add(cboItem);
            
            // Add other standard channels
            if (selectedProduct.channel != "Current")
                cboUpdateChannel.Items.Add(new ComboBoxItem { Content = "Current", Tag = "Current" });
            if (selectedProduct.channel != "MonthlyEnterprise")
                cboUpdateChannel.Items.Add(new ComboBoxItem { Content = "MonthlyEnterprise", Tag = "MonthlyEnterprise" });
        }

        private OfficeProductInfo GetSelectedProduct()
        {
            foreach (RadioButton rb in PanelOfficeSubVersion.Children)
            {
                if (rb.IsChecked == true)
                    return rb.Tag as OfficeProductInfo;
            }
            return null;
        }

        private void QuickSelect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            var bc = new System.Windows.Media.BrushConverter();
            var bgNormal = (System.Windows.Media.Brush)bc.ConvertFrom("#FFFFFF");
            var fgNormal = (System.Windows.Media.Brush)bc.ConvertFrom("#333333");
            var bdNormal = (System.Windows.Media.Brush)bc.ConvertFrom("#D0D0D0");

            var bgActive = (System.Windows.Media.Brush)bc.ConvertFrom("#EAF3FF");
            var fgActive = (System.Windows.Media.Brush)bc.ConvertFrom("#0067C0");

            // Reset tráº¡ng thÃ¡i cÃ¡c nÃºt
            btnSelectMin.Background = bgNormal; btnSelectMin.Foreground = fgNormal; btnSelectMin.BorderBrush = bdNormal;
            btnSelectRecommended.Background = bgNormal; btnSelectRecommended.Foreground = fgNormal; btnSelectRecommended.BorderBrush = bdNormal;
            btnSelectFull.Background = bgNormal; btnSelectFull.Foreground = fgNormal; btnSelectFull.BorderBrush = bdNormal;
            btnSelectNone.Background = bgNormal; btnSelectNone.Foreground = fgNormal; btnSelectNone.BorderBrush = bdNormal;

            // Äá»•i mÃ u nÃºt Ä‘ang chá»n
            btn.Background = bgActive;
            btn.Foreground = fgActive;
            btn.BorderBrush = fgActive;

            // Xá»­ lÃ½ Checkbox
            bool w = false, xl = false, p = false, o = false, n = false, a = false;
            bool pb = false, t = false, od = false, vp = false, vs = false, pjp = false, pjs = false, sk = false;

            if (btn == btnSelectMin)
            {
                w = true; xl = true; p = true;
            }
            else if (btn == btnSelectRecommended)
            {
                w = true; xl = true; p = true; o = true; t = true; od = true;
            }
            else if (btn == btnSelectFull)
            {
                w = true; xl = true; p = true; o = true; n = true; a = true;
                pb = true; t = true; od = true; vp = true; pjp = true; sk = true;
            }

            // Temporarily unhook events to prevent recursive triggers
            chkWord.IsChecked = w;
            chkExcel.IsChecked = xl;
            chkPowerPoint.IsChecked = p;
            chkOutlook.IsChecked = o;
            chkOneNote.IsChecked = n;
            chkAccess.IsChecked = a;
            chkPublisher.IsChecked = pb;
            chkTeams.IsChecked = t;
            chkOneDrive.IsChecked = od;
            chkVisioPro.IsChecked = vp;
            chkVisioStd.IsChecked = vs;
            chkProjectPro.IsChecked = pjp;
            chkProjectStd.IsChecked = pjs;
            chkSkype.IsChecked = sk;
        }

        private void VisioPro_Checked(object sender, RoutedEventArgs e)
        {
            if (chkVisioPro.IsChecked == true && chkVisioStd != null)
                chkVisioStd.IsChecked = false;
            Config_Changed(sender, e);
        }

        private void VisioStd_Checked(object sender, RoutedEventArgs e)
        {
            if (chkVisioStd.IsChecked == true && chkVisioPro != null)
                chkVisioPro.IsChecked = false;
            Config_Changed(sender, e);
        }

        private void ProjectPro_Checked(object sender, RoutedEventArgs e)
        {
            if (chkProjectPro.IsChecked == true && chkProjectStd != null)
                chkProjectStd.IsChecked = false;
            Config_Changed(sender, e);
        }

        private void ProjectStd_Checked(object sender, RoutedEventArgs e)
        {
            if (chkProjectStd.IsChecked == true && chkProjectPro != null)
                chkProjectPro.IsChecked = false;
            Config_Changed(sender, e);
        }

        private void Config_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            GenerateXmlPreview();
        }

        private void GenerateXmlPreview()
        {
            var product = GetSelectedProduct();
            if (product == null)
            {
                txtXmlPreview.Text = "Please select a detailed version.";
                return;
            }

            string arch = rbArch64.IsChecked == true ? "64" : "32";
            string channel = (cboUpdateChannel.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? product.channel;
            string lang = (cboLanguage.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "en-us";
            string langText = (cboLanguage.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "English";
            
            string licenseSuffix = rdoLicenseVolume.IsChecked == true ? "Volume" : "Retail";
            string finalProductId = product.productid;

            // Äá»•i Ä‘uÃ´i Volume/Retail cho cÃ¡c phiÃªn báº£n khÃ´ng pháº£i 365
            if (!finalProductId.StartsWith("O365"))
            {
                if (finalProductId.EndsWith("Volume"))
                    finalProductId = finalProductId.Substring(0, finalProductId.Length - 6) + licenseSuffix;
                else if (finalProductId.EndsWith("Retail"))
                    finalProductId = finalProductId.Substring(0, finalProductId.Length - 6) + licenseSuffix;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<Configuration>");
            sb.AppendLine($"  <Add OfficeClientEdition=\"{arch}\" Channel=\"{channel}\">");
            sb.AppendLine($"    <Product ID=\"{finalProductId}\" PIDKEY=\"{product.pidkey}\">");
            sb.AppendLine($"      <Language ID=\"{lang}\" />");

            if (chkWord.IsChecked != true) sb.AppendLine("      <ExcludeApp ID=\"Word\" />");
            if (chkExcel.IsChecked != true) sb.AppendLine("      <ExcludeApp ID=\"Excel\" />");
            if (chkPowerPoint.IsChecked != true) sb.AppendLine("      <ExcludeApp ID=\"PowerPoint\" />");
            if (chkAccess.IsChecked != true) sb.AppendLine("      <ExcludeApp ID=\"Access\" />");
            if (chkOutlook.IsChecked != true) sb.AppendLine("      <ExcludeApp ID=\"Outlook\" />");
            if (chkOneNote.IsChecked != true) sb.AppendLine("      <ExcludeApp ID=\"OneNote\" />");
            if (chkPublisher.IsChecked != true) sb.AppendLine("      <ExcludeApp ID=\"Publisher\" />");
            if (chkOneDrive.IsChecked != true) sb.AppendLine("      <ExcludeApp ID=\"OneDrive\" />");
            if (chkSkype.IsChecked != true) sb.AppendLine("      <ExcludeApp ID=\"Lync\" />");

            sb.AppendLine("    </Product>");

            // Visio
            if (chkVisioPro.IsChecked == true || chkVisioStd.IsChecked == true)
            {
                bool isPro = chkVisioPro.IsChecked == true;
                string type = isPro ? "Pro" : "Std";
                string visioId = product.name.Contains("2019") ? $"Visio{type}2019{licenseSuffix}" :
                                 product.name.Contains("2021") ? $"Visio{type}2021{licenseSuffix}" :
                                 product.name.Contains("2024") ? $"Visio{type}2024{licenseSuffix}" : $"Visio{type}{licenseSuffix}";
                sb.AppendLine($"    <Product ID=\"{visioId}\">");
                sb.AppendLine($"      <Language ID=\"{lang}\" />");
                sb.AppendLine("    </Product>");
            }

            // Project
            if (chkProjectPro.IsChecked == true || chkProjectStd.IsChecked == true)
            {
                bool isPro = chkProjectPro.IsChecked == true;
                string type = isPro ? "Pro" : "Std";
                string projectId = product.name.Contains("2019") ? $"Project{type}2019{licenseSuffix}" :
                                   product.name.Contains("2021") ? $"Project{type}2021{licenseSuffix}" :
                                   product.name.Contains("2024") ? $"Project{type}2024{licenseSuffix}" : $"Project{type}{licenseSuffix}";
                sb.AppendLine($"    <Product ID=\"{projectId}\">");
                sb.AppendLine($"      <Language ID=\"{lang}\" />");
                sb.AppendLine("    </Product>");
            }

            sb.AppendLine("  </Add>");
            
            if (chkCreateDesktopShortcut.IsChecked == false)
            {
                sb.AppendLine("  <Property Name=\"Icons\" Value=\"0\" />");
            }

            sb.AppendLine("  <Property Name=\"SharedComputerLicensing\" Value=\"0\" />");
            sb.AppendLine("  <Property Name=\"FORCEAPPSHUTDOWN\" Value=\"FALSE\" />");
            sb.AppendLine("  <Property Name=\"DeviceBasedLicensing\" Value=\"0\" />");
            sb.AppendLine("  <Updates Enabled=\"TRUE\" />");
            
            if (chkRemoveOffice.IsChecked == true)
            {
                sb.AppendLine("  <RemoveMSI />");
            }
            
            sb.AppendLine("</Configuration>");

            _currentXml = sb.ToString();
            
            // Cáº­p nháº­t Overview
            lblOverviewVersion.Text = product.name;
            lblOverviewArch.Text = arch == "64" ? "x64 (64-bit)" : "x86 (32-bit)";
            lblOverviewChannel.Text = channel;
            lblOverviewLang.Text = langText;
            
            var apps = new System.Collections.Generic.List<string>();
            if (chkWord.IsChecked == true) apps.Add("Word");
            if (chkExcel.IsChecked == true) apps.Add("Excel");
            if (chkPowerPoint.IsChecked == true) apps.Add("PowerPoint");
            if (chkOutlook.IsChecked == true) apps.Add("Outlook");
            if (chkOneNote.IsChecked == true) apps.Add("OneNote");
            if (chkAccess.IsChecked == true) apps.Add("Access");
            if (chkPublisher.IsChecked == true) apps.Add("Publisher");
            if (chkTeams.IsChecked == true) apps.Add("Teams");
            if (chkOneDrive.IsChecked == true) apps.Add("OneDrive");
            if (chkSkype.IsChecked == true) apps.Add("Skype");
            if (chkVisioPro.IsChecked == true) apps.Add("Visio Pro");
            if (chkVisioStd.IsChecked == true) apps.Add("Visio Std");
            if (chkProjectPro.IsChecked == true) apps.Add("Project Pro");
            if (chkProjectStd.IsChecked == true) apps.Add("Project Std");
            lblOverviewApps.Text = string.Join(", ", apps);

            var opts = new System.Collections.Generic.List<string>();
            if (chkRemoveOffice.IsChecked == true) opts.Add(LocalizationService.Get("chkRemoveOffice") ?? "Remove existing Office");
            if (chkCreateDesktopShortcut.IsChecked == true) opts.Add(LocalizationService.Get("chkCreateDesktopShortcut") ?? "Create Desktop Shortcut");
            opts.Add(LocalizationService.Get("lblIncludeUpdates") ?? "Include Updates");
            lblOverviewOpts.Text = string.Join(", ", opts);

            HighlightXml(_currentXml);
        }

        private void HighlightXml(string xml)
        {
            txtXmlPreview.Inlines.Clear();
            var purple = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(137, 82, 179)); // #8952B3
            var red = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 75, 75));    // #D24B4B
            var blue = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 132, 179));  // #3A84B3
            var gray = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)); // #64748B

            int i = 0;
            while (i < xml.Length)
            {
                if (xml[i] == '<')
                {
                    int end = xml.IndexOf('>', i);
                    if (end == -1) end = xml.Length - 1;
                    string tagContent = xml.Substring(i, end - i + 1);

                    bool inString = false;
                    string buffer = "";
                    for (int j = 0; j < tagContent.Length; j++)
                    {
                        char c = tagContent[j];
                        if (c == '"')
                        {
                            if (inString)
                            {
                                buffer += c;
                                txtXmlPreview.Inlines.Add(new System.Windows.Documents.Run(buffer) { Foreground = blue });
                                buffer = "";
                                inString = false;
                            }
                            else
                            {
                                if (buffer.Length > 0)
                                {
                                    string[] parts = System.Text.RegularExpressions.Regex.Split(buffer, @"(\s|=)");
                                    foreach (var p in parts)
                                    {
                                        if (string.IsNullOrEmpty(p)) continue;
                                        if (p == "=" || string.IsNullOrWhiteSpace(p))
                                            txtXmlPreview.Inlines.Add(new System.Windows.Documents.Run(p) { Foreground = gray });
                                        else if (p.Contains("<") || p.Contains(">") || p.Contains("/"))
                                            txtXmlPreview.Inlines.Add(new System.Windows.Documents.Run(p) { Foreground = purple });
                                        else
                                            txtXmlPreview.Inlines.Add(new System.Windows.Documents.Run(p) { Foreground = red });
                                    }
                                }
                                buffer = "\"";
                                inString = true;
                            }
                        }
                        else
                        {
                            buffer += c;
                        }
                    }

                    if (buffer.Length > 0)
                    {
                        string[] parts = System.Text.RegularExpressions.Regex.Split(buffer, @"(\s|=)");
                        foreach (var p in parts)
                        {
                            if (string.IsNullOrEmpty(p)) continue;
                            if (p == "=" || string.IsNullOrWhiteSpace(p))
                                txtXmlPreview.Inlines.Add(new System.Windows.Documents.Run(p) { Foreground = gray });
                            else if (p.Contains("<") || p.Contains(">") || p.Contains("/"))
                                txtXmlPreview.Inlines.Add(new System.Windows.Documents.Run(p) { Foreground = purple });
                            else
                                txtXmlPreview.Inlines.Add(new System.Windows.Documents.Run(p) { Foreground = purple }); // Node name
                        }
                    }
                    i = end + 1;
                }
                else
                {
                    int end = xml.IndexOf('<', i);
                    if (end == -1) end = xml.Length;
                    string text = xml.Substring(i, end - i);
                    txtXmlPreview.Inlines.Add(new System.Windows.Documents.Run(text) { Foreground = gray });
                    i = end;
                }
            }
        }

        private void btnCopyXml_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_currentXml))
            {
                Clipboard.SetText(_currentXml);
                Components.CustomMessageBoxWindow.Show(
                    GetLocalizedText("msgOfficeXmlCopied", "XML configuration copied to clipboard."),
                    GetLocalizedText("msgTitleSuccess", "Success"));
            }
        }

        private void btnDownloadOffice_Click(object sender, RoutedEventArgs e)
        {
            Components.CustomMessageBoxWindow.Show(
                GetLocalizedText("msgOfficeDownloadUseActions", "Use the Download action in section 11."),
                GetLocalizedText("msgTitleNotice", "Notice"));
        }

        private static string SanitizePathSegment(string value)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidChar, '_');
            }

            return string.IsNullOrWhiteSpace(value) ? "Office" : value.Trim();
        }

        private async void btnDownloadOffice_ExecuteClick(object sender, RoutedEventArgs e)
        {
            var selectedProduct = GetSelectedProduct();
            if (selectedProduct == null)
            {
                Components.CustomMessageBoxWindow.Show(
                    GetLocalizedText("msgOfficeDownloadSelectVersion", "Please select a detailed Office version before downloading."),
                    GetLocalizedText("msgTitleNotice", "Notice"));
                return;
            }

            if (string.IsNullOrWhiteSpace(_currentXml))
            {
                GenerateXmlPreview();
            }

            if (string.IsNullOrWhiteSpace(_currentXml))
            {
                Components.CustomMessageBoxWindow.Show(
                    GetLocalizedText("msgOfficeConfigGenerateFailed", "Cannot generate configuration.xml from current settings."),
                    GetLocalizedText("msgTitleError", "Error"));
                return;
            }

            bool isOffice2013 = rbOffice2013.IsChecked == true || selectedProduct.name.Contains("2013", StringComparison.OrdinalIgnoreCase);
            string? setupSourcePath = ResolveSetupExecutablePath(isOffice2013);
            if (string.IsNullOrWhiteSpace(setupSourcePath))
            {
                string missingExe = isOffice2013 ? "setup2013.exe" : "setup.exe";
                Components.CustomMessageBoxWindow.Show(
                    FormatLocalizedText("msgOfficeMissingSetupForDownload", "Cannot find {0} in Files/Tools.", missingExe),
                    GetLocalizedText("msgOfficeMissingSetupTitle", "Missing setup file"));
                return;
            }

            Window? ownerWindow = Window.GetWindow(this);
            string defaultBaseFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var locationDialog = new Components.OfficeDownloadLocationWindow(defaultBaseFolder)
            {
                Owner = ownerWindow
            };

            if (locationDialog.ShowDialog() != true)
            {
                return;
            }

            string baseDownloadFolder = locationDialog.SelectedFolderPath;
            if (string.IsNullOrWhiteSpace(baseDownloadFolder))
            {
                Components.CustomMessageBoxWindow.Show(
                    GetLocalizedText("msgOfficeDownloadSelectLocation", "Please choose a destination folder before downloading."),
                    GetLocalizedText("msgTitleNotice", "Notice"));
                return;
            }

            MainWindow? mainWindow = Window.GetWindow(this) as MainWindow;
            btnInstallOffice.IsEnabled = false;
            btnDownloadOffice.IsEnabled = false;
            btnExportXml.IsEnabled = false;
            btnUninstallOffice.IsEnabled = false;

            string downloadRoot = Path.Combine(
                baseDownloadFolder,
                $"{SanitizePathSegment(selectedProduct.name)}_{DateTime.Now:yyyyMMdd_HHmmss}");

            try
            {
                Directory.CreateDirectory(downloadRoot);

                string configurationPath = Path.Combine(downloadRoot, "configuration.xml");
                await File.WriteAllTextAsync(configurationPath, _currentXml, Encoding.UTF8);

                string localSetupPath = Path.Combine(downloadRoot, Path.GetFileName(setupSourcePath));
                File.Copy(setupSourcePath, localSetupPath, true);

                mainWindow?.ShowLoading(GetLocalizedText("msgOfficeDownloadInProgress", "Downloading Microsoft Office files..."), 0, 0);

                int exitCode = await System.Threading.Tasks.Task.Run(() =>
                {
                    using var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = localSetupPath,
                            Arguments = "/download \"configuration.xml\"",
                            WorkingDirectory = downloadRoot,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    if (!process.Start())
                    {
                        throw new InvalidOperationException(GetLocalizedText("msgOfficeDownloadLaunchError", "Cannot start Office download process."));
                    }

                    process.WaitForExit();
                    return process.ExitCode;
                });

                if (exitCode == 0)
                {
                    Components.CustomMessageBoxWindow.Show(
                        FormatLocalizedText("msgOfficeDownloadSuccess", "Office installation files downloaded successfully.\nFolder: {0}", downloadRoot),
                        GetLocalizedText("msgTitleSuccess", "Success"));
                    Process.Start(new ProcessStartInfo("explorer.exe", $"\"{downloadRoot}\"") { UseShellExecute = true });
                }
                else
                {
                    Components.CustomMessageBoxWindow.Show(
                        FormatLocalizedText("msgOfficeDownloadExitCode", "Office download finished with exit code: {0}", FormatExitCode(exitCode)),
                        GetLocalizedText("msgTitleError", "Error"));
                }
            }
            catch (Exception ex)
            {
                Components.CustomMessageBoxWindow.Show(
                    FormatLocalizedText("msgOfficeDownloadFailed", "Office download failed: {0}", ex.Message),
                    GetLocalizedText("msgTitleError", "Error"));
            }
            finally
            {
                mainWindow?.HideLoading();
                btnInstallOffice.IsEnabled = true;
                btnDownloadOffice.IsEnabled = true;
                btnExportXml.IsEnabled = true;
                UpdateUninstallButtonState();
            }
        }

        private void btnExportXml_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "XML File (*.xml)|*.xml",
                FileName = "configuration.xml",
                DefaultExt = ".xml"
            };
            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, txtXmlPreview.Text);
                Components.CustomMessageBoxWindow.Show("XML exported successfully!", "Success");
            }
        }

        private static string? ResolveSetupExecutablePath(bool useOffice2013Setup)
        {
            string fileName = useOffice2013Setup ? "setup2013.exe" : "setup.exe";
            foreach (string root in OfficeFilesRootCandidates)
            {
                string candidate = Path.Combine(root, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private async void btnInstallOffice_Click(object sender, RoutedEventArgs e)
        {
            var selectedProduct = GetSelectedProduct();
            if (selectedProduct == null)
            {
                Components.CustomMessageBoxWindow.Show(
                    GetLocalizedText("msgOfficeInstallSelectVersion", "Please select a detailed Office version before installing."),
                    GetLocalizedText("msgTitleNotice", "Notice"));
                return;
            }

            if (string.IsNullOrWhiteSpace(_currentXml))
            {
                GenerateXmlPreview();
            }

            if (string.IsNullOrWhiteSpace(_currentXml))
            {
                Components.CustomMessageBoxWindow.Show(
                    GetLocalizedText("msgOfficeConfigGenerateFailed", "Cannot generate configuration.xml from current settings."),
                    GetLocalizedText("msgTitleError", "Error"));
                return;
            }

            bool isOffice2013 = rbOffice2013.IsChecked == true || selectedProduct.name.Contains("2013", StringComparison.OrdinalIgnoreCase);
            string? setupSourcePath = ResolveSetupExecutablePath(isOffice2013);
            if (string.IsNullOrWhiteSpace(setupSourcePath))
            {
                string missingExe = isOffice2013 ? "setup2013.exe" : "setup.exe";
                Components.CustomMessageBoxWindow.Show(
                    FormatLocalizedText(
                        "msgOfficeMissingSetupWithPath",
                        "Cannot find {0} in Files.\nExpected path: {1}",
                        missingExe,
                        @"E:\GGAI\Design\Warehouse\MSEDU\msoffice-main\Files"),
                    GetLocalizedText("msgOfficeMissingSetupTitle", "Missing setup file"));
                return;
            }

            MainWindow? mainWindow = Window.GetWindow(this) as MainWindow;
            btnInstallOffice.IsEnabled = false;
            btnDownloadOffice.IsEnabled = false;
            btnExportXml.IsEnabled = false;
            btnUninstallOffice.IsEnabled = false;

            try
            {
                string installRoot = Path.Combine(Path.GetTempPath(), "HyperLis", "OfficeSetup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(installRoot);

                string configurationPath = Path.Combine(installRoot, "configuration.xml");
                await File.WriteAllTextAsync(configurationPath, _currentXml, Encoding.UTF8);

                string localSetupPath = Path.Combine(installRoot, Path.GetFileName(setupSourcePath));
                File.Copy(setupSourcePath, localSetupPath, true);

                mainWindow?.ShowLoading(GetLocalizedText("msgOfficeInstallInProgress", "Installing Microsoft Office..."), 0, 0);

                int exitCode = await System.Threading.Tasks.Task.Run(() =>
                {
                    using var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = localSetupPath,
                            Arguments = "/configure \"configuration.xml\"",
                            WorkingDirectory = installRoot,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    if (!process.Start())
                    {
                        throw new InvalidOperationException(GetLocalizedText("msgOfficeInstallLaunchError", "Cannot start Office installer."));
                    }

                    process.WaitForExit();
                    return process.ExitCode;
                });

                if (exitCode == 0)
                {
                    Components.CustomMessageBoxWindow.Show(
                        GetLocalizedText("msgOfficeInstallSuccess", "Office installation command started successfully."),
                        GetLocalizedText("msgTitleSuccess", "Success"));
                }
                else
                {
                    Components.CustomMessageBoxWindow.Show(
                        FormatLocalizedText("msgOfficeInstallExitCode", "Office installation finished with exit code: {0}", FormatExitCode(exitCode)),
                        GetLocalizedText("msgTitleError", "Error"));
                }
            }
            catch (Exception ex)
            {
                Components.CustomMessageBoxWindow.Show(
                    FormatLocalizedText("msgOfficeInstallFailed", "Office installation failed: {0}", ex.Message),
                    GetLocalizedText("msgTitleError", "Error"));
            }
            finally
            {
                mainWindow?.HideLoading();
                btnInstallOffice.IsEnabled = true;
                btnDownloadOffice.IsEnabled = true;
                btnExportXml.IsEnabled = true;
                UpdateUninstallButtonState();
            }
        }

        private void chkConfirmUninstall_CheckedChanged(object sender, RoutedEventArgs e)
        {
            UpdateUninstallButtonState();
        }

        private void UpdateUninstallButtonState()
        {
            btnUninstallOffice.IsEnabled = chkConfirmUninstall.IsChecked == true;
        }

        private static string? ResolveUninstallSetupExecutablePath()
        {
            string[] preferredRoots = new[]
            {
                @"E:\GGAI\Design\Warehouse\MSEDU\msoffice-main\Files",
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Warehouse\MSEDU\msoffice-main\Files")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Warehouse\MSEDU\msoffice-main\Files")),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools")
            };

            foreach (string root in preferredRoots)
            {
                string candidate = Path.Combine(root, "setup.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string? ResolveUninstallConfigurationXmlPath()
        {
            string[] candidates = new[]
            {
                @"E:\GGAI\Design\Warehouse\MSEDU\msoffice-main\Files\RemoveAll\configuration.xml",
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Warehouse\MSEDU\msoffice-main\Files\RemoveAll\configuration.xml")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Warehouse\MSEDU\msoffice-main\Files\RemoveAll\configuration.xml")),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", "RemoveAll", "configuration.xml"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "RemoveAll", "configuration.xml")
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private static string PrepareUninstallConfigurationXml(string sourceConfigurationPath, string logDirectory)
        {
            Directory.CreateDirectory(logDirectory);

            XDocument document = XDocument.Load(sourceConfigurationPath);
            XElement? root = document.Element("Configuration");
            if (root == null)
            {
                throw new InvalidOperationException("Invalid Office uninstall configuration: missing <Configuration> root.");
            }

            XElement? loggingNode = root.Elements("Logging").FirstOrDefault();
            if (loggingNode == null)
            {
                loggingNode = new XElement("Logging");
                root.Add(loggingNode);
            }

            loggingNode.SetAttributeValue("Level", "Standard");
            loggingNode.SetAttributeValue("Path", logDirectory);

            bool hasForceShutdown = root.Elements("Property")
                .Any(node => string.Equals((string?)node.Attribute("Name"), "FORCEAPPSHUTDOWN", StringComparison.OrdinalIgnoreCase));
            if (!hasForceShutdown)
            {
                root.Add(new XElement("Property",
                    new XAttribute("Name", "FORCEAPPSHUTDOWN"),
                    new XAttribute("Value", "TRUE")));
            }

            bool hasRemoveMsi = root.Elements("RemoveMSI").Any();
            if (!hasRemoveMsi)
            {
                root.Add(new XElement("RemoveMSI"));
            }

            string runtimeConfigPath = Path.Combine(logDirectory, "configuration.runtime.xml");
            using (var writer = new StreamWriter(runtimeConfigPath, false, new UTF8Encoding(false)))
            {
                document.Save(writer);
            }

            return runtimeConfigPath;
        }

        private static string? TryGetLatestUninstallLogPath(string logDirectory)
        {
            if (!Directory.Exists(logDirectory))
            {
                return null;
            }

            string[] logFiles = Directory.GetFiles(logDirectory, "*.log", SearchOption.AllDirectories);
            if (logFiles.Length == 0)
            {
                return null;
            }

            return logFiles.OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
        }

        /// <summary>
        /// Checks if Office 2013 MSI (Office15) is still installed after ODT removal.
        /// Matches office.ps1 logic: Test-Path "C:\Program Files*\Microsoft Office\Office15\ospp.vbs"
        /// </summary>
        private static bool IsOffice15MSIStillInstalled()
        {
            string[] programFilesPaths = {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            foreach (string pfPath in programFilesPaths)
            {
                string osppPath = Path.Combine(pfPath, "Microsoft Office", "Office15", "ospp.vbs");
                if (File.Exists(osppPath)) return true;
            }

            return false;
        }

        /// <summary>
        /// Downloads and runs the Microsoft SaRA (Support and Recovery Assistant) tool
        /// to scrub remaining MSI-based Office installations.
        /// Matches office.ps1 logic lines 413-424.
        /// </summary>
        private async System.Threading.Tasks.Task<bool> RunSaRAToolAsync(MainWindow? mainWindow, string workingDirectory)
        {
            const string saraDownloadUrl = "https://aka.ms/SaRA_EnterpriseVersionFiles";
            string saraZipPath = Path.Combine(workingDirectory, "SaRA.zip");
            string saraExtractPath = Path.Combine(workingDirectory, "SaRA");
            string saraCmdPath = Path.Combine(saraExtractPath, "SaRACmd.exe");

            try
            {
                // Step 1: Download SaRA
                mainWindow?.ShowLoading(
                    GetLocalizedText("msgOfficeSaRADownloading", "Downloading Microsoft Support and Recovery Assistant..."), 0, 0);

                await System.Threading.Tasks.Task.Run(() =>
                {
                    using var client = new System.Net.Http.HttpClient();
                    client.Timeout = TimeSpan.FromMinutes(5);
                    using var response = client.GetAsync(saraDownloadUrl).Result;
                    response.EnsureSuccessStatusCode();
                    using var fs = new FileStream(saraZipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    response.Content.CopyToAsync(fs).Wait();
                });

                // Step 2: Extract SaRA
                mainWindow?.ShowLoading(
                    GetLocalizedText("msgOfficeSaRAExtracting", "Extracting SaRA tool..."), 0, 0);

                await System.Threading.Tasks.Task.Run(() =>
                {
                    if (Directory.Exists(saraExtractPath))
                        Directory.Delete(saraExtractPath, true);
                    System.IO.Compression.ZipFile.ExtractToDirectory(saraZipPath, saraExtractPath);
                });

                // Find SaRACmd.exe (may be in a subfolder)
                string? actualSaraCmdPath = await System.Threading.Tasks.Task.Run(() =>
                {
                    if (File.Exists(saraCmdPath)) return saraCmdPath;
                    var found = Directory.GetFiles(saraExtractPath, "SaRACmd.exe", SearchOption.AllDirectories);
                    return found.Length > 0 ? found[0] : null;
                });

                if (string.IsNullOrEmpty(actualSaraCmdPath))
                {
                    Components.CustomMessageBoxWindow.Show(
                        GetLocalizedText("msgOfficeSaRANotFound", "SaRACmd.exe not found after extraction."),
                        GetLocalizedText("msgTitleError", "Error"));
                    return false;
                }

                // Step 3: Run SaRA OfficeScrubScenario
                mainWindow?.ShowLoading(
                    GetLocalizedText("msgOfficeSaRARunning", "Running Office Scrub Scenario (SaRA)..."), 0, 0);

                var (exitCode, stdOut, stdErr) = await System.Threading.Tasks.Task.Run(() =>
                {
                    using var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = actualSaraCmdPath,
                            Arguments = "-S OfficeScrubScenario -AcceptEula -OfficeVersion All",
                            WorkingDirectory = Path.GetDirectoryName(actualSaraCmdPath) ?? saraExtractPath,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };

                    if (!process.Start())
                        throw new InvalidOperationException("Cannot start SaRACmd.exe");

                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    return (ExitCode: process.ExitCode, StdOut: stdout, StdErr: stderr);
                });

                return exitCode == 0;
            }
            catch (Exception ex)
            {
                Components.CustomMessageBoxWindow.Show(
                    FormatLocalizedText("msgOfficeSaRAFailed", "SaRA tool failed: {0}", ex.Message),
                    GetLocalizedText("msgTitleError", "Error"));
                return false;
            }
        }

        private async void btnUninstallOffice_ExecuteClick(object sender, RoutedEventArgs e)
        {
            bool isAdministrator = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);
            if (!isAdministrator)
            {
                Components.CustomMessageBoxWindow.Show(
                    GetLocalizedText("msgOfficeRequireAdmin", "Please run the app as Administrator before uninstalling Office."),
                    GetLocalizedText("msgTitleNotice", "Notice"));
                return;
            }

            if (chkConfirmUninstall.IsChecked != true)
            {
                Components.CustomMessageBoxWindow.Show(
                    GetLocalizedText("msgOfficeUninstallNeedConfirm", "Please confirm before uninstalling Office."),
                    GetLocalizedText("msgTitleNotice", "Notice"));
                return;
            }

            Window? ownerWindow = Window.GetWindow(this);
            bool confirmed = Components.CustomConfirmWindow.ShowConfirm(
                GetLocalizedText(
                    "msgOfficeUninstallConfirmBody",
                    "This action will uninstall Microsoft Office, Visio, and Project from your machine.\n\nChanges might be irreversible without a System Restore point.\n\nDo you want to continue?"),
                GetLocalizedText("msgOfficeUninstallConfirmTitle", "Confirm Office Uninstall"),
                GetLocalizedText("btnYes", "Yes"),
                GetLocalizedText("btnNo", "No"),
                ownerWindow);

            if (!confirmed)
            {
                return;
            }

            // Step 1: Resolve setup.exe and RemoveAll configuration
            string? setupSourcePath = ResolveUninstallSetupExecutablePath();
            if (string.IsNullOrWhiteSpace(setupSourcePath))
            {
                Components.CustomMessageBoxWindow.Show(
                    GetLocalizedText("msgOfficeMissingSetup", "Cannot find setup.exe in Files/Tools for Office uninstall."),
                    GetLocalizedText("msgOfficeMissingSetupTitle", "Missing setup file"));
                return;
            }

            string? configurationPath = ResolveUninstallConfigurationXmlPath();
            if (string.IsNullOrWhiteSpace(configurationPath))
            {
                Components.CustomMessageBoxWindow.Show(
                    GetLocalizedText("msgOfficeMissingRemoveAllConfig", "Cannot find RemoveAll\\configuration.xml for Office uninstall."),
                    GetLocalizedText("msgTitleError", "Error"));
                return;
            }

            // Working directory for the entire uninstall process (matches office.ps1: %TEMP%\ClickToRunU)
            string workingDirectory = Path.Combine(Path.GetTempPath(), "HyperLisOfficeUninstall");
            Directory.CreateDirectory(workingDirectory);

            string logDirectory = Path.Combine(workingDirectory, "Logs", DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            string runtimeConfigurationPath;
            try
            {
                runtimeConfigurationPath = PrepareUninstallConfigurationXml(configurationPath, logDirectory);
            }
            catch (Exception ex)
            {
                Components.CustomMessageBoxWindow.Show(
                    FormatLocalizedText("msgOfficeUninstallFailed", "Office uninstall failed: {0}", ex.Message),
                    GetLocalizedText("msgTitleError", "Error"));
                return;
            }

            MainWindow? mainWindow = ownerWindow as MainWindow;
            btnInstallOffice.IsEnabled = false;
            btnDownloadOffice.IsEnabled = false;
            btnExportXml.IsEnabled = false;
            btnUninstallOffice.IsEnabled = false;
            chkConfirmUninstall.IsEnabled = false;

            try
            {
                // ── Step 2: Run ODT setup.exe /configure RemoveAll (matches office.ps1 line 411) ──
                mainWindow?.ShowLoading(GetLocalizedText("msgOfficeUninstallInProgress", "Uninstalling Microsoft Office..."), 0, 0);

                var (odtExitCode, odtStdOut, odtStdErr) = await System.Threading.Tasks.Task.Run(() =>
                {
                    using var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = setupSourcePath,
                            Arguments = $"/configure \"{runtimeConfigurationPath}\"",
                            WorkingDirectory = Path.GetDirectoryName(setupSourcePath) ?? AppDomain.CurrentDomain.BaseDirectory,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };

                    if (!process.Start())
                    {
                        throw new InvalidOperationException(GetLocalizedText("msgOfficeUninstallLaunchError", "Cannot start Office uninstall process."));
                    }

                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    return (ExitCode: process.ExitCode, StdOut: stdout, StdErr: stderr);
                });

                // ── Step 3: Check for Office 2013 MSI remnants (matches office.ps1 lines 413-424) ──
                bool ranSaRA = false;
                if (IsOffice15MSIStillInstalled())
                {
                    ranSaRA = await RunSaRAToolAsync(mainWindow, workingDirectory);
                }

                // ── Step 4: Show results ──
                if (odtExitCode == 0)
                {
                    string successMsg = GetLocalizedText("msgOfficeUninstallSuccess", "Office uninstall command executed successfully.");
                    if (ranSaRA)
                    {
                        successMsg += "\n" + GetLocalizedText("msgOfficeSaRACompleted", "SaRA Office Scrub completed.");
                    }
                    Components.CustomMessageBoxWindow.Show(successMsg, GetLocalizedText("msgTitleSuccess", "Success"));
                }
                else
                {
                    var details = new StringBuilder();
                    details.AppendLine(FormatLocalizedText("msgOfficeUninstallExitCode", "Office uninstall finished with exit code: {0}", FormatExitCode(odtExitCode)));

                    if (!string.IsNullOrWhiteSpace(odtStdErr))
                    {
                        details.AppendLine();
                        details.AppendLine(GetLocalizedText("msgOfficeUninstallStdErr", "Error output:"));
                        details.AppendLine(odtStdErr.Trim());
                    }

                    if (!string.IsNullOrWhiteSpace(odtStdOut))
                    {
                        details.AppendLine();
                        details.AppendLine(GetLocalizedText("msgOfficeUninstallStdOut", "Process output:"));
                        details.AppendLine(odtStdOut.Trim());
                    }

                    string? latestLogPath = TryGetLatestUninstallLogPath(logDirectory);
                    if (latestLogPath != null)
                    {
                        details.AppendLine();
                        details.AppendLine(FormatLocalizedText("msgOfficeUninstallLogPath", "Log: {0}", latestLogPath));
                    }

                    if (ranSaRA)
                    {
                        details.AppendLine();
                        details.AppendLine(GetLocalizedText("msgOfficeSaRACompleted", "SaRA Office Scrub completed."));
                    }

                    // Diagnostic info
                    details.AppendLine();
                    details.AppendLine($"Setup: {setupSourcePath}");
                    details.AppendLine($"Config: {runtimeConfigurationPath}");

                    Components.CustomMessageBoxWindow.Show(
                        details.ToString(),
                        GetLocalizedText("msgTitleError", "Error"));
                }
            }
            catch (Exception ex)
            {
                Components.CustomMessageBoxWindow.Show(
                    FormatLocalizedText("msgOfficeUninstallFailed", "Office uninstall failed: {0}", ex.Message),
                    GetLocalizedText("msgTitleError", "Error"));
            }
            finally
            {
                mainWindow?.HideLoading();
                btnInstallOffice.IsEnabled = true;
                btnDownloadOffice.IsEnabled = true;
                btnExportXml.IsEnabled = true;
                chkConfirmUninstall.IsEnabled = true;
                UpdateUninstallButtonState();
            }
        }
    }
}

