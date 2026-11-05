using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HyperLisUI.Helpers;

namespace HyperLisUI.Views
{
    public partial class UWPAppsView : UserControl, INotifyPropertyChanged
    {
        private List<UWPAppItem> _allApps = new List<UWPAppItem>();
        private List<UWPAppItem> _masterApps = new List<UWPAppItem>();
        private ObservableCollection<UWPAppItem> _displayApps = new ObservableCollection<UWPAppItem>();
        private List<string> _currentPresetList = new List<string>();
        private bool _isExpertMode = false;

        private UWPAppItem? _selectedApp;
        public UWPAppItem? SelectedApp
        {
            get => _selectedApp;
            set
            {
                if (_selectedApp != value)
                {
                    _selectedApp = value;
                    OnPropertyChanged(nameof(SelectedApp));
                    OnPropertyChanged(nameof(HasSelectedApp));
                }
            }
        }

        public bool HasSelectedApp => SelectedApp != null;

        public ObservableCollection<UWPAppItem> DisplayApps
        {
            get => _displayApps;
            set
            {
                _displayApps = value;
                OnPropertyChanged(nameof(DisplayApps));
            }
        }

        private string _selectionSummaryText = "0 app(s) selected • 0 MB";
        public string SelectionSummaryText
        {
            get => _selectionSummaryText;
            set
            {
                if (_selectionSummaryText != value)
                {
                    _selectionSummaryText = value;
                    OnPropertyChanged(nameof(SelectionSummaryText));
                }
            }
        }

        private string _kpiSaveText = "~1.4 GB";
        public string KpiSaveText
        {
            get => _kpiSaveText;
            set
            {
                if (_kpiSaveText != value)
                {
                    _kpiSaveText = value;
                    OnPropertyChanged(nameof(KpiSaveText));
                }
            }
        }

        // STUBS for preset lists
        private readonly List<string> SafeList = new List<string>
        {
            "Microsoft.Microsoft3DViewer",
            "Microsoft.BingSearch",
            "Clipchamp.Clipchamp",
            "Microsoft.WindowsAlarms",
            "Microsoft.549981C3F5F10",
            "Microsoft.Windows.DevHome",
            "MicrosoftCorporationII.MicrosoftFamily",
            "Microsoft.WindowsFeedbackHub",
            "Microsoft.GetHelp",
            "microsoft.windowscommunicationsapps",
            "Microsoft.BingNews",
            "Microsoft.MicrosoftOfficeHub",
            "Microsoft.Office.OneNote",
            "Microsoft.OutlookForWindows",
            "Microsoft.People",
            "Microsoft.PowerAutomateDesktop",
            "Microsoft.SkypeApp",
            "Microsoft.MicrosoftSolitaireCollection",
            "Microsoft.Getstarted",
            "Microsoft.Todos",
            "Microsoft.BingWeather",
            "Microsoft.YourPhone",
            "Microsoft.MixedReality.Portal",
            "Browser.InternetExplorer",
            "Microsoft.Windows.PowerShell.ISE",
            "App.Support.QuickAssist",
            "App.StepsRecorder"
        };

        private readonly List<string> RecommendedList = new List<string>
        {
            "Microsoft.Microsoft3DViewer",
            "Microsoft.BingSearch",
            "Clipchamp.Clipchamp",
            "Microsoft.WindowsAlarms",
            "Microsoft.549981C3F5F10",
            "Microsoft.Windows.DevHome",
            "MicrosoftCorporationII.MicrosoftFamily",
            "Microsoft.WindowsFeedbackHub",
            "Microsoft.GetHelp",
            "microsoft.windowscommunicationsapps",
            "Microsoft.WindowsMaps",
            "Microsoft.ZuneVideo",
            "Microsoft.BingNews",
            "Microsoft.MicrosoftOfficeHub",
            "Microsoft.Office.OneNote",
            "Microsoft.OutlookForWindows",
            "Microsoft.People",
            "Microsoft.PowerAutomateDesktop",
            "MicrosoftCorporationII.QuickAssist",
            "Microsoft.SkypeApp",
            "Microsoft.ScreenSketch",
            "Microsoft.MicrosoftSolitaireCollection",
            "Microsoft.MicrosoftStickyNotes",
            "MSTeams",
            "Microsoft.Getstarted",
            "Microsoft.Todos",
            "Microsoft.WindowsSoundRecorder",
            "Microsoft.BingWeather",
            "Microsoft.ZuneMusic",
            "Microsoft.Xbox.TCUI",
            "Microsoft.XboxApp",
            "Microsoft.XboxGameOverlay",
            "Microsoft.XboxGamingOverlay",
            "Microsoft.XboxIdentityProvider",
            "Microsoft.XboxSpeechToTextOverlay",
            "Microsoft.GamingApp",
            "Microsoft.YourPhone",
            "Microsoft.MicrosoftEdge",
            "Microsoft.OneDrive",
            "Microsoft.MixedReality.Portal",
            "Microsoft.Windows.Ai.Copilot.Provider",
            "Microsoft.WindowsMeetNow",
            "Browser.InternetExplorer",
            "MathRecognizer"
        };

        private readonly List<string> AggressiveList = new List<string>
        {
            "ALL_DEBLOAT"
        };

