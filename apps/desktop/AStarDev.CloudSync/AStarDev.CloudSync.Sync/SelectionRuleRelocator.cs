using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Moves selection rules to follow folders that were renamed or moved remotely (#1398).</summary>
/// <param name="indexReader">Computes each folder's current path.</param>
/// <param name="ruleReader">Reads the rules.</param>
/// <param name="ruleWriter">Saves the relocations in one transaction.</param>
/// <param name="logger">Reports failures by exception type only.</param>
internal sealed class SelectionRuleRelocator(IRemoteIndexReader indexReader, ISelectionRuleReader ruleReader, ISelectionRuleWriter ruleWriter, ILogger<SelectionRuleRelocator> logger) : ISelectionRuleRelocator
{
    /// <inheritdoc />
    public Task<Result<RuleRelocationSummary, ErrorResponse>> RelocateAsync(AccountId accountId, IReadOnlyList<RemoteChange.Moved> moves, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{accountId}{moves}{cancellationToken}{indexReader}{ruleReader}{ruleWriter}{logger}");
}
