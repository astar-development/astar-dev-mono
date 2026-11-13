using System.IO.Abstractions;
using System.Text.RegularExpressions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="roots">Reads the account's sync root.</param>
/// <param name="loader">Reads the planner's inputs.</param>
/// <param name="planner">Plans the pass.</param>
/// <param name="executor">Carries out the plan.</param>
/// <param name="attentionWriter">Records the deletions the guard held.</param>
/// <param name="time">Stamps the held deletions.</param>
/// <param name="logger">Reports the pass with counts and the hashed account id only.</param>
internal sealed partial class DownloadPass(ISyncRootReader roots, DownloadPlanLoader loader, IDownloadPlanner planner, DownloadExecutor executor, ISyncAttentionWriter attentionWriter, TimeProvider time, ILogger<DownloadPass> logger) : IDownloadPass
{
    private const string Location = nameof(DownloadPass);

    [GeneratedRegex(@"\.[0-9a-fA-F]{8}(-?[0-9a-fA-F]{4}){3}-?[0-9a-fA-F]{12}\.download$", RegexOptions.CultureInvariant)]
    private static partial Regex TempFile();

    /// <inheritdoc />
    public async Task<Result<DownloadPassSummary, DownloadPassFailure>> RunAsync(AccountId accountId, LocalChangeSet localChanges, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);
        var root = await roots.GetAsync(accountId, cancellationToken).ConfigureAwait(false);

        return await root.MatchAsync(found => found.Match(syncRoot => RunInRootAsync(accountId, syncRoot.Directory, localChanges, cancellationToken), () => Task.FromResult(RootUnavailable())), _ => Task.FromResult(RootUnavailable())).ConfigureAwait(false);
    }

    private static Result<DownloadPassSummary, DownloadPassFailure> RootUnavailable() => Result.Failure<DownloadPassSummary, DownloadPassFailure>(DownloadPassFailureFactory.CreateRootUnavailable());

    private static int DeleteOrphans(IDirectoryInfo root) => Try.Run(() => root.EnumerateFiles("*.download", SearchOption.AllDirectories).Where(file => TempFile().IsMatch(file.Name)).Count(DeleteQuietly)).Match(count => count, _ => 0);

    private static bool DeleteQuietly(IFileInfo file) => Try.Run(() =>
    {
        file.Delete();

        return true;
    }).Match(deleted => deleted, _ => false);

    private async Task<Result<DownloadPassSummary, DownloadPassFailure>> RunInRootAsync(AccountId accountId, IDirectoryInfo root, LocalChangeSet localChanges, CancellationToken cancellationToken)
    {
        root.Refresh();
        if (!root.Exists)
            return RootUnavailable();

        var orphans = DeleteOrphans(root);
        var summary = await loader.LoadAsync(accountId, root, localChanges, cancellationToken)
            .BindAsync(request => PlanAndRunAsync(accountId, root, request, cancellationToken))
            .ConfigureAwait(false);
        LogPass(orphans, summary);

        return summary;
    }

    private async Task<Result<DownloadPassSummary, DownloadPassFailure>> PlanAndRunAsync(AccountId accountId, IDirectoryInfo root, DownloadPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = planner.Plan(request);
        var recorded = await RecordHeldAsync(accountId, plan, cancellationToken).ConfigureAwait(false);

        return await recorded.BindAsync(_ => executor.RunAsync(accountId, root, plan, request.Baseline, cancellationToken).BindAsync(outcomes => Task.FromResult(Result.Success<DownloadPassSummary, DownloadPassFailure>(DownloadPassSummaryFactory.Create(outcomes, plan.Held))))).ConfigureAwait(false);
    }

    private async Task<Result<Unit, DownloadPassFailure>> RecordHeldAsync(AccountId accountId, DownloadPlan plan, CancellationToken cancellationToken)
    {
        if (plan.Held.Count == 0)
            return Result.Success<Unit, DownloadPassFailure>(Unit.Instance);

        var items = plan.Held.Select(held => SyncAttentionItemFactory.CreateHeldDeletions(new HeldDeletionId(Guid.NewGuid()), accountId, held.PairId, DeletionDirectionFactory.CreateDeleteLocal(), held.Paths, time.GetUtcNow())).ToList();
        var recorded = await attentionWriter.RecordAsync(items, cancellationToken).ConfigureAwait(false);

        return recorded.Match(Result.Success<Unit, DownloadPassFailure>, _ => HeldNotRecorded());
    }

    private Result<Unit, DownloadPassFailure> HeldNotRecorded()
    {
        LogMessage.Warning(logger, Location, "The download pass could not record the deletions it held.");

        return Result.Failure<Unit, DownloadPassFailure>(DownloadPassFailureFactory.CreateUnexpected());
    }

    private void LogPass(int orphans, Result<DownloadPassSummary, DownloadPassFailure> summary)
    {
        var message = summary.Match(
            done => $"The download pass finished: {done.Downloaded} downloaded ({done.Bytes} bytes), {done.FoldersCreated} folders created, {done.Moved} moved, {done.DeletedLocally} deleted, {done.HeldByGuard} held, {done.SentToConflicts} sent to conflicts, {done.Skipped} skipped, {done.Failed} failed, {orphans} orphan temp files removed.",
            failure => $"The download pass stopped ({failure.GetType().Name}), {orphans} orphan temp files removed.");
        LogMessage.Information(logger, Location, message);
    }
}
