using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="reader">Reads every account's root.</param>
/// <param name="provider">Proposes the default root.</param>
/// <param name="writer">Saves the root.</param>
public sealed class DefaultSyncRootAssigner(ISyncRootReader reader, IDefaultSyncRootProvider provider, ISyncRootWriter writer) : IDefaultSyncRootAssigner
{
    private const int MaxAttempts = 5;

    /// <inheritdoc />
    public Task<Result<SyncRoot, SyncRootRejection>> AssignAsync(AccountId accountId, AccountRegistration registration, CancellationToken cancellationToken) =>
        AttemptAsync(accountId, registration, MaxAttempts, cancellationToken);

    private static bool IsClash(SyncRootRejection rejection) => rejection is SyncRootRejection.SameAs or SyncRootRejection.InsideOf or SyncRootRejection.Contains;

    private Task<Result<SyncRoot, SyncRootRejection>> AttemptAsync(AccountId accountId, AccountRegistration registration, int remaining, CancellationToken cancellationToken) =>
        reader.GetAllAsync(cancellationToken)
            .MatchAsync(
                existing => provider.CreateFor(registration, existing).Match(root => SaveAsync(accountId, registration, root, remaining, cancellationToken), rejection => Task.FromResult(Result.Failure<SyncRoot, SyncRootRejection>(rejection))),
                error => Task.FromResult(Result.Failure<SyncRoot, SyncRootRejection>(SyncRootRejectionFactory.CreateStore(error))));

    private Task<Result<SyncRoot, SyncRootRejection>> SaveAsync(AccountId accountId, AccountRegistration registration, SyncRoot root, int remaining, CancellationToken cancellationToken) =>
        writer.SetAsync(accountId, root.Directory, cancellationToken)
            .MatchAsync(
                saved => Task.FromResult(Result.Success<SyncRoot, SyncRootRejection>(saved)),
                rejection => IsClash(rejection) && remaining > 1 ? AttemptAsync(accountId, registration, remaining - 1, cancellationToken) : Task.FromResult(Result.Failure<SyncRoot, SyncRootRejection>(rejection)));
}
