namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>Creates <see cref="DisplaySettingsSections"/> values.</summary>
public static class DisplaySettingsSectionsFactory
{
    /// <summary>Bundles the setting groups.</summary>
    /// <param name="schedule">The automatic sync interval setting.</param>
    /// <param name="trashRetention">The sync trash retention setting.</param>
    /// <param name="autostart">The start at login setting.</param>
    /// <param name="logLevels">The per-account verbose logging setting.</param>
    /// <param name="fileAudit">The per-account file audit.</param>
    public static DisplaySettingsSections Create(SyncScheduleSettingsViewModel schedule, TrashRetentionSettingsViewModel trashRetention, AutostartSettingsViewModel autostart, AccountLogLevelsViewModel logLevels, FileAuditViewModel fileAudit) => new(schedule, trashRetention, autostart, logLevels, fileAudit);
}
