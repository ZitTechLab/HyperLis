using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using HyperLisUI.Models;

namespace HyperLisUI.Helpers
{
    internal static class Utilities
    {
        // Using reflection/dynamic for Shell32 to avoid direct dependency if possible
        // But for reliable shortcut resolution, COM interop is best.
        // We will try to resolve simple shortcuts or use a simple COM wrapper if available.
        // For now, let's implement the Shell32 part using dynamic to see if we can resolve standard COM.
        // Type.GetTypeFromProgID("WScript.Shell") is a common alternative.

        public static bool IsAdministrator =>
            new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent())
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

        public static bool IsWindows11
        {
            get
            {
                try
                {
                    using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                                                .OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    {
                        if (key != null)
                        {
                            var buildObj = key.GetValue("CurrentBuildNumber");
                            if (buildObj != null && int.TryParse(buildObj.ToString(), out int build))
                            {
                                return build >= 22000;
                            }
                        }
                    }
                }
                catch { }
                // Fallback
                return Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build >= 22000;
            }
        }

        public static void SetRegistryValue(string keyPath, string valueName, object value, RegistryValueKind kind = RegistryValueKind.DWord)
        {
             try
            {
                // Parse Hive
                RegistryHive hive = RegistryHive.LocalMachine;
                string subKeyPath = keyPath;

                if (keyPath.StartsWith("HKEY_LOCAL_MACHINE") || keyPath.StartsWith("HKLM"))
                {
                    hive = RegistryHive.LocalMachine;
                    subKeyPath = keyPath.Substring(keyPath.IndexOf('\\') + 1);
                }
                else if (keyPath.StartsWith("HKEY_CURRENT_USER") || keyPath.StartsWith("HKCU"))
                {
                    hive = RegistryHive.CurrentUser;
                    subKeyPath = keyPath.Substring(keyPath.IndexOf('\\') + 1);
                }

                using (var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                using (var key = baseKey.CreateSubKey(subKeyPath, true))
                {
                    key.SetValue(valueName, value, kind);
                }
            }
            catch (Exception ex)
            {
                 Debug.WriteLine($"Error writing registry {keyPath}\\{valueName}: {ex.Message}");
                 throw;
            }
        }

        public static bool TrySetRegistryValue(string keyPath, string valueName, object value, RegistryValueKind kind = RegistryValueKind.DWord)
        {
            try
            {
                SetRegistryValue(keyPath, valueName, value, kind);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static T GetRegistryValue<T>(string keyPath, string valueName, T defaultValue)
        {
            try
            {
                 // Parse Hive
                RegistryHive hive = RegistryHive.LocalMachine;
                string subKeyPath = keyPath;

                if (keyPath.StartsWith("HKEY_LOCAL_MACHINE") || keyPath.StartsWith("HKLM"))
                {
                    hive = RegistryHive.LocalMachine;
                    subKeyPath = keyPath.Substring(keyPath.IndexOf('\\') + 1);
                }
                else if (keyPath.StartsWith("HKEY_CURRENT_USER") || keyPath.StartsWith("HKCU"))
                {
                    hive = RegistryHive.CurrentUser;
                    subKeyPath = keyPath.Substring(keyPath.IndexOf('\\') + 1);
                }

                using (var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                using (var key = baseKey.OpenSubKey(subKeyPath))
                {
                    if (key != null)
                    {
                        object val = key.GetValue(valueName);
                        if (val != null)
                        {
                            return (T)Convert.ChangeType(val, typeof(T));
                        }
                    }
                }
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        internal static (int ExitCode, string Output, string Error) RunCommandWithOutput(string command)
        {
            if (string.IsNullOrEmpty(command)) return (-1, "", "Empty command");
            try
            {
                Logger.LogProcess("cmd.exe", $"/c {command}");
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using Process? p = Process.Start(psi);
                if (p == null)
                {
                    return (-1, string.Empty, "Failed to start process.");
                }

                // Read output before waiting to avoid deadlocks
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();
                p.WaitForExit();
                return (p.ExitCode, output, error);
            }
            catch (Exception ex)
            {
                return (-1, "", ex.Message);
            }
        }

        internal static async Task<(int ExitCode, string Output)> RunCommandAsync(string command, bool returnOutput = false)
        {
            return await Task.Run(() =>
            {
                if (returnOutput)
                {
                    var result = RunCommandWithOutput(command);
                    return (result.ExitCode, result.Output);
                }
                else
                {
                    // User requested minimized window (hidden in taskbar but accessible)
                    int exitCode = RunCommand(command, ProcessWindowStyle.Minimized);
                    return (exitCode, string.Empty);
                }
            });
        }

        internal static int RunCommand(string command, ProcessWindowStyle windowStyle = ProcessWindowStyle.Hidden)
        {
            if (string.IsNullOrEmpty(command)) return -1;
            try
            {
                Logger.LogProcess("cmd.exe", $"/c {command}");
                ProcessStartInfo procesStartInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    UseShellExecute = false
                };

                if (windowStyle == ProcessWindowStyle.Hidden)
                {
                    procesStartInfo.CreateNoWindow = true;
                    procesStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }
                else
                {
                    procesStartInfo.CreateNoWindow = false;
                    procesStartInfo.WindowStyle = windowStyle;
                }

                using Process? p = Process.Start(procesStartInfo);
                if (p == null)
                {
                    return -1;
                }

                p.WaitForExit();
                return p.ExitCode;
            }
            catch { return -1; }
        }

        // Overload for backward compatibility
        internal static void RunCommand(string command, bool visible)
        {
            RunCommand(command, visible ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden);
        }

        internal static async Task RunCommandVisible(string command)
        {
            await Task.Run(() =>
            {
                try
                {
                    Logger.LogProcess("cmd.exe (Visible)", command);
                    // Use 'start /min /wait' to launch a separate, minimized console window AND WAIT for it to close.
                    // 'cmd /c' allows it to close automatically when done.
                    // It will be minimized in the taskbar, accessible if the user clicks it.
                    string arguments = $"/c start /min /wait \"HyperLis - Winget\" cmd /c \"{command}\"";

                    ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", arguments)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using Process? p = Process.Start(psi);
                    p?.WaitForExit();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"RunCommandVisible error: {ex.Message}");
                }
            });
        }

        internal static async Task<CommandExecutionResult> RunCommandMonitoredAsync(
            string command,
            CommandKind commandKind,
            int timeoutMs,
            bool captureOutput = true,
            ProcessWindowStyle windowStyle = ProcessWindowStyle.Hidden)
        {
            var result = new CommandExecutionResult
            {
                CommandKind = commandKind,
                CommandText = command,
                ExitCode = -1
            };

            if (string.IsNullOrWhiteSpace(command))
            {
                result.StdErr = "Empty command";
                return result;
            }

            var stopwatch = Stopwatch.StartNew();
            Logger.LogInfo("CMD-START", $"Kind={commandKind}; TimeoutMs={timeoutMs}; Command={command}");

            try
            {
                using Process process = new Process();
                process.StartInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    UseShellExecute = false,
                    CreateNoWindow = windowStyle == ProcessWindowStyle.Hidden,
                    WindowStyle = windowStyle,
                    RedirectStandardOutput = captureOutput,
                    RedirectStandardError = captureOutput
                };

                process.Start();

                Task<string>? outputTask = captureOutput ? process.StandardOutput.ReadToEndAsync() : null;
                Task<string>? errorTask = captureOutput ? process.StandardError.ReadToEndAsync() : null;
                Task waitForExitTask = process.WaitForExitAsync();
                Task timeoutTask = Task.Delay(timeoutMs);

                Task completedTask = await Task.WhenAny(waitForExitTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    result.TimedOut = true;
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(true);
                        }
                    }
                    catch (Exception killEx)
                    {
                        Debug.WriteLine($"Failed to kill timed out process: {killEx.Message}");
                    }
                }

                try
                {
                    await waitForExitTask;
                }
                catch (Exception waitEx)
                {
                    Debug.WriteLine($"WaitForExitAsync error: {waitEx.Message}");
                }

                if (captureOutput)
                {
                    result.StdOut = outputTask == null ? string.Empty : await outputTask;
                    result.StdErr = errorTask == null ? string.Empty : await errorTask;
                }

                result.ExitCode = result.TimedOut ? -1 : process.ExitCode;
            }
            catch (Exception ex)
            {
                result.StdErr = ex.ToString();
                result.ExitCode = -1;
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }

            Logger.LogInfo(
                "CMD-END",
                $"Kind={commandKind}; ExitCode={result.ExitCode}; TimedOut={result.TimedOut}; DurationMs={result.Duration.TotalMilliseconds:F0}; StdOut={TruncateForLog(result.StdOut)}; StdErr={TruncateForLog(result.StdErr)}");

            return result;
        }

        private static string TruncateForLog(string? value, int maxLength = 300)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string flattened = value.Replace("\r", " ").Replace("\n", " ").Trim();
            if (flattened.Length <= maxLength)
            {
                return flattened;
            }

            return flattened.Substring(0, maxLength) + "...";
        }

