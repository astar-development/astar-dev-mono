using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

internal static partial class AccountAddLog
{
    [LoggerMessage(EventId = 5100, Level = LogLevel.Information, Message = "Add-account attempt started.")]
    public static partial void AttemptStarted(ILogger logger);

    [LoggerMessage(EventId = 5101, Level = LogLevel.Information, Message = "Opening the system browser for Microsoft sign-in.")]
    public static partial void BrowserLaunched(ILogger logger);

    [LoggerMessage(EventId = 5102, Level = LogLevel.Information, Message = "Sign-in succeeded in {DurationMs} ms with scopes {Scopes} for account {AccountKeyHash}.")]
    public static partial void AuthSucceeded(ILogger logger, double durationMs, string scopes, string accountKeyHash);

    [LoggerMessage(EventId = 5103, Level = LogLevel.Information, Message = "The user cancelled sign-in.")]
    public static partial void AuthCancelled(ILogger logger);

    [LoggerMessage(EventId = 5104, Level = LogLevel.Warning, Message = "Sign-in failed: {ExceptionType}, MSAL error {ErrorCode}, correlation id {CorrelationId}.")]
    public static partial void AuthFailed(ILogger logger, string exceptionType, string errorCode, string correlationId);

    [LoggerMessage(EventId = 5105, Level = LogLevel.Warning, Message = "Sign-in returned no account key.")]
    public static partial void AccountKeyMissing(ILogger logger);

    [LoggerMessage(EventId = 5106, Level = LogLevel.Information, Message = "Drive read in {DurationMs} ms.")]
    public static partial void DriveFetched(ILogger logger, double durationMs);

    [LoggerMessage(EventId = 5107, Level = LogLevel.Information, Message = "The account was already added; it was signed in again and its settings were kept.")]
    public static partial void DuplicateDetected(ILogger logger);

    [LoggerMessage(EventId = 5108, Level = LogLevel.Information, Message = "Account saved in {DurationMs} ms.")]
    public static partial void Persisted(ILogger logger, double durationMs);

    [LoggerMessage(EventId = 5109, Level = LogLevel.Information, Message = "The new account was removed from the token cache because the attempt did not complete.")]
    public static partial void RollbackPerformed(ILogger logger);

    [LoggerMessage(EventId = 5110, Level = LogLevel.Information, Message = "The account was already signed in before the attempt, so it was kept in the token cache.")]
    public static partial void RollbackSkipped(ILogger logger);

    [LoggerMessage(EventId = 5111, Level = LogLevel.Warning, Message = "The new account could not be removed from the token cache ({ExceptionType}).")]
    public static partial void RollbackFailed(ILogger logger, string exceptionType);

    [LoggerMessage(EventId = 5112, Level = LogLevel.Information, Message = "Add-account attempt completed: {Outcome} in {DurationMs} ms.")]
    public static partial void AttemptCompleted(ILogger logger, string outcome, double durationMs);

    [LoggerMessage(EventId = 5113, Level = LogLevel.Warning, Message = "The {Step} step timed out.")]
    public static partial void StepTimedOut(ILogger logger, string step);

    [LoggerMessage(EventId = 5114, Level = LogLevel.Error, Message = "The add-account attempt failed unexpectedly ({ExceptionType}).")]
    public static partial void AttemptFaulted(ILogger logger, string exceptionType);

    [LoggerMessage(EventId = 5120, Level = LogLevel.Information, Message = "Token cache attached: {Mode}.")]
    public static partial void TokenCacheInitialised(ILogger logger, string mode);

    [LoggerMessage(EventId = 5121, Level = LogLevel.Information, Message = "Plaintext token file consent: {Answer}.")]
    public static partial void TokenCacheConsent(ILogger logger, string answer);

    [LoggerMessage(EventId = 5122, Level = LogLevel.Warning, Message = "The secure token store is unavailable ({ExceptionType}).")]
    public static partial void SecureStoreUnavailable(ILogger logger, string exceptionType);

    [LoggerMessage(EventId = 5123, Level = LogLevel.Warning, Message = "The token cache is unavailable: {Reason} ({ExceptionType}).")]
    public static partial void TokenCacheUnavailable(ILogger logger, string reason, string exceptionType);

    [LoggerMessage(EventId = 5124, Level = LogLevel.Information, Message = "The tokens in the old plaintext token file were moved to the secure store and the file was deleted.")]
    public static partial void PlaintextTokensMoved(ILogger logger);

    [LoggerMessage(EventId = 5125, Level = LogLevel.Warning, Message = "The old plaintext token file could not be moved to the secure store ({ExceptionType}); it is kept and moved on the next attempt.")]
    public static partial void PlaintextTokenMoveFailed(ILogger logger, string exceptionType);

    [LoggerMessage(EventId = 5126, Level = LogLevel.Information, Message = "The plaintext token file consent was withdrawn; the file was deleted and the accounts must sign in again.")]
    public static partial void PlaintextConsentWithdrawn(ILogger logger);

    [LoggerMessage(EventId = 5127, Level = LogLevel.Warning, Message = "The plaintext token file consent could not be withdrawn: {Reason} ({ExceptionType}).")]
    public static partial void PlaintextConsentWithdrawFailed(ILogger logger, string reason, string exceptionType);

    [LoggerMessage(EventId = 5130, Level = LogLevel.Warning, Message = "The drive could not be read: {Reason}, HTTP {StatusCode} ({ExceptionType}).")]
    public static partial void DriveLookupFailed(ILogger logger, string reason, int statusCode, string exceptionType);

    [LoggerMessage(EventId = 5199, Message = "MSAL: {MsalMessage}")]
    public static partial void MsalMessage(ILogger logger, LogLevel level, string msalMessage);
}
