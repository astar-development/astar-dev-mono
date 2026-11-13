using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="providers">Finds the provider capabilities.</param>
/// <param name="inputs">Reads the root, rules, baseline and remote index.</param>
/// <param name="planner">Plans the steps.</param>
/// <param name="executor">Runs the steps.</param>
/// <param name="logger">Reports the counts of a pass, with the hashed account id only.</param>
internal sealed class UploadPass(UploadPassProviders providers, UploadPassInputs inputs, IUploadPlanner planner, UploadItemExecutor executor, ILogger<UploadPass> logger) : IUploadPass
{
    private const string Location = nameof(UploadPass);

    /// <inheritdoc />
    public async Task<Result<UploadPassSummary, UploadPassFailure>> RunAsync(AccountId accountId, LocalChangeSet changes, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);
        var outcome = await providers.ResolveAsync(accountId, cancellationToken)
            .BindAsync(resolved => inputs.LoadAsync(accountId, changes, cancellationToken)
                .BindAsync(input => executor.ExecuteAsync(accountId, planner.Plan(input.Changes, input.Baseline, input.Remote, input.Rules, resolved.Names), input, resolved, cancellationToken)))
            .ConfigureAwait(false);

        return outcome.Match(Completed, Stopped);
    }

    private Result<UploadPassSummary, UploadPassFailure> Completed(IReadOnlyList<UploadItemOutcome> outcomes)
    {
        var summary = UploadPassSummaryFactory.Create(outcomes);
        var message = $"The upload pass finished: {summary.Uploaded} uploaded ({summary.BytesUploaded} bytes), {summary.FoldersCreated} folders, {summary.Removed} removed, {summary.SentToConflicts} to conflicts, {summary.Deferred} deferred, {summary.Failed} failed, {summary.Rejected.Count} rejected.";
        LogMessage.Information(logger, Location, message);

        return Result.Success<UploadPassSummary, UploadPassFailure>(summary);
    }

    private Result<UploadPassSummary, UploadPassFailure> Stopped(UploadPassFailure failure)
    {
        var message = $"The upload pass stopped ({failure.GetType().Name}).";
        LogMessage.Warning(logger, Location, message);

        return Result.Failure<UploadPassSummary, UploadPassFailure>(failure);
    }
}
