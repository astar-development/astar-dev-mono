using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GatedSyncRootWriter(ISyncRootWriter inner, WriteGate gate) : ISyncRootWriter
{
    public async Task<Result<SyncRoot, SyncRootRejection>> SetAsync(AccountId accountId, IDirectoryInfo candidate, CancellationToken cancellationToken)
    {
        await gate.PassAsync();

        return await inner.SetAsync(accountId, candidate, cancellationToken);
    }
}
