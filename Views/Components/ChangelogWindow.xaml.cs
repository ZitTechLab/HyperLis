using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HyperLisUI.Helpers;

namespace HyperLisUI.Views.Components
{
    public partial class ChangelogWindow : Window
    {
        public class FullChangelogItem
        {
            public string VersionName { get; set; } = "";
            public string ReleaseDate { get; set; } = "";
            public bool IsLatest { get; set; }
            public SolidColorBrush DotBrush { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));
            public List<string> Content { get; set; } = new();
        }

        private readonly List<FullChangelogItem> _allChangelogs = new();
        private readonly ObservableCollection<FullChangelogItem> _displayedChangelogs = new();

        public ChangelogWindow()
        {
            InitializeComponent();
            ChangelogItemsList.ItemsSource = _displayedChangelogs;

            ApplyLocalization();
            LoadAllChangelogs();
        }

        private void ApplyLocalization()
        {
            try
            {
                bool isVn = (LocalizationService.CurrentLanguage ?? "").Equals("VN", StringComparison.OrdinalIgnoreCase);

                if (isVn)
                {
                    TxtWindowTitle.Text = "HyperLis - Nhật ký thay đổi";
                    TxtWindowSubtitle.Text = "Toàn bộ lịch sử phát triển, tính năng mới và các bản sửa lỗi";
                    BtnClose.Content = "Đóng";
                    SearchBox.Tag = "Tìm kiếm tính năng, phiên bản...";
                }
                else
                {
                    TxtWindowTitle.Text = "HyperLis - Changelog";
                    TxtWindowSubtitle.Text = "Full release history, new features and bug fixes";
                    BtnClose.Content = "Close";
                    SearchBox.Tag = "Search features, versions...";
                }
            }
            catch { }
        }

        private void LoadAllChangelogs()
        {
            try
            {
                var lang = (LocalizationService.CurrentLanguage ?? "EN").ToLower();
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Changelog", $"{lang}.json");

                if (!File.Exists(path))
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Changelog", "en.json");
                }

                if (!File.Exists(path))
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Changelog", "vn.json");
                }

                if (!File.Exists(path)) return;

                string json = File.ReadAllText(path);
                var allVersions = JsonSerializer.Deserialize<Dictionary<string, DashboardView.ChangelogData>>(json);
                if (allVersions == null || allVersions.Count == 0) return;

                // Semantic sorting helper
                var sortedKeys = allVersions.Keys
                    .OrderByDescending(k => ParseSemanticVersion(k))
                    .ToList();

                var dateMap = new Dictionary<string, string>
                {
                    { "v1.0.7", "2026-11-05" },
                    { "v1.0.6", "2026-10-20" },
                    { "v1.0.5", "2026-09-12" },
                    { "v1.0.4", "2026-08-03" },
                    { "v1.0.3", "2026-07-15" },
                    { "v1.0.2", "2026-06-20" },
                    { "v1.0.1", "2026-05-10" },
                    { "v1.0.0", "2026-04-01" },
                    { "v0.1.1", "2026-02-15" },
                    { "v0.1.0", "2026-01-01" }
                };

                var brushMap = new SolidColorBrush[]
                {
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")), // Green (Latest)
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4")), // Blue
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6366F1")), // Indigo
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")), // Amber
                };

                _allChangelogs.Clear();
                int idx = 0;
                foreach (var key in sortedKeys)
                {
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

                    var item = new FullChangelogItem
                    {
                        VersionName = data.version_value ?? key,
                        ReleaseDate = dateStr,
                        IsLatest = (idx == 0),
                        DotBrush = (idx == 0) ? brushMap[0] : brushMap[(idx % (brushMap.Length - 1)) + 1],
                        Content = data.content ?? new List<string>()
                    };

                    _allChangelogs.Add(item);
                    idx++;
                }

                FilterChangelogs("");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChangelogWindow] Error loading changelogs: {ex.Message}");
            }
        }

        private static Version ParseSemanticVersion(string key)
        {
            try
            {
                var match = Regex.Match(key, @"\bv?(\d+(\.\d+)+)");
                if (match.Success && Version.TryParse(match.Groups[1].Value, out var v))
                {
                    return v;
                }
            }
            catch { }
            return new Version(0, 0, 0);
        }

        private void FilterChangelogs(string query)
        {
            _displayedChangelogs.Clear();
            query = query?.Trim().ToLower() ?? "";

            bool isVn = (LocalizationService.CurrentLanguage ?? "").Equals("VN", StringComparison.OrdinalIgnoreCase);

            foreach (var item in _allChangelogs)
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    _displayedChangelogs.Add(item);
                }
                else
                {
                    bool matchVersion = item.VersionName.ToLower().Contains(query);
                    bool matchContent = item.Content.Any(c => c.ToLower().Contains(query));

                    if (matchVersion || matchContent)
                    {
                        // If matching content, create a filtered copy so the user sees matching bullet points
                        var filteredItem = new FullChangelogItem
                        {
                            VersionName = item.VersionName,
                            ReleaseDate = item.ReleaseDate,
                            IsLatest = item.IsLatest,
                            DotBrush = item.DotBrush,
                            Content = matchVersion ? item.Content : item.Content.Where(c => c.ToLower().Contains(query)).ToList()
                        };
                        _displayedChangelogs.Add(filteredItem);
                    }
                }
            }

            string unit = isVn ? "phiên bản" : "releases";
            TxtTotalReleases.Text = $"{_displayedChangelogs.Count} {unit}";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterChangelogs(SearchBox.Text);
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GitHubReleases_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://github.com/ZitTechLab/HyperLis/releases")
                {
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}

