using System.Collections.Concurrent;
using System.Diagnostics;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
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
    private const string Location = nameof(RemoteIndexUpdater);

    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> gates = new();

    /// <inheritdoc />
    public async Task<Result<RemoteUpdateSummary, ProviderError>> UpdateAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);
        using var entered = await EnterAsync(accountId, cancellationToken).ConfigureAwait(false);

        return await feeds.ForAccountAsync(accountId, cancellationToken)
            .BindAsync(feed => Try.RunAsync(() => RunAsync(feed, accountId, cancellationToken), cancellationToken).MatchAsync(result => result, exception => Failed(exception)))
            .ConfigureAwait(false);
    }

    private static Result<RemoteUpdateSummary, ProviderError> Succeeded(RemoteUpdateSummary summary) => Result.Success<RemoteUpdateSummary, ProviderError>(summary);

    private static Result<RemoteUpdateSummary, ProviderError> Stopped(ProviderError error) => Result.Failure<RemoteUpdateSummary, ProviderError>(error);

    private async Task<IDisposable> EnterAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var gate = gates.GetOrAdd(accountId.Value, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        return new GateRelease(gate);
    }

    private async Task<Result<RemoteUpdateSummary, ProviderError>> RunAsync(IRemoteChangeFeed feed, AccountId accountId, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var cursor = (await indexReader.GetCursorAsync(accountId, cancellationToken).ConfigureAwait(false)).OrThrow();
        var running = (await indexReader.GetResyncPassAsync(accountId, cancellationToken).ConfigureAwait(false)).OrThrow();
        var run = new UpdateRun(accountId, cursor, running.Match(_ => Option.Some(ResyncKindFactory.CreateUnknown()), () => Option.None<ResyncKind>()));

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = await feed.GetChangesAsync(accountId, run.Cursor, cancellationToken).ConfigureAwait(false);
            var step = await read.Match(page => ApplyAsync(run, page, cancellationToken), error => OnErrorAsync(run, error, cancellationToken)).ConfigureAwait(false);

            if (step is Step.Failed failed)
                return Stopped(failed.Error);

            if (step is Step.Finished)
                break;
        }

        await FinishResyncAsync(run, cancellationToken).ConfigureAwait(false);
        var summary = run.ToSummary();
        var elapsedMilliseconds = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        var message = $"The remote index was updated: {summary.PagesRead} pages, {summary.Changes.Added} added, {summary.Changes.Modified} modified, {summary.Changes.Moved} moved, {summary.Changes.Deleted} deleted, {elapsedMilliseconds} ms.";
        LogMessage.Information(logger, Location, message);

        return Succeeded(summary);
    }

    private async Task<Step> ApplyAsync(UpdateRun run, RemoteChangePage page, CancellationToken cancellationToken)
    {
        var before = (await ruleReader.GetAsync(run.AccountId, cancellationToken).ConfigureAwait(false)).OrThrow();
        var delta = (await indexWriter.ApplyPageAsync(run.AccountId, page, cancellationToken).ConfigureAwait(false)).OrThrow();
        var moves = delta.Changes.OfType<RemoteChange.Moved>().ToList();
        var relocation = moves.Count == 0 ? RuleRelocationSummaryFactory.None : (await relocator.RelocateAsync(run.AccountId, moves, cancellationToken).ConfigureAwait(false)).OrThrow();
        var after = moves.Count == 0 ? before : (await ruleReader.GetAsync(run.AccountId, cancellationToken).ConfigureAwait(false)).OrThrow();
        run.CountPage();
        run.Add(await relevance.CountAsync(run.AccountId, delta.Changes, before, after, cancellationToken).ConfigureAwait(false), relocation);

        return page.Next switch
        {
            PageContinuation.MorePages more => Advance(run, more.Next),
            _ => Step.Done,
        };
    }

    private Step Advance(UpdateRun run, ChangeCursor next)
    {
        if (!run.Seen.Add(next.Value))
        {
            LogMessage.Warning(logger, Location, "The change feed returned a link it had already returned, so the update stopped.");

            return Step.Stop(ProviderErrorFactory.CreateUnexpected());
        }

        run.Cursor = Option.Some(next);

        return Step.Next;
    }

    private async Task<Step> OnErrorAsync(UpdateRun run, ProviderError error, CancellationToken cancellationToken)
    {
        if (error is not ProviderError.CursorExpired expired || run.Expirations >= 1)
            return Step.Stop(error);

        run.Expirations++;
        _ = (await indexWriter.BeginResyncAsync(run.AccountId, cancellationToken).ConfigureAwait(false)).OrThrow();
        run.Resync = Option.Some(expired.Kind);
        run.Cursor = Option.Some(expired.RestartFrom);
        _ = run.Seen.Add(expired.RestartFrom.Value);
        LogMessage.Warning(logger, Location, $"The change cursor expired, so a resync of kind {expired.Kind.GetType().Name} started.");

        return Step.Next;
    }

    private async Task FinishResyncAsync(UpdateRun run, CancellationToken cancellationToken)
    {
        if (run.Resync.Match(_ => false, () => true))
            return;

        var delta = (await indexWriter.CompleteResyncAsync(run.AccountId, cancellationToken).ConfigureAwait(false)).OrThrow();
        var rules = (await ruleReader.GetAsync(run.AccountId, cancellationToken).ConfigureAwait(false)).OrThrow();
        run.Add(await relevance.CountAsync(run.AccountId, delta.Changes, rules, rules, cancellationToken).ConfigureAwait(false), RuleRelocationSummaryFactory.None);
    }

    private Result<RemoteUpdateSummary, ProviderError> Failed(Exception exception)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Stopped(ProviderErrorFactory.CreateUnexpected());
    }

    private abstract record Step
    {
        internal static readonly Step Next = new Continue();

        internal static readonly Step Done = new Finished();

        private Step()
        {
        }

        internal static Failed Stop(ProviderError error) => new Failed(error);

        internal sealed record Continue : Step;

        internal sealed record Finished : Step;

        internal sealed record Failed(ProviderError Error) : Step;
    }

    private sealed class GateRelease(SemaphoreSlim gate) : IDisposable
    {
        public void Dispose() => gate.Release();
    }

    private sealed class UpdateRun(AccountId accountId, Option<ChangeCursor> cursor, Option<ResyncKind> resync)
    {
        private RelevantChangeCounts counts = RelevantChangeCountsFactory.None;
        private RuleRelocationSummary rules = RuleRelocationSummaryFactory.None;
        private int pages;

        internal AccountId AccountId { get; } = accountId;

        internal Option<ChangeCursor> Cursor { get; set; } = cursor;

        internal Option<ResyncKind> Resync { get; set; } = resync;

        internal int Expirations { get; set; }

        internal HashSet<string> Seen { get; } = cursor.Match(found => new HashSet<string>(StringComparer.Ordinal) { found.Value }, () => new HashSet<string>(StringComparer.Ordinal));

        internal void Add(RelevantChangeCounts found, RuleRelocationSummary relocated)
        {
            counts = counts.Plus(found);
            rules = RuleRelocationSummaryFactory.Create(rules.Relocated + relocated.Relocated, rules.Dropped + relocated.Dropped);
        }

        internal void CountPage() => pages++;

        internal RemoteUpdateSummary ToSummary() => RemoteUpdateSummaryFactory.Create(counts, rules, Resync, pages);
    }
}