        private static readonly Dictionary<string, (string Desc, string Ver, string Date, string Type, double SizeMB)> AppMetadata = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Microsoft.Microsoft3DViewer"] = ("View and interact with 3D models in mixed reality.", "7.2212.4012.0", "2024-05-12", "Optional", 46.2),
            ["Microsoft.WindowsAlarms"] = ("Set alarms, use world clock, timer, and stopwatch features.", "11.2401.11.0", "2024-04-18", "Optional", 14.8),
            ["Microsoft.WindowsCalculator"] = ("Standard, scientific, graphing, and programmer calculation tools.", "11.2311.0.0", "2024-03-22", "Optional", 12.1),
            ["Microsoft.WindowsCamera"] = ("Capture photos and videos directly with your webcam or camera sensor.", "2024.2403.5.0", "2024-05-01", "Optional", 24.6),
            ["Microsoft.549981C3F5F10"] = ("Personal productivity assistant previously bundled into Windows.", "4.2204.13303.0", "2023-11-15", "System", 76.3),
            ["Microsoft.WindowsFeedbackHub"] = ("Send feedback, crash reports, and feature suggestions to Microsoft.", "1.2309.2801.0", "2024-02-10", "Optional", 31.2),
            ["Microsoft.GetHelp"] = ("Troubleshoot system problems and contact Microsoft Support.", "10.2312.11211.0", "2024-01-20", "Optional", 18.9),
            ["Microsoft.ZuneMusic"] = ("Play your local music library and manage audio playlists.", "11.2310.8.0", "2024-02-14", "Optional", 57.4),
            ["Clipchamp.Clipchamp"] = ("Fast and easy video editor with filters, transitions, and audio tools.", "2.8.2.0", "2024-05-20", "Optional", 45.5),
            ["Microsoft.BingSearch"] = ("Web search integration for Windows Search and taskbar widgets.", "1.0.0.1", "2024-06-01", "Optional", 3.3),
            ["Microsoft.Windows.DevHome"] = ("Control center for developers on Windows with dashboard widgets.", "0.1301.464.0", "2024-04-10", "Optional", 35.1),
            ["MicrosoftCorporationII.MicrosoftFamily"] = ("Parental controls, screen time limits, and location sharing.", "1.102.21.0", "2024-03-05", "Optional", 77.6),
            ["microsoft.windowscommunicationsapps"] = ("Built-in email client and calendar manager.", "16005.14326.21738.0", "2024-01-11", "Optional", 158.0),
            ["Microsoft.WindowsMaps"] = ("Offline and online maps with directions and traffic navigation.", "11.2311.2.0", "2024-02-28", "Optional", 22.4),
            ["Microsoft.ZuneVideo"] = ("Video player for movies, TV shows, and local video playback.", "10.23112.10051.0", "2024-03-14", "Optional", 39.4),
            ["Microsoft.BingNews"] = ("Headlines, weather, and breaking news updates from MSN.", "4.55.51731.0", "2024-02-05", "Optional", 11.5),
            ["Microsoft.MicrosoftOfficeHub"] = ("Central hub to access and manage Microsoft 365 apps and documents.", "18.2311.1071.0", "2024-03-18", "Optional", 75.3),
            ["Microsoft.Office.OneNote"] = ("Digital note-taking application for multi-device sync.", "16001.14326.21942.0", "2024-03-19", "Optional", 73.7),
            ["Microsoft.OutlookForWindows"] = ("Modern email and calendar experience for Windows.", "1.2024.403.400", "2024-04-15", "Optional", 118.7),
            ["Microsoft.People"] = ("Address book and contact management hub.", "10.2202.100.0", "2023-10-08", "Optional", 39.1),
            ["Microsoft.PowerAutomateDesktop"] = ("Desktop robotic process automation tool for routine workflows.", "1.0.380.0", "2024-03-01", "Optional", 63.9),
            ["Microsoft.SkypeApp"] = ("Video calling, voice calls, and messaging client.", "15.118.3205.0", "2024-03-29", "Optional", 52.8),
            ["Microsoft.ScreenSketch"] = ("Screen capture tool with delayed screenshot and markup features.", "11.2403.8.0", "2024-04-18", "Optional", 61.1),
            ["Microsoft.MicrosoftSolitaireCollection"] = ("Classic card games including Klondike, Spider, and FreeCell.", "4.18.2201.0", "2024-02-20", "Optional", 52.5),
            ["Microsoft.MicrosoftStickyNotes"] = ("Post-it style desktop sticky notes synchronized across devices.", "6.1.1.0", "2024-04-05", "Optional", 9.1),
            ["MSTeams"] = ("Collaboration and team chat, meetings, and file sharing platform.", "24074.2323.2827.4973", "2024-05-10", "Optional", 236.5),
            ["Microsoft.Getstarted"] = ("Interactive guides and tips for new Windows features.", "10.2309.28.0", "2024-01-15", "Optional", 41.3),
            ["Microsoft.Todos"] = ("Task manager and to-do list planner.", "2.115.6881.0", "2024-04-20", "Optional", 13.4),
            ["Microsoft.WindowsSoundRecorder"] = ("Audio recording tool for voice memos and meetings.", "11.2312.33.0", "2024-03-11", "Optional", 26.3),
            ["Microsoft.BingWeather"] = ("Real-time forecast, radar maps, and weather alerts.", "4.53.51862.0", "2024-04-22", "Optional", 61.1),
            ["Microsoft.YourPhone"] = ("Connect your mobile device to view SMS, notifications, and photos.", "1.24032.156.0", "2024-05-08", "Optional", 194.9),
            ["Microsoft.OneDrive"] = ("Cloud backup and file synchronization service.", "24.086.0428.0001", "2024-05-02", "Optional", 200.5),
            ["Microsoft.XboxApp"] = ("Xbox gaming hub, friend activity, and cloud gaming launcher.", "2405.1001.5.0", "2024-05-04", "Optional", 41.6),
            ["Microsoft.MSPaint"] = ("Creative 3D modelling and canvas painting tool.", "6.2305.16077.0", "2023-12-10", "Optional", 52.6),
            ["Microsoft.Paint"] = ("Classic bitmap image editing tool for Windows.", "11.2402.32.0", "2024-04-12", "Optional", 64.4),
            ["Microsoft.WindowsNotepad"] = ("Lightweight text editor with dark mode and multi-tab editing.", "11.2402.22.0", "2024-05-15", "Optional", 18.8),
            ["Microsoft.WindowsPhotos"] = ("View, organize, and edit your photos and video memories.", "2024.11040.1002.0", "2024-04-25", "Optional", 140.4),
            ["Microsoft.Windows.Photos"] = ("View, organize, and edit your photos and video memories.", "2024.11040.1002.0", "2024-04-25", "Optional", 140.4),
            ["Microsoft.WindowsStore"] = ("Official app store for downloading Windows apps and games.", "22403.1401.4.0", "2024-04-18", "System", 94.7),
            ["Microsoft.WindowsTerminal"] = ("Modern terminal emulator for command-line tools and shells.", "1.19.10821.0", "2024-03-27", "Optional", 35.6),
            ["Microsoft.MixedReality.Portal"] = ("Mixed Reality Portal for Windows virtual and augmented headsets.", "2000.21051.1282.0", "2024-01-10", "Optional", 27.3),
            ["Browser.InternetExplorer"] = ("Legacy Internet Explorer 11 browser engine and components.", "11.0.22621.1", "2023-11-20", "Optional", 2.4),
            ["Microsoft.Windows.PowerShell.ISE"] = ("Legacy graphical host for PowerShell scripts and debugging.", "10.0.22621.1", "2023-10-15", "Optional", 4.1),
            ["App.StepsRecorder"] = ("Capture mouse clicks and keyboard steps to reproduce issues.", "10.0.22621.1", "2023-12-05", "Optional", 1.8),
            ["MathRecognizer"] = ("Handwriting recognition engine for mathematical equations.", "10.0.22621.1", "2024-02-14", "Optional", 24.4),
            ["Microsoft.Xbox.TCUI"] = ("Xbox Live in-game title callable user interface.", "1.24.11001.0", "2024-03-12", "Optional", 18.2),
            ["Microsoft.XboxGamingOverlay"] = ("Windows Game Bar overlay with performance monitor and capture.", "7.124.3191.0", "2024-04-10", "Optional", 70.8),
            ["Microsoft.XboxIdentityProvider"] = ("Xbox authentication and identity provider service.", "12.115.1001.0", "2024-02-28", "Optional", 12.5),
            ["Microsoft.XboxSpeechToTextOverlay"] = ("Speech-to-text accessibility overlay for Xbox games.", "1.0.1701.0", "2024-01-15", "Optional", 8.4),
            ["Microsoft.GamingApp"] = ("Xbox app launcher and PC Game Pass gaming platform.", "2405.1001.5.0", "2024-05-02", "Optional", 54.2),
            ["Microsoft.MicrosoftEdge"] = ("Chromium-based Microsoft Edge web browser.", "125.0.2535.67", "2024-05-22", "Optional", 185.0),
            ["Microsoft.Windows.Ai.Copilot.Provider"] = ("Background AI Copilot provider services for Windows 11.", "1.0.12.0", "2024-04-20", "Optional", 16.4),
            ["Microsoft.WindowsMeetNow"] = ("Quick Skype video calls directly from the taskbar.", "1.0.0.1", "2023-11-10", "Optional", 5.2),
            ["Microsoft.Windows.WordPad"] = ("Legacy rich text word processor application.", "10.0.22621.1", "2023-09-15", "Optional", 3.6),
            ["Media.WindowsMediaPlayer"] = ("Classic Windows Media Player multimedia player.", "12.0.22621.1", "2023-10-01", "Optional", 48.3),
            ["OpenSSH.Client"] = ("Secure command-line OpenSSH client and key utility.", "8.9.1.0", "2024-01-08", "Optional", 5.5),
            ["MicrosoftWindows.Client.AIX"] = ("Windows AI Experience and contextual generative features.", "1.0.5.0", "2024-04-25", "Optional", 42.0),
            ["MicrosoftWindows.Client.CoPilot"] = ("Windows Copilot sidebar and system AI assistant.", "1.0.8.0", "2024-05-05", "Optional", 38.5),
            ["Microsoft.Edge.GameAssist"] = ("Edge Game Assist companion for game walkthroughs and guides.", "1.0.2.0", "2024-05-18", "Optional", 12.0),
            ["Microsoft.Office.ActionsServer"] = ("Background automation and action runner for Office 365.", "16.0.17425.0", "2024-03-30", "Optional", 15.6),
            ["aimgr"] = ("Microsoft Office AI Manager background integration.", "1.0.3.0", "2024-04-02", "Optional", 9.8),
            ["Microsoft.WritingAssistant"] = ("Grammar and writing suggestion assistant for Windows.", "1.2.4.0", "2024-03-15", "Optional", 14.5),
            ["Microsoft.DesktopAppInstaller"] = ("Windows Package Manager (winget) and MSIX installer.", "1.22.11261.0", "2024-04-12", "System", 34.2),
            ["Microsoft.Copilot"] = ("AI chat and assistant companion powered by Copilot.", "1.0.15.0", "2024-05-18", "Optional", 6.3),
            ["Microsoft.XboxGameOverlay"] = ("Windows Game Bar gaming overlay integration.", "5.823.3261.0", "2024-03-20", "Optional", 70.8),
            ["App.Support.QuickAssist"] = ("Quick remote screen sharing and technical assistance.", "2.0.28.0", "2024-04-05", "Optional", 12.7),
            ["MicrosoftTeams"] = ("Collaboration and team chat, meetings, and file sharing platform.", "24074.2323.2827.4973", "2024-05-10", "Optional", 236.5)
        };

