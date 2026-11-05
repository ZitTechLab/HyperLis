using System;

namespace HyperLisUI.Helpers
{
    internal static class CleanHelper
    {
        // ProgramData = C:\ProgramData
        internal static readonly string ProgramData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        
        // ProfileAppDataRoaming = C:\Users\<User>\AppData\Roaming
        internal static readonly string ProfileAppDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // Files/Shortcuts in these folders start with Windows
        internal static readonly string LocalMachineStartupFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
        internal static readonly string CurrentUserStartupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
    }
}
