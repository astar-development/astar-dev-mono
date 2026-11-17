using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class ThrowingRootChangeParticipant : ISyncRootChangeParticipant
{
    public Task<Result<Unit, ErrorResponse>> RemoveRootDataAsync(AccountId accountId, CancellationToken cancellationToken) => throw new InvalidOperationException("The participant failed.");
}