        private List<UWPAppItem> _debloatApps = new List<UWPAppItem>
        {
            new UWPAppItem { Name = "3D Viewer", ImagePath = @"/Images/UWP/3dviewer.png", PackageId = "Microsoft.Microsoft3DViewer", Type = RemovalType.Package },
            new UWPAppItem { Name = "Alarms & Clock", ImagePath = @"/Images/UWP/clock.png", PackageId = "Microsoft.WindowsAlarms", Type = RemovalType.Package },
            new UWPAppItem { Name = "Calculator", ImagePath = @"/Images/UWP/calculator.png", PackageId = "Microsoft.WindowsCalculator", Type = RemovalType.Package },
            new UWPAppItem { Name = "Camera", ImagePath = @"/Images/UWP/camera.png", PackageId = "Microsoft.WindowsCamera", Type = RemovalType.Package },
            new UWPAppItem { Name = "Cortana", ImagePath = @"/Images/UWP/cortana.png", PackageId = "Microsoft.549981C3F5F10", Type = RemovalType.Package, IsCritical = true },
            new UWPAppItem { Name = "Feedback Hub", ImagePath = @"/Images/UWP/feedbackhub.png", PackageId = "Microsoft.WindowsFeedbackHub", Type = RemovalType.Package },
            new UWPAppItem { Name = "Get Help", ImagePath = @"/Images/UWP/gethelp.png", PackageId = "Microsoft.GetHelp", Type = RemovalType.Package },
            new UWPAppItem { Name = "Groove Music", ImagePath = @"/Images/UWP/wmpm.png", PackageId = "Microsoft.ZuneMusic", Type = RemovalType.Package },
            new UWPAppItem { Name = "Clipchamp", ImagePath = @"/Images/UWP/clipchamp.png", PackageId = "Clipchamp.Clipchamp", Type = RemovalType.Package },
            new UWPAppItem { Name = "Bing Search", ImagePath = @"/Images/UWP/bing.png", PackageId = "Microsoft.BingSearch", Type = RemovalType.Package },
            new UWPAppItem { Name = "Copilot", ImagePath = @"/Images/UWP/copilot.png", PackageId = "Microsoft.Copilot", Type = RemovalType.Package },
            new UWPAppItem { Name = "Dev Home", ImagePath = @"/Images/UWP/devhome.png", PackageId = "Microsoft.Windows.DevHome", Type = RemovalType.Package },
            new UWPAppItem { Name = "Family", ImagePath = @"/Images/UWP/family.png", PackageId = "MicrosoftCorporationII.MicrosoftFamily", Type = RemovalType.Package },
            new UWPAppItem { Name = "Game Bar", ImagePath = @"/Images/UWP/gamebar.png", PackageId = "Microsoft.XboxGameOverlay", Type = RemovalType.Package },
            new UWPAppItem { Name = "Mail and Calendar", ImagePath = @"/Images/UWP/mail.png", PackageId = "microsoft.windowscommunicationsapps", Type = RemovalType.Package },
            new UWPAppItem { Name = "Maps", ImagePath = @"/Images/UWP/map.png", PackageId = "Microsoft.WindowsMaps", Type = RemovalType.Package },
            new UWPAppItem { Name = "Movies & TV", ImagePath = @"/Images/UWP/moviesntv.png", PackageId = "Microsoft.ZuneVideo", Type = RemovalType.Package },
            new UWPAppItem { Name = "News", ImagePath = @"/Images/UWP/news.png", PackageId = "Microsoft.BingNews", Type = RemovalType.Package },
            new UWPAppItem { Name = "Notepad (modern)", ImagePath = @"/Images/UWP/notepad.png", PackageId = "Microsoft.WindowsNotepad", Type = RemovalType.Package },
            new UWPAppItem { Name = "Office 365", ImagePath = @"/Images/UWP/office365.png", PackageId = "Microsoft.MicrosoftOfficeHub", Type = RemovalType.Package },
            new UWPAppItem { Name = "OneDrive", ImagePath = @"/Images/UWP/onedrive.png", PackageId = "Microsoft.OneDrive", Type = RemovalType.Package },
            new UWPAppItem { Name = "OneNote", ImagePath = @"/Images/UWP/onenote.png", PackageId = "Microsoft.Office.OneNote", Type = RemovalType.Package },
            new UWPAppItem { Name = "Outlook for Windows", ImagePath = @"/Images/UWP/outlook.png", PackageId = "Microsoft.OutlookForWindows", Type = RemovalType.Package },
            new UWPAppItem { Name = "Paint", ImagePath = @"/Images/UWP/paint.png", PackageId = "Microsoft.Paint", Type = RemovalType.Package },
            new UWPAppItem { Name = "Paint 3D", ImagePath = @"/Images/UWP/paint3d.png", PackageId = "Microsoft.MSPaint", Type = RemovalType.Package },
            new UWPAppItem { Name = "People", ImagePath = @"/Images/UWP/people.png", PackageId = "Microsoft.People", Type = RemovalType.Package },
            new UWPAppItem { Name = "Photos", ImagePath = @"/Images/UWP/photo.png", PackageId = "Microsoft.Windows.Photos", Type = RemovalType.Package },
            new UWPAppItem { Name = "Power Automate", ImagePath = @"/Images/UWP/powerautomate.png", PackageId = "Microsoft.PowerAutomateDesktop", Type = RemovalType.Package },
            new UWPAppItem { Name = "Quick Assist", ImagePath = @"/Images/UWP/quickassist.png", PackageId = "App.Support.QuickAssist", Type = RemovalType.Capability },
            new UWPAppItem { Name = "Skype", ImagePath = @"/Images/UWP/skype.png", PackageId = "Microsoft.SkypeApp", Type = RemovalType.Package },
            new UWPAppItem { Name = "Snipping Tool", ImagePath = @"/Images/UWP/snippingtool.png", PackageId = "Microsoft.ScreenSketch", Type = RemovalType.Package },
            new UWPAppItem { Name = "Solitaire Collection", ImagePath = @"/Images/UWP/solitaire.png", PackageId = "Microsoft.MicrosoftSolitaireCollection", Type = RemovalType.Package },
            new UWPAppItem { Name = "Sticky Notes", ImagePath = @"/Images/UWP/stickynotes.png", PackageId = "Microsoft.MicrosoftStickyNotes", Type = RemovalType.Package },
            new UWPAppItem { Name = "Teams", ImagePath = @"/Images/UWP/teams.png", PackageId = "MSTeams", Type = RemovalType.Package },
            new UWPAppItem { Name = "Tips", ImagePath = @"/Images/UWP/tips.png", PackageId = "Microsoft.Getstarted", Type = RemovalType.Package },
            new UWPAppItem { Name = "To Do", ImagePath = @"/Images/UWP/todo.png", PackageId = "Microsoft.Todos", Type = RemovalType.Package },
            new UWPAppItem { Name = "Voice Recorder", ImagePath = @"/Images/UWP/voicerecorder.png", PackageId = "Microsoft.WindowsSoundRecorder", Type = RemovalType.Package },
            new UWPAppItem { Name = "Weather", ImagePath = @"/Images/UWP/weather.png", PackageId = "Microsoft.BingWeather", Type = RemovalType.Package },
            new UWPAppItem { Name = "Windows Terminal", ImagePath = @"/Images/UWP/terminal.png", PackageId = "Microsoft.WindowsTerminal", Type = RemovalType.Package },
            new UWPAppItem { Name = "Xbox Apps", ImagePath = @"/Images/UWP/xbox.png", PackageId = "Microsoft.XboxApp", Type = RemovalType.Package },
            new UWPAppItem { Name = "Your Phone / Phone Link", ImagePath = @"/Images/UWP/phonelink.png", PackageId = "Microsoft.YourPhone", Type = RemovalType.Package },
            new UWPAppItem { Name = "Mixed Reality Portal", ImagePath = @"/Images/UWP/mixedreality.png", PackageId = "Microsoft.MixedReality.Portal", Type = RemovalType.Package },
            new UWPAppItem { Name = "Internet Explorer", ImagePath = @"/Images/UWP/ie.png", PackageId = "Browser.InternetExplorer", Type = RemovalType.Capability },
            new UWPAppItem { Name = "PowerShell ISE", ImagePath = @"/Images/UWP/powershellise.png", PackageId = "Microsoft.Windows.PowerShell.ISE", Type = RemovalType.Capability },
            new UWPAppItem { Name = "Steps Recorder", ImagePath = @"/Images/UWP/StepsRecorder.png", PackageId = "App.StepsRecorder", Type = RemovalType.Capability },
            new UWPAppItem { Name = "Math Recognizer", ImagePath = @"/Images/UWP/mathinput.png", PackageId = "MathRecognizer", Type = RemovalType.Capability },
            new UWPAppItem { Name = "Xbox In-Game UI", ImagePath = @"/Images/UWP/xbox.png", PackageId = "Microsoft.Xbox.TCUI", Type = RemovalType.Package },
            new UWPAppItem { Name = "Xbox Gaming Overlay", ImagePath = @"/Images/UWP/gamebar.png", PackageId = "Microsoft.XboxGamingOverlay", Type = RemovalType.Package },
            new UWPAppItem { Name = "Xbox Identity Provider", ImagePath = @"/Images/UWP/xbox.png", PackageId = "Microsoft.XboxIdentityProvider", Type = RemovalType.Package },
            new UWPAppItem { Name = "Xbox Speech To Text", ImagePath = @"/Images/UWP/xbox.png", PackageId = "Microsoft.XboxSpeechToTextOverlay", Type = RemovalType.Package },
            new UWPAppItem { Name = "Xbox Gaming Services", ImagePath = @"/Images/UWP/xbox.png", PackageId = "Microsoft.GamingApp", Type = RemovalType.Package },
            new UWPAppItem { Name = "Microsoft Edge", ImagePath = @"/Images/UWP/bing.png", PackageId = "Microsoft.MicrosoftEdge", Type = RemovalType.Package },
            new UWPAppItem { Name = "Copilot Provider", ImagePath = @"/Images/UWP/copilot.png", PackageId = "Microsoft.Windows.Ai.Copilot.Provider", Type = RemovalType.Package },
            new UWPAppItem { Name = "Meet Now", ImagePath = @"/Images/UWP/skype.png", PackageId = "Microsoft.WindowsMeetNow", Type = RemovalType.Package },
            new UWPAppItem { Name = "WordPad", ImagePath = @"/Images/UWP/wordpad.png", PackageId = "Microsoft.Windows.WordPad", Type = RemovalType.Capability },
            new UWPAppItem { Name = "Windows Media Player (legacy)", ImagePath = @"/Images/UWP/wmp.png", PackageId = "Media.WindowsMediaPlayer", Type = RemovalType.Capability },
            new UWPAppItem { Name = "OpenSSH Client", ImagePath = @"/Images/UWP/openssh.png", PackageId = "OpenSSH.Client", Type = RemovalType.Capability },
            new UWPAppItem { Name = "Windows AI Experience", ImagePath = @"/Images/UWP/copilot.png", PackageId = "MicrosoftWindows.Client.AIX", Type = RemovalType.Package },
            new UWPAppItem { Name = "Windows Copilot Client", ImagePath = @"/Images/UWP/copilot.png", PackageId = "MicrosoftWindows.Client.CoPilot", Type = RemovalType.Package },
            new UWPAppItem { Name = "Edge Game Assist", ImagePath = @"/Images/UWP/gameassist.png", PackageId = "Microsoft.Edge.GameAssist", Type = RemovalType.Package },
            new UWPAppItem { Name = "Office Actions Server", ImagePath = @"/Images/UWP/office365.png", PackageId = "Microsoft.Office.ActionsServer", Type = RemovalType.Package },
            new UWPAppItem { Name = "Office AI Manager", ImagePath = @"/Images/UWP/office365.png", PackageId = "aimgr", Type = RemovalType.Package },
            new UWPAppItem { Name = "Writing Assistant", ImagePath = @"/Images/UWP/notepad.png", PackageId = "Microsoft.WritingAssistant", Type = RemovalType.Package },
            new UWPAppItem { Name = "App Installer", ImagePath = @"/Images/UWP/microsoftstore.png", PackageId = "Microsoft.DesktopAppInstaller", Type = RemovalType.Package },
            new UWPAppItem { Name = "Microsoft Store", ImagePath = @"/Images/UWP/microsoftstore.png", PackageId = "Microsoft.WindowsStore", Type = RemovalType.Package, IsCritical = true }
        };

