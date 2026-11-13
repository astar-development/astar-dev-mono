using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads what an upload pass needs.</summary>
/// <param name="roots">Reads the account's sync root.</param>
/// <param name="rules">Reads the selection rules.</param>
/// <param name="quarantine">Tells which paths may not be synced now.</param>
/// <param name="baseline">Reads the baseline.</param>
/// <param name="remoteIndex">Reads the remote index.</param>
internal sealed class UploadPassInputs(ISyncRootReader roots, ISelectionRuleReader rules, IPathQuarantine quarantine, ISyncBaselineReader baseline, IRemoteIndexReader remoteIndex)
{
    internal Task<Result<UploadPassInput, UploadPassFailure>> LoadAsync(AccountId accountId, LocalChangeSet changes, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{roots}{rules}{quarantine}{baseline}{remoteIndex}{accountId}{changes}{cancellationToken}");
}
