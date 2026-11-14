using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeSyncBaselineCounter : ISyncBaselineCounter
{
    private readonly Lock gate = new();
    private readonly List<AccountId> counted = [];

    public Func<AccountId, Result<BaselineCounts, ErrorResponse>> Counts { get; init; } = _ => Result.Success<BaselineCounts, ErrorResponse>(BaselineCountsFactory.Create(0, 0));

    public IReadOnlyList<AccountId> Counted
    {
        get
        {
            lock (gate)
                return [.. counted];
        }
    }

    public Task<Result<BaselineCounts, ErrorResponse>> CountAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        lock (gate)
            counted.Add(accountId);

        return Task.FromResult(Counts(accountId));
    }
}
