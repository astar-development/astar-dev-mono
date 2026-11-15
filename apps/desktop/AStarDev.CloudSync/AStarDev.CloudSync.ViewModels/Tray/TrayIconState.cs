namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Which tray icon to show: the overall state of the app at a glance (#1409).</summary>
public abstract record TrayIconState
{
    private TrayIconState()
    {
    }

    /// <summary>Nothing is syncing and nothing needs the user.</summary>
    public sealed record Idle : TrayIconState
    {
        internal Idle()
        {
        }
    }

    /// <summary>At least one account is syncing.</summary>
    public sealed record Syncing : TrayIconState
    {
        internal Syncing()
        {
        }
    }

    /// <summary>The user has something to do: sign in, resolve a conflict or confirm a held deletion.</summary>
    public sealed record Attention : TrayIconState
    {
        internal Attention()
        {
        }
    }

    /// <summary>At least one account's last run failed.</summary>
    public sealed record Faulted : TrayIconState
    {
        internal Faulted()
        {
        }
    }
}
