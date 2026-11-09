using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Signs a stored OneDrive account in again with the stored account pre-selected (#1393 decision 3). A different account is rejected; its tokens are removed unless another stored account owns them (decision B).</summary>
/// <param name="tokenCache">Connects the token cache, asking for plaintext consent if needed (the user started this).</param>
/// <param name="accountStore">Finds the stored account and checks other stored keys.</param>
/// <param name="msal">Runs the interactive sign-in.</param>
/// <param name="logger">Reports failures by type only.</param>
internal sealed class MicrosoftReauthenticator(TokenCacheInitialiser tokenCache, IAccountStore accountStore, IMsalClient msal, ILogger<MicrosoftReauthenticator> logger) : IAccountReauthenticator
{
    private const string Location = nameof(MicrosoftReauthenticator);

    /// <inheritdoc />
    public Task<Result<Unit, ReauthFailure>> ReauthenticateAsync(AccountId accountId, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{tokenCache}{accountStore}{msal}{logger}{accountId}{cancellationToken}");

    private static Result<Unit, ReauthFailure> Failure(ReauthFailure failure) => Result.Failure<Unit, ReauthFailure>(failure);

    private async Task<Result<Unit, ReauthFailure>> SignInAgainAsync(StoredAccount account, IReadOnlyList<StoredAccount> storedAccounts, CancellationToken cancellationToken)
    {
        var cacheConnected = await tokenCache.EnsureAsync(ConsentPromptFactory.CreateAllowed(), cancellationToken).MatchAsync(_ => true, _ => false).ConfigureAwait(false);
        if (!cacheConnected)
            return Failure(ReauthFailureFactory.CreateProvider(ProviderErrorFactory.CreateUnexpected()));

        return await Try.RunAsync(() => msal.ReauthenticateAsync(account.Registration.Key, account.Registration.Email, cancellationToken), cancellationToken)
            .MatchAsync(signIn => CheckSameAccountAsync(account, signIn, storedAccounts), exception => Task.FromResult(SignInFailed(exception)))
            .Unwrap()
            .ConfigureAwait(false);
    }

    private async Task<Result<Unit, ReauthFailure>> CheckSameAccountAsync(StoredAccount account, MicrosoftSignIn signIn, IReadOnlyList<StoredAccount> storedAccounts)
    {
        if (string.Equals(signIn.AccountKey, account.Registration.Key.Value, StringComparison.OrdinalIgnoreCase))
            return Result.Success<Unit, ReauthFailure>(Unit.Instance);

        var ownedByAnotherStoredAccount = storedAccounts.Any(stored => string.Equals(stored.Registration.Key.Value, signIn.AccountKey, StringComparison.OrdinalIgnoreCase));
        if (!ownedByAnotherStoredAccount)
            _ = await ProviderAccountKeyFactory.Create(signIn.AccountKey).MatchAsync(RemoveStrayTokensAsync, _ => Task.FromResult(Unit.Instance)).ConfigureAwait(false);

        LogMessage.Warning(logger, Location, "The user signed in as a different account; nothing changed.");

        return Failure(ReauthFailureFactory.CreateDifferentAccount());
    }

    private Task<Unit> RemoveStrayTokensAsync(ProviderAccountKey key) =>
        Try.RunAsync(() => UnitTask.RunAsync(() => msal.RemoveAccountAsync(key)), CancellationToken.None).MatchAsync(removed => removed, StrayTokensNotRemoved);

    private Unit StrayTokensNotRemoved(Exception exception)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Unit.Instance;
    }

    private Result<Unit, ReauthFailure> SignInFailed(Exception exception)
    {
        if (exception is MsalClientException { ErrorCode: MsalError.AuthenticationCanceledError })
            return Failure(ReauthFailureFactory.CreateCancelled());

        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Failure(ReauthFailureFactory.CreateProvider(exception is MsalServiceException or HttpRequestException ? ProviderErrorFactory.CreateTransient() : ProviderErrorFactory.CreateUnexpected()));
    }
}
