using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="roots">Reads the account's sync root.</param>
/// <param name="ruleReader">Reads the selection rules, so ignored folders are not counted.</param>
/// <param name="walker">Walks the local folder with the same filters the sync uses.</param>
/// <param name="feeds">Finds the change feed of the account's provider.</param>
/// <param name="logger">Reports counts and error types only.</param>
internal sealed class FileAuditor(ISyncRootReader roots, ISelectionRuleReader ruleReader, LocalTreeWalker walker, IProviderCapability<IRemoteChangeFeed> feeds, ILogger<FileAuditor> logger) : IFileAuditor
{
    /// <inheritdoc />
    public Task<Result<FileAuditCounts, FileAuditFailure>> AuditAsync(AccountId accountId, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{accountId}{cancellationToken}{roots}{ruleReader}{walker}{feeds}{logger}");
}
