namespace AStarDev.CloudSync.Sync;

/// <summary>Why a whole download pass failed (#1400). One file failing does not fail the pass.</summary>
public abstract record DownloadPassFailure
{
    private DownloadPassFailure()
    {
    }

    /// <summary>The account's sync root is missing or unreadable, so nothing was changed.</summary>
    public sealed record RootUnavailable : DownloadPassFailure
    {
        internal RootUnavailable()
        {
        }
    }

    /// <summary>The volume ran out of space. The files already downloaded keep their baseline rows, and the caller must stop the whole sync run: no upload pass follows.</summary>
    public sealed record InsufficientDiskSpace : DownloadPassFailure
    {
        internal InsufficientDiskSpace()
        {
        }
    }

    /// <summary>The account must sign in again.</summary>
    public sealed record AuthenticationRequired : DownloadPassFailure
    {
        internal AuthenticationRequired()
        {
        }
    }

    /// <summary>Anything else.</summary>
    public sealed record Unexpected : DownloadPassFailure
    {
        internal Unexpected()
        {
        }
    }
}
