using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads the change feed page by page into the remote index and reports what matters to the selection (#1398).</summary>
/// <param name="feeds">Finds the change feed of the account's provider.</param>
/// <param name="indexReader">Reads the saved cursor and resync pass.</param>
/// <param name="indexWriter">Applies pages and runs the resync.</param>
/// <param name="ruleReader">Reads the selection rules.</param>
/// <param name="relocator">Moves rules after remote moves.</param>
/// <param name="relevance">Counts the changes that matter to the selection.</param>
/// <param name="logger">Reports counts and timings only.</param>
internal sealed class RemoteIndexUpdater(IProviderCapability<IRemoteChangeFeed> feeds, IRemoteIndexReader indexReader, IRemoteIndexWriter indexWriter, ISelectionRuleReader ruleReader, ISelectionRuleRelocator relocator, RemoteChangeRelevance relevance, ILogger<RemoteIndexUpdater> logger) : IRemoteIndexUpdater
{
    /// <inheritdoc />
    public Task<Result<RemoteUpdateSummary, ProviderError>> UpdateAsync(AccountId accountId, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{accountId}{cancellationToken}{feeds}{indexReader}{indexWriter}{ruleReader}{relocator}{relevance}{logger}");
}
