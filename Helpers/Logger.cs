using System;
using System.IO;

namespace HyperLisUI.Helpers
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_log.txt");
        private static readonly object _lockObj = new object();

        // Log activities intentionally requested by the user.
        public static void LogActivity(string activity)
        {
            WriteLog($"[ACTIVITY] {activity}");
        }

        // Log script executions and processes running behind the scenes.
        public static void LogProcess(string processName, string arguments = "")
        {
            string msg = string.IsNullOrEmpty(arguments) ? processName : $"{processName} {arguments}";
            WriteLog($"[PROCESS] {msg}");
        }

        public static void LogInfo(string category, string message)
        {
            WriteLog($"[{category}] {message}");
        }

        // Log application crashes.
        public static void LogCrash(Exception? ex, string source)
        {
            if (ex == null) return;
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logContent = $"[{timestamp}] [CRASH - {source}]:\n{ex.Message}\nChi tiết StackTrace:\n{ex.StackTrace}\n\n";

                if (ex.InnerException != null)
                {
                     logContent += $"Căn nguyên (InnerException):\n{ex.InnerException.Message}\n{ex.InnerException.StackTrace}\n\n";
                }

                if (ex is System.Reflection.ReflectionTypeLoadException rtle)
                {
                    logContent += "==== Báo cáo lỗi nạp thư viện (DLL) ====\n";
                    foreach (var loaderEx in rtle.LoaderExceptions)
                    {
                        if (loaderEx != null)
                            logContent += $"- {loaderEx.Message}\n";
                    }
                }

                lock (_lockObj)
                {
                    File.AppendAllText(LogFilePath, logContent + "\n", System.Text.Encoding.UTF8);
                }
            }
            catch
            {
                // Bỏ qua nếu lỗi
            }
        }

        private static void WriteLog(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                lock (_lockObj)
                {
                    File.AppendAllText(LogFilePath, $"[{timestamp}] {message}\n", System.Text.Encoding.UTF8);
                }
            }
            catch
            {
                // Bỏ qua nếu không ghi được log
            }
        }
    }
}
