using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Counts the remote changes whose old or new path the selection includes (#1398).</summary>
/// <param name="indexReader">Computes paths from the parent chain.</param>
/// <param name="evaluator">Decides whether a path is included.</param>
internal sealed class RemoteChangeRelevance(IRemoteIndexReader indexReader, ISyncSelectionEvaluator evaluator)
{
    internal Task<RelevantChangeCounts> CountAsync(AccountId accountId, IReadOnlyList<RemoteChange> changes, IReadOnlyList<SelectionRule> rulesBefore, IReadOnlyList<SelectionRule> rulesAfter, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{accountId}{changes}{rulesBefore}{rulesAfter}{cancellationToken}{indexReader}{evaluator}");
}
