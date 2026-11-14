using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="syncLock">Gives the account lease for the call.</param>
/// <param name="reader">Reads the open held sets.</param>
/// <param name="closer">Closes a settled set.</param>
/// <param name="transfers">Reads the root and the remote rows of the held paths.</param>
/// <param name="steps">Carries out the confirm or restore.</param>
/// <param name="logger">Reports the outcome with no paths.</param>
internal sealed class HeldDeletionDecider(IAccountSyncLock syncLock, ISyncAttentionReader reader, ISyncAttentionCloser closer, ResolutionTransfers transfers, HeldDeletionSteps steps, ILogger<HeldDeletionDecider> logger) : IHeldDeletionDecider
{
    private const string Location = nameof(HeldDeletionDecider);
    private const string NotInQueue = "The held deletions are not in the queue any more.";
    private const string NotSettled = "The held deletions were not settled and stay in the queue.";

    private delegate Task<Result<Unit, ResolutionFailure>> Operation(ResolutionContext context, SyncAttentionItem.HeldDeletions held, CancellationToken cancellationToken);

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> ConfirmAsync(AccountId accountId, HeldDeletionId id, CancellationToken cancellationToken) =>
        SettleAsync(accountId, id, steps.ConfirmAsync, AttentionResolutionFactory.CreateConfirmDeletions(), cancellationToken);

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RestoreAsync(AccountId accountId, HeldDeletionId id, CancellationToken cancellationToken) =>
        SettleAsync(accountId, id, steps.RestoreAsync, AttentionResolutionFactory.CreateRestoreItems(), cancellationToken);

    private static Result<Unit, ErrorResponse> Failed(string message) => Result.Failure<Unit, ErrorResponse>(new ErrorResponse(message));

    private async Task<Result<Unit, ErrorResponse>> SettleAsync(AccountId accountId, HeldDeletionId id, Operation operation, AttentionResolution resolution, CancellationToken cancellationToken)
    {
        var attempt = syncLock.TryAcquire(accountId);
        if (attempt is not SyncLockAttempt.Acquired acquired)
            return Result.Failure<Unit, ErrorResponse>(ResolutionRefusal.For(attempt));

        using var lease = acquired.Lease;

        return await reader.GetOpenAsync(accountId, cancellationToken)
            .BindAsync(open => open.OfType<SyncAttentionItem.HeldDeletions>().FirstOrNone(held => held.Id == id).Match(
                held => RunAsync(accountId, held, operation, resolution, cancellationToken),
                () => Task.FromResult(Failed(NotInQueue))))
            .ConfigureAwait(false);
    }

    private Task<Result<Unit, ErrorResponse>> RunAsync(AccountId accountId, SyncAttentionItem.HeldDeletions held, Operation operation, AttentionResolution resolution, CancellationToken cancellationToken) =>
        Try.RunAsync(() => transfers.LoadAsync(accountId, held.Paths, cancellationToken).BindAsync(context => operation(context, held, cancellationToken)), cancellationToken)
            .MatchAsync(done => done, _ => Result.Failure<Unit, ResolutionFailure>(ResolutionFailureFactory.CreateUnexpected()))
            .MatchAsync(_ => CloseAsync(held, resolution, cancellationToken), failure => Task.FromResult(NotDone(failure)));

    private Result<Unit, ErrorResponse> NotDone(ResolutionFailure failure)
    {
        var message = $"A held deletion set was not settled ({failure.GetType().Name}).";
        LogMessage.Warning(logger, Location, message);

        return Failed(NotSettled);
    }

    private Task<Result<Unit, ErrorResponse>> CloseAsync(SyncAttentionItem.HeldDeletions held, AttentionResolution resolution, CancellationToken cancellationToken) =>
        closer.CloseAsync(held.Id, resolution, cancellationToken).MatchAsync(
            _ =>
            {
                var message = $"A held deletion set of {held.Paths.Count} items was settled.";
                LogMessage.Information(logger, Location, message);

                return Result.Success<Unit, ErrorResponse>(Unit.Instance);
            },
            _ => Failed(NotSettled));
}
