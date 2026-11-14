namespace AStarDev.CloudSync.Sync;

/// <summary>Why one conflict was not resolved. The item stays in the queue, so the user can try again (#1406).</summary>
public abstract record ResolutionFailure
{
    private ResolutionFailure()
    {
    }

    /// <summary>The strategy does not apply to the item.</summary>
    public sealed record NotAllowed : ResolutionFailure
    {
        internal NotAllowed(ResolutionNotAllowed reason)
        {
            Reason = reason;
        }

        /// <summary>The reason.</summary>
        public ResolutionNotAllowed Reason { get; }
    }

    /// <summary>The conflict is not in the queue any more.</summary>
    public sealed record NotOpen : ResolutionFailure
    {
        internal NotOpen()
        {
        }
    }

    /// <summary>The resolution is not available yet for this kind of conflict.</summary>
    public sealed record Unsupported : ResolutionFailure
    {
        internal Unsupported()
        {
        }
    }

    /// <summary>The local file the resolution needs is gone.</summary>
    public sealed record LocalFileMissing : ResolutionFailure
    {
        internal LocalFileMissing()
        {
        }
    }

    /// <summary>The cloud file the resolution needs is gone.</summary>
    public sealed record RemoteFileMissing : ResolutionFailure
    {
        internal RemoteFileMissing()
        {
        }
    }

    /// <summary>The local file changed while it was transferred, so nothing was resolved.</summary>
    public sealed record LocalChanged : ResolutionFailure
    {
        internal LocalChanged()
        {
        }
    }

    /// <summary>The cloud file changed after the conflict was shown, so the transfer was refused (412).</summary>
    public sealed record RemoteChanged : ResolutionFailure
    {
        internal RemoteChanged()
        {
        }
    }

    /// <summary>A provider call failed.</summary>
    public sealed record Transfer : ResolutionFailure
    {
        internal Transfer(ProviderError error)
        {
            Error = error;
        }

        /// <summary>What the provider reported.</summary>
        public ProviderError Error { get; }
    }

    /// <summary>A local file operation failed.</summary>
    public sealed record Local : ResolutionFailure
    {
        internal Local(LocalOperationFailure failure)
        {
            Failure = failure;
        }

        /// <summary>What the file system reported.</summary>
        public LocalOperationFailure Failure { get; }
    }

    /// <summary>The transfer succeeded but the item could not be closed.</summary>
    public sealed record NotRecorded : ResolutionFailure
    {
        internal NotRecorded()
        {
        }
    }

    /// <summary>Anything else.</summary>
    public sealed record Unexpected : ResolutionFailure
    {
        internal Unexpected()
        {
        }
    }
}
