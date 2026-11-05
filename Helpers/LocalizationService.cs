using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HyperLisUI.Helpers
{
    /// <summary>
    /// Singleton service for managing application localization (i18n).
    /// Loads translation JSON files and applies them to UI elements.
    /// </summary>
    public static class LocalizationService
    {
        private static Dictionary<string, string> _translations = new();
        private static string _currentLanguage = "EN";
        private static readonly string _i18nFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Language");

        /// <summary>Current language code (e.g. "EN", "VN")</summary>
        public static string CurrentLanguage => _currentLanguage;

        /// <summary>Event fired when language changes, so Views can refresh.</summary>
        public static event Action? LanguageChanged;

        /// <summary>
        /// Load a language file. If the file doesn't exist, falls back to EN.
        /// </summary>
        public static void LoadLanguage(string langCode)
        {
            string filePath = Path.Combine(_i18nFolder, $"{langCode}.json");

            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"[i18n] Language file not found: {filePath}, falling back to EN");
                filePath = Path.Combine(_i18nFolder, "EN.json");
                langCode = "EN";
            }

            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"[i18n] EN.json not found either. No translations loaded.");
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    _translations = dict;
                    _currentLanguage = langCode;
                    Debug.WriteLine($"[i18n] Loaded {_translations.Count} keys for language: {langCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[i18n] Error loading language {langCode}: {ex.Message}");
            }

            // Load supplemental popup tooltip file (popupen.json / popupvn.json)
            string popupFileName = $"popup{langCode.ToLower()}.json";
            string popupFilePath = Path.Combine(_i18nFolder, popupFileName);

            if (File.Exists(popupFilePath))
            {
                try
                {
                    string popupJson = File.ReadAllText(popupFilePath);
                    var popupDict = JsonSerializer.Deserialize<Dictionary<string, string>>(popupJson);
                    if (popupDict != null)
                    {
                        foreach (var entry in popupDict)
                            _translations[entry.Key] = entry.Value;
                        Debug.WriteLine($"[i18n] Merged {popupDict.Count} popup tooltip keys from: {popupFileName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[i18n] Error loading popup file {popupFileName}: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine($"[i18n] No popup file found: {popupFilePath}");
            }

            // Notify all subscribers (Views) to refresh their UI
            LanguageChanged?.Invoke();
        }

        /// <summary>
        /// Get a translated string by key. Returns the key itself if not found.
        /// </summary>
        public static string Get(string key)
        {
            if (_translations.TryGetValue(key, out var value))
            {
                return value;
            }
            return key; // Fallback: return the key itself
        }

        /// <summary>
        /// Translate all named controls in a visual tree.
        /// Matches control x:Name against translation keys.
        /// Supports: Button (Content), TextBlock (Text), RadioButton (Content),
        /// CheckBox (Content), Label (Content), ComboBoxItem (Content).
        /// </summary>
        public static void TranslateUI(DependencyObject root)
        {
            if (root == null) return;

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);

                if (child is FrameworkElement fe && !string.IsNullOrEmpty(fe.Name))
                {
                    if (_translations.TryGetValue(fe.Name, out var translated))
                    {
                        switch (fe)
                        {
                            case TextBlock tb:
                                tb.Text = translated;
                                break;
                            case Button btn:
                                btn.Content = translated;
                                break;
                            case RadioButton rb:
                                rb.Content = translated;
                                break;
                            case CheckBox cb:
                                cb.Content = translated;
                                break;
                            case Label lbl:
                                lbl.Content = translated;
                                break;
                        }
                    }
                }

                // Recurse into children
                TranslateUI(child);
            }
        }

        /// <summary>
        /// Save selected language to a simple config file.
        /// </summary>
        public static void SaveLanguagePreference(string langCode)
        {
            AppConfigManager.Config.Language = langCode;
            AppConfigManager.SaveConfig();
        }

        /// <summary>
        /// Load saved language preference. Returns "EN" if not found.
        /// </summary>
        public static string LoadLanguagePreference()
        {
            return AppConfigManager.Config.Language;
        }
    }
}
