using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="roots">Reads the account's sync root.</param>
/// <param name="ruleReader">Reads the selection rules, so ignored folders are not counted.</param>
/// <param name="walker">Walks the local folder with the same filters the sync uses.</param>
/// <param name="feeds">Finds the change feed of the account's provider.</param>
/// <param name="cloudCounter">Counts the files the change feed reports.</param>
/// <param name="logger">Reports counts and error types only.</param>
internal sealed class FileAuditor(ISyncRootReader roots, ISelectionRuleReader ruleReader, LocalTreeWalker walker, IProviderCapability<IRemoteChangeFeed> feeds, CloudFileCounter cloudCounter, ILogger<FileAuditor> logger) : IFileAuditor
{
    private const string Location = nameof(FileAuditor);

    /// <inheritdoc />
    public async Task<Result<FileAuditCounts, FileAuditFailure>> AuditAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);

        return await ruleReader.GetAsync(accountId, cancellationToken)
            .MatchAsync(rules => CountAsync(accountId, rules, cancellationToken), _ => Task.FromResult(Fail<FileAuditCounts>(FileAuditFailureFactory.CreateRulesUnavailable())))
            .ConfigureAwait(false);
    }

    private static Result<T, FileAuditFailure> Fail<T>(FileAuditFailure failure) => Result.Failure<T, FileAuditFailure>(failure);

    private static bool IsFile(LocalWalkItem item) => item.Info.Match(info => info is IFileInfo, () => false);

    private async Task<Result<FileAuditCounts, FileAuditFailure>> CountAsync(AccountId accountId, IReadOnlyList<SelectionRule> rules, CancellationToken cancellationToken)
    {
        var local = await CountLocalAsync(accountId, rules, cancellationToken).ConfigureAwait(false);

        return await local.MatchAsync(
            localFiles => CountCloudAsync(accountId, rules, cancellationToken).MatchAsync(cloudFiles => Report(localFiles, cloudFiles), failure => Result.Failure<FileAuditCounts, FileAuditFailure>(failure)),
            failure => Task.FromResult(Result.Failure<FileAuditCounts, FileAuditFailure>(failure))).ConfigureAwait(false);
    }

    private Result<FileAuditCounts, FileAuditFailure> Report(long localFiles, long cloudFiles)
    {
        var message = $"The audit counted {localFiles} local files and {cloudFiles} cloud files.";
        LogMessage.Information(logger, Location, message);

        return Result.Success<FileAuditCounts, FileAuditFailure>(FileAuditCountsFactory.Create(localFiles, cloudFiles));
    }

    private Task<Result<long, FileAuditFailure>> CountLocalAsync(AccountId accountId, IReadOnlyList<SelectionRule> rules, CancellationToken cancellationToken) =>
        roots.GetAsync(accountId, cancellationToken).MatchAsync(
            option => option.Match(root => CountFilesAsync(root, rules, cancellationToken), () => Task.FromResult(Fail<long>(FileAuditFailureFactory.CreateRootUnavailable()))),
            _ => Task.FromResult(Fail<long>(FileAuditFailureFactory.CreateRootUnavailable())));

    private async Task<Result<long, FileAuditFailure>> CountFilesAsync(SyncRoot root, IReadOnlyList<SelectionRule> rules, CancellationToken cancellationToken)
    {
        root.Directory.Refresh();
        if (!root.Directory.Exists)
            return Fail<long>(FileAuditFailureFactory.CreateRootUnavailable());

        return await Try.RunAsync(() => Task.Run(() => walker.Walk(root.Directory, rules, cancellationToken).LongCount(IsFile), cancellationToken), cancellationToken)
            .MatchAsync(count => Result.Success<long, FileAuditFailure>(count), exception => Faulted(exception, FileAuditFailureFactory.CreateRootUnavailable())).ConfigureAwait(false);
    }

    private async Task<Result<long, FileAuditFailure>> CountCloudAsync(AccountId accountId, IReadOnlyList<SelectionRule> rules, CancellationToken cancellationToken) =>
        await feeds.ForAccountAsync(accountId, cancellationToken).MatchAsync(
            feed => Try.RunAsync(() => cloudCounter.CountAsync(feed, accountId, rules, cancellationToken), cancellationToken)
                .MatchAsync(counted => counted.Match(count => Result.Success<long, FileAuditFailure>(count), error => Fail<long>(FileAuditFailureFactory.CreateCloudUnavailable(error))), exception => Faulted(exception, FileAuditFailureFactory.CreateCloudUnavailable(ProviderErrorFactory.CreateUnexpected()))),
            error => Task.FromResult(Fail<long>(FileAuditFailureFactory.CreateCloudUnavailable(error)))).ConfigureAwait(false);

    private Result<long, FileAuditFailure> Faulted(Exception exception, FileAuditFailure failure)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Fail<long>(failure);
    }
}
