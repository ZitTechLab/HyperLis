using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace HyperLisUI.Models
{
    public class AppItem : INotifyPropertyChanged
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Link { get; set; }
        public string? IconPath { get; set; }
        public string? ChocoId { get; set; }
        public string? WingetId { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private bool _isInstalled;
        public bool IsInstalled
        {
            get => _isInstalled;
            set { _isInstalled = value; OnPropertyChanged(); }
        }

        private string _statusText = "Installed";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class CategoryGroup
    {
        public string? CategoryName { get; set; }
        public List<AppItem>? Apps { get; set; }
    }

    public class AppJson
    {
        public string? category { get; set; }
        public string? content { get; set; }
        public string? description { get; set; }
        public string? link { get; set; }
        public string? choco { get; set; }
        public string? winget { get; set; }
    }

    public enum PackageManagerKind
    {
        Unknown,
        Chocolatey,
        Winget
    }

    public enum CommandKind
    {
        Install,
        Uninstall,
        Verify,
        Refresh
    }

    public enum PackageOperationStatus
    {
        Succeeded,
        Failed,
        TimedOut,
        VerificationDeferred
    }

    public class CommandExecutionResult
    {
        public CommandKind CommandKind { get; set; }
        public string CommandText { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public TimeSpan Duration { get; set; }
        public bool TimedOut { get; set; }
        public string StdOut { get; set; } = string.Empty;
        public string StdErr { get; set; } = string.Empty;
    }

    public class PackageOperationResult
    {
        public AppItem? App { get; set; }
        public PackageManagerKind PackageManager { get; set; }
        public PackageOperationStatus Status { get; set; }
        public CommandExecutionResult? Execution { get; set; }
        public bool? DetectedInstalledState { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
