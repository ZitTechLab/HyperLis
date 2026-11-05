using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Threading.Tasks;
using System.Threading;

namespace HyperLisUI.Helpers
{
    // Extracted from Optimizer 16.7 and adapted for HyperLisUI

    #region Dependencies (Enums & ByteSize)

    internal enum RecycleFlag : int
    {
        SHERB_NOCONFIRMATION = 0x00000001,
        SHERB_NOPROGRESSUI = 0x00000001,
        SHERB_NOSOUND = 0x00000004
    }

    /// <summary>
    /// Represents a byte size value. Simplified from ByteSize.cs
    /// </summary>
    public struct ByteSize : IComparable<ByteSize>, IEquatable<ByteSize>
    {
        public static readonly ByteSize MinValue = ByteSize.FromBits(long.MinValue);
        public static readonly ByteSize MaxValue = ByteSize.FromBits(long.MaxValue);
        public const long BitsInByte = 8;
        public long Bits { get; private set; }
        public double Bytes { get; private set; }

        public ByteSize(long bits) : this()
        {
            Bits = bits;
            Bytes = bits / BitsInByte;
        }

        public ByteSize(double bytes) : this()
        {
            Bits = (long)Math.Ceiling(bytes * BitsInByte);
            Bytes = bytes;
        }

        public static ByteSize FromBits(long value) => new ByteSize(value);
        public static ByteSize FromBytes(double value) => new ByteSize(value);

        public override string ToString() => this.ToString("0.##", CultureInfo.CurrentCulture);
        public string ToString(string format, IFormatProvider provider) => this.Bytes.ToString(format, provider) + " B"; // Simplified

        public static ByteSize operator +(ByteSize b1, ByteSize b2) => new ByteSize(b1.Bytes + b2.Bytes);
        public static ByteSize operator -(ByteSize b1, ByteSize b2) => new ByteSize(b1.Bytes - b2.Bytes);
        
        // Equality members
        public override bool Equals(object? value) => value is ByteSize other && Equals(other);
        public bool Equals(ByteSize value) => this.Bits == value.Bits;
        public override int GetHashCode() => this.Bits.GetHashCode();
        public int CompareTo(ByteSize other) => this.Bits.CompareTo(other.Bits);

        public ByteSize AddBytes(double value) => this + ByteSize.FromBytes(value);
    }

    #endregion
    public enum CleanupFailureReason
    {
        Unknown,
        Locked,
        AccessDenied,
        IO
    }

