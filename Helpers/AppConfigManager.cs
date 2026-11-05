using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HyperLisUI.Helpers
{
    public class AppConfig
    {
        [JsonPropertyName("language")]
        public string Language { get; set; } = "EN";

        [JsonPropertyName("skipped_version")]
        public string SkippedVersion { get; set; } = string.Empty;

        [JsonPropertyName("remind_date")]
        public DateTime? RemindDate { get; set; }
    }

    public static class AppConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "HyperLis", "cfg.conf");

        public static AppConfig Config { get; private set; } = new AppConfig();

        static AppConfigManager()
        {
            LoadConfig();
        }

        public static void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json);
                    if (config != null)
                    {
                        Config = config;
                        return;
                    }
                }
                
                // Fallback: migrate from old language.conf if it exists
                string oldLangPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.conf");
                if (File.Exists(oldLangPath))
                {
                    Config.Language = File.ReadAllText(oldLangPath).Trim();
                }
            }
            catch { }
        }

        public static void SaveConfig()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(Config, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }
    }
}
