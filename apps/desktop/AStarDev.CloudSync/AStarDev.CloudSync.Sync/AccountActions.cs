using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>The <see cref="IAccountActions"/> the UI calls.</summary>
/// <param name="remover">Removes accounts.</param>
/// <param name="leases">Holds the account while the sign-in window is open.</param>
/// <param name="reauthenticators">Finds the account's provider sign-in.</param>
/// <param name="states">Records a successful sign-in.</param>
/// <param name="syncRoots">Saves a changed sync root.</param>
internal sealed class AccountActions(IAccountRemover remover, AccountLeaseWaiter leases, IProviderCapability<IAccountReauthenticator> reauthenticators, IAccountSignInStates states, ISyncRootWriter syncRoots) : IAccountActions
{
    /// <inheritdoc />
    public Task<Result<Unit, AccountRemovalFailure>> RemoveAsync(AccountId accountId, CancellationToken cancellationToken) => remover.RemoveAsync(accountId, cancellationToken);

    /// <inheritdoc />
    public async Task<Result<Unit, ReauthFailure>> ReauthenticateAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var lease = await leases.AcquireAsync(accountId, cancellationToken).ConfigureAwait(false);

        return await lease.MatchAsync(held => ReauthenticateHoldingAsync(held, accountId, cancellationToken), () => Result.Failure<Unit, ReauthFailure>(ReauthFailureFactory.CreateSyncStillRunning())).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<SyncRoot, SyncRootRejection>> ChangeSyncRootAsync(AccountId accountId, IDirectoryInfo candidate, CancellationToken cancellationToken)
    {
        var lease = await leases.AcquireAsync(accountId, cancellationToken).ConfigureAwait(false);

        return await lease.MatchAsync(held => ChangeSyncRootHoldingAsync(held, accountId, candidate, cancellationToken), () => Result.Failure<SyncRoot, SyncRootRejection>(SyncRootRejectionFactory.CreateSyncStillRunning())).ConfigureAwait(false);
    }

    private static Result<bool, SyncRootRejection> EnsureFolder(IDirectoryInfo? candidate)
    {
        if (candidate is null || candidate.Exists)
            return Result.Success<bool, SyncRootRejection>(false);

        if (candidate.Parent is not { Exists: true })
            return Result.Failure<bool, SyncRootRejection>(SyncRootRejectionFactory.CreateUnavailable());

        try
        {
            candidate.Create();

            return Result.Success<bool, SyncRootRejection>(true);
        }
        catch (UnauthorizedAccessException)
        {
            return Result.Failure<bool, SyncRootRejection>(SyncRootRejectionFactory.CreateNotWritable());
        }
        catch (IOException)
        {
            return Result.Failure<bool, SyncRootRejection>(SyncRootRejectionFactory.CreateUnavailable());
        }
    }

    private static Result<SyncRoot, SyncRootRejection> RemoveCreatedFolder(IDirectoryInfo candidate, bool created, SyncRootRejection rejection)
    {
        if (created)
            DeleteIfEmpty(candidate);

        return Result.Failure<SyncRoot, SyncRootRejection>(rejection);
    }

    private static void DeleteIfEmpty(IDirectoryInfo folder)
    {
        try
        {
            folder.Delete();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }
    }

    private async Task<Result<SyncRoot, SyncRootRejection>> ChangeSyncRootHoldingAsync(SyncLease lease, AccountId accountId, IDirectoryInfo candidate, CancellationToken cancellationToken)
    {
        using (lease)
        {
            return await EnsureFolder(candidate)
                .Match(created => SaveSyncRootAsync(accountId, candidate, created, cancellationToken), rejection => Task.FromResult(Result.Failure<SyncRoot, SyncRootRejection>(rejection)))
                .ConfigureAwait(false);
        }
    }

    private async Task<Result<SyncRoot, SyncRootRejection>> SaveSyncRootAsync(AccountId accountId, IDirectoryInfo candidate, bool created, CancellationToken cancellationToken)
    {
        var saved = await syncRoots.SetAsync(accountId, candidate, cancellationToken).ConfigureAwait(false);

        return saved.Match(Result.Success<SyncRoot, SyncRootRejection>, rejection => RemoveCreatedFolder(candidate, created, rejection));
    }

    private async Task<Result<Unit, ReauthFailure>> ReauthenticateHoldingAsync(SyncLease lease, AccountId accountId, CancellationToken cancellationToken)
    {
        using (lease)
        {
            return await reauthenticators.ForAccountAsync(accountId, cancellationToken)
                .MatchAsync(reauthenticator => reauthenticator.ReauthenticateAsync(accountId, cancellationToken), error => Task.FromResult(Result.Failure<Unit, ReauthFailure>(ReauthFailureFactory.CreateProvider(error))))
                .MatchAsync(signedIn => SignedIn(accountId, signedIn), Result.Failure<Unit, ReauthFailure>)
                .ConfigureAwait(false);
        }
    }

    private Result<Unit, ReauthFailure> SignedIn(AccountId accountId, Unit signedIn)
    {
        states.Record(accountId, AccountSignInStateFactory.CreateSignedIn());

        return Result.Success<Unit, ReauthFailure>(signedIn);
    }
}
