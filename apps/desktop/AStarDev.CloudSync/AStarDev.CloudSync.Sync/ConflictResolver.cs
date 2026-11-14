using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="syncLock">Gives the account lease for the call.</param>
/// <param name="reader">Reads the open conflicts.</param>
/// <param name="closer">Closes a settled conflict.</param>
/// <param name="planner">Plans each path.</param>
/// <param name="steps">Carries out the plan.</param>
/// <param name="logger">Reports the counts with no paths.</param>
internal sealed class ConflictResolver(IAccountSyncLock syncLock, ISyncAttentionReader reader, ISyncAttentionCloser closer, IConflictResolutionPlanner planner, ResolutionSteps steps, ILogger<ConflictResolver> logger) : IConflictResolver
{
    private const string Location = nameof(ConflictResolver);

    /// <inheritdoc />
    public async Task<Result<ResolutionSummary, ErrorResponse>> ResolveAsync(AccountId accountId, IReadOnlyList<ConflictId> ids, ConflictStrategy strategy, CancellationToken cancellationToken)
    {
        var attempt = syncLock.TryAcquire(accountId);
        if (attempt is not SyncLockAttempt.Acquired acquired)
            return Result.Failure<ResolutionSummary, ErrorResponse>(ResolutionRefusal.For(attempt));

        using var lease = acquired.Lease;

        return await reader.GetOpenAsync(accountId, cancellationToken)
            .BindAsync(open => ResolveOpenAsync(accountId, [.. open.OfType<SyncAttentionItem.Conflict>()], [.. (ids ?? []).Distinct()], strategy, cancellationToken))
            .ConfigureAwait(false);
    }

    private static AttentionResolution ResolutionOf(ConflictStrategy strategy) => strategy switch
    {
        ConflictStrategy.LocalWins => AttentionResolutionFactory.CreateKeepLocal(),
        ConflictStrategy.RemoteWins => AttentionResolutionFactory.CreateKeepCloud(),
        _ => AttentionResolutionFactory.CreateKeepBoth(),
    };

    private async Task<Result<ResolutionSummary, ErrorResponse>> ResolveOpenAsync(AccountId accountId, IReadOnlyList<SyncAttentionItem.Conflict> open, IReadOnlyList<ConflictId> ids, ConflictStrategy strategy, CancellationToken cancellationToken)
    {
        var tally = new Tally();
        var selected = open.Where(conflict => ids.Contains(conflict.Id)).ToList();
        foreach (var id in ids.Where(id => selected.All(conflict => conflict.Id != id)))
            tally.Failed.Add(FailedResolutionFactory.Create(id, ResolutionFailureFactory.CreateNotOpen()));

        foreach (var path in selected.Select(conflict => conflict.Path).Distinct())
            await ResolvePathAsync(accountId, selected.First(conflict => conflict.Path == path), [.. open.Where(conflict => conflict.Path == path)], strategy, tally, cancellationToken).ConfigureAwait(false);

        var summary = ResolutionSummaryFactory.Create(tally.Resolved, tally.Failed, tally.Skipped);
        var message = $"Conflict resolution finished: {summary.Resolved} resolved, {summary.Failed.Count} failed, {summary.Skipped} skipped.";
        LogMessage.Information(logger, Location, message);

        return Result.Success<ResolutionSummary, ErrorResponse>(summary);
    }

    private async Task ResolvePathAsync(AccountId accountId, SyncAttentionItem.Conflict chosen, IReadOnlyList<SyncAttentionItem.Conflict> members, ConflictStrategy strategy, Tally tally, CancellationToken cancellationToken)
    {
        var planned = planner.Plan(chosen, strategy);
        await planned.MatchAsync(
            plan => plan is ResolutionPlan.LeaveOpen ? Task.FromResult(tally.Skipped += members.Count) : ApplyAsync(accountId, chosen, members, plan, strategy, tally, cancellationToken),
            reason => Task.FromResult(Fail(members, ResolutionFailureFactory.CreateNotAllowed(reason), tally))).ConfigureAwait(false);
    }

    private static int Fail(IReadOnlyList<SyncAttentionItem.Conflict> members, ResolutionFailure reason, Tally tally)
    {
        tally.Failed.AddRange(members.Select(member => FailedResolutionFactory.Create(member.Id, reason)));

        return tally.Failed.Count;
    }

    private async Task<int> ApplyAsync(AccountId accountId, SyncAttentionItem.Conflict chosen, IReadOnlyList<SyncAttentionItem.Conflict> members, ResolutionPlan plan, ConflictStrategy strategy, Tally tally, CancellationToken cancellationToken)
    {
        var done = await Try.RunAsync(() => steps.RunAsync(accountId, chosen, plan, cancellationToken), cancellationToken)
            .MatchAsync(result => result, _ => Result.Failure<Unit, ResolutionFailure>(ResolutionFailureFactory.CreateUnexpected())).ConfigureAwait(false);

        return await done.MatchAsync(_ => CloseAllAsync(members, ResolutionOf(strategy), tally, cancellationToken), failure => Task.FromResult(Fail(members, failure, tally))).ConfigureAwait(false);
    }

    private async Task<int> CloseAllAsync(IReadOnlyList<SyncAttentionItem.Conflict> members, AttentionResolution resolution, Tally tally, CancellationToken cancellationToken)
    {
        foreach (var member in members)
        {
            var closed = await closer.CloseAsync(member.Id, resolution, cancellationToken).ConfigureAwait(false);
            _ = closed.Match(
                _ => tally.Resolved++,
                _ =>
                {
                    tally.Failed.Add(FailedResolutionFactory.Create(member.Id, ResolutionFailureFactory.CreateNotRecorded()));

                    return tally.Resolved;
                });
        }

        return tally.Resolved;
    }

    private sealed class Tally
    {
        internal int Resolved { get; set; }

        internal int Skipped { get; set; }

        internal List<FailedResolution> Failed { get; } = [];
    }
}
