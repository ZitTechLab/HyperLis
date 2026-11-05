using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HyperLisUI.Models;

namespace HyperLisUI.Helpers
{
    internal static class StartupHelper
    {
        internal static readonly string LocalMachineRun = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        internal static readonly string LocalMachineRunOnce = "Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce";
        internal static readonly string LocalMachineRunWoW = "Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run";
        internal static readonly string LocalMachineRunOnceWow = "Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce";
        internal static readonly string CurrentUserRun = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        internal static readonly string CurrentUserRunOnce = "Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce";

        private static void GetRegistryStartupItemsHelper(ref List<StartupItem> list, StartupItemLocation location, StartupItemType type)
        {
            string keyPath = string.Empty;
            RegistryHive hiveType = RegistryHive.LocalMachine; 
            RegistryKey hive = null;

            if (location == StartupItemLocation.HKLM)
            {
                hive = Registry.LocalMachine;
                hiveType = RegistryHive.LocalMachine;
                if (type == StartupItemType.Run) keyPath = LocalMachineRun;
                else if (type == StartupItemType.RunOnce) keyPath = LocalMachineRunOnce;
            }
            else if (location == StartupItemLocation.HKLMWoW)
            {
                hive = Registry.LocalMachine;
                hiveType = RegistryHive.LocalMachine;
                if (type == StartupItemType.Run) keyPath = LocalMachineRunWoW;
                else if (type == StartupItemType.RunOnce) keyPath = LocalMachineRunOnceWow;
            }
            else if (location == StartupItemLocation.HKCU)
            {
                hive = Registry.CurrentUser;
                hiveType = RegistryHive.CurrentUser;
                if (type == StartupItemType.Run) keyPath = CurrentUserRun;
                else if (type == StartupItemType.RunOnce) keyPath = CurrentUserRunOnce;
            }

            if (hive != null)
            {
                // Read Enabled Items
                ReadRegistryKeyItems(ref list, hive, hiveType, keyPath, location, type, false);
                // Read Disabled Items
                ReadRegistryKeyItems(ref list, hive, hiveType, keyPath + "Disabled", location, type, true);
            }
        }