    public sealed class CleanupFailure
    {
        public string Path { get; set; } = string.Empty;
        public CleanupFailureReason Reason { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class CleanupResult
    {
        public int ProcessedItems { get; set; }
        public int DeletedFiles { get; set; }
        public int DeletedDirectories { get; set; }
        public int SkippedMissing { get; set; }
        public int TotalFailures { get; set; }
        public int LockedFailures { get; set; }
        public int AccessDeniedFailures { get; set; }
        public long FreedBytes { get; set; }
        public List<CleanupFailure> Failures { get; } = new List<CleanupFailure>();

        public int DeletedTotal => DeletedFiles + DeletedDirectories;
        public bool HasIssues => TotalFailures > 0;

        public void AddFailure(string path, CleanupFailureReason reason, string? message)
        {
            TotalFailures++;

            if (reason == CleanupFailureReason.Locked) LockedFailures++;
            if (reason == CleanupFailureReason.AccessDenied) AccessDeniedFailures++;

            Failures.Add(new CleanupFailure
            {
                Path = path,
                Reason = reason,
                Message = message ?? string.Empty
            });
        }
    }
    public static class CleaningHelper
    {
        [DllImport("Shell32.dll")]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlag dwFlags);

        // SYSTEM FOLDERS
        internal static readonly string System32Folder = Environment.GetFolderPath(Environment.SpecialFolder.System);
        internal static readonly string TempFolder = Path.GetTempPath();
        internal static readonly string ProfileAppDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        internal static readonly string ProgramData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        internal static readonly string ProfileAppDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        internal static readonly string OSDriveWindows = Environment.GetEnvironmentVariable("WINDIR", EnvironmentVariableTarget.Machine)!;

        public static List<string> PreviewCleanList = new List<string>();
        public static ByteSize PreviewSizeToBeFreed = new ByteSize(0);
        private static readonly HashSet<string> PreviewPathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static void ResetPreview()
        {
            PreviewCleanList.Clear();
            PreviewSizeToBeFreed = new ByteSize(0);
            PreviewPathSet.Clear();
        }

        public static List<string> GetDistinctPreviewItems()
        {
            return BuildDistinctItemList(PreviewCleanList);
        }

        /// <summary>
        /// Scans a path and adds all files/subdirs to PreviewCleanList.
        /// Also calculates total size inline to avoid a second traversal.
        /// </summary>
        private static void PreviewFolder(string path)
        {
            if (File.Exists(path))
            {
                AddPreviewItem(path);
                try { PreviewSizeToBeFreed = PreviewSizeToBeFreed.AddBytes(new FileInfo(path).Length); } catch { }
                return;
            }

            if (!Directory.Exists(path)) return;

            Stack<string> stack = new Stack<string>();
            stack.Push(path);

            while (stack.Count > 0)
            {
                string dir = stack.Pop();

                try
                {
                    foreach (string file in Directory.GetFiles(dir))
                    {
                        AddPreviewItem(file);
                        try { PreviewSizeToBeFreed = PreviewSizeToBeFreed.AddBytes(new FileInfo(file).Length); } catch { }
                    }

                    foreach (string subDir in Directory.GetDirectories(dir))
                    {
                        if (IsReparsePoint(subDir)) continue;

                        AddPreviewItem(subDir);
                        stack.Push(subDir);
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (Exception) { }
            }
        }

        private static void AddPreviewItem(string itemPath)
        {
            if (string.IsNullOrWhiteSpace(itemPath)) return;

            string normalized = NormalizePath(itemPath);
            if (PreviewPathSet.Add(normalized))
            {
                PreviewCleanList.Add(normalized);
            }
        }
        /// <summary>
        /// Cleans the given items with throttled progress reporting.
        /// progressCallback receives (processedCount, totalCount, currentFileName).
        /// If items is a pre-built distinct list, set isPreBuilt=true to skip redundant dedup.
        /// </summary>
        public static async Task<CleanupResult> CleanAsync(
            IEnumerable<string>? itemsToClean = null,
            Action<int, int, string>? progressCallback = null,
            bool isPreBuilt = false)
        {
            Logger.LogActivity("User ran System Clean");
            return await Task.Run(() =>
            {
                var result = new CleanupResult();
                var sourceItems = itemsToClean ?? PreviewCleanList;

                // Skip redundant dedup if caller already provides a distinct list
                List<string> items;
                if (isPreBuilt && sourceItems is List<string> preBuilt)
                {
                    items = new List<string>(preBuilt);
                    items.Reverse();
                }
                else
                {
                    items = BuildDistinctItemList(sourceItems);
                    items.Reverse();
                }

                int total = items.Count;
                if (total == 0) return result;

                // Throttle: only report every batchSize items OR every 200ms
                int batchSize = Math.Max(1, Math.Min(200, total / 100));
                var sw = System.Diagnostics.Stopwatch.StartNew();
                long lastReportMs = 0;

                foreach (string x in items)
                {
                    result.ProcessedItems++;
                    try
                    {
                        if (File.Exists(x))
                        {
                            long fileBytes = 0;
                            try { fileBytes = new FileInfo(x).Length; } catch { }

                            if (TryDeleteFile(x, out var reason, out var errorMessage))
                            {
                                result.DeletedFiles++;
                                result.FreedBytes += Math.Max(0, fileBytes);
                            }
                            else
                            {
                                result.AddFailure(x, reason, errorMessage);
                            }
                        }
                        else if (Directory.Exists(x))
                        {
                            if (TryDeleteDirectory(x, out var reason, out var errorMessage))
                            {
                                result.DeletedDirectories++;
                            }
                            else
                            {
                                result.AddFailure(x, reason, errorMessage);
                            }
                        }
                        else
                        {
                            result.SkippedMissing++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AddFailure(x, CleanupFailureReason.Unknown, ex.Message);
                    }

                    // Throttled progress: report every batchSize items OR every 200ms
                    long elapsed = sw.ElapsedMilliseconds;
                    if (progressCallback != null &&
                        (result.ProcessedItems % batchSize == 0 || (elapsed - lastReportMs) >= 200 || result.ProcessedItems == total))
                    {
                        try
                        {
                            progressCallback.Invoke(result.ProcessedItems, total, Path.GetFileName(x));
                        }
                        catch (OperationCanceledException)
                        {
                            // User cancelled via loading overlay — stop cleaning gracefully
                            break;
                        }
                        catch { /* Swallow UI dispatch errors */ }
                        lastReportMs = elapsed;
                    }
                }

                return result;
            });
        }

        private static bool TryDeleteFile(string path, out CleanupFailureReason reason, out string? errorMessage)
        {
            reason = CleanupFailureReason.Unknown;
            errorMessage = null;

            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    // Only clear ReadOnly attribute when needed to avoid unnecessary syscall
                    var attrs = File.GetAttributes(path);
                    if ((attrs & FileAttributes.ReadOnly) != 0)
                        File.SetAttributes(path, attrs & ~FileAttributes.ReadOnly);

                    File.Delete(path);
                    return true;
                }
                catch (UnauthorizedAccessException ex)
                {
                    reason = CleanupFailureReason.AccessDenied;
                    errorMessage = ex.Message;
                    if (attempt < 1) Thread.Sleep(50);
                }
                catch (IOException ex)
                {
                    reason = IsLockError(ex) ? CleanupFailureReason.Locked : CleanupFailureReason.IO;
                    errorMessage = ex.Message;
                    if (attempt < 1) Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    reason = CleanupFailureReason.Unknown;
                    errorMessage = ex.Message;
                    break; // Unknown errors are unlikely to resolve with retry
                }
            }

            return false;
        }

        private static bool TryDeleteDirectory(string path, out CleanupFailureReason reason, out string? errorMessage)
        {
            reason = CleanupFailureReason.Unknown;
            errorMessage = null;

            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    if (IsReparsePoint(path))
                        Directory.Delete(path, false);
                    else
                        Directory.Delete(path, true);

                    return true;
                }
                catch (UnauthorizedAccessException ex)
                {
                    reason = CleanupFailureReason.AccessDenied;
                    errorMessage = ex.Message;
                    if (attempt < 1) Thread.Sleep(50);
                }
                catch (IOException ex)
                {
                    reason = IsLockError(ex) ? CleanupFailureReason.Locked : CleanupFailureReason.IO;
                    errorMessage = ex.Message;
                    if (attempt < 1) Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    reason = CleanupFailureReason.Unknown;
                    errorMessage = ex.Message;
                    break;
                }
            }

            return false;
        }

