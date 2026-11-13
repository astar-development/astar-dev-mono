using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads the status each account's last sync run left (#1402). It is stored, so a failure still shows after a restart.</summary>
public interface ISyncOutcomeReader
{
    /// <summary>Raised after a status is saved, on the saving thread, so the UI can reload.</summary>
    event EventHandler? Changed;

    /// <summary>The stored status of every account that has one. An account without a stored status has not synced yet.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyDictionary<AccountId, SyncStatus>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken);
}
