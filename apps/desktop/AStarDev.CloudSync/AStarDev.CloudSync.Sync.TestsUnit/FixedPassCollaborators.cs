using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FixedSelectionRuleReader(IReadOnlyList<SelectionRule> rules) : ISelectionRuleReader
{
    public Task<Result<IReadOnlyList<SelectionRule>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>(rules));
}

public sealed class FixedPathQuarantine(QuarantineSnapshot snapshot) : IPathQuarantine
{
    public Task<Result<QuarantineSnapshot, ErrorResponse>> ReadAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(Result.Success<QuarantineSnapshot, ErrorResponse>(snapshot));
}
