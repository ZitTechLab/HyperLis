using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using HyperLisUI.Helpers;

namespace HyperLisUI.Services
{
    public class UpdateInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("release_date")]
        public string ReleaseDate { get; set; } = string.Empty;

        [JsonPropertyName("download_url_setup")]
        public string DownloadUrlSetup { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public List<string> Content { get; set; } = new List<string>();

        [JsonPropertyName("size")]
        public string Size { get; set; } = string.Empty;

        [JsonPropertyName("is_critical")]
        public bool IsCritical { get; set; }
    }

    public class UpdateService
    {
        private const string UPDATE_EN_URL = "https://raw.githubusercontent.com/ZitTechLab/HyperLis/refs/heads/main/update_en.json";
        private const string UPDATE_VN_URL = "https://raw.githubusercontent.com/ZitTechLab/HyperLis/refs/heads/main/update_vn.json";
        private static readonly HttpClient _httpClient = new HttpClient();

        public UpdateService()
        {
            // Set User-Agent as required by some APIs/servers
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "HyperLis-AutoUpdater");
            }
        }

        public Version GetCurrentVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version ?? new Version(1, 0, 0, 0);
            }
            catch
            {
                return new Version(1, 0, 0, 0);
            }
        }

        public async Task<UpdateInfo?> CheckForUpdateAsync()
        {
            try
            {
                // Choose base URL by current language
                var currentLang = LocalizationService.CurrentLanguage?.ToLower() ?? "en";
                var baseUrl = currentLang.Contains("vn") ? UPDATE_VN_URL : UPDATE_EN_URL;

                // To avoid cache, append timestamp
                var url = $"{baseUrl}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                var response = await _httpClient.GetStringAsync(url);

                if (string.IsNullOrWhiteSpace(response))
                    return null;

                var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(response);
                if (updateInfo == null || string.IsNullOrWhiteSpace(updateInfo.Version))
                    return null;

                // Clean up version string (e.g., "v1.0.5" -> "1.0.5")
                var cleanVersion = updateInfo.Version.ToLower().Replace("v", "").Trim();
                
                if (Version.TryParse(cleanVersion, out var serverVersion))
                {
                    var currentVersion = GetCurrentVersion();
                    if (serverVersion > currentVersion)
                    {
                        return updateInfo;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.LogCrash(ex, "UpdateService.CheckForUpdateAsync");
                return null; // Fail silently on network errors
            }
        }

        public async Task DownloadAndRunInstallerAsync(string url, IProgress<double> progress)
        {
            try
            {
                var fileName = "HyperLis_Setup_Update.exe";
                var tempPath = Path.Combine(Path.GetTempPath(), fileName);

                using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var canReportProgress = totalBytes != -1 && progress != null;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var totalRead = 0L;
                        var buffer = new byte[8192];
                        var isMoreToRead = true;

                        do
                        {
                            var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0)
                            {
                                isMoreToRead = false;
                            }
                            else
                            {
                                await fileStream.WriteAsync(buffer, 0, read);

                                totalRead += read;
                                if (canReportProgress)
                                {
                                    var percentage = Math.Round((double)totalRead / totalBytes * 100, 2);
                                    progress?.Report(percentage);
                                }
                            }
                        }
                        while (isMoreToRead);
                    }
                }

                // Run the installer silently or semi-silently
                var psi = new ProcessStartInfo
                {
                    FileName = tempPath,
                    // /SILENT = shows progress, /VERYSILENT = fully hidden, /SUPPRESSMSGBOXES = no popups
                    Arguments = "/SILENT /SUPPRESSMSGBOXES", 
                    UseShellExecute = true
                };

                Process.Start(psi);
                
                // Exit app so installer can overwrite files
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown();
                });
            }
            catch (Exception ex)
            {
                Logger.LogCrash(ex, "UpdateService.DownloadAndRunInstallerAsync");
                throw;
            }
        }
    }
}
