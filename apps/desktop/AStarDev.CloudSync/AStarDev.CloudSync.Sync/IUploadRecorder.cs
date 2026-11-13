using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Records one successful upload or folder creation: the baseline row and the remote index row are written in one transaction, so the next delta that returns our own change finds both already matching (echo suppression).</summary>
public interface IUploadRecorder
{
    /// <summary>Writes the baseline row and upserts the remote index row together.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="baseline">The baseline row to write.</param>
    /// <param name="remote">The remote index row to upsert.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<Unit, ErrorResponse>> RecordAsync(AccountId accountId, SyncBaselineEntry baseline, RemoteItemSnapshot remote, CancellationToken cancellationToken);
}
