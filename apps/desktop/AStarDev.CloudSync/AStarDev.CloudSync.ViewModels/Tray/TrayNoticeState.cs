namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Whether the one-off "still running in the tray" notice has been shown (#1409).</summary>
public abstract record TrayNoticeState
{
    private TrayNoticeState()
    {
    }

    /// <summary>The notice has not been shown yet, so the next close shows it.</summary>
    public sealed record Pending : TrayNoticeState
    {
        internal Pending()
        {
        }
    }

    /// <summary>The notice was shown once and is not shown again.</summary>
    public sealed record Shown : TrayNoticeState
    {
        internal Shown()
        {
        }
    }
}
