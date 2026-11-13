using System.Diagnostics;
using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="roots">Reads the account's sync root.</param>
/// <param name="baselineWriter">Refreshes the time of touched-only files.</param>
/// <param name="walker">Walks the sync root.</param>
/// <param name="comparer">Compares each scanned item with the baseline.</param>
/// <param name="deletions">Finds baseline items that are gone and applies the mass-deletion guard.</param>
/// <param name="newFolders">Reads the rules and gives new local folders their default rule.</param>
/// <param name="logger">Reports counts and elapsed time only, never a path.</param>
internal sealed class LocalChangeDetector(ISyncRootReader roots, ISyncBaselineWriter baselineWriter, LocalTreeWalker walker, LocalChangeComparer comparer, LocalDeletionGuard deletions, NewFolderRules newFolders, ILogger<LocalChangeDetector> logger) : ILocalChangeDetector
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
        if (!root.Directory.Exists || !IsListable(root.Directory))
            return Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateRootUnavailable());

        return await Try.RunAsync(() => ScanAsync(accountId, root, started, cancellationToken), cancellationToken)
            .MatchAsync(set => Result.Success<LocalChangeSet, LocalScanFailure>(set), Failed).ConfigureAwait(false);
    }

    private static bool IsListable(IDirectoryInfo directory) => Try.Run(() => directory.EnumerateFileSystemInfos().Any()).Match(_ => true, _ => false);

    private Result<LocalChangeSet, LocalScanFailure> Failed(Exception exception)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<LocalChangeSet, LocalScanFailure>(LocalScanFailureFactory.CreateUnexpected(new ErrorResponse(exception.GetType().Name)));
    }

    private async Task RefreshAsync(AccountId accountId, SyncBaselineEntry[] chunk, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var saved = await baselineWriter.UpsertAsync(accountId, chunk, cancellationToken).ConfigureAwait(false);
        _ = saved.Match(_ => true, _ => Warn("The baseline time of touched files could not be refreshed; the next scan will try again."));
    }

    private bool Warn(string message)
    {
        LogMessage.Warning(logger, Location, message);

        return false;
    }

    private async Task<LocalChangeSet> ScanAsync(AccountId accountId, SyncRoot root, long started, CancellationToken cancellationToken)
    {
        var rules = await newFolders.PrepareAsync(accountId, root.Directory, cancellationToken).ConfigureAwait(false);
        var changes = new List<LocalChange>();
        var touched = new List<SyncBaselineEntry>();
        var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hashed = 0;
        foreach (var item in walker.Walk(root.Directory, rules, cancellationToken))
        {
            _ = item.Info.Match(_ => false, () => blocked.Add(item.Path.Value ?? string.Empty));
            var comparison = await comparer.CompareAsync(accountId, item, cancellationToken).ConfigureAwait(false);
            _ = comparison.Change.Match(change => { changes.Add(change); return true; }, () => false);
            _ = comparison.Touched.Match(entry => { touched.Add(entry); return true; }, () => false);
            hashed += comparison.Hashed ? 1 : 0;
        }

        var guarded = await deletions.FindAsync(accountId, root.Directory, rules, blocked, cancellationToken).ConfigureAwait(false);
        changes.AddRange(guarded.Changes);
        foreach (var chunk in touched.Chunk(RefreshChunkSize))
            await RefreshAsync(accountId, chunk, cancellationToken).ConfigureAwait(false);

        var set = LocalChangeSetFactory.Create(changes, guarded.Holds, touched.Count, hashed);
        var message = $"The local scan found {set.Summary.Added} added, {set.Summary.Modified} modified, {set.Summary.Deleted} deleted, {set.Summary.Deferred} deferred and {set.Summary.Unreadable} unreadable items, held the deletions of {set.Holds.Count} pairs and hashed {set.Summary.Hashed} files in {Stopwatch.GetElapsedTime(started).TotalMilliseconds:0} ms.";
        LogMessage.Information(logger, Location, message);

        return set;
    }
}
