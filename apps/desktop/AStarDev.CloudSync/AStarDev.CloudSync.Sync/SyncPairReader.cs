using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="rules">Reads the account's selection rules.</param>
public sealed class SyncPairReader(ISelectionRuleReader rules) : ISyncPairReader
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<SyncPair>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) =>
        rules.GetAsync(accountId, cancellationToken)
            .MatchAsync(
                stored => Result.Success<IReadOnlyList<SyncPair>, ErrorResponse>(PairsOf(stored)),
                Result.Failure<IReadOnlyList<SyncPair>, ErrorResponse>);

    private static bool IsInclude(SelectionRule rule) => rule.Kind is SelectionRuleKind.Include;

    private static IReadOnlyList<SyncPair> PairsOf(IReadOnlyList<SelectionRule> stored) =>
        [.. stored
            .Where(IsInclude)
            .Where(rule => !stored.Any(other => IsInclude(other) && SelectionPaths.IsStrictlyBelow(rule.Path, other.Path)))
            .OrderBy(rule => rule.Path.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(ToPair)];

    private static SyncPair ToPair(SelectionRule rule) =>
        SyncPairFactory.Create(
            SyncPairIdFactory.CreateForRule(rule.AccountId, rule.Path),
            rule.AccountId,
            SelectionPaths.Segments(rule.Path).LastOrDefault() ?? string.Empty,
            rule.Path,
            rule.RemoteId,
            rule.Kind is SelectionRuleKind.Include include ? include.Direction : SyncDirectionFactory.CreateTwoWay(),
            SyncStatusFactory.CreateUpToDate(),
            0,
            Option.None<DateTimeOffset>());
}
