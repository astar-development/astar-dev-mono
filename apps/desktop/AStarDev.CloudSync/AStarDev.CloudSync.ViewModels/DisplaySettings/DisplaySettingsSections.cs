namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>The setting groups below the layout and theme choices (#1411). They are bundled so the settings screen takes one argument for them.</summary>
public sealed record DisplaySettingsSections
{
    internal DisplaySettingsSections(SyncScheduleSettingsViewModel schedule, TrashRetentionSettingsViewModel trashRetention, AutostartSettingsViewModel autostart, AccountLogLevelsViewModel logLevels, FileAuditViewModel fileAudit)
    {
        Schedule = schedule;
        TrashRetention = trashRetention;
        Autostart = autostart;
        LogLevels = logLevels;
        FileAudit = fileAudit;
    }

    /// <summary>The automatic sync interval setting.</summary>
    public SyncScheduleSettingsViewModel Schedule { get; }

    /// <summary>The sync trash retention setting.</summary>
    public TrashRetentionSettingsViewModel TrashRetention { get; }

    /// <summary>The start at login setting.</summary>
    public AutostartSettingsViewModel Autostart { get; }

    /// <summary>The per-account verbose logging setting.</summary>
    public AccountLogLevelsViewModel LogLevels { get; }

    /// <summary>The per-account file audit.</summary>
    public FileAuditViewModel FileAudit { get; }
}
