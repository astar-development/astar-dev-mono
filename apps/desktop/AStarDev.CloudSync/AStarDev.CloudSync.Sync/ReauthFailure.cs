namespace AStarDev.CloudSync.Sync;

/// <summary>Why re-authenticating an account failed (#1393).</summary>
public abstract record ReauthFailure
{
    private ReauthFailure()
    {
    }

    /// <summary>The user closed the sign-in window.</summary>
    public sealed record Cancelled : ReauthFailure
    {
        internal Cancelled()
        {
        }
    }

    /// <summary>The user signed in as someone else; nothing changes.</summary>
    public sealed record DifferentAccount : ReauthFailure
    {
        internal DifferentAccount()
        {
        }
    }

    /// <summary>The provider failed.</summary>
    public sealed record Provider : ReauthFailure
    {
        internal Provider(ProviderError error)
        {
            Error = error;
        }

        /// <summary>The provider's error.</summary>
        public ProviderError Error { get; }
    }
}