        private static bool IsLockError(IOException ex)
        {
            int code = ex.HResult & 0xFFFF;
            return code == 32 || code == 33;
        }

        private static bool IsReparsePoint(string path)
        {
            try
            {
                var attrs = File.GetAttributes(path);
                return (attrs & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
            }
            catch
            {
                return false;
            }
        }

        private static List<string> BuildDistinctItemList(IEnumerable<string> source)
        {
            var list = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var raw in source)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string normalized = NormalizePath(raw);
                if (seen.Add(normalized)) list.Add(normalized);
            }

            return list;
        }

        private static string NormalizePath(string path)
        {
            try
            {
                return Path.GetFullPath(path.Trim());
            }
            catch
            {
                return path.Trim();
            }
        }

        public static void EmptyRecycleBin()
        {
            try
            {
                SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlag.SHERB_NOSOUND | RecycleFlag.SHERB_NOCONFIRMATION);
            }
            catch { }
        }

        public static async Task PreviewFeaturesAsync(bool tempFiles, bool miniDumps, bool errorReports)
        {
            await Task.Run(() =>
            {
                ResetPreview();

                if (tempFiles) PreviewTemp();
                if (miniDumps) PreviewMinidumps();
                if (errorReports) PreviewErrorReports();
            });
        }

        internal static readonly string WindowsTemp = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot")!, "Temp");

        internal static void PreviewTemp()
        {
            PreviewFolder(TempFolder);
            PreviewFolder(WindowsTemp);
        }

        public static void ResetTempFolders()
        {
            AggressiveCleanFolder(TempFolder);
            AggressiveCleanFolder(WindowsTemp);
        }

        /// <summary>
        /// Aggressively deletes all files and empty directories inside a folder.
        /// Uses stack-based traversal to handle access exceptions per-directory
        /// instead of aborting the entire scan (unlike EnumerateFiles with AllDirectories).
        /// Phase 1: Traverse with Stack, delete files (clearing ReadOnly first).
        /// Phase 2: Remove empty directories bottom-up (deepest first).
        /// </summary>
        private static void AggressiveCleanFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;

            var allDirs = new List<string>();
            var stack = new Stack<string>();
            stack.Push(folderPath);

            // Phase 1: Stack-based traversal — delete files, skip inaccessible directories individually
            while (stack.Count > 0)
            {
                string dir = stack.Pop();

                try
                {
                    // Delete all files in this directory
                    foreach (string file in Directory.GetFiles(dir))
                    {
                        try
                        {
                            var fi = new FileInfo(file);
                            if (fi.IsReadOnly) fi.IsReadOnly = false;
                            fi.Delete();
                        }
                        catch { }
                    }

                    // Queue subdirectories for traversal
                    foreach (string subDir in Directory.GetDirectories(dir))
                    {
                        try
                        {
                            var attrs = File.GetAttributes(subDir);
                            if ((attrs & FileAttributes.ReparsePoint) != 0)
                            {
                                try { Directory.Delete(subDir, false); } catch { }
                                continue;
                            }
                        }
                        catch { continue; }

                        allDirs.Add(subDir);
                        stack.Push(subDir);
                    }
                }
                catch { } // Skip entirely inaccessible directories
            }

            // Phase 2: Remove empty directories bottom-up (deepest paths first)
            allDirs.Sort((a, b) => b.Length.CompareTo(a.Length));
            foreach (string dir in allDirs)
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                        Directory.Delete(dir, false);
                }
                catch { }
            }
        }

        internal static void PreviewMinidumps()
        {
            string minidumpPath = Path.Combine(OSDriveWindows, "Minidump");
            PreviewFolder(minidumpPath);
        }

        internal static void PreviewErrorReports()
        {
            var paths = new[]
            {
                Path.Combine(ProfileAppDataLocal, "Microsoft\\Windows\\WER\\ReportArchive"),
                Path.Combine(ProfileAppDataLocal, "Microsoft\\Windows\\WER\\ReportQueue"),
                Path.Combine(ProfileAppDataLocal, "Microsoft\\Windows\\WER\\Temp"),
                Path.Combine(ProfileAppDataLocal, "Microsoft\\Windows\\WER\\ERC"),
                Path.Combine(ProgramData, "Microsoft\\Windows\\WER\\ReportArchive"),
                Path.Combine(ProgramData, "Microsoft\\Windows\\WER\\ReportQueue"),
                Path.Combine(ProgramData, "Microsoft\\Windows\\WER\\Temp"),
                Path.Combine(ProgramData, "Microsoft\\Windows\\WER\\ERC")
            };

            foreach (var p in paths)
            {
                PreviewFolder(p);
            }
        }

        internal static ByteSize CalculateSize(string fileOrFolder)
        {
            ByteSize totalSize = new ByteSize(0);

            if (File.Exists(fileOrFolder))
            {
                try { totalSize = totalSize.AddBytes(new FileInfo(fileOrFolder).Length); } catch { }
                return totalSize;
            }

            if (!Directory.Exists(fileOrFolder)) return totalSize;

            Stack<string> stack = new Stack<string>();
            stack.Push(fileOrFolder);

            while (stack.Count > 0)
            {
                string dir = stack.Pop();
                try
                {
                    foreach (string file in Directory.GetFiles(dir))
                    {
                        try { totalSize = totalSize.AddBytes(new FileInfo(file).Length); } catch { }
                    }

                    foreach (string subDir in Directory.GetDirectories(dir))
                    {
                        if (IsReparsePoint(subDir))
                        {
                            continue;
                        }

                        stack.Push(subDir);
                    }
                }
                catch { }
            }

            return totalSize;
        }

        // =========================================================
        // BROWSER CLEANING SECTIONS
        // =========================================================

        // INTERNET EXPLORER CACHE
        static string[] ieCache =
        {
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Windows\\INetCache\\IE"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Windows\\WebCache.old")
        };

        // CHROME FOLDERS
        static string chromeFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\Local\\Google\\Chrome\\User Data");
        static string[] chromeUserDataCacheDirs = { "Default\\Cache", "Default\\Code Cache\\", "Default\\GPUCache\\", "ShaderCache", "Default\\Service Worker\\CacheStorage\\", "Default\\Service Worker\\ScriptCache\\", "GrShaderCache\\GPUCache\\", "Default\\File System\\", "Default\\JumpListIconsMostVisited\\", "Default\\JumpListIconsRecentClosed\\", "Default\\Service Worker\\Database" };
        static string chromePasswordsDir = Path.Combine(ProfileAppDataLocal, "Google\\Chrome\\User Data\\Default\\Login Data");
        static string[] chromeSessionDirs =
        {
            Path.Combine(ProfileAppDataLocal, "Google\\Chrome\\User Data\\Default\\Sessions"),
            Path.Combine(ProfileAppDataLocal, "Google\\Chrome\\User Data\\Default\\Session Storage"),
            Path.Combine(ProfileAppDataLocal, "Google\\Chrome\\User Data\\Default\\Extension State"),
        };
        static string[] chromeCookiesDirs =
        {
            Path.Combine(ProfileAppDataLocal, "Google\\Chrome\\User Data\\Default\\IndexedDB"),
            Path.Combine(ProfileAppDataLocal, "Google\\Chrome\\User Data\\Default\\Cookies"),
            Path.Combine(ProfileAppDataLocal, "Google\\Chrome\\User Data\\Default\\Cookies-journal")
        };
        static string[] chromeHistoryDirs =
        {
            Path.Combine(ProfileAppDataLocal, "Google\\Chrome\\User Data\\Default\\History"),
            Path.Combine(ProfileAppDataLocal, "Google\\Chrome\\User Data\\Default\\History Provider Cache"),
            Path.Combine(ProfileAppDataLocal, "Google\\Chrome\\User Data\\Default\\History-journal")
        };

        // FIREFOX FOLDERS
        static string firefoxRoaming = Path.Combine(ProfileAppDataRoaming, "Mozilla\\Firefox\\Profiles");
        static string firefoxLocal = Path.Combine(ProfileAppDataLocal, "Mozilla\\Firefox\\Profiles");

        // EDGE FOLDERS
        static string edgeHistory = Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\History");
        static string[] edgeCookies =
        {
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\Cookies"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\IndexedDB"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\Local Storage")
        };
        static string[] edgeSession =
        {
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\Sessions"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\Session Storage"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\Extension State"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\Local Extension Settings"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\Local Storage")
        };
        static string[] edgeCache =
        {
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\Cache"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\Code Cache"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\GPUCache"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\ShaderCache"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\Service Worker\\CacheStorage"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\Service Worker\\ScriptCache"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\GrShaderCache\\GPUCache"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\Service Worker\\Database"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\Service Worker"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\LocalCache"),
            Path.Combine(ProfileAppDataLocal, "Microsoft\\Edge\\User Data\\Default\\#!001\\Cache")
        };

        // BRAVE FOLDERS
        static string braveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\Local\\BraveSoftware\\Brave-Browser\\User Data");
        static string[] braveUserDataCacheDirs = { "Default\\Cache", "Default\\Code Cache\\", "Default\\GPUCache\\", "ShaderCache", "Default\\Service Worker\\CacheStorage\\", "Default\\Service Worker\\ScriptCache\\", "GrShaderCache\\GPUCache\\", "Default\\File System\\", "Default\\JumpListIconsMostVisited\\", "Default\\JumpListIconsRecentClosed\\", "Default\\Service Worker\\Database" };
        static string bravePasswordsDir = Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\Login Data");
        static string[] braveSessionDirs =
        {
            Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\Sessions"),
            Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\Session Storage"),
            Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\Extension State"),
            Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\Local Storage"),
            Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\IndexedDB"),
            Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\Service Worker")
        };
        static string[] braveCookiesDirs =
        {
            Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\IndexedDB"),
            Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\Cookies"),
            Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\Cookies-journal"),
            Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\Local Storage")
        };
        static string[] braveHistoryDirs =
        {
            Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\History"),
            Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\History Provider Cache"),
            Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\History-journal"),
            Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\Top Sites"),
            Path.Combine(ProfileAppDataLocal, "BraveSoftware\\Brave-Browser\\User Data\\Default\\Visited Links")
        };

        public static void PreviewEdgeClean(bool cache, bool cookies, bool seachHistory, bool session)
        {
            if (cache)
                foreach (string x in edgeCache)
                    PreviewFolder(x);

            if (cookies)
                foreach (string x in edgeCookies)
                    PreviewFolder(x);

            if (seachHistory)
                PreviewFolder(edgeHistory);

            if (session)
                foreach (string x in edgeSession)
                    PreviewFolder(x);
        }

        public static void PreviewFireFoxClean(bool cache, bool cookies, bool searchHistory)
        {
            if (Directory.Exists(firefoxRoaming))
            {
                foreach (string x in Directory.EnumerateDirectories(firefoxRoaming))
                {
                    if (!Directory.Exists(x)) continue;

                    if (cookies)
                        PreviewFolder(Path.Combine(x, "cookies.sqlite"));

                    if (searchHistory)
                        PreviewFolder(Path.Combine(x, "places.sqlite"));

                    if (cache)
                        PreviewFolder(Path.Combine(x, "shader-cache"));
                }
            }

            if (cache && Directory.Exists(firefoxLocal))
            {
                foreach (string x in Directory.EnumerateDirectories(firefoxLocal))
                {
                    if (Directory.Exists(x))
                        PreviewFolder(Path.Combine(x, "cache2"));
                }
            }
        }

        public static void PreviewBraveClean(bool cache, bool cookies, bool searchHistory, bool session, bool passwords)
        {
            if (cache)
                foreach (string x in braveUserDataCacheDirs)
                    PreviewFolder(Path.Combine(braveFolder, x));

            if (session)
                foreach (string x in braveSessionDirs)
                    PreviewFolder(x);

            if (cookies)
                foreach (string x in braveCookiesDirs)
                    PreviewFolder(x);

            if (searchHistory)
                foreach (string x in braveHistoryDirs)
                    PreviewFolder(x);

            if (passwords)
                PreviewFolder(bravePasswordsDir);
        }

        public static void PreviewChromeClean(bool cache, bool cookies, bool searchHistory, bool session, bool passwords)
        {
            if (cache)
                foreach (string x in chromeUserDataCacheDirs)
                    PreviewFolder(Path.Combine(chromeFolder, x));

            if (session)
                foreach (string x in chromeSessionDirs)
                    PreviewFolder(x);

            if (cookies)
                foreach (string x in chromeCookiesDirs)
                    PreviewFolder(x);

            if (searchHistory)
                foreach (string x in chromeHistoryDirs)
                    PreviewFolder(x);

            if (passwords)
                PreviewFolder(chromePasswordsDir);
        }
    }
}


















