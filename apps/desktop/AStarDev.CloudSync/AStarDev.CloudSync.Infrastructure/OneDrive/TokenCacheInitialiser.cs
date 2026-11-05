using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Connects the MSAL token cache to storage once. It prefers the secure store and uses a plaintext file only with the user's consent.</summary>
/// <param name="storage">Connects the cache to the secure store or the plaintext file.</param>
/// <param name="consent">Asks the user before the plaintext file is used.</param>
/// <param name="logger">Reports the storage chosen and failures, without paths or account details.</param>
public sealed class TokenCacheInitialiser(IMsalTokenCacheStorage storage, ITokenCacheConsent consent, ILogger<TokenCacheInitialiser> logger)
{
    private readonly Lock gate = new();
    private Option<Task<Result<Unit, AccountAddFailure>>> attachment = Option.None<Task<Result<Unit, AccountAddFailure>>>();

    /// <summary>Connects the token cache, unless an earlier call already did or is doing so. A failed, declined or cancelled attempt is tried again on the next call.</summary>
    /// <param name="cancellationToken">Cancels the consent question.</param>
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
        Try.RunAsync(() => UnitTask.RunAsync(storage.AttachSecureStoreAsync), cancellationToken)
            .ToResultAsync(exception => exception)
            .MatchAsync(_ => Task.FromResult(Attached("The token cache uses the secure store.")), exception => AskForPlaintextFileAsync(exception, cancellationToken));

    private async Task<Result<Unit, AccountAddFailure>> AskForPlaintextFileAsync(Exception exception, CancellationToken cancellationToken)
    {
        LogMessage.Warning(logger, nameof(TokenCacheInitialiser), $"The secure token store is unavailable ({exception.GetType().Name}); asking the user before using a plaintext token file.");

        return await consent.AskAsync(cancellationToken).ConfigureAwait(false) switch
        {
            PlaintextCacheConsent.Granted => await AttachPlaintextFileAsync(cancellationToken).ConfigureAwait(false),
            _ => Declined(),
        };
    }

    private Task<Result<Unit, AccountAddFailure>> AttachPlaintextFileAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => UnitTask.RunAsync(storage.AttachPlaintextFileAsync), cancellationToken)
            .MatchAsync(_ => Attached("The user agreed to a plaintext token file; the token cache uses it."), PlaintextFileFailed);

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

    private Result<Unit, AccountAddFailure> PlaintextFileFailed(Exception exception)
    {
        LogMessage.Warning(logger, nameof(TokenCacheInitialiser), $"The plaintext token file could not be used ({exception.GetType().Name}); the token cache is unavailable.");

        return Result.Failure<Unit, AccountAddFailure>(AccountAddFailureFactory.CreateTokenCacheUnavailable());
    }
}
