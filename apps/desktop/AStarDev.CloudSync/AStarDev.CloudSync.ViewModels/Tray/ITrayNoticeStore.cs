namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Remembers that the "still running in the tray" notice was shown (#1409).</summary>
public interface ITrayNoticeStore
{
    /// <summary>Reads whether the notice was shown. A store that cannot be read says <see cref="TrayNoticeState.Pending"/>.</summary>
    TrayNoticeState Read();

    /// <summary>Records that the notice was shown. A store that cannot be written changes nothing and does not throw.</summary>
    void MarkShown();
}
