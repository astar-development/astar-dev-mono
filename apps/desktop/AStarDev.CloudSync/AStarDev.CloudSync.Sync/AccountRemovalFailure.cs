using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Why removing an account stopped. Nothing is deleted after the step that failed.</summary>
public abstract record AccountRemovalFailure
{
    private AccountRemovalFailure()
    {
    }

    /// <summary>No stored account has the id (already removed).</summary>
    public sealed record NotFound : AccountRemovalFailure
    {
        internal NotFound()
        {
        }
    }

    /// <summary>A sync pass or a sign-in for the account did not finish in time; nothing was deleted.</summary>
    public sealed record SyncStillRunning : AccountRemovalFailure
    {
        internal SyncStillRunning()
        {
        }
    }

    /// <summary>The app database could not read or delete the account's data.</summary>
    public sealed record Store : AccountRemovalFailure
    {
        internal Store(ErrorResponse error) => Error = error;

        /// <summary>The store's error.</summary>
        public ErrorResponse Error { get; }
    }

    /// <summary>The provider could not sign the account out.</summary>
    public sealed record Provider : AccountRemovalFailure
    {
        internal Provider(ProviderError error) => Error = error;

        /// <summary>The provider's error.</summary>
        public ProviderError Error { get; }
    }
}