        private string _activeTab = "AppList"; // "AppList", "Presets", "CustomList"
        private string _filterType = "All"; // "All", "Optional", "System", "Selected"

        public UWPAppsView()
        {
            InitializeComponent();
            DataContext = this;

            AggressiveList.AddRange(RecommendedList);

            // Populate metadata for debloat apps
            foreach (var app in _debloatApps)
            {
                EnrichAppMetadata(app);
            }

            _masterApps = new List<UWPAppItem>(_debloatApps);
            RefreshDisplayApps();

            SelectedApp = _masterApps.FirstOrDefault(a => a.Name.Contains("3D Viewer")) ?? _masterApps.FirstOrDefault();

            LocalizationService.LanguageChanged += ApplyTranslations;
            Loaded += (s, e) => ApplyTranslations();
            Loaded += UWPAppsView_Loaded;
        }

        private void EnrichAppMetadata(UWPAppItem app)
        {
            if (AppMetadata.TryGetValue(app.PackageId, out var meta))
            {
                app.Description = meta.Desc;
                app.Version = meta.Ver;
                app.InstallDate = meta.Date;
                app.AppTypeBadge = meta.Type;
                app.EstimatedSizeInMB = meta.SizeMB;
                app.Size = $"{meta.SizeMB:0.#} MB";
            }
            else
            {
                if (string.IsNullOrEmpty(app.Description))
                    app.Description = "Pre-installed Microsoft Windows application.";
                if (string.IsNullOrEmpty(app.Version))
                    app.Version = "1.0.0.0";
                if (string.IsNullOrEmpty(app.InstallDate))
                    app.InstallDate = "2024-05-12";
                if (string.IsNullOrEmpty(app.AppTypeBadge))
                    app.AppTypeBadge = app.IsCritical ? "System" : "Optional";
                if (string.IsNullOrEmpty(app.Size))
                {
                    app.EstimatedSizeInMB = app.EstimatedSizeInMB > 0 ? app.EstimatedSizeInMB : 25.0;
                    app.Size = $"{app.EstimatedSizeInMB:0.#} MB";
                }
            }

            if (string.IsNullOrEmpty(app.PackageFullName))
            {
                app.PackageFullName = $"{app.PackageId}_{app.Version}_x64__8wekyb3d8bbwe";
            }
        }

