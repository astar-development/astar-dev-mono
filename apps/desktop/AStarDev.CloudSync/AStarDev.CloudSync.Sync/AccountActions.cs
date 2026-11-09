using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>The <see cref="IAccountActions"/> the UI calls.</summary>
/// <param name="remover">Removes accounts.</param>
/// <param name="leases">Holds the account while the sign-in window is open.</param>
/// <param name="reauthenticators">Finds the account's provider sign-in.</param>
/// <param name="states">Records a successful sign-in.</param>
internal sealed class AccountActions(IAccountRemover remover, AccountLeaseWaiter leases, IProviderCapability<IAccountReauthenticator> reauthenticators, IAccountSignInStates states) : IAccountActions
{
    /// <inheritdoc />
    public Task<Result<Unit, AccountRemovalFailure>> RemoveAsync(AccountId accountId, CancellationToken cancellationToken) => remover.RemoveAsync(accountId, cancellationToken);

    /// <inheritdoc />
    public async Task<Result<Unit, ReauthFailure>> ReauthenticateAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException($"{leases}{reauthenticators}{states}{accountId}{cancellationToken}");
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
