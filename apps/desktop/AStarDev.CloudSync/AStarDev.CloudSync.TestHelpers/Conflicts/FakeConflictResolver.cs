using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Conflicts;

public sealed class FakeConflictResolver : IConflictResolver
{
    private readonly List<(AccountId AccountId, IReadOnlyList<ConflictId> Ids, ConflictStrategy Strategy)> calls = [];
    private readonly Lock gate = new();

    public IReadOnlyList<(AccountId AccountId, IReadOnlyList<ConflictId> Ids, ConflictStrategy Strategy)> Calls
    {
        get
        {
            lock (gate)
                return [.. calls];
        }
    }

    public Func<AccountId, IReadOnlyList<ConflictId>, ConflictStrategy, CancellationToken, Task<Result<ResolutionSummary, ErrorResponse>>> Outcome { get; set; } = (_, ids, strategy, _) =>
        Task.FromResult(Result.Success<ResolutionSummary, ErrorResponse>(strategy is ConflictStrategy.Skip ? ResolutionSummaryFactory.Create(0, [], ids.Count) : ResolutionSummaryFactory.Create(ids.Count, [], 0)));

    public Task<Result<ResolutionSummary, ErrorResponse>> ResolveAsync(AccountId accountId, IReadOnlyList<ConflictId> ids, ConflictStrategy strategy, CancellationToken cancellationToken)
    {
        lock (gate)
            calls.Add((accountId, ids, strategy));

        return Outcome(accountId, ids, strategy, cancellationToken);
    }
}
