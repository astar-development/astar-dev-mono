using System.Security.Cryptography;
using System.Text;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using SignInAttempt = (AStarDev.CloudSync.Accounts.ProviderAccountKey Key, AStarDev.CloudSync.Accounts.OneDrive.MicrosoftSignIn SignIn, System.Collections.Generic.IReadOnlyList<string> CachedAccountKeys);

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Adds a personal Microsoft account: connects the token cache, signs in, reads the drive and saves the account. A new MSAL account is removed again if the attempt fails after sign-in.</summary>
/// <param name="tokenCache">Connects the token cache before sign-in.</param>
/// <param name="msal">Signs in and manages MSAL accounts.</param>
/// <param name="driveLookup">Reads the drive after sign-in.</param>
/// <param name="accountStore">Saves the account.</param>
/// <param name="timeProvider">Times each step for the attempt log.</param>
/// <param name="logger">Logs every step of the attempt under one attempt id, without tokens, error messages or account details.</param>
public sealed class MicrosoftAccountAdder(TokenCacheInitialiser tokenCache, IMsalClient msal, IDriveLookup driveLookup, IAccountStore accountStore, TimeProvider timeProvider, ILogger<MicrosoftAccountAdder> logger) : IAccountAdder
{
    private const string ProviderName = "OneDrive";

    /// <inheritdoc />
    public async Task<AccountAddOutcome> AddAsync(CancellationToken cancellationToken)
    {
        var started = timeProvider.GetTimestamp();
        using var scope = logger.BeginScope(new Dictionary<string, object?> { ["AccountAddAttemptId"] = AccountAddAttemptIdFactory.Create(Guid.NewGuid()).Value, ["Provider"] = ProviderName });
        AccountAddLog.AttemptStarted(logger);

        var outcome = await Try.RunAsync(() => RunStepsAsync(cancellationToken), CancellationToken.None)
            .MatchAsync(finished => finished, Faulted)
            .ConfigureAwait(false);

        var outcomeName = Describe(outcome);
        var durationMs = ElapsedMilliseconds(started);
        AccountAddLog.AttemptCompleted(logger, outcomeName, durationMs);

        return outcome;
    }

    private static string Describe(AccountAddOutcome outcome) => outcome switch
    {
        AccountAddOutcome.Added => "Added",
        AccountAddOutcome.ReAuthenticated => "ReAuthenticated",
        AccountAddOutcome.Cancelled => "Cancelled",
        AccountAddOutcome.Failed failed => $"Failed:{failed.Failure.GetType().Name}",
        _ => outcome.GetType().Name,
    };