        internal static string GetShortcutTargetFile(string shortcutFilename)
        {
            string pathOnly = Path.GetDirectoryName(shortcutFilename);
            string filenameOnly = Path.GetFileName(shortcutFilename);

            try
            {
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic folder = shell.NameSpace(pathOnly);
                dynamic folderItem = folder.ParseName(filenameOnly);

                if (folderItem != null)
                {
                    // Shell32.ShellLinkObject
                    dynamic link = folderItem.GetLink;
                    return link.Path;
                }
            }
            catch
            {
                // Fallback or failure
            }

            return string.Empty;
        }

        internal static void FindFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                Process.Start("explorer.exe", $"/select, \"{fileName}\"");
            }
        }

        internal static void FindKeyInRegistry(string key)
        {
            try
            {
                // Note: Regedit LastKey only works if Regedit is not running usually, or requires some tricks.
                // We'll trust the original logic which sets the LastKey value.
                RegistryKey regKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit");
                regKey.SetValue("LastKey", key);
                regKey.Close();

                // Start regedit
                Process.Start("regedit.exe");
            }
            catch { }
        }

        // Helper to get registry key properly
        internal static void TryDeleteRegistryValue(bool localMachine, string path, string valueName)
        {
            try
            {
                RegistryHive hive = localMachine ? RegistryHive.LocalMachine : RegistryHive.CurrentUser;
                using (var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                using (var subKey = baseKey.OpenSubKey(path, true))
                {
                    if (subKey != null)
                    {
                        subKey.DeleteValue(valueName, false);
                    }
                }
            }
            catch { }
        }

        internal static void TryDeleteRegistryValueDefaultUsers(string path, string valueName)
        {
            try
            {
                // Load Default User Hive if acceptable, or just access HKEY_USERS\.DEFAULT
                // The reference uses Utilities.TryDeleteRegistryValueDefaultUsers(@".DEFAULT\...", ...)
                // implying the path argument starts with .DEFAULT

                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64))
                using (var subKey = baseKey.OpenSubKey(path, true))
                {
                    if (subKey != null)
                    {
                        subKey.DeleteValue(valueName, false);
                    }
                }
            }
            catch { }
        }

        internal static bool ServiceExists(string serviceName)
        {
            return Array.Exists(ServiceController.GetServices(), (serviceController => serviceController.ServiceName.Equals(serviceName)));
        }

        internal static void StopService(string serviceName)
        {
            if (ServiceExists(serviceName))
            {
                ServiceController sc = new ServiceController(serviceName);
                if (sc.CanStop)
                {
                    try
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
                    }
                    catch { }
                }
            }
        }

        internal static void StartService(string serviceName)
        {
            if (ServiceExists(serviceName))
            {
                ServiceController sc = new ServiceController(serviceName);

                try
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                }
                catch { }
            }
        }
        public static void PreventProcessFromRunning(string processName)
        {
            try
            {
                // Disallow execution via IFEO (Image File Execution Options)
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{processName}"))
                {
                    key.SetValue("Debugger", "systray.exe", RegistryValueKind.String);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error blocking process {processName}: {ex.Message}");
            }
        }

        public static void AllowProcessToRun(string processName)
        {
            try
            {
                // Remove IFEO entry
                Registry.LocalMachine.DeleteSubKeyTree($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{processName}", false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error allowing process {processName}: {ex.Message}");
            }
        }
        public static void StartProcess(string processName, string arguments = "")
        {
            try
            {
                Logger.LogProcess(processName, arguments);
                ProcessStartInfo startInfo = new ProcessStartInfo(processName);
                if (!string.IsNullOrEmpty(arguments))
                {
                    startInfo.Arguments = arguments;
                }
                startInfo.UseShellExecute = true; // Often needed for launching programs normally
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start process {processName}: {ex.Message}");
            }
        }
    }
}
