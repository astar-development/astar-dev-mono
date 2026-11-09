using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Why a level of the selection tree could not be listed (#1397). A failed side is never shown as an empty one.</summary>
public abstract record FolderTreeFailure
{
    private FolderTreeFailure()
    {
    }

    /// <summary>The cloud listing failed.</summary>
    public sealed record RemoteFailed : FolderTreeFailure
    {
        internal RemoteFailed(ProviderError error)
        {
            Error = error;
        }

        /// <summary>The Error.</summary>
        public ProviderError Error { get; }
    }

    /// <summary>The listing on disk failed.</summary>
    public sealed record LocalFailed : FolderTreeFailure
    {
        internal LocalFailed(LocalFolderError error)
        {
            Error = error;
        }

        /// <summary>The Error.</summary>
        public LocalFolderError Error { get; }
    }

    /// <summary>Both listings failed.</summary>
    public sealed record BothFailed : FolderTreeFailure
    {
        internal BothFailed(ProviderError remoteError, LocalFolderError localError)
        {
            RemoteError = remoteError;
            LocalError = localError;
        }

        /// <summary>The RemoteError.</summary>
        public ProviderError RemoteError { get; }

        /// <summary>The LocalError.</summary>
        public LocalFolderError LocalError { get; }
    }

    /// <summary>The stored selection rules could not be read.</summary>
    public sealed record RulesFailed : FolderTreeFailure
    {
        internal RulesFailed(ErrorResponse error)
        {
            Error = error;
        }

        /// <summary>The Error.</summary>
        public ErrorResponse Error { get; }
    }
}
