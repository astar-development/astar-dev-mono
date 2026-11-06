using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Connects the MSAL token cache to storage once. It prefers the secure store and uses a plaintext file only with the user's consent, given now or remembered from an earlier run.</summary>
/// <param name="storage">Connects the cache to the secure store or the plaintext file.</param>
/// <param name="consent">Asks the user before the plaintext file is used.</param>
/// <param name="consentStore">Remembers between runs that the user agreed to the plaintext file.</param>
/// <param name="logger">Reports the storage chosen and failures, without paths or account details.</param>
public sealed class TokenCacheInitialiser(IMsalTokenCacheStorage storage, ITokenCacheConsent consent, IPlaintextCacheConsentStore consentStore, ILogger<TokenCacheInitialiser> logger) : IPlaintextCacheConsentSettings
{
    private readonly Lock gate = new();
    private Option<Task<Result<Unit, AccountAddFailure>>> attachment = Option.None<Task<Result<Unit, AccountAddFailure>>>();

    /// <summary>Connects the token cache, unless an earlier call already did or is doing so. A failed, declined or cancelled attempt is tried again on the next call.</summary>
    /// <param name="cancellationToken">Cancels the consent question. Calls made while an attempt is running share that attempt, so they also see its cancellation.</param>
    /// <returns><see cref="Unit"/> once the cache is connected, or <see cref="AccountAddFailure.TokenCacheUnavailable"/>.</returns>
    public Task<Result<Unit, AccountAddFailure>> EnsureAsync(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var current = attachment.Filter(IsAttachedOrAttaching).Match(Reuse, () => AttachAsync(cancellationToken));
            attachment = Option.Some(current);

            return current;
        }
    }

    /// <inheritdoc />
    public PlaintextCacheConsent Current() => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> WithdrawAsync() => throw new NotImplementedException();

    private Task<Result<Unit, AccountAddFailure>> Reuse(Task<Result<Unit, AccountAddFailure>> attached)
    {
        if (attached.IsCompletedSuccessfully)
            AccountAddLog.TokenCacheInitialised(logger, "AlreadyAttached");

        return attached;
    }

    private static bool IsAttachedOrAttaching(Task<Result<Unit, AccountAddFailure>> attempt) => !attempt.IsCompleted || (attempt.IsCompletedSuccessfully && attempt.Result.Match(_ => true, _ => false));

    private Task<Result<Unit, AccountAddFailure>> AttachAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => Task.Run(() => UnitTask.RunAsync(storage.AttachSecureStoreAsync), cancellationToken), cancellationToken)
            .ToResultAsync(exception => exception)
            .MatchAsync(_ => Task.FromResult(Attached("SecureStore")), exception => UsePlaintextFileIfAllowedAsync(exception, cancellationToken));

    private Task<Result<Unit, AccountAddFailure>> UsePlaintextFileIfAllowedAsync(Exception exception, CancellationToken cancellationToken)
    {
        AccountAddLog.SecureStoreUnavailable(logger, exception.GetType().Name);

        return consentStore.Load()
            .Filter(remembered => remembered is PlaintextCacheConsent.Granted)
            .Match(_ => UseRememberedConsentAsync(cancellationToken), () => AskForConsentAsync(cancellationToken));
    }

    private Task<Result<Unit, AccountAddFailure>> UseRememberedConsentAsync(CancellationToken cancellationToken)
    {
        AccountAddLog.TokenCacheConsent(logger, "Remembered");

        return AttachPlaintextFileAsync(cancellationToken);
    }

    private Task<Result<Unit, AccountAddFailure>> AskForConsentAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => consent.AskAsync(cancellationToken), cancellationToken)
            .ToResultAsync(exception => exception)
            .MatchAsync(answer => AnsweredAsync(answer, cancellationToken), exception => Task.FromResult(ConsentQuestionFailed(exception)));

    private Task<Result<Unit, AccountAddFailure>> AnsweredAsync(PlaintextCacheConsent answer, CancellationToken cancellationToken) => answer switch
    {
        PlaintextCacheConsent.Granted => RememberAndAttachPlaintextFileAsync(answer, cancellationToken),
        _ => Task.FromResult(Declined()),
    };

    private Task<Result<Unit, AccountAddFailure>> RememberAndAttachPlaintextFileAsync(PlaintextCacheConsent answer, CancellationToken cancellationToken)
    {
        AccountAddLog.TokenCacheConsent(logger, "Granted");
        _ = consentStore.Save(answer);

        return AttachPlaintextFileAsync(cancellationToken);
    }

    private Task<Result<Unit, AccountAddFailure>> AttachPlaintextFileAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => Task.Run(() => UnitTask.RunAsync(storage.AttachPlaintextFileAsync), cancellationToken), cancellationToken)
            .MatchAsync(_ => Attached("PlaintextFile"), PlaintextFileFailed);

    private Result<Unit, AccountAddFailure> Attached(string mode)
    {
        AccountAddLog.TokenCacheInitialised(logger, mode);

        return Result.Success<Unit, AccountAddFailure>(Unit.Instance);
    }

    private Result<Unit, AccountAddFailure> Declined()
    {
        AccountAddLog.TokenCacheConsent(logger, "Declined");

        return Result.Failure<Unit, AccountAddFailure>(AccountAddFailureFactory.CreateTokenCacheUnavailable());
    }

    private Result<Unit, AccountAddFailure> ConsentQuestionFailed(Exception exception)
    {
        AccountAddLog.TokenCacheUnavailable(logger, "the plaintext token file question could not be asked", exception.GetType().Name);

        return Result.Failure<Unit, AccountAddFailure>(AccountAddFailureFactory.CreateTokenCacheUnavailable());
    }

    private Result<Unit, AccountAddFailure> PlaintextFileFailed(Exception exception)
    {
        AccountAddLog.TokenCacheUnavailable(logger, "the plaintext token file could not be used", exception.GetType().Name);

        return Result.Failure<Unit, AccountAddFailure>(AccountAddFailureFactory.CreateTokenCacheUnavailable());
    }
}
