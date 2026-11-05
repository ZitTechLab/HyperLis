using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace HyperLisUI.Services
{
    public class WinPEService
    {
        private const string JsonFileName = "winpe.json";
        private const string IsoFileName = "winpe.iso";

        public async Task<string> EnsureWinPEIsoExistsAsync()
        {
            string downloadDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Download");
            if (!Directory.Exists(downloadDir)) Directory.CreateDirectory(downloadDir);

            string isoPath = Path.Combine(downloadDir, IsoFileName);

            if (File.Exists(isoPath))
            {
                // Verify size > 10MB to ensure it's not a failed download
                if (new FileInfo(isoPath).Length > 10 * 1024 * 1024)
                {
                    return isoPath;
                }
                // If too small, delete and redownload
                File.Delete(isoPath);
            }

            // Get URL from JSON
            string jsonPath = Path.Combine(downloadDir, JsonFileName);
            if (!File.Exists(jsonPath))
            {
                 // Try to look in project structure if running in debug (optional, but good for dev)
                 string devPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Download", JsonFileName));
                 if (File.Exists(devPath)) jsonPath = devPath;
                 else throw new FileNotFoundException($"Config file {JsonFileName} not found in Download folder.");
            }

            string url = await GetWinPEDownloadUrl(jsonPath);
            if (string.IsNullOrEmpty(url)) throw new Exception("Could not find WinPE download link in JSON.");

            // Download using curl
            bool success = await DownloadWithCurlAsync(url, isoPath);
            if (!success || !File.Exists(isoPath))
            {
                throw new Exception("Failed to download WinPE ISO.");
            }

            return isoPath;
        }

        private async Task<string> GetWinPEDownloadUrl(string jsonPath)
        {
            try
            {
                string json = await File.ReadAllTextAsync(jsonPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<Dictionary<string, WinPEInfo>>(json, options);

                if (data != null && data.TryGetValue("WSAP_WinPE", out var info))
                {
                    return info.link;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading WinPE JSON: {ex.Message}");
            }
            return string.Empty;
        }

        private async Task<bool> DownloadWithCurlAsync(string url, string outputPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "curl",
                        Arguments = $"-L -o \"{outputPath}\" \"{url}\"",
                        UseShellExecute = true, // Show window
                        CreateNoWindow = false,
                        WindowStyle = ProcessWindowStyle.Minimized // Minimized as requested
                    };

                    using (var p = Process.Start(psi))
                    {
                        if (p == null) return false;
                        p.WaitForExit();
                        return p.ExitCode == 0;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Curl error: {ex.Message}");
                    return false;
                }
            });
        }

        private class WinPEInfo
        {
            public string name { get; set; }
            public string link { get; set; }
        }
    }
}
