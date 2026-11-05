using Microsoft.Win32;
using System;
using System.IO;
using HyperLisUI.Helpers;

namespace HyperLisUI.Models
{
    public class StartupItem
    {
        public string Name { get; set; }
        public string FileLocation { get; set; }
        public StartupItemLocation RegistryLocation { get; set; }
        public StartupItemType StartupType { get; set; }
        public bool IsDisabled { get; set; }

        public virtual void Remove() { }
        public virtual void Disable() { }
        public virtual void Enable() { }
        public virtual void LocateFile() { }
        public virtual void LocateKey() { }

        public override string ToString()
        {
            if (RegistryLocation == StartupItemLocation.LMStartupFolder) return RegistryLocation.ToString();
            return string.Format("{0}:{1}", RegistryLocation, StartupType);
        }
    }

    public sealed class FolderStartupItem : StartupItem
    {
        public string Shortcut { get; set; }

        public override void Remove()
        {
            try
            {
                if (File.Exists(Shortcut)) File.Delete(Shortcut);
            }
            catch { }
        }

        public override void Disable()
        {
            try
            {
                if (File.Exists(Shortcut) && !IsDisabled)
                {
                    string newPath = Shortcut + ".disabled";
                    File.Move(Shortcut, newPath);
                    Shortcut = newPath;
                    IsDisabled = true;
                }
            }
            catch { }
        }

        public override void Enable()
        {
            try
            {
                if (File.Exists(Shortcut) && IsDisabled)
                {
                    string newPath = Shortcut.Replace(".disabled", "");
                    File.Move(Shortcut, newPath);
                    Shortcut = newPath;
                    IsDisabled = false;
                }
            }
            catch { }
        }

        public override void LocateFile()
        {
            try { Utilities.FindFile(FileLocation); } catch { }
        }
    }

    public sealed class RegistryStartupItem : StartupItem
    {
        public RegistryHive Hive { get; set; }
        public string KeyPath { get; set; }

        private string GetDisabledKeyPath()
        {
            // e.g. "Software\Microsoft\Windows\CurrentVersion\Run" -> "Software\Microsoft\Windows\CurrentVersion\RunDisabled"
            return KeyPath + "Disabled";
        }

        public override void LocateKey()
        {
            try
            {
                string root = Hive == RegistryHive.LocalMachine ? "HKEY_LOCAL_MACHINE" : "HKEY_CURRENT_USER";
                string fullPath = $"{root}\\{(IsDisabled ? GetDisabledKeyPath() : KeyPath)}";
                Utilities.FindKeyInRegistry(fullPath);
            }
            catch { }
        }

        public override void Remove()
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(Hive, RegistryView.Default))
                {
                    string targetPath = IsDisabled ? GetDisabledKeyPath() : KeyPath;
                    using (var key = baseKey.OpenSubKey(targetPath, true))
                    {
                        if (key != null) key.DeleteValue(Name, false);
                    }
                }
            }
            catch { }
        }

        public override void Disable()
        {
            try
            {
                if (IsDisabled) return;
                using (var baseKey = RegistryKey.OpenBaseKey(Hive, RegistryView.Default))
                {
                    using (var srcKey = baseKey.OpenSubKey(KeyPath, true))
                    using (var destKey = baseKey.CreateSubKey(GetDisabledKeyPath(), true))
                    {
                        if (srcKey != null && destKey != null)
                        {
                            object val = srcKey.GetValue(Name);
                            if (val != null)
                            {
                                destKey.SetValue(Name, val, srcKey.GetValueKind(Name));
                                srcKey.DeleteValue(Name, false);
                                IsDisabled = true;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public override void Enable()
        {
            try
            {
                if (!IsDisabled) return;
                using (var baseKey = RegistryKey.OpenBaseKey(Hive, RegistryView.Default))
                {
                    using (var srcKey = baseKey.OpenSubKey(GetDisabledKeyPath(), true))
                    using (var destKey = baseKey.OpenSubKey(KeyPath, true))
                    {
                        if (srcKey != null && destKey != null)
                        {
                            object val = srcKey.GetValue(Name);
                            if (val != null)
                            {
                                destKey.SetValue(Name, val, srcKey.GetValueKind(Name));
                                srcKey.DeleteValue(Name, false);
                                IsDisabled = false;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public override void LocateFile()
        {
            try
            {
                string path = SanitizePath(FileLocation);
                if (!string.IsNullOrEmpty(path)) Utilities.FindFile(path);
            }
            catch { }
        }

        internal static string SanitizePath(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            s = s.Replace("\"", string.Empty);
            int exeIndex = s.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exeIndex > -1) s = s.Substring(0, exeIndex + 4);
            return s.Trim();
        }
    }

    public sealed class TaskStartupItem : StartupItem
    {
        public string TaskPathAndName { get; set; }

        private void RunCmd(string cmd)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c " + cmd)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    p?.WaitForExit();
                }
            }
            catch { }
        }

        public override void Remove()
        {
            try
            {
                RunCmd($"schtasks /Delete /TN \"{TaskPathAndName}\" /F");
            }
            catch { }
        }

        public override void Disable()
        {
            try
            {
                if (!IsDisabled)
                {
                    RunCmd($"schtasks /Change /TN \"{TaskPathAndName}\" /DISABLE");
                    IsDisabled = true;
                }
            }
            catch { }
        }

        public override void Enable()
        {
            try
            {
                if (IsDisabled)
                {
                    RunCmd($"schtasks /Change /TN \"{TaskPathAndName}\" /ENABLE");
                    IsDisabled = false;
                }
            }
            catch { }
        }

        public override void LocateFile()
        {
            try
            {
                string path = RegistryStartupItem.SanitizePath(FileLocation);
                if (!string.IsNullOrEmpty(path)) Utilities.FindFile(path);
            }
            catch { }
        }
    }
}
