using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>The provider-neutral <see cref="IAccountRemover"/>.</summary>
/// <param name="accountStore">Finds and deletes the account.</param>
/// <param name="leases">Waits for the account's running work.</param>
/// <param name="signOuts">Finds the provider's sign-out.</param>
/// <param name="participants">Every module that stores data for an account.</param>
/// <param name="states">Forgets the account's sign-in state.</param>
/// <param name="logger">Reports why a removal stopped, with the hashed account id only.</param>
internal sealed class AccountRemover(IAccountStore accountStore, AccountLeaseWaiter leases, IProviderCapability<IProviderSignOut> signOuts, IEnumerable<IAccountRemovalParticipant> participants, IAccountSignInStates states, ILogger<AccountRemover> logger) : IAccountRemover
{
    private const string Location = nameof(AccountRemover);

    /// <inheritdoc />
    public async Task<Result<Unit, AccountRemovalFailure>> RemoveAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        using var scope = SyncLogScope.Begin(logger, accountId);
        var outcome = await FindAsync(accountId, cancellationToken)
            .BindAsync(_ => RemoveWithLeaseAsync(accountId, cancellationToken))
            .ConfigureAwait(false);

        return outcome.Match(Result.Success<Unit, AccountRemovalFailure>, Logged);
    }

    private Task<Result<StoredAccount, AccountRemovalFailure>> FindAsync(AccountId accountId, CancellationToken cancellationToken) =>
        accountStore.GetAllAsync(cancellationToken)
            .MatchAsync(
                accounts => accounts.FirstOrNone(account => account.Id == accountId).Match(Result.Success<StoredAccount, AccountRemovalFailure>, () => Result.Failure<StoredAccount, AccountRemovalFailure>(AccountRemovalFailureFactory.CreateNotFound())),
                error => Result.Failure<StoredAccount, AccountRemovalFailure>(AccountRemovalFailureFactory.CreateStore(error)));

    private async Task<Result<Unit, AccountRemovalFailure>> RemoveWithLeaseAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var lease = await leases.AcquireAsync(accountId, cancellationToken).ConfigureAwait(false);

        return await lease.MatchAsync(held => RemoveHoldingAsync(held, accountId, cancellationToken), () => Result.Failure<Unit, AccountRemovalFailure>(AccountRemovalFailureFactory.CreateSyncStillRunning())).ConfigureAwait(false);
    }

    private async Task<Result<Unit, AccountRemovalFailure>> RemoveHoldingAsync(SyncLease lease, AccountId accountId, CancellationToken cancellationToken)
    {
        using (lease)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await FindAsync(accountId, cancellationToken)
                .BindAsync(_ => SignOutAsync(accountId))
                .BindAsync(_ => RemoveParticipantDataAsync(accountId))
                .BindAsync(_ => DeleteAccountAsync(accountId))
                .ConfigureAwait(false);
        }
    }

    private Task<Result<Unit, AccountRemovalFailure>> SignOutAsync(AccountId accountId) =>
        signOuts.ForAccountAsync(accountId, CancellationToken.None)
            .MatchAsync(signOut => signOut.SignOutAsync(accountId, CancellationToken.None), error => Task.FromResult(Result.Failure<Unit, ProviderError>(error)))
            .MatchAsync(Result.Success<Unit, AccountRemovalFailure>, error => Result.Failure<Unit, AccountRemovalFailure>(AccountRemovalFailureFactory.CreateProvider(error)));

    private Task<Result<Unit, AccountRemovalFailure>> RemoveParticipantDataAsync(AccountId accountId) =>
        participants.Aggregate(
            Task.FromResult(Result.Success<Unit, AccountRemovalFailure>(Unit.Instance)),
            (previous, participant) => previous.BindAsync(_ => participant.RemoveAccountDataAsync(accountId, CancellationToken.None).MatchAsync(Result.Success<Unit, AccountRemovalFailure>, error => Result.Failure<Unit, AccountRemovalFailure>(AccountRemovalFailureFactory.CreateStore(error)))));

    private Task<Result<Unit, AccountRemovalFailure>> DeleteAccountAsync(AccountId accountId) =>
        accountStore.RemoveAsync(accountId, CancellationToken.None)
            .MatchAsync(
                removed => Forgotten(accountId, removed),
                error => Result.Failure<Unit, AccountRemovalFailure>(AccountRemovalFailureFactory.CreateStore(error)));

    private Result<Unit, AccountRemovalFailure> Forgotten(AccountId accountId, Unit removed)
    {
        states.Forget(accountId);
        LogMessage.Information(logger, Location, "The account was removed.");

        return Result.Success<Unit, AccountRemovalFailure>(removed);
    }

    private Result<Unit, AccountRemovalFailure> Logged(AccountRemovalFailure failure)
    {
        LogMessage.Warning(logger, Location, $"The account was not removed ({failure.GetType().Name}).");

        return Result.Failure<Unit, AccountRemovalFailure>(failure);
    }
}