        private static void ReadRegistryKeyItems(ref List<StartupItem> list, RegistryKey hive, RegistryHive hiveType, string fullKeyPath, StartupItemLocation location, StartupItemType type, bool isDisabled)
        {
            try
            {
                using (RegistryKey key = hive.OpenSubKey(fullKeyPath, true))
                {
                    if (key != null)
                    {
                        string[] valueNames = key.GetValueNames();

                        foreach (string x in valueNames)
                        {
                            try
                            {
                                RegistryStartupItem item = new RegistryStartupItem();
                                item.Name = x;
                                item.FileLocation = key.GetValue(x)?.ToString() ?? string.Empty;
                                
                                item.Hive = hiveType;
                                // We store the original keyPath so Enable/Disable works correctly
                                item.KeyPath = isDisabled ? fullKeyPath.Substring(0, fullKeyPath.Length - 8) : fullKeyPath;

                                item.RegistryLocation = location;
                                item.StartupType = type;
                                item.IsDisabled = isDisabled;

                                list.Add(item);
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }

        private static void GetFolderStartupItemsHelper(ref List<StartupItem> list, IEnumerable<string> files, IEnumerable<string> shortcuts, StartupItemLocation folderOrigin)
        {
            foreach (string file in files)
            {
                try
                {
                    bool isDisabled = file.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
                    
                    FolderStartupItem item = new FolderStartupItem();
                    item.Name = Path.GetFileNameWithoutExtension(isDisabled ? file.Substring(0, file.Length - 9) : file);
                    item.FileLocation = file;
                    item.Shortcut = file;
                    item.RegistryLocation = folderOrigin;
                    item.IsDisabled = isDisabled;

                    list.Add(item);
                }
                catch { }
            }

            foreach (string shortcut in shortcuts)
            {
                try
                {
                    bool isDisabled = shortcut.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
                    
                    FolderStartupItem item = new FolderStartupItem();
                    item.Name = Path.GetFileNameWithoutExtension(isDisabled ? shortcut.Substring(0, shortcut.Length - 9) : shortcut);
                    item.FileLocation = Utilities.GetShortcutTargetFile(shortcut);
                    item.Shortcut = shortcut;
                    item.RegistryLocation = folderOrigin;
                    item.IsDisabled = isDisabled;

                    list.Add(item);
                }
                catch { }
            }
        }

        private static void GetTaskSchedulerStartupItemsHelper(ref List<StartupItem> list)
        {
            try
            {
                string script = @"Get-ScheduledTask | Where-Object { $_.Triggers.CimClass.CimClassName -contains 'MSFT_TaskLogonTrigger' -or $_.Triggers.CimClass.CimClassName -contains 'MSFT_TaskBootTrigger' } | Select-Object TaskName, TaskPath, @{Name='StateStr';Expression={$_.State.ToString()}}, @{Name='Action';Expression={$_.Actions[0].Execute}} | ConvertTo-Json -Compress";
                var psi = new System.Diagnostics.ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        using (var doc = System.Text.Json.JsonDocument.Parse(output))
                        {
                            var root = doc.RootElement;
                            if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var element in root.EnumerateArray())
                                {
                                    try
                                    {
                                        string taskName = element.GetProperty("TaskName").GetString();
                                        string taskPath = element.GetProperty("TaskPath").GetString();
                                        string stateStr = element.GetProperty("StateStr").GetString();
                                        string action = element.TryGetProperty("Action", out var act) ? act.GetString() : "";

                                        bool isDisabled = string.Equals(stateStr, "Disabled", StringComparison.OrdinalIgnoreCase);

                                        TaskStartupItem item = new TaskStartupItem
                                        {
                                            Name = taskName,
                                            FileLocation = action,
                                            TaskPathAndName = (taskPath == "\\" ? "" : taskPath) + "\\" + taskName,
                                            RegistryLocation = StartupItemLocation.TaskScheduler,
                                            StartupType = StartupItemType.Task,
                                            IsDisabled = isDisabled
                                        };
                                        list.Add(item);
                                    }
                                    catch { }
                                }
                            }
                            else if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                try
                                {
                                    string taskName = root.GetProperty("TaskName").GetString();
                                    string taskPath = root.GetProperty("TaskPath").GetString();
                                    string stateStr = root.GetProperty("StateStr").GetString();
                                    string action = root.TryGetProperty("Action", out var act) ? act.GetString() : "";

                                    bool isDisabled = string.Equals(stateStr, "Disabled", StringComparison.OrdinalIgnoreCase);

                                    TaskStartupItem item = new TaskStartupItem
                                    {
                                        Name = taskName,
                                        FileLocation = action,
                                        TaskPathAndName = (taskPath == "\\" ? "" : taskPath) + "\\" + taskName,
                                        RegistryLocation = StartupItemLocation.TaskScheduler,
                                        StartupType = StartupItemType.Task,
                                        IsDisabled = isDisabled
                                    };
                                    list.Add(item);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        internal static List<StartupItem> GetStartupItems()
        {
            List<StartupItem> startupItems = new List<StartupItem>();

            GetRegistryStartupItemsHelper(ref startupItems, StartupItemLocation.HKLM, StartupItemType.Run);
            GetRegistryStartupItemsHelper(ref startupItems, StartupItemLocation.HKLM, StartupItemType.RunOnce);

            GetRegistryStartupItemsHelper(ref startupItems, StartupItemLocation.HKCU, StartupItemType.Run);
            GetRegistryStartupItemsHelper(ref startupItems, StartupItemLocation.HKCU, StartupItemType.RunOnce);

            if (Environment.Is64BitOperatingSystem)
            {
                GetRegistryStartupItemsHelper(ref startupItems, StartupItemLocation.HKLMWoW, StartupItemType.Run);
                GetRegistryStartupItemsHelper(ref startupItems, StartupItemLocation.HKLMWoW, StartupItemType.RunOnce);
            }

            if (Directory.Exists(CleanHelper.CurrentUserStartupFolder))
            {
                IEnumerable<string> currentUserFiles = Directory.EnumerateFiles(CleanHelper.CurrentUserStartupFolder, "*.*", SearchOption.AllDirectories)
                    .Where(s => s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || 
                                s.EndsWith(".exe.disabled", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".bat.disabled", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".cmd.disabled", StringComparison.OrdinalIgnoreCase));

                IEnumerable<string> currentUserShortcuts = Directory.EnumerateFiles(CleanHelper.CurrentUserStartupFolder, "*.*", SearchOption.AllDirectories)
                    .Where(s => s.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".lnk.disabled", StringComparison.OrdinalIgnoreCase));

                GetFolderStartupItemsHelper(ref startupItems, currentUserFiles, currentUserShortcuts, StartupItemLocation.CUStartupFolder);
            }

            if (Directory.Exists(CleanHelper.LocalMachineStartupFolder))
            {
                IEnumerable<string> localMachineFiles = Directory.EnumerateFiles(CleanHelper.LocalMachineStartupFolder, "*.*", SearchOption.AllDirectories)
                    .Where(s => s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
                                s.EndsWith(".exe.disabled", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".bat.disabled", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".cmd.disabled", StringComparison.OrdinalIgnoreCase));

                IEnumerable<string> localMachineShortcuts = Directory.EnumerateFiles(CleanHelper.LocalMachineStartupFolder, "*.*", SearchOption.AllDirectories)
                    .Where(s => s.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".lnk.disabled", StringComparison.OrdinalIgnoreCase));

                GetFolderStartupItemsHelper(ref startupItems, localMachineFiles, localMachineShortcuts, StartupItemLocation.LMStartupFolder);
            }

            GetTaskSchedulerStartupItemsHelper(ref startupItems);

            return startupItems;
        }
    }
}
