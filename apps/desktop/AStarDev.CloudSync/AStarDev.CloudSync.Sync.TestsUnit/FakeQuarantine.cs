using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeQuarantine : IPathQuarantine
{
    public QuarantineSnapshot Snapshot { get; set; } = QuarantineSnapshotFactory.CreateEmpty();

    public Task<Result<QuarantineSnapshot, ErrorResponse>> ReadAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(Result.Success<QuarantineSnapshot, ErrorResponse>(Snapshot));
}
