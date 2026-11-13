namespace AStarDev.CloudSync.Sync;

/// <summary>Why one item of the download pass failed (#1400): the provider or the local file system.</summary>
public abstract record DownloadFailure
{
    private DownloadFailure()
    {
    }

    /// <summary>The provider failed the request.</summary>
    public sealed record Provider : DownloadFailure
    {
        internal Provider(ProviderError error) => Error = error;

        /// <summary>The provider's error.</summary>
        public ProviderError Error { get; }
    }

    /// <summary>A local file operation failed.</summary>
    public sealed record Local : DownloadFailure
    {
        internal Local(LocalOperationFailure failure) => Failure = failure;

        /// <summary>The local failure.</summary>
        public LocalOperationFailure Failure { get; }
    }
}
