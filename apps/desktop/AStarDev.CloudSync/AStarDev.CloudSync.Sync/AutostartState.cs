namespace AStarDev.CloudSync.Sync;

/// <summary>Whether the app starts itself when the user logs in (#1410).</summary>
public abstract record AutostartState
{
    private AutostartState()
    {
    }

    /// <summary>The app is registered to start at login.</summary>
    public sealed record Enabled : AutostartState
    {
        internal Enabled()
        {
        }
    }

    /// <summary>The app is not registered.</summary>
    public sealed record Disabled : AutostartState
    {
        internal Disabled()
        {
        }
    }

    /// <summary>The app cannot register itself here.</summary>
    public sealed record NotSupported : AutostartState
    {
        internal NotSupported(AutostartBlocker blocker) => Blocker = blocker;

        /// <summary>Why not.</summary>
        public AutostartBlocker Blocker { get; }
    }
}
