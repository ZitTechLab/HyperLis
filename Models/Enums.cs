namespace HyperLisUI.Models
{
    public enum StartupItemLocation
    {
        LMStartupFolder,
        CUStartupFolder,
        HKLM,
        HKLMWoW,
        HKCU,
        TaskScheduler
    }

    public enum StartupItemType
    {
        None,
        Run,
        RunOnce,
        Task
    }
}
