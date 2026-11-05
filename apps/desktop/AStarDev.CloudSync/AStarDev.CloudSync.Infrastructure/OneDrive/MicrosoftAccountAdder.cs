using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using SignInAttempt = (AStarDev.CloudSync.Core.Accounts.ProviderAccountKey Key, AStarDev.CloudSync.Infrastructure.OneDrive.MicrosoftSignIn SignIn, System.Collections.Generic.IReadOnlyList<string> CachedAccountKeys);

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Adds a personal Microsoft account: connects the token cache, signs in, reads the drive and saves the account. A new MSAL account is removed again if the attempt fails after sign-in.</summary>
/// <param name="tokenCache">Connects the token cache before sign-in.</param>
/// <param name="msal">Signs in and manages MSAL accounts.</param>
/// <param name="driveLookup">Reads the drive after sign-in.</param>
/// <param name="accountStore">Saves the account.</param>
/// <param name="timeProvider">Times each step for the attempt log.</param>
/// <param name="logger">Logs every step of the attempt under one attempt id, without tokens, error messages or account details.</param>
public sealed class MicrosoftAccountAdder(TokenCacheInitialiser tokenCache, IMsalClient msal, IDriveLookup driveLookup, IAccountStore accountStore, TimeProvider timeProvider, ILogger<MicrosoftAccountAdder> logger) : IAccountAdder
{
    /// <inheritdoc />
    public Task<AccountAddOutcome> AddAsync(CancellationToken cancellationToken)
    {
        _ = timeProvider;

        return PrepareTokenCacheAsync(cancellationToken)
            .BindAsync(_ => SignInAsync(cancellationToken))
            .BindAsync(attempt => RegisterAsync(attempt, cancellationToken))
            .MatchAsync(outcome => outcome, outcome => outcome);
    }

    private Task<Result<Unit, AccountAddOutcome>> PrepareTokenCacheAsync(CancellationToken cancellationToken) =>
        tokenCache.EnsureAsync(cancellationToken)
            .MatchAsync(Result.Success<Unit, AccountAddOutcome>, failure => Result.Failure<Unit, AccountAddOutcome>(AccountAddOutcomeFactory.CreateFailed(failure)))
            .OrCancelledAsync(cancellationToken);

    private Task<Result<SignInAttempt, AccountAddOutcome>> SignInAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadCacheAndSignInAsync(cancellationToken), cancellationToken)
            .MatchAsync(ToAttempt, SignInFailed)
            .OrCancelledAsync(cancellationToken);

    private async Task<(IReadOnlyList<string> CachedAccountKeys, MicrosoftSignIn SignIn)> ReadCacheAndSignInAsync(CancellationToken cancellationToken)
    {
        var cachedAccountKeys = await msal.GetAccountKeysAsync().ConfigureAwait(false);

        return (cachedAccountKeys, await msal.SignInAsync(cancellationToken).ConfigureAwait(false));
    }

    private Result<SignInAttempt, AccountAddOutcome> ToAttempt((IReadOnlyList<string> CachedAccountKeys, MicrosoftSignIn SignIn) signedIn) =>
        ProviderAccountKeyFactory.Create(signedIn.SignIn.AccountKey).Match(
            key => Result.Success<SignInAttempt, AccountAddOutcome>((key, signedIn.SignIn, signedIn.CachedAccountKeys)),
            _ => Stopped<SignInAttempt>("Sign-in returned no account key.", AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateAuthRejected())));

    private Result<SignInAttempt, AccountAddOutcome> SignInFailed(Exception exception) =>
        Stopped<SignInAttempt>($"Sign-in did not complete ({exception.GetType().Name}, {(exception as MsalException)?.ErrorCode ?? "no MSAL error code"}).", ToOutcome(exception));

    private static AccountAddOutcome ToOutcome(Exception exception) => exception switch
    {
        MsalClientException { ErrorCode: MsalError.AuthenticationCanceledError } => AccountAddOutcomeFactory.CreateCancelled(),
        MsalClientException { ErrorCode: MsalError.LinuxXdgOpen } => AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateBrowserUnavailable()),
        MsalException { ErrorCode: MsalError.RequestTimeout } => AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork()),
        HttpRequestException or MsalException { InnerException: HttpRequestException } => AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork()),
        _ => AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateAuthRejected()),
    };

    private Task<Result<AccountAddOutcome, AccountAddOutcome>> RegisterAsync(SignInAttempt attempt, CancellationToken cancellationToken) =>
        driveLookup.GetDriveAsync(attempt.SignIn.AccessToken, cancellationToken)
            .MatchAsync(Result.Success<MicrosoftDrive, AccountAddOutcome>, failure => Result.Failure<MicrosoftDrive, AccountAddOutcome>(AccountAddOutcomeFactory.CreateFailed(failure)))
            .BindAsync(drive => SaveAsync(attempt, drive, cancellationToken))
            .OrCancelledAsync(cancellationToken)
            .OrElseAsync(outcome => RollBackNewAccountAsync(attempt, outcome));

    private async Task<Result<AccountAddOutcome, AccountAddOutcome>> SaveAsync(SignInAttempt attempt, MicrosoftDrive drive, CancellationToken cancellationToken)
    {
        var registration = AccountRegistrationFactory.Create(attempt.Key, CloudProviderFactory.CreateOneDrive(), attempt.SignIn.DisplayName, attempt.SignIn.Email, drive.DriveId, drive.Storage);
        var outcome = await accountStore.AddAsync(registration, cancellationToken).ConfigureAwait(false);

        return outcome is AccountAddOutcome.Added or AccountAddOutcome.ReAuthenticated
            ? Result.Success<AccountAddOutcome, AccountAddOutcome>(outcome)
            : Result.Failure<AccountAddOutcome, AccountAddOutcome>(outcome);
    }

    private Task<Result<AccountAddOutcome, AccountAddOutcome>> RollBackNewAccountAsync(SignInAttempt attempt, AccountAddOutcome outcome) =>
        attempt.CachedAccountKeys.Contains(attempt.Key.Value, StringComparer.OrdinalIgnoreCase)
            ? Task.FromResult(Stopped<AccountAddOutcome>("The add did not complete; the account was already signed in, so it was kept in the token cache.", outcome))
            : Try.RunAsync(() => UnitTask.RunAsync(() => msal.RemoveAccountAsync(attempt.Key)))
                .MatchAsync(
                    _ => Stopped<AccountAddOutcome>("The add did not complete; the new account was removed from the token cache.", outcome),
                    exception => Stopped<AccountAddOutcome>($"The add did not complete, and the new account could not be removed from the token cache ({exception.GetType().Name}).", outcome));

    private Result<T, AccountAddOutcome> Stopped<T>(string message, AccountAddOutcome outcome)
    {
        LogMessage.Warning(logger, nameof(MicrosoftAccountAdder), message);

        return Result.Failure<T, AccountAddOutcome>(outcome);
    }
}
