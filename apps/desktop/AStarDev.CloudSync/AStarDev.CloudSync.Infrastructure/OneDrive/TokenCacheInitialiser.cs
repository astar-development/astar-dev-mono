using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Connects the MSAL token cache to storage once. It prefers the secure store and uses a plaintext file only with the user's consent, given now or remembered from an earlier run.</summary>
/// <param name="storage">Connects the cache to the secure store or the plaintext file.</param>
/// <param name="consent">Asks the user before the plaintext file is used.</param>
/// <param name="consentStore">Remembers between runs that the user agreed to the plaintext file.</param>
/// <param name="logger">Reports the storage chosen and failures, without paths or account details.</param>
public sealed class TokenCacheInitialiser(IMsalTokenCacheStorage storage, ITokenCacheConsent consent, IPlaintextCacheConsentStore consentStore, ILogger<TokenCacheInitialiser> logger)
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
            var current = attachment.Filter(IsAttachedOrAttaching).Match(inProgress => inProgress, () => AttachAsync(cancellationToken));
            attachment = Option.Some(current);

            return current;
        }
    }

    private static bool IsAttachedOrAttaching(Task<Result<Unit, AccountAddFailure>> attempt) => !attempt.IsCompleted || (attempt.IsCompletedSuccessfully && attempt.Result.Match(_ => true, _ => false));

    private Task<Result<Unit, AccountAddFailure>> AttachAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => Task.Run(() => UnitTask.RunAsync(storage.AttachSecureStoreAsync)), cancellationToken)
            .ToResultAsync(exception => exception)
            .MatchAsync(_ => Task.FromResult(Attached("The token cache uses the secure store.")), exception => UsePlaintextFileIfAllowedAsync(exception, cancellationToken));

    private Task<Result<Unit, AccountAddFailure>> UsePlaintextFileIfAllowedAsync(Exception exception, CancellationToken cancellationToken)
    {
        LogMessage.Warning(logger, nameof(TokenCacheInitialiser), $"The secure token store is unavailable ({exception.GetType().Name}).");

        return consentStore.Load()
            .Filter(remembered => remembered is PlaintextCacheConsent.Granted)
            .Match(_ => AttachPlaintextFileAsync("The user agreed to a plaintext token file on an earlier run; the token cache uses it.", cancellationToken), () => AskForConsentAsync(cancellationToken));
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
        _ = consentStore.Save(answer);

        return AttachPlaintextFileAsync("The user agreed to a plaintext token file; the token cache uses it.", cancellationToken);
    }

    private Task<Result<Unit, AccountAddFailure>> AttachPlaintextFileAsync(string attachedMessage, CancellationToken cancellationToken) =>
        Try.RunAsync(() => Task.Run(() => UnitTask.RunAsync(storage.AttachPlaintextFileAsync)), cancellationToken)
            .MatchAsync(_ => Attached(attachedMessage), PlaintextFileFailed);

    private Result<Unit, AccountAddFailure> Attached(string message)
    {
        LogMessage.Information(logger, nameof(TokenCacheInitialiser), message);

        return Result.Success<Unit, AccountAddFailure>(Unit.Instance);
    }

    private Result<Unit, AccountAddFailure> Declined()
    {
        LogMessage.Information(logger, nameof(TokenCacheInitialiser), "The user declined a plaintext token file; the token cache is unavailable.");

        return Result.Failure<Unit, AccountAddFailure>(AccountAddFailureFactory.CreateTokenCacheUnavailable());
    }

    private Result<Unit, AccountAddFailure> ConsentQuestionFailed(Exception exception)
    {
        LogMessage.Warning(logger, nameof(TokenCacheInitialiser), $"The plaintext token file question could not be asked ({exception.GetType().Name}); the token cache is unavailable.");

        return Result.Failure<Unit, AccountAddFailure>(AccountAddFailureFactory.CreateTokenCacheUnavailable());
    }

    private Result<Unit, AccountAddFailure> PlaintextFileFailed(Exception exception)
    {
        LogMessage.Warning(logger, nameof(TokenCacheInitialiser), $"The plaintext token file could not be used ({exception.GetType().Name}); the token cache is unavailable.");

        return Result.Failure<Unit, AccountAddFailure>(AccountAddFailureFactory.CreateTokenCacheUnavailable());
    }
}
