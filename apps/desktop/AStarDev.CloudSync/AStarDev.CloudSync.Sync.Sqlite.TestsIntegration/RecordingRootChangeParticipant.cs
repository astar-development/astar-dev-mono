using System.Collections.Concurrent;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class RecordingRootChangeParticipant : ISyncRootChangeParticipant
{
    private readonly ConcurrentQueue<AccountId> calls = new();

    public IReadOnlyList<AccountId> Calls => [.. calls];

    public Task<Result<Unit, ErrorResponse>> RemoveRootDataAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        calls.Enqueue(accountId);

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }
}
