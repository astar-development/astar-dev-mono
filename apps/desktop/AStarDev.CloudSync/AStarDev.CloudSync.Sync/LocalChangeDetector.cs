using System.Diagnostics;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="roots">Reads the account's sync root.</param>
/// <param name="ruleReader">Reads the account's selection rules.</param>
/// <param name="baselineWriter">Refreshes the time of touched-only files.</param>
/// <param name="walker">Walks the sync root.</param>
/// <param name="comparer">Compares each scanned item with the baseline.</param>
/// <param name="deletions">Finds baseline items that are gone.</param>
/// <param name="logger">Reports counts and elapsed time only, never a path.</param>
internal sealed class LocalChangeDetector(ISyncRootReader roots, ISelectionRuleReader ruleReader, ISyncBaselineWriter baselineWriter, LocalTreeWalker walker, LocalChangeComparer comparer, LocalDeletionFinder deletions, ILogger<LocalChangeDetector> logger) : ILocalChangeDetector
{
    private const string Location = nameof(LocalChangeDetector);
    private const int RefreshChunkSize = 500;

    /// <inheritdoc />
    public async Task<Result<LocalChangeSet, LocalScanFailure>> DetectAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);
        var started = Stopwatch.GetTimestamp();

        return await roots.GetAsync(accountId, cancellationToken).MatchAsync(
            option => option.Match(root => ScanRootAsync(accountId, root, started, cancellationToken), () => Task.FromResult(Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateRootUnavailable()))),
            error => Task.FromResult(Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateUnexpected(error)))).ConfigureAwait(false);
    }

    private async Task<Result<LocalChangeSet, LocalScanFailure>> ScanRootAsync(AccountId accountId, SyncRoot root, long started, CancellationToken cancellationToken)
    {
        root.Directory.Refresh();
        if (!root.Directory.Exists)
            return Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateRootUnavailable());

        return await Try.RunAsync(() => ScanAsync(accountId, root, started, cancellationToken), cancellationToken)
            .MatchAsync(set => Result.Success<LocalChangeSet, LocalScanFailure>(set), Failed).ConfigureAwait(false);
    }

    private Result<LocalChangeSet, LocalScanFailure> Failed(Exception exception)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateUnexpected(new ErrorResponse(exception.GetType().Name)));
    }

    private async Task<LocalChangeSet> ScanAsync(AccountId accountId, SyncRoot root, long started, CancellationToken cancellationToken)
    {
        var rules = (await ruleReader.GetAsync(accountId, cancellationToken).ConfigureAwait(false)).OrThrow();
        var changes = new List<LocalChange>();
        var touched = new List<SyncBaselineEntry>();
        var hashed = 0;
        foreach (var item in walker.Walk(root.Directory, rules, cancellationToken))
        {
            var comparison = await comparer.CompareAsync(accountId, item, cancellationToken).ConfigureAwait(false);
            _ = comparison.Change.Match(change => { changes.Add(change); return true; }, () => false);
            _ = comparison.Touched.Match(entry => { touched.Add(entry); return true; }, () => false);
            hashed += comparison.Hashed ? 1 : 0;
        }

        changes.AddRange(await deletions.FindAsync(accountId, root.Directory, rules, cancellationToken).ConfigureAwait(false));
        foreach (var chunk in touched.Chunk(RefreshChunkSize))
            _ = (await baselineWriter.UpsertAsync(accountId, chunk, cancellationToken).ConfigureAwait(false)).OrThrow();

        var set = LocalChangeSetFactory.Create(changes, touched.Count, hashed);
        var message = $"The local scan found {set.Summary.Added} added, {set.Summary.Modified} modified, {set.Summary.Deleted} deleted, {set.Summary.Deferred} deferred and {set.Summary.Unreadable} unreadable items and hashed {set.Summary.Hashed} files in {Stopwatch.GetElapsedTime(started).TotalMilliseconds:0} ms.";
        LogMessage.Information(logger, Location, message);

        return set;
    }
}
