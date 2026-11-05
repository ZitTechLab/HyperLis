using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HyperLisUI.Models;

namespace HyperLisUI.Helpers
{
    public static class DownloadHelper
    {
        private static readonly string JsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Download", "applications.json");
        private static readonly string ImagesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Apps");

        private const int InstallTimeoutMs = 12 * 60 * 1000;
        private const int UninstallTimeoutMs = 10 * 60 * 1000;
        private const int RefreshTimeoutMs = 20 * 1000;
        private const int WingetVerifyTimeoutMs = 15 * 1000;
        private const int ChocoVerifyTimeoutMs = 10 * 1000;

        public static async Task<List<CategoryGroup>> LoadAppsAsync()
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(JsonPath)) return new List<CategoryGroup>();

                try
                {
                    string json = File.ReadAllText(JsonPath);
                    var rawData = JsonSerializer.Deserialize<Dictionary<string, AppJson>>(json);

                    if (rawData == null) return new List<CategoryGroup>();

                    var apps = new List<AppItem>();
                    var iconFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (Directory.Exists(ImagesPath))
                    {
                        foreach (var file in Directory.GetFiles(ImagesPath))
                        {
                            iconFiles[Path.GetFileNameWithoutExtension(file)] = file;
                        }
                    }

                    foreach (var kvp in rawData)
                    {
                        string id = kvp.Key;
                        var data = kvp.Value;
                        string? iconPath = null;

                        if (iconFiles.TryGetValue(id, out string? realPath))
                        {
                            iconPath = realPath;
                        }

                        apps.Add(new AppItem
                        {
                            Id = id,
                            Name = data.content,
                            Description = data.description,
                            Category = data.category,
                            Link = data.link,
                            ChocoId = data.choco,
                            WingetId = data.winget,
                            IconPath = iconPath,
                            IsSelected = false,
                            IsInstalled = false
                        });
                    }

                    return apps
                        .GroupBy(a => a.Category)
                        .Select(g => new CategoryGroup
                        {
                            CategoryName = g.Key,
                            Apps = g.ToList()
                        })
                        .OrderBy(g => g.CategoryName)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading apps: {ex.Message}");
                    Logger.LogCrash(ex, nameof(DownloadHelper) + ".LoadAppsAsync");
                    return new List<CategoryGroup>();
                }
            });
        }

        public static async Task UpdateInstalledStatusAsync(List<CategoryGroup> categories)
        {
            if (categories == null || categories.Count == 0)
            {
                return;
            }

            var chocoTask = GetInstalledChocoPackages(RefreshTimeoutMs, CommandKind.Refresh);
            var wingetTask = GetInstalledWingetPackages(RefreshTimeoutMs, CommandKind.Refresh);

            await Task.WhenAll(chocoTask, wingetTask);

            ApplyInstalledState(
                categories.SelectMany(group => group.Apps ?? Enumerable.Empty<AppItem>()),
                chocoTask.Result,
                wingetTask.Result);
        }

        public static async Task<HashSet<string>> GetInstalledChocoPackages(int timeoutMs = RefreshTimeoutMs, CommandKind commandKind = CommandKind.Refresh)
        {
            var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                string cmd = $"{GetChocoCommand()} list --local-only --limit-output";
                var result = await Utilities.RunCommandMonitoredAsync(cmd, commandKind, timeoutMs, captureOutput: true);
                if (result.TimedOut)
                {
                    Logger.LogInfo("DOWNLOAD", "Chocolatey refresh timed out.");
                    return installed;
                }

                if (string.IsNullOrWhiteSpace(result.StdOut))
                {
                    return installed;
                }

                using var reader = new StringReader(result.StdOut);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('|');
                    if (parts.Length > 0)
                    {
                        installed.Add(parts[0].Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting choco packages: {ex.Message}");
                Logger.LogCrash(ex, nameof(DownloadHelper) + ".GetInstalledChocoPackages");
            }
            return installed;
        }

        public static async Task<HashSet<string>> GetInstalledWingetPackages(int timeoutMs = RefreshTimeoutMs, CommandKind commandKind = CommandKind.Refresh)
        {
            var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string psCommand = $"& '{WingetPath}' list | Out-String -Width 512";
                string cmd = $"powershell -NoProfile -Command \"{psCommand}\"";

                var result = await Utilities.RunCommandMonitoredAsync(cmd, commandKind, timeoutMs, captureOutput: true);
                if (result.TimedOut)
                {
                    Logger.LogInfo("DOWNLOAD", "Winget refresh timed out.");
                    return installed;
                }

                if (string.IsNullOrWhiteSpace(result.StdOut)) return installed;

                using var reader = new StringReader(result.StdOut);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.StartsWith("Name") && line.Contains("Id")) continue;
                    if (line.StartsWith("---")) continue;

                    var tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var token in tokens)
                    {
                        if (token.Length > 2)
                        {
                            installed.Add(token.Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting winget packages: {ex.Message}");
                Logger.LogCrash(ex, nameof(DownloadHelper) + ".GetInstalledWingetPackages");
            }
            return installed;
        }

        private static string GetChocoCommand()
        {
            string chocoPath = @"C:\ProgramData\chocolatey\bin\choco.exe";
            if (File.Exists(chocoPath))
            {
                return $"\"{chocoPath}\"";
            }
            return "choco";
        }

        public static async Task<List<PackageOperationResult>> InstallPackages(List<AppItem> apps, Action<double>? progressCallback = null)
        {
            return await RunPackageOperationsAsync(apps, CommandKind.Install, InstallTimeoutMs, progressCallback);
        }

        public static async Task<List<PackageOperationResult>> UninstallPackages(List<AppItem> apps, Action<double>? progressCallback = null)
        {
            return await RunPackageOperationsAsync(apps, CommandKind.Uninstall, UninstallTimeoutMs, progressCallback);
        }

        public static async Task<List<PackageOperationResult>> VerifyInstalledAppsAsync(IEnumerable<AppItem> apps, bool expectedInstalled)
        {
            var results = new List<PackageOperationResult>();
            if (apps == null)
            {
                return results;
            }

            foreach (var app in apps.Where(a => a != null).Distinct())
            {
                var packageManager = GetPackageManager(app);
                if (packageManager == PackageManagerKind.Unknown)
                {
                    results.Add(new PackageOperationResult
                    {
                        App = app,
                        PackageManager = packageManager,
                        Status = PackageOperationStatus.VerificationDeferred,
                        Message = "No supported package manager metadata found."
                    });
                    continue;
                }

                CommandExecutionResult execution;
                if (packageManager == PackageManagerKind.Winget)
                {
                    string wingetId = app.WingetId ?? string.Empty;
                    string source = IsMsStoreApp(app) ? "msstore" : "winget";
                    string psCommand = $"& '{WingetPath}' list --id '{EscapePowerShellValue(wingetId)}' --exact --source {source} | Out-String -Width 512";
                    execution = await Utilities.RunCommandMonitoredAsync(
                        $"powershell -NoProfile -Command \"{psCommand}\"",
                        CommandKind.Verify,
                        WingetVerifyTimeoutMs,
                        captureOutput: true);
                }
                else
                {
                    string chocoId = SanitizePackageId(app.ChocoId);
                    string cmd = $"{GetChocoCommand()} list --local-only {chocoId} --limit-output";
                    execution = await Utilities.RunCommandMonitoredAsync(
                        cmd,
                        CommandKind.Verify,
                        ChocoVerifyTimeoutMs,
                        captureOutput: true);
                }

                if (execution.TimedOut)
                {
                    results.Add(new PackageOperationResult
                    {
                        App = app,
                        PackageManager = packageManager,
                        Status = PackageOperationStatus.VerificationDeferred,
                        Execution = execution,
                        Message = "Verification timed out."
                    });
                    continue;
                }

                bool isInstalled = packageManager == PackageManagerKind.Winget
                    ? OutputIndicatesWingetPackagePresent(execution.StdOut, app.WingetId)
                    : OutputIndicatesChocolateyPackagePresent(execution.StdOut, app.ChocoId);

                results.Add(new PackageOperationResult
                {
                    App = app,
                    PackageManager = packageManager,
                    Status = isInstalled == expectedInstalled ? PackageOperationStatus.Succeeded : PackageOperationStatus.Failed,
                    Execution = execution,
                    DetectedInstalledState = isInstalled,
                    Message = isInstalled == expectedInstalled ? "Verification succeeded." : "Verification result did not match expected state."
                });
            }

            return results;
        }

        private static async Task<List<PackageOperationResult>> RunPackageOperationsAsync(
            List<AppItem> apps,
            CommandKind commandKind,
            int timeoutMs,
            Action<double>? progressCallback)
        {
            var results = new List<PackageOperationResult>();
            if (apps == null || apps.Count == 0) return results;

            int total = apps.Count;
            int processed = 0;

            foreach (var app in apps)
            {
                var packageManager = GetPackageManager(app);
                PackageOperationResult operationResult;

                if (packageManager == PackageManagerKind.Unknown)
                {
                    operationResult = new PackageOperationResult
                    {
                        App = app,
                        PackageManager = packageManager,
                        Status = PackageOperationStatus.Failed,
                        Message = "No supported package manager metadata found."
                    };
                }
                else if (commandKind == CommandKind.Uninstall && packageManager == PackageManagerKind.Chocolatey)
                {
                    Logger.LogInfo("DOWNLOAD", $"{commandKind} started for {app.Name} via {packageManager}.");
                    operationResult = await RunChocolateyUninstallAsync(app, timeoutMs);
                }
                else
                {
                    string command = BuildPackageCommand(app, packageManager, commandKind);
                    Logger.LogInfo("DOWNLOAD", $"{commandKind} started for {app.Name} via {packageManager}.");
                    var execution = await Utilities.RunCommandMonitoredAsync(command, commandKind, timeoutMs, captureOutput: false, windowStyle: ProcessWindowStyle.Minimized);

                    operationResult = new PackageOperationResult
                    {
                        App = app,
                        PackageManager = packageManager,
                        Execution = execution,
                        Status = MapCommandResultStatus(execution, commandKind),
                        Message = BuildOperationMessage(execution)
                    };
                }

                results.Add(operationResult);
                processed++;
                progressCallback?.Invoke((double)processed / total * 100);
                Logger.LogInfo("DOWNLOAD", $"{commandKind} finished for {app.Name}. Status={operationResult.Status}.");
            }

            return results;
        }

        private static async Task<PackageOperationResult> RunChocolateyUninstallAsync(AppItem app, int timeoutMs)
        {
            string baseId = SanitizePackageId(app.ChocoId);
            var installedPackages = await GetInstalledChocoPackages(RefreshTimeoutMs, CommandKind.Refresh);
            var uninstallTargets = GetChocolateyPackageAliases(baseId)
                .Where(installedPackages.Contains)
                .OrderBy(target => target.Equals(baseId, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ToList();

            if (uninstallTargets.Count == 0)
            {
                return new PackageOperationResult
                {
                    App = app,
                    PackageManager = PackageManagerKind.Chocolatey,
                    Status = PackageOperationStatus.Succeeded,
                    Message = "Package is already absent."
                };
            }

            CommandExecutionResult? lastExecution = null;
            bool anyTimedOut = false;
            var messageParts = new List<string>();

            foreach (string target in uninstallTargets)
            {
                string uninstallCmd = $"{GetChocoCommand()} uninstall {target} -y";
                lastExecution = await Utilities.RunCommandMonitoredAsync(uninstallCmd, CommandKind.Uninstall, timeoutMs, captureOutput: false, windowStyle: ProcessWindowStyle.Minimized);
                messageParts.Add($"{target}: {BuildOperationMessage(lastExecution)}");

                if (lastExecution.TimedOut)
                {
                    anyTimedOut = true;
                    continue;
                }

                if (!IsSuccessfulUninstallExitCode(lastExecution.ExitCode))
                {
                    string forceCmd = $"{GetChocoCommand()} uninstall {target} -y -n --skip-autouninstaller";
                    Logger.LogInfo("DOWNLOAD", $"Retrying uninstall for {app.Name} target {target} with Chocolatey force options.");
                    lastExecution = await Utilities.RunCommandMonitoredAsync(forceCmd, CommandKind.Uninstall, timeoutMs, captureOutput: false, windowStyle: ProcessWindowStyle.Minimized);
                    messageParts.Add($"{target} force: {BuildOperationMessage(lastExecution)}");
                    if (lastExecution.TimedOut)
                    {
                        anyTimedOut = true;
                    }
                }
            }

            var remainingPackages = await GetInstalledChocoPackages(RefreshTimeoutMs, CommandKind.Refresh);
            bool stillInstalled = GetChocolateyPackageAliases(baseId).Any(remainingPackages.Contains);

            return new PackageOperationResult
            {
                App = app,
                PackageManager = PackageManagerKind.Chocolatey,
                Execution = lastExecution,
                Status = stillInstalled
                    ? (anyTimedOut ? PackageOperationStatus.TimedOut : PackageOperationStatus.Failed)
                    : PackageOperationStatus.Succeeded,
                Message = stillInstalled
                    ? $"Remaining Chocolatey packages detected for {baseId}. {string.Join(" | ", messageParts)}"
                    : string.Join(" | ", messageParts)
            };
        }

        private static string BuildPackageCommand(AppItem app, PackageManagerKind packageManager, CommandKind commandKind)
        {
            if (packageManager == PackageManagerKind.Winget)
            {
                string wingetId = SanitizePackageId(app.WingetId);
                string source = IsMsStoreApp(app) ? "msstore" : "winget";
                string exePath = WingetPath == "winget" ? "winget" : $"\"{WingetPath}\"";
                string cmd;
                if (commandKind == CommandKind.Install)
                {
                    cmd = $"{exePath} install --id {wingetId} --exact --source {source} --accept-source-agreements --accept-package-agreements";
                }
                else
                {
                    cmd = $"{exePath} uninstall --id {wingetId} --exact --source {source} --accept-source-agreements";
                }
                return WingetPath == "winget" ? cmd : $"\"{cmd}\"";
            }

            string chocoId = SanitizePackageId(app.ChocoId);
            if (commandKind == CommandKind.Install)
            {
                return $"{GetChocoCommand()} install {chocoId} -y";
            }

            return $"{GetChocoCommand()} uninstall {chocoId} -y";
        }

        private static PackageOperationStatus MapCommandResultStatus(CommandExecutionResult execution, CommandKind commandKind)
        {
            if (execution.TimedOut)
            {
                return PackageOperationStatus.TimedOut;
            }

            bool success = commandKind == CommandKind.Uninstall
                ? IsSuccessfulUninstallExitCode(execution.ExitCode)
                : IsSuccessfulInstallExitCode(execution.ExitCode);

            return success ? PackageOperationStatus.Succeeded : PackageOperationStatus.Failed;
        }

        private static bool IsSuccessfulInstallExitCode(int exitCode)
        {
            return exitCode == 0 || exitCode == 1641 || exitCode == 3010;
        }

        private static bool IsSuccessfulUninstallExitCode(int exitCode)
        {
            return exitCode == 0 || exitCode == 1605 || exitCode == 1614 || exitCode == 1641 || exitCode == 3010;
        }

        private static string BuildOperationMessage(CommandExecutionResult execution)
        {
            if (execution.TimedOut)
            {
                return "Command timed out.";
            }

            if (!string.IsNullOrWhiteSpace(execution.StdErr))
            {
                return execution.StdErr.Trim();
            }

            if (!string.IsNullOrWhiteSpace(execution.StdOut))
            {
                return execution.StdOut.Trim();
            }

            return execution.ExitCode == 0 ? "Command completed." : $"Command failed with exit code {execution.ExitCode}.";
        }

        private static PackageManagerKind GetPackageManager(AppItem app)
        {
            if (IsWingetApp(app))
            {
                return PackageManagerKind.Winget;
            }

            if (!string.IsNullOrWhiteSpace(app.ChocoId) &&
                !app.ChocoId.Equals("na", StringComparison.OrdinalIgnoreCase))
            {
                return PackageManagerKind.Chocolatey;
            }

            return PackageManagerKind.Unknown;
        }

        private static void ApplyInstalledState(IEnumerable<AppItem> apps, HashSet<string> installedChoco, HashSet<string> installedWinget)
        {
            foreach (var app in apps)
            {
                bool installed = IsChocolateyAppInstalled(app, installedChoco);

                if (!installed && !string.IsNullOrEmpty(app.WingetId) && installedWinget.Contains(app.WingetId))
                {
                    installed = true;
                }

                app.IsInstalled = installed;
            }
        }

        private static bool IsChocolateyAppInstalled(AppItem app, HashSet<string> installedChoco)
        {
            if (string.IsNullOrWhiteSpace(app.ChocoId) || app.ChocoId.Equals("na", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return GetChocolateyPackageAliases(app.ChocoId).Any(installedChoco.Contains);
        }

        private static IEnumerable<string> GetChocolateyPackageAliases(string? chocoId)
        {
            string baseId = SanitizePackageId(chocoId);
            if (string.IsNullOrWhiteSpace(baseId))
            {
                return Enumerable.Empty<string>();
            }

            if (baseId.EndsWith(".install", StringComparison.OrdinalIgnoreCase) ||
                baseId.EndsWith(".portable", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { baseId };
            }

            return new[]
            {
                baseId,
                baseId + ".install",
                baseId + ".portable"
            };
        }

        private static bool OutputIndicatesWingetPackagePresent(string output, string? wingetId)
        {
            if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(wingetId))
            {
                return false;
            }

            if (output.IndexOf("No installed package found", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            using var reader = new StringReader(output);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.IndexOf(wingetId, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool OutputIndicatesChocolateyPackagePresent(string output, string? chocoId)
        {
            if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(chocoId))
            {
                return false;
            }

            var aliases = GetChocolateyPackageAliases(chocoId);
            using var reader = new StringReader(output);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                foreach (string alias in aliases)
                {
                    if (line.StartsWith(alias + "|", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsWingetApp(AppItem app)
        {
            return (string.IsNullOrEmpty(app.ChocoId) || app.ChocoId.Equals("na", StringComparison.OrdinalIgnoreCase))
                   && !string.IsNullOrEmpty(app.WingetId);
        }

        private static bool IsMsStoreApp(AppItem app)
        {
            return !string.IsNullOrEmpty(app.WingetId) && !app.WingetId.Contains('.');
        }

        private static string SanitizePackageId(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace("\"", string.Empty).Trim();
        }

        private static string EscapePowerShellValue(string value)
        {
            return value.Replace("'", "''");
        }

        public static async Task UpdateAll()
        {
            string cmdChoco = $"{GetChocoCommand()} upgrade all -y";
            string exePath = WingetPath == "winget" ? "winget" : $"\"{WingetPath}\"";
            string cmdWinget = $"{exePath} upgrade --all --silent --accept-source-agreements --accept-package-agreements";
            if (WingetPath != "winget") cmdWinget = $"\"{cmdWinget}\"";

            var chocoResult = await Utilities.RunCommandMonitoredAsync(cmdChoco, CommandKind.Install, InstallTimeoutMs, captureOutput: false, windowStyle: ProcessWindowStyle.Minimized);
            Logger.LogInfo("DOWNLOAD", $"UpdateAll Chocolatey completed with exit code {chocoResult.ExitCode}.");

            var wingetResult = await Utilities.RunCommandMonitoredAsync(cmdWinget, CommandKind.Install, InstallTimeoutMs, captureOutput: false, windowStyle: ProcessWindowStyle.Minimized);
            Logger.LogInfo("DOWNLOAD", $"UpdateAll Winget completed with exit code {wingetResult.ExitCode}.");
        }

        public static async Task<bool> IsChocoInstalled()
        {
            if (File.Exists(@"C:\ProgramData\chocolatey\bin\choco.exe")) return true;

            var result = await Utilities.RunCommandAsync("choco -v", true);
            string version = result.Output;
            return !string.IsNullOrWhiteSpace(version) && !version.ToLower().Contains("not recognized");
        }

        public static async Task InstallChoco()
        {
             string installCmd = @"powershell -NoProfile -ExecutionPolicy Bypass -Command ""Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))""";
             await Utilities.RunCommandAsync(installCmd);
        }

        public static string WingetPath { get; private set; } = "winget";

        public static async Task<bool> DetermineWingetPathAsync()
        {
            try
            {
                var result = await Utilities.RunCommandAsync("winget -v", true);
                string output = result.Output?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(output) && !output.ToLower().Contains("not recognized") && !output.ToLower().Contains("is not recognized"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(output, @"(\d+)\.(\d+)");
                    if (match.Success)
                    {
                        int major = int.Parse(match.Groups[1].Value);
                        int minor = int.Parse(match.Groups[2].Value);
                        if (major > 1 || (major == 1 && minor >= 6))
                        {
                            WingetPath = "winget";
                            return true;
                        }
                    }
                }
            }
            catch { }

            string portablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "winget-cli", "winget.exe");
            if (File.Exists(portablePath))
            {
                WingetPath = portablePath;
                return false;
            }

            WingetPath = "winget";
            return true;
        }

        public static async Task InstallWinget()
        {
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "winget-install.ps1");
            if (!File.Exists(scriptPath))
            {
                Logger.LogInfo("DOWNLOAD", $"Winget install script not found at: {scriptPath}");
                return;
            }

            string installCmd = $"powershell -NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"";
            await Utilities.RunCommandAsync(installCmd);
        }
    }
}

