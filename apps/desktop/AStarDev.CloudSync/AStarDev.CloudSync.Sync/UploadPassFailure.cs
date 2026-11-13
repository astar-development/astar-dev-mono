namespace AStarDev.CloudSync.Sync;

/// <summary>Why a whole upload pass stopped. One file failing never fails the pass.</summary>
public abstract record UploadPassFailure
{
    private UploadPassFailure()
    {
    }

    /// <summary>The account's sync root is missing or unreadable.</summary>
    public sealed record RootUnavailable : UploadPassFailure
    {
        internal RootUnavailable()
        {
        }
    }

    /// <summary>The provider is full: no more uploads for the account in this pass.</summary>
    public sealed record QuotaExceeded : UploadPassFailure
    {
        internal QuotaExceeded()
        {
        }
    }

    /// <summary>The account must sign in again.</summary>
    public sealed record AuthenticationRequired : UploadPassFailure
    {
        internal AuthenticationRequired()
        {
        }
    }

    /// <summary>Something unexpected stopped the pass.</summary>
    public sealed record Unexpected : UploadPassFailure
    {
        internal Unexpected()
        {
        }
    }
}