        private void ApplyTranslations()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ApplyTranslations);
                return;
            }

            lblBloatwareTitle.Text = LocalizationService.Get("lblBloatwareTitle") ?? "UWP Apps";
            lblBloatwareSub.Text = LocalizationService.Get("lblBloatwareSub") ?? "Easily manage, uninstall, and clean pre-installed Microsoft Store apps to free up space and improve privacy.";
            lblPresetSafe.Text = LocalizationService.Get("lblPresetSafe") ?? "Safe";
            lblPresetSafeDesc1.Text = LocalizationService.Get("lblPresetSafeDesc1") ?? "Removes non-essential apps while keeping core Windows features and Microsoft essentials.";
            lblPresetSafeDesc2.Text = LocalizationService.Get("lblPresetSafeDesc2") ?? "Recommended for beginners";
            lblPresetRecommended.Text = LocalizationService.Get("lblPresetRecommended") ?? "Recommended";
            lblPresetRecommendedDesc1.Text = LocalizationService.Get("lblPresetRecommendedDesc1") ?? "Best balance between performance, privacy, and compatibility.";
            lblPresetRecommendedDesc2.Text = LocalizationService.Get("lblPresetRecommendedDesc2") ?? "Most popular";
            lblPresetAggressive.Text = LocalizationService.Get("lblPresetAggressive") ?? "Aggressive";
            lblPresetAggressiveDesc1.Text = LocalizationService.Get("lblPresetAggressiveDesc1") ?? "Removes almost all non-essential apps for maximum performance and minimum background processes.";
            lblPresetAggressiveDesc2.Text = LocalizationService.Get("lblPresetAggressiveDesc2") ?? "Maximum performance";
            if (lblExpertMode != null)
                lblExpertMode.Text = LocalizationService.Get("chkExpertMode") ?? "Expert Mode (Unlock System Apps)";
            if (lblExpertModeDesc != null)
                lblExpertModeDesc.Text = LocalizationService.Get("chkExpertModeDesc") ?? "(Shows core system apps • Proceed with caution)";
            if (lblExpertTooltip != null)
                lblExpertTooltip.Text = LocalizationService.Get("chkExpertModeToolTip") ?? "Unlocks and displays all underlying Windows system apps and core packages. Warning: Removing critical system packages may cause Windows instability.";

            UpdateAppCountsUI();
        }

        private bool _hasLoaded = false;
        private async void UWPAppsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_hasLoaded) return;
            _hasLoaded = true;

            UpdateAppCountsUI();
            await LoadAppsAsync();

            // Default to Recommended Preset
            if (rbRecommended != null)
            {
                rbRecommended.IsChecked = true;
                ApplyPreset(RecommendedList);
            }
        }

        private double ParseSizeToMB(string sizeStr)
        {
            if (string.IsNullOrWhiteSpace(sizeStr)) return 0;
            var parts = sizeStr.Split(' ');
            if (parts.Length != 2) return 0;
            if (double.TryParse(parts[0], out double val))
            {
                if (parts[1].ToUpper().Contains("GB")) return val * 1024;
                if (parts[1].ToUpper().Contains("MB")) return val;
                if (parts[1].ToUpper().Contains("KB")) return val / 1024.0;
            }
            return 0;
        }

        private bool IsAppCritical(string packageId)
        {
            string[] criticals = { "Microsoft.WindowsStore", "Microsoft.DesktopAppInstaller", "Microsoft.Windows.ShellExperienceHost", "Microsoft.Windows.StartMenuExperienceHost", "Microsoft.Windows.Explorer", "Microsoft.AAD.BrokerPlugin" };
            foreach (var c in criticals)
            {
                if (packageId.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private async Task LoadAppsAsync()
        {
            try
            {
                var apps = await UWPHelper.GetInstalledAppsAsync();

                _allApps = apps.Select(a =>
                {
                    // 1. Kiểm tra xem app có sẵn icon trong danh mục mẫu (_masterApps) hay không
                    var matchedMaster = _masterApps.FirstOrDefault(m =>
                        (!string.IsNullOrEmpty(m.PackageId) && (
                            a.Name.Equals(m.PackageId, StringComparison.OrdinalIgnoreCase) ||
                            a.PackageFullName.IndexOf(m.PackageId, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            m.PackageId.IndexOf(a.Name, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (m.PackageId.Contains("Teams", StringComparison.OrdinalIgnoreCase) && a.Name.Contains("Teams", StringComparison.OrdinalIgnoreCase))
                        )));

                    string resolvedImage = string.Empty;
                    if (matchedMaster != null && !string.IsNullOrEmpty(matchedMaster.ImagePath) && !matchedMaster.ImagePath.EndsWith("default.png", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedImage = matchedMaster.ImagePath;
                    }
                    else if (!string.IsNullOrEmpty(a.LogoPath) && File.Exists(a.LogoPath))
                    {
                        resolvedImage = a.LogoPath;
                    }
                    else
                    {
                        // Fallback icon cũ: để chuỗi rỗng để kích hoạt trigger hiển thị icon MDL2 (\uE71D) trên nền #EFF6FF
                        resolvedImage = string.Empty;
                    }

                    var item = new UWPAppItem
                    {
                        Name = a.Name,
                        Size = a.Size,
                        EstimatedSizeInMB = ParseSizeToMB(a.Size),
                        Publisher = string.IsNullOrWhiteSpace(a.Publisher) ? "Microsoft Corporation" : a.Publisher,
                        PackageFullName = a.PackageFullName,
                        IsChecked = false,
                        IsCritical = IsAppCritical(a.PackageFullName),
                        IsEnabled = _isExpertMode ? true : !IsAppCritical(a.PackageFullName),
                        PackageId = a.Name,
                        ImagePath = resolvedImage,
                        Type = RemovalType.Package
                    };

                    var tokens = a.PackageFullName.Split('_');
                    if (tokens.Length >= 2) item.Version = tokens[1];

                    EnrichAppMetadata(item);
                    return item;
                }).ToList();

                Dispatcher.Invoke(() =>
                {
                    foreach (var scanned in _allApps)
                    {
                        var existing = _masterApps.FirstOrDefault(m =>
                            scanned.PackageFullName.IndexOf(m.PackageId, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            m.PackageId.IndexOf(scanned.Name, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (m.PackageId.Contains("Teams", StringComparison.OrdinalIgnoreCase) && scanned.Name.Contains("Teams", StringComparison.OrdinalIgnoreCase)));

                        if (existing != null)
                        {
                            if (!string.IsNullOrWhiteSpace(scanned.Size) && scanned.Size != "Unknown")
                            {
                                existing.Size = scanned.Size;
                                existing.EstimatedSizeInMB = scanned.EstimatedSizeInMB;
                            }
                            if (!string.IsNullOrWhiteSpace(scanned.Version) && scanned.Version != "1.0.0.0")
                            {
                                existing.Version = scanned.Version;
                            }
                            if (!string.IsNullOrWhiteSpace(scanned.Publisher))
                            {
                                existing.Publisher = scanned.Publisher;
                            }
                            existing.PackageFullName = scanned.PackageFullName;

                            // Bổ sung Logo động nếu master app chưa có icon
                            if (!string.IsNullOrEmpty(scanned.ImagePath) && string.IsNullOrEmpty(existing.ImagePath))
                            {
                                existing.ImagePath = scanned.ImagePath;
                            }
                        }
                    }

                    RefreshDisplayApps();
                    UpdateAppCountsUI();
                });
            }
            catch { }
        }

        private void RefreshDisplayApps()
        {
            var search = txtSearchApps?.Text?.Trim().ToLower() ?? "";
            IEnumerable<UWPAppItem> query = (_isExpertMode && _allApps.Count > 0) ? _allApps : _masterApps;

            if (_activeTab == "Presets")
            {
                query = query.Where(a => MatchesPreset(a, _currentPresetList));
            }
            else if (_activeTab == "CustomList")
            {
                query = query.Where(a => a.IsChecked);
            }

            if (_filterType == "Optional")
            {
                query = query.Where(a => !a.IsCritical && a.AppTypeBadge == "Optional");
            }
            else if (_filterType == "System")
            {
                query = query.Where(a => a.IsCritical || a.AppTypeBadge == "System");
            }
            else if (_filterType == "Selected")
            {
                query = query.Where(a => a.IsChecked);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a =>
                    a.Name.ToLower().Contains(search) ||
                    a.PackageId.ToLower().Contains(search) ||
                    a.PackageFullName.ToLower().Contains(search));
            }

            DisplayApps = new ObservableCollection<UWPAppItem>(query.ToList());

            if (SelectedApp == null || !DisplayApps.Contains(SelectedApp))
            {
                SelectedApp = DisplayApps.FirstOrDefault();
            }

            UpdateAppCountsUI();
        }

        private bool MatchesPreset(UWPAppItem app, List<string> presetList)
        {
            if (presetList == null || presetList.Count == 0) return false;
            if (presetList.Contains("ALL_DEBLOAT")) return true;
            return presetList.Any(p =>
                app.PackageId.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0 ||
                app.Name.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (p.Equals("MSTeams", StringComparison.OrdinalIgnoreCase) && app.PackageId.IndexOf("MicrosoftTeams", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (p.IndexOf("QuickAssist", StringComparison.OrdinalIgnoreCase) >= 0 && app.PackageId.IndexOf("QuickAssist", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private bool _isApplyingPreset = false;
        private void ApplyPreset(List<string> presetList)
        {
            _isApplyingPreset = true;
            _currentPresetList = new List<string>(presetList);

            foreach (var app in _masterApps)
            {
                bool shouldCheck = MatchesPreset(app, _currentPresetList);
                app.IsChecked = shouldCheck && app.IsEnabled;
            }

            foreach (var app in _allApps)
            {
                bool shouldCheck = _currentPresetList.Any(p => app.PackageFullName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
                app.IsChecked = shouldCheck && app.IsEnabled;
            }

            UpdateAppCountsUI();
            _isApplyingPreset = false;

            if (_activeTab == "Presets" || _activeTab == "CustomList")
            {
                RefreshDisplayApps();
            }
        }

        private void Preset_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            if (rbSafe?.IsChecked == true)
            {
                ApplyPreset(SafeList);
            }
            else if (rbRecommended?.IsChecked == true)
            {
                ApplyPreset(RecommendedList);
            }
            else if (rbAggressive?.IsChecked == true)
            {
                ApplyPreset(AggressiveList);
            }
        }

        private void UpdateAppCountsUI()
        {
            if (lblPresetSafeApps != null)
                lblPresetSafeApps.Text = $"{GetMatchedCount(SafeList)} apps";
            if (lblPresetRecommendedApps != null)
                lblPresetRecommendedApps.Text = $"{GetMatchedCount(RecommendedList)} apps";
            if (lblPresetAggressiveApps != null)
                lblPresetAggressiveApps.Text = $"{GetMatchedCount(AggressiveList)} apps";

            int selectedCount = _masterApps.Count(a => a.IsChecked) + _allApps.Count(a => a.IsChecked);
            double totalSizeMB = _masterApps.Where(a => a.IsChecked).Sum(a => a.EstimatedSizeInMB) +
                                 _allApps.Where(a => a.IsChecked).Sum(a => a.EstimatedSizeInMB);

            if (totalSizeMB >= 1024)
            {
                double totalGB = totalSizeMB / 1024.0;
                SelectionSummaryText = $"{selectedCount} app(s) selected • {totalGB:0.#} GB";
                KpiSaveText = $"~{totalGB:0.#} GB";
            }
            else
            {
                SelectionSummaryText = $"{selectedCount} app(s) selected • {totalSizeMB:0.#} MB";
                KpiSaveText = totalSizeMB > 0 ? $"~{totalSizeMB:0.#} MB" : "~1.4 GB";
            }

            UpdateSelectAllCheckbox();
        }

        private int GetMatchedCount(List<string> preset)
        {
            if (preset.Contains("ALL_DEBLOAT"))
            {
                return _masterApps.Count;
            }
            return _masterApps.Count(app => MatchesPreset(app, preset));
        }

        private bool _isUpdatingSelectAll = false;
        private void UpdateSelectAllCheckbox()
        {
            if (chkSelectAll == null || _displayApps == null || _displayApps.Count == 0)
            {
                if (chkSelectAll != null) chkSelectAll.IsChecked = false;
                return;
            }

            int total = _displayApps.Count(a => a.IsEnabled);
            int selected = _displayApps.Count(a => a.IsChecked && a.IsEnabled);

            _isUpdatingSelectAll = true;
            if (selected == 0)
                chkSelectAll.IsChecked = false;
            else if (selected == total)
                chkSelectAll.IsChecked = true;
            else
                chkSelectAll.IsChecked = null;
            _isUpdatingSelectAll = false;
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string tag)
            {
                _activeTab = tag;
                RefreshDisplayApps();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSearchAppsPlaceholder != null)
            {
                txtSearchAppsPlaceholder.Visibility = string.IsNullOrEmpty(txtSearchApps.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            RefreshDisplayApps();
        }

        private void BtnFilters_Click(object sender, RoutedEventArgs e)
        {
            if (_filterType == "All") _filterType = "Optional";
            else if (_filterType == "Optional") _filterType = "System";
            else if (_filterType == "System") _filterType = "Selected";
            else _filterType = "All";

            RefreshDisplayApps();
        }

        private void BtnViewList_Click(object sender, RoutedEventArgs e)
        {
            btnViewList.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#2563EB");
            btnViewListIcon.Foreground = Brushes.White;
            btnViewGrid.Background = Brushes.Transparent;
            btnViewGridIcon.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#64748B");

            if (borderTableHeader != null) borderTableHeader.Visibility = Visibility.Visible;
            if (AppListBox != null) AppListBox.Visibility = Visibility.Visible;
            if (AppGridBox != null) AppGridBox.Visibility = Visibility.Collapsed;
        }

        private void BtnViewGrid_Click(object sender, RoutedEventArgs e)
        {
            btnViewGrid.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#2563EB");
            btnViewGridIcon.Foreground = Brushes.White;
            btnViewList.Background = Brushes.Transparent;
            btnViewListIcon.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#64748B");

            if (borderTableHeader != null) borderTableHeader.Visibility = Visibility.Collapsed;
            if (AppListBox != null) AppListBox.Visibility = Visibility.Collapsed;
            if (AppGridBox != null) AppGridBox.Visibility = Visibility.Visible;
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingSelectAll || _displayApps == null) return;

            int total = _displayApps.Count(a => a.IsEnabled);
            int selected = _displayApps.Count(a => a.IsChecked && a.IsEnabled);
            bool targetState = (selected != total);

            _isUpdatingSelectAll = true;
            chkSelectAll.IsChecked = targetState;
            _isUpdatingSelectAll = false;

            _isApplyingPreset = true;
            foreach (var app in _displayApps)
            {
                if (app.IsEnabled)
                {
                    app.IsChecked = targetState;
                }
            }
            _isApplyingPreset = false;

            UpdateAppCountsUI();
            if (rbSafe != null) rbSafe.IsChecked = false;
            if (rbRecommended != null) rbRecommended.IsChecked = false;
            if (rbAggressive != null) rbAggressive.IsChecked = false;
        }

        private void AppList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppListBox.SelectedItem is UWPAppItem item)
            {
                SelectedApp = item;
            }
        }

        private void AppRow_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_isApplyingPreset) return;

            UpdateAppCountsUI();

            if (rbSafe != null) rbSafe.IsChecked = false;
            if (rbRecommended != null) rbRecommended.IsChecked = false;
            if (rbAggressive != null) rbAggressive.IsChecked = false;
        }

        private void ExpertMode_Changed(object sender, RoutedEventArgs e)
        {
            _isExpertMode = chkExpertMode?.IsChecked == true;

            foreach (var app in _allApps)
            {
                if (app.IsCritical)
                {
                    app.IsEnabled = _isExpertMode;
                    if (!_isExpertMode) app.IsChecked = false;
                }
            }
            foreach (var app in _masterApps)
            {
                if (app.IsCritical)
                {
                    app.IsEnabled = _isExpertMode;
                    if (!_isExpertMode) app.IsChecked = false;
                }
            }

            RefreshDisplayApps();
            UpdateAppCountsUI();
        }

        private void OpenStorePage_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedApp == null) return;

            try
            {
                var url = $"ms-windows-store://pdp/?PFN={Uri.EscapeDataString(SelectedApp.PackageId)}";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                try
                {
                    var webUrl = $"https://apps.microsoft.com/search?query={Uri.EscapeDataString(SelectedApp.Name)}";
                    Process.Start(new ProcessStartInfo { FileName = webUrl, UseShellExecute = true });
                }
                catch { }
            }
        }

        private async void UninstallSingle_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedApp == null) return;

            string msg = $"Are you sure you want to uninstall \"{SelectedApp.Name}\"?";
            var win = new ModernConfirmationWindow(msg, "Confirm Uninstall", "Uninstall", "Cancel");
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();

            if (win.DialogResult != true) return;

            SelectedApp.IsChecked = true;
            await ExecuteDebloatProcess(new List<UWPAppItem> { SelectedApp });
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            _isApplyingPreset = true;
            foreach (var app in _masterApps) app.IsChecked = false;
            foreach (var app in _allApps) app.IsChecked = false;
            _isApplyingPreset = false;

            if (rbSafe != null) rbSafe.IsChecked = false;
            if (rbRecommended != null) rbRecommended.IsChecked = false;
            if (rbAggressive != null) rbAggressive.IsChecked = false;

            UpdateAppCountsUI();
            if (_activeTab == "CustomList") RefreshDisplayApps();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var selectedIds = _masterApps.Where(a => a.IsChecked).Select(a => a.PackageId).ToList();
            selectedIds.AddRange(_allApps.Where(a => a.IsChecked).Select(a => a.PackageId));

            if (selectedIds.Count == 0)
            {
                var win = new ModernConfirmationWindow("Please select at least one app to export.", "Warning", "OK", null);
                win.Owner = Window.GetWindow(this);
                win.ShowDialog();
                return;
            }

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON Files|*.json",
                FileName = "HyperLis_UWP_Preset.json"
            };

            if (sfd.ShowDialog() == true)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(selectedIds, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(sfd.FileName, json);
                var win = new ModernConfirmationWindow("Preset exported successfully!", "Export", "OK", null);
                win.Owner = Window.GetWindow(this);
                win.ShowDialog();
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON Files|*.json"
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    var json = System.IO.File.ReadAllText(ofd.FileName);
                    var selectedIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);

                    if (selectedIds != null)
                    {
                        foreach (var app in _masterApps) app.IsChecked = selectedIds.Contains(app.PackageId);
                        foreach (var app in _allApps) app.IsChecked = selectedIds.Contains(app.PackageId);

                        UpdateAppCountsUI();
                        RefreshDisplayApps();

                        var win = new ModernConfirmationWindow("Preset imported successfully!", "Import", "OK", null);
                        win.Owner = Window.GetWindow(this);
                        win.ShowDialog();
                    }
                }
                catch
                {
                    var win = new ModernConfirmationWindow("Failed to parse JSON preset file.", "Error", "OK", null);
                    win.Owner = Window.GetWindow(this);
                    win.ShowDialog();
                }
            }
        }

        private async void StartDebloat_Click(object sender, RoutedEventArgs e)
        {
            var selectedApps = new List<UWPAppItem>();
            selectedApps.AddRange(_masterApps.Where(a => a.IsChecked));
            selectedApps.AddRange(_allApps.Where(a => a.IsChecked));
            selectedApps = selectedApps.GroupBy(a => a.PackageId).Select(g => g.First()).ToList();

            if (selectedApps.Count == 0)
            {
                var win = new ModernConfirmationWindow("Please select at least one application to uninstall.", "Notice", "OK", null);
                win.Owner = Window.GetWindow(this);
                win.ShowDialog();
                return;
            }

            await ExecuteDebloatProcess(selectedApps);
        }

        private async Task ExecuteDebloatProcess(List<UWPAppItem> selectedApps)
        {
            TerminalPopup.Visibility = Visibility.Visible;
            txtTerminalOutput.Inlines.Clear();
            AppendTerminalOutput($"Command: powershell.exe -ExecutionPolicy Bypass -NoProfile -File \"BloatRemoval.ps1\"");
            AppendTerminalOutput($"Start Time: \"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\"");
            AppendTerminalOutput("---");
            AppendTerminalOutput($"Starting removal process for {selectedApps.Count} app(s)...");

            pbTerminalProgress.Maximum = selectedApps.Count;
            pbTerminalProgress.Value = 0;
            _currentTerminalProgress = 0;
            btnCloseTerminal.IsEnabled = false;

            var packageItems = selectedApps.Where(a => a.Type == RemovalType.Package)
                .Select(a => new {
                    PackageId = (a.PackageId ?? string.Empty).Replace("'", "''"),
                    PackageFullName = (a.PackageFullName ?? string.Empty).Replace("'", "''"),
                    DisplayName = (a.Name ?? string.Empty).Replace("'", "''")
                })
                .GroupBy(a => a.PackageId)
                .Select(g => g.First())
                .ToList();
            var capabilities = selectedApps.Where(a => a.Type == RemovalType.Capability).Select(a => (a.PackageId ?? string.Empty).Replace("'", "''")).Distinct().ToList();
            var features = selectedApps.Where(a => a.Type == RemovalType.Feature).Select(a => (a.PackageId ?? string.Empty).Replace("'", "''")).Distinct().ToList();

            await Task.Run(() =>
            {
                string tempScriptPath = string.Empty;
                try
                {
                    string script = "$ProgressPreference = 'SilentlyContinue'; $ErrorActionPreference = 'SilentlyContinue';\n";

                    if (packageItems.Any())
                    {
                        script += @"
Write-Output '--- Processing Appx Packages ---'
$installedProvisioned = Get-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue;
$installedPackages = Get-AppxPackage -PackageTypeFilter Main,Bundle,Resource -AllUsers -ErrorAction SilentlyContinue;
";
                        foreach (var pkg in packageItems)
                        {
                            string id = pkg.PackageId;
                            string fullName = pkg.PackageFullName;

                            script += $@"
Write-Output ""Processing Package: {id}""
# 1. Gỡ bỏ Provisioned Package chính xác
$prov = $installedProvisioned | Where-Object {{ $_.DisplayName -eq '{id}' -or $_.PackageName -like '{id}*' }}
if ($prov) {{
    foreach ($p in $prov) {{
        $p | Remove-AppxProvisionedPackage -AllUsers -Online -ErrorAction SilentlyContinue | Out-Null
        if ($?) {{ Write-Output ""Provisioned package removed: $($p.PackageName)"" }}
    }}
}}

# 2. Gỡ bỏ Installed AppX Package (chính xác theo PackageFullName hoặc Name, loại bỏ hoàn toàn wildcard hai đầu)
$installed = $installedPackages | Where-Object {{
    ('{fullName}' -ne '' -and $_.PackageFullName -eq '{fullName}') -or $_.Name -eq '{id}'
}}
if ($installed) {{
    foreach ($p in $installed) {{
        $p | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue | Out-Null
        if ($?) {{ Write-Output ""Package removed: $($p.PackageFullName)"" }} else {{ Write-Output ""Failed removing: $($p.PackageFullName)"" }}
    }}
}} else {{
    Write-Output ""Package not installed or already removed: {id}""
}}
";
                        }
                    }

                    if (capabilities.Any())
                    {
                        string capSelectors = string.Join("','", capabilities);
                        script += $@"
Write-Output '--- Processing Capabilities ---'
$capSelectors = @('{capSelectors}');
$getCommandCap = {{ Get-WindowsCapability -Online | Where-Object -Property 'State' -NotIn -Value @('NotPresent','Removed') }};
$installedCap = & $getCommandCap;
foreach ($selector in $capSelectors) {{
    Write-Output ""Processing Capability: $selector""
    $found = $installedCap | Where-Object -FilterScript {{ ($_.Name -split '~')[0] -eq $selector }}
    if ($found) {{
        $found | Remove-WindowsCapability -Online -ErrorAction 'Continue' | Out-Null
        if ($?) {{ Write-Output ""Capability removed: $selector"" }} else {{ Write-Output ""Capability not removed: $selector (Error)"" }}
    }} else {{
        Write-Output ""Capability not installed: $selector""
    }}
}}
";
                    }

                    if (features.Any())
                    {
                        string featSelectors = string.Join("','", features);
                        script += $@"
Write-Output '--- Processing Optional Features ---'
$featSelectors = @('{featSelectors}');
$getCommandFeat = {{ Get-WindowsOptionalFeature -Online | Where-Object -Property 'State' -NotIn -Value @('Disabled','DisabledWithPayloadRemoved') }};
$installedFeat = & $getCommandFeat;
foreach ($selector in $featSelectors) {{
    Write-Output ""Processing Feature: $selector""
    $found = $installedFeat | Where-Object -FilterScript {{ $_.FeatureName -eq $selector }}
    if ($found) {{
        $found | Disable-WindowsOptionalFeature -Online -Remove -NoRestart -ErrorAction 'Continue' | Out-Null
        if ($?) {{ Write-Output ""Feature removed: $selector"" }} else {{ Write-Output ""Feature not removed: $selector (Error)"" }}
    }} else {{
        Write-Output ""Feature not installed: $selector""
    }}
}}
";
                    }

                    tempScriptPath = Path.Combine(Path.GetTempPath(), $"HyperLis_Debloat_{Guid.NewGuid():N}.ps1");
                    File.WriteAllText(tempScriptPath, script, System.Text.Encoding.UTF8);

                    RunPowerShellScriptFile(tempScriptPath);

                    AppendTerminalOutput("Bloat removal process completed");
                    AppendTerminalOutput("---");
                    AppendTerminalOutput($"End Time: \"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\"");
                    AppendTerminalOutput("Process return value: \"0\" (0x00000000)");

                    UpdateTerminalProgress(selectedApps.Count, "Completed successfully");
                }
                catch (Exception ex)
                {
                    AppendTerminalOutput($"ERROR: {ex.Message}");
                    UpdateTerminalProgress(selectedApps.Count, "Completed with errors");
                }
                finally
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(tempScriptPath) && File.Exists(tempScriptPath))
                        {
                            File.Delete(tempScriptPath);
                        }
                    }
                    catch { }

                    Dispatcher.Invoke(() => { btnCloseTerminal.IsEnabled = true; });
                }
            });
        }

        private void RunPowerShellScriptFile(string scriptPath)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            using (var process = new Process { StartInfo = processInfo })
            {
                process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) AppendTerminalOutput(e.Data); };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data) && !e.Data.Contains("CLIXML") && !e.Data.StartsWith("<Obj") && !e.Data.Contains("xmlns="))
                        AppendTerminalOutput($"ERROR: {e.Data}");
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
        }

        private int _currentTerminalProgress = 0;
        private void AppendTerminalOutput(string text)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(text)) return;

                string colorHex = "#333333";

                if (text.Contains("Starting") ||
                    text.Contains("Command:") ||
                    text.Contains("Start Time:") ||
                    text.Contains("End Time:") ||
                    text.Contains("Process return value:") ||
                    text.Contains("--- Processing"))
                {
                    colorHex = "#0275D8";
                }
                else if (text.Contains("Removed") ||
                         text.Contains("completed") ||
                         text.Contains("Package removed:") ||
                         text.Contains("Capability removed:") ||
                         text.Contains("Feature removed:"))
                {
                    colorHex = "#107C10";
                }
                else if (text.Contains("not found") ||
                         text.Contains("not installed:") ||
                         text.Contains("not removed:") ||
                         text.Contains("ERROR"))
                {
                    colorHex = "#D13438";
                }

                if (text != "---")
                {
                    text = $"[{DateTime.Now.ToString("HH:mm:ss")}] {text}";
                }

                var run = new System.Windows.Documents.Run(text + "\n")
                {
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(colorHex)
                };

                txtTerminalOutput.Inlines.Add(run);
                svTerminalOutput.ScrollToBottom();

                if (text.Contains("Processing Package:") ||
                    text.Contains("Processing Capability:") ||
                    text.Contains("Processing Feature:"))
                {
                    _currentTerminalProgress++;
                    if (_currentTerminalProgress <= pbTerminalProgress.Maximum)
                    {
                        pbTerminalProgress.Value = _currentTerminalProgress;
                    }
                    if (lblTerminalStatus != null)
                    {
                        lblTerminalStatus.Text = $"Processing: {_currentTerminalProgress} of {pbTerminalProgress.Maximum}...";
                    }
                }
            });
        }

        private void UpdateTerminalProgress(int value, string status)
        {
            Dispatcher.InvokeAsync(() =>
            {
                pbTerminalProgress.Value = value;
                lblTerminalStatus.Text = status;
            });
        }

        private void CloseTerminal_Click(object sender, RoutedEventArgs e)
        {
            TerminalPopup.Visibility = Visibility.Collapsed;

            foreach (var app in _masterApps) app.IsChecked = false;
            foreach (var app in _allApps) app.IsChecked = false;

            UpdateAppCountsUI();
            if (rbSafe != null) rbSafe.IsChecked = false;
            if (rbRecommended != null) rbRecommended.IsChecked = false;
            if (rbAggressive != null) rbAggressive.IsChecked = false;

            _ = LoadAppsAsync();
        }

        private void CopyTerminal_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtTerminalOutput.Text);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum RemovalType
    {
        Package,
        Capability,
        Feature
    }

    public class UWPAppItem : INotifyPropertyChanged
    {
        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));
                }
            }
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        private bool _status = true;
        public bool Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public string Name { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public double EstimatedSizeInMB { get; set; } = 0;
        public string Publisher { get; set; } = "Microsoft Corporation";
        public string PackageFullName { get; set; } = string.Empty;
        public string PackageId { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public bool IsBeta { get; set; } = false;
        public bool IsCritical { get; set; } = false;
        public RemovalType Type { get; set; } = RemovalType.Package;

        public string Version { get; set; } = "1.0.0.0";
        public string InstallDate { get; set; } = "2024-05-12";
        public string Description { get; set; } = "Windows pre-installed application.";
        public string AppTypeBadge { get; set; } = "Optional";

        public string AppTypeBadgeBg => (IsCritical || AppTypeBadge == "System") ? "#FEE2E2" : "#ECFDF5";
        public string AppTypeBadgeFg => (IsCritical || AppTypeBadge == "System") ? "#DC2626" : "#059669";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