    private static string HashOf(ProviderAccountKey key) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key.Value.ToLowerInvariant())))[..16];

    private static AccountAddOutcome ToOutcome(Exception exception) => exception switch
    {
        MsalClientException { ErrorCode: MsalError.AuthenticationCanceledError } => AccountAddOutcomeFactory.CreateCancelled(),
        MsalClientException { ErrorCode: MsalError.LinuxXdgOpen } => AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateBrowserUnavailable()),
        MsalException { ErrorCode: MsalError.RequestTimeout } => AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork()),
        HttpRequestException or MsalException { InnerException: HttpRequestException } => AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork()),
        _ => AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateAuthRejected()),
    };

    private Task<AccountAddOutcome> RunStepsAsync(CancellationToken cancellationToken) =>
        PrepareTokenCacheAsync(cancellationToken)
            .BindAsync(_ => SignInAsync(cancellationToken))
            .BindAsync(attempt => RegisterAsync(attempt, cancellationToken))
            .MatchAsync(added => added, stopped => stopped);

    private AccountAddOutcome Faulted(Exception exception)
    {
        AccountAddLog.AttemptFaulted(logger, exception.GetType().Name);

        return AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateAuthRejected());
    }

    private double ElapsedMilliseconds(long started) => timeProvider.GetElapsedTime(started).TotalMilliseconds;

    private Task<Result<Unit, AccountAddOutcome>> PrepareTokenCacheAsync(CancellationToken cancellationToken) =>
        tokenCache.EnsureAsync(cancellationToken)
            .MatchAsync(Result.Success<Unit, AccountAddOutcome>, failure => Result.Failure<Unit, AccountAddOutcome>(AccountAddOutcomeFactory.CreateFailed(failure)))
            .OrCancelledAsync(() => AccountAddLog.StepTimedOut(logger, "token cache"), cancellationToken);

    private Task<Result<SignInAttempt, AccountAddOutcome>> SignInAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadCacheAndSignInAsync(cancellationToken), cancellationToken)
            .MatchAsync(ToAttempt, SignInFailed)
            .OrCancelledAsync(() => AccountAddLog.StepTimedOut(logger, "sign-in"), cancellationToken);

    private async Task<(IReadOnlyList<string> CachedAccountKeys, MicrosoftSignIn SignIn, double DurationMs)> ReadCacheAndSignInAsync(CancellationToken cancellationToken)
    {
        var cachedAccountKeys = await msal.GetAccountKeysAsync().ConfigureAwait(false);
        AccountAddLog.BrowserLaunched(logger);
        var started = timeProvider.GetTimestamp();
        var signIn = await msal.SignInAsync(cancellationToken).ConfigureAwait(false);

        return (cachedAccountKeys, signIn, ElapsedMilliseconds(started));
    }

    private Result<SignInAttempt, AccountAddOutcome> ToAttempt((IReadOnlyList<string> CachedAccountKeys, MicrosoftSignIn SignIn, double DurationMs) signedIn) =>
        ProviderAccountKeyFactory.Create(signedIn.SignIn.AccountKey).Match(
            key => SignedIn((key, signedIn.SignIn, signedIn.CachedAccountKeys), signedIn.DurationMs),
            _ => KeyMissing());

    private Result<SignInAttempt, AccountAddOutcome> SignedIn(SignInAttempt attempt, double durationMs)
    {
        var scopes = string.Join(' ', attempt.SignIn.GrantedScopes);
        var accountKeyHash = HashOf(attempt.Key);
        AccountAddLog.AuthSucceeded(logger, durationMs, scopes, accountKeyHash);

        return Result.Success<SignInAttempt, AccountAddOutcome>(attempt);
    }

    private Result<SignInAttempt, AccountAddOutcome> KeyMissing()
    {
        AccountAddLog.AccountKeyMissing(logger);

        return Result.Failure<SignInAttempt, AccountAddOutcome>(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateAuthRejected()));
    }

    private Result<SignInAttempt, AccountAddOutcome> SignInFailed(Exception exception)
    {
        var outcome = ToOutcome(exception);

        if (outcome is AccountAddOutcome.Cancelled)
            AccountAddLog.AuthCancelled(logger);
        else
            LogAuthFailed(exception);

        return Result.Failure<SignInAttempt, AccountAddOutcome>(outcome);
    }

    private void LogAuthFailed(Exception exception)
    {
        var msalException = exception as MsalException;
        AccountAddLog.AuthFailed(logger, exception.GetType().Name, msalException?.ErrorCode ?? "none", msalException?.CorrelationId ?? "none");
    }

    private Task<Result<AccountAddOutcome, AccountAddOutcome>> RegisterAsync(SignInAttempt attempt, CancellationToken cancellationToken) =>
        ReadDriveAsync(attempt, cancellationToken)
            .BindAsync(drive => SaveAsync(attempt, drive, cancellationToken))
            .OrCancelledAsync(() => AccountAddLog.StepTimedOut(logger, "drive lookup and save"), cancellationToken)
            .OrElseAsync(outcome => RollBackNewAccountAsync(attempt, outcome));

    private async Task<Result<MicrosoftDrive, AccountAddOutcome>> ReadDriveAsync(SignInAttempt attempt, CancellationToken cancellationToken)
    {
        var started = timeProvider.GetTimestamp();

        return await driveLookup.GetDriveAsync(attempt.SignIn.AccessToken, cancellationToken)
            .MatchAsync(drive => DriveRead(drive, started), failure => Result.Failure<MicrosoftDrive, AccountAddOutcome>(AccountAddOutcomeFactory.CreateFailed(failure)))
            .ConfigureAwait(false);
    }

    private Result<MicrosoftDrive, AccountAddOutcome> DriveRead(MicrosoftDrive drive, long started)
    {
        var durationMs = ElapsedMilliseconds(started);
        AccountAddLog.DriveFetched(logger, durationMs);

        return Result.Success<MicrosoftDrive, AccountAddOutcome>(drive);
    }

    private async Task<Result<AccountAddOutcome, AccountAddOutcome>> SaveAsync(SignInAttempt attempt, MicrosoftDrive drive, CancellationToken cancellationToken)
    {
        var registration = AccountRegistrationFactory.Create(attempt.Key, CloudProviderFactory.CreateOneDrive(), attempt.SignIn.DisplayName, attempt.SignIn.Email, drive.DriveId, drive.Storage);
        var started = timeProvider.GetTimestamp();
        var outcome = await accountStore.AddAsync(registration, cancellationToken).ConfigureAwait(false);

        if (outcome is AccountAddOutcome.ReAuthenticated)
            AccountAddLog.DuplicateDetected(logger);

        if (outcome is not (AccountAddOutcome.Added or AccountAddOutcome.ReAuthenticated))
            return Result.Failure<AccountAddOutcome, AccountAddOutcome>(outcome);

        var durationMs = ElapsedMilliseconds(started);
        AccountAddLog.Persisted(logger, durationMs);

        return Result.Success<AccountAddOutcome, AccountAddOutcome>(outcome);
    }

    private Task<Result<AccountAddOutcome, AccountAddOutcome>> RollBackNewAccountAsync(SignInAttempt attempt, AccountAddOutcome outcome) =>
        attempt.CachedAccountKeys.Contains(attempt.Key.Value, StringComparer.OrdinalIgnoreCase)
            ? Task.FromResult(Stopped(AccountAddLog.RollbackSkipped, outcome))
            : Try.RunAsync(() => UnitTask.RunAsync(() => msal.RemoveAccountAsync(attempt.Key)), CancellationToken.None)
                .MatchAsync(_ => Stopped(AccountAddLog.RollbackPerformed, outcome), exception => Stopped(log => AccountAddLog.RollbackFailed(log, exception.GetType().Name), outcome));

    private Result<AccountAddOutcome, AccountAddOutcome> Stopped(Action<ILogger> log, AccountAddOutcome outcome)
    {
        log(logger);

        return Result.Failure<AccountAddOutcome, AccountAddOutcome>(outcome);
    }
}
