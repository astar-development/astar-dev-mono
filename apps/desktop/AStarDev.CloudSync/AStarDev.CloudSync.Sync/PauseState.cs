namespace AStarDev.CloudSync.Sync;

/// <summary>Whether the user paused an account's scheduled syncing (#1402). A paused account still syncs when the user presses Sync Now.</summary>
public abstract record PauseState
{
    private PauseState()
    {
    }

    /// <summary>Scheduled syncs run.</summary>
    public sealed record Active : PauseState
    {
        internal Active()
        {
        }
    }

    /// <summary>Scheduled syncs are skipped until the user resumes the account.</summary>
    public sealed record Paused : PauseState
    {
        internal Paused()
        {
        }
    }
}
