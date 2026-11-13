using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Why a local file could not be read during a scan (#1399).</summary>
public abstract record LocalScanFailure
{
    private LocalScanFailure()
    {
    }

    /// <summary>The file is locked, was deleted during the scan or cannot be opened; the scan reports it as unreadable, never as deleted.</summary>
    public sealed record Unreadable : LocalScanFailure
    {
        internal Unreadable()
        {
        }
    }

    /// <summary>Something else went wrong.</summary>
    public sealed record Unexpected : LocalScanFailure
    {
        internal Unexpected(ErrorResponse error)
        {
            Error = error;
        }

        /// <summary>The error.</summary>
        public ErrorResponse Error { get; }
    }
}
