using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Connects the MSAL token cache to storage once. It prefers the secure store and uses a plaintext file only with the user's consent, given now or remembered from an earlier run.</summary>
/// <param name="storage">Connects the cache to the secure store or the plaintext file.</param>
/// <param name="consent">Asks the user before the plaintext file is used.</param>
/// <param name="consentStore">Remembers between runs that the user agreed to the plaintext file.</param>
/// <param name="logger">Reports the storage chosen and failures, without paths or account details.</param>
internal sealed class TokenCacheInitialiser(IMsalTokenCacheStorage storage, ITokenCacheConsent consent, IPlaintextCacheConsentStore consentStore, ILogger<TokenCacheInitialiser> logger) : IPlaintextCacheConsentSettings
{
    private readonly Lock gate = new();
    private Option<Task<Result<Unit, AccountAddFailure>>> attachment = Option.None<Task<Result<Unit, AccountAddFailure>>>();
    private ConsentPrompt attachmentPrompt = ConsentPromptFactory.CreateAllowed();
    private int generation;

    /// <summary>Connects the token cache, unless an earlier call already did or is doing so. A failed, declined or cancelled attempt is tried again on the next call.</summary>
    /// <param name="prompt">Whether the plaintext-file consent question may be shown. A user-started call that finds a background attempt running waits for it and, if it fails, tries again with the question allowed.</param>
    /// <param name="cancellationToken">Cancels the consent question. Calls made while an attempt is running share that attempt, so they also see its cancellation.</param>
    /// <returns><see cref="Unit"/> once the cache is connected, or <see cref="AccountAddFailure.TokenCacheUnavailable"/>.</returns>
    public Task<Result<Unit, AccountAddFailure>> EnsureAsync(ConsentPrompt prompt, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var current = attachment.Filter(IsAttachedOrAttaching).Match(running => Next(running, prompt, cancellationToken), () => Start(AttachAsync(generation, prompt, cancellationToken), prompt));
            attachment = Option.Some(current);

            return current;
        }
    }

    /// <inheritdoc />
    public PlaintextCacheConsent Current() => consentStore.Load().Match(remembered => remembered, PlaintextCacheConsentFactory.CreateDeclined);

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> WithdrawAsync()
    {
        lock (gate)
        {
            attachment = Option.None<Task<Result<Unit, AccountAddFailure>>>();
            generation++;
        }

        return Try.RunAsync(() => Task.Run(() => UnitTask.RunAsync(storage.DetachPlaintextFileAsync)), CancellationToken.None)
            .MatchAsync(_ => ForgetConsent(), PlaintextFileNotDetached);
    }

    private Task<Result<Unit, AccountAddFailure>> Next(Task<Result<Unit, AccountAddFailure>> running, ConsentPrompt prompt, CancellationToken cancellationToken) =>
        running.IsCompleted || attachmentPrompt is ConsentPrompt.Allowed || prompt is ConsentPrompt.Never
            ? Reuse(running)
            : Start(AttachAfterAsync(running, generation, cancellationToken), prompt);

    private Task<Result<Unit, AccountAddFailure>> Start(Task<Result<Unit, AccountAddFailure>> attempt, ConsentPrompt prompt)
    {
        attachmentPrompt = prompt;

        return attempt;
    }

    private async Task<Result<Unit, AccountAddFailure>> AttachAfterAsync(Task<Result<Unit, AccountAddFailure>> running, int startedGeneration, CancellationToken cancellationToken)
    {
        await ((Task)running).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (IsAttachedOrAttaching(running))
            return running.Result;

        return await AttachAsync(startedGeneration, ConsentPromptFactory.CreateAllowed(), cancellationToken).ConfigureAwait(false);
    }

    private Task<Result<Unit, AccountAddFailure>> Reuse(Task<Result<Unit, AccountAddFailure>> attached)
    {
        if (attached.IsCompletedSuccessfully)
            AccountAddLog.TokenCacheInitialised(logger, "AlreadyAttached");

        return attached;
    }

    private static bool IsAttachedOrAttaching(Task<Result<Unit, AccountAddFailure>> attempt) => !attempt.IsCompleted || (attempt.IsCompletedSuccessfully && attempt.Result.Match(_ => true, _ => false));

    private bool IsCurrent(int startedGeneration)
    {
        lock (gate)
            return generation == startedGeneration;
    }

    private Task<Result<Unit, AccountAddFailure>> AttachAsync(int startedGeneration, ConsentPrompt prompt, CancellationToken cancellationToken) =>
        Try.RunAsync(() => Task.Run(() => UnitTask.RunAsync(storage.AttachSecureStoreAsync), cancellationToken), cancellationToken)
            .ToResultAsync(exception => exception)
            .MatchAsync(_ => MoveOldPlaintextTokensAsync(), exception => UsePlaintextFileIfAllowedAsync(exception, startedGeneration, prompt, cancellationToken));

    private Task<Result<Unit, AccountAddFailure>> MoveOldPlaintextTokensAsync() =>
        Try.RunAsync(() => Task.Run(storage.MovePlaintextFileToSecureStoreAsync), CancellationToken.None)
            .MatchAsync(PlaintextFileChecked, PlaintextTokenMoveFailed);

    private Result<Unit, AccountAddFailure> PlaintextFileChecked(PlaintextTokenMove move)
    {
        if (move is PlaintextTokenMove.Moved)
            AccountAddLog.PlaintextTokensMoved(logger);

        _ = consentStore.Save(PlaintextCacheConsentFactory.CreateDeclined());

        return Attached("SecureStore");
    }

    private Result<Unit, AccountAddFailure> PlaintextTokenMoveFailed(Exception exception)
    {
        AccountAddLog.PlaintextTokenMoveFailed(logger, exception.GetType().Name);

        return Attached("SecureStore");
    }

    private Task<Result<Unit, AccountAddFailure>> UsePlaintextFileIfAllowedAsync(Exception exception, int startedGeneration, ConsentPrompt prompt, CancellationToken cancellationToken)
    {
        AccountAddLog.SecureStoreUnavailable(logger, exception.GetType().Name);

        return consentStore.Load()
            .Filter(remembered => remembered is PlaintextCacheConsent.Granted)
            .Match(_ => UseRememberedConsentAsync(startedGeneration, cancellationToken), () => AskForConsentIfAllowedAsync(startedGeneration, prompt, cancellationToken));
    }

    private Task<Result<Unit, AccountAddFailure>> AskForConsentIfAllowedAsync(int startedGeneration, ConsentPrompt prompt, CancellationToken cancellationToken) => prompt switch
    {
        ConsentPrompt.Allowed => AskForConsentAsync(startedGeneration, cancellationToken),
        _ => Task.FromResult(ConsentNotAsked()),
    };

    private Result<Unit, AccountAddFailure> ConsentNotAsked()
    {
        AccountAddLog.TokenCacheConsent(logger, "NotAskedInBackground");

        return Result.Failure<Unit, AccountAddFailure>(AccountAddFailureFactory.CreateTokenCacheUnavailable());
    }

    private Task<Result<Unit, AccountAddFailure>> UseRememberedConsentAsync(int startedGeneration, CancellationToken cancellationToken)
    {
        AccountAddLog.TokenCacheConsent(logger, "Remembered");

        return AttachPlaintextFileAsync(startedGeneration, cancellationToken);
    }

    private Task<Result<Unit, AccountAddFailure>> AskForConsentAsync(int startedGeneration, CancellationToken cancellationToken) =>
        Try.RunAsync(() => consent.AskAsync(cancellationToken), cancellationToken)
            .ToResultAsync(exception => exception)
            .MatchAsync(answer => AnsweredAsync(answer, startedGeneration, cancellationToken), exception => Task.FromResult(ConsentQuestionFailed(exception)));

    private Task<Result<Unit, AccountAddFailure>> AnsweredAsync(PlaintextCacheConsent answer, int startedGeneration, CancellationToken cancellationToken) => answer switch
    {
        PlaintextCacheConsent.Granted when !IsCurrent(startedGeneration) => Task.FromResult(WithdrawnDuringAttempt()),
        PlaintextCacheConsent.Granted => RememberAndAttachPlaintextFileAsync(answer, startedGeneration, cancellationToken),
        _ => Task.FromResult(Declined()),
    };

    private Task<Result<Unit, AccountAddFailure>> RememberAndAttachPlaintextFileAsync(PlaintextCacheConsent answer, int startedGeneration, CancellationToken cancellationToken)
    {
        AccountAddLog.TokenCacheConsent(logger, "Granted");
        _ = consentStore.Save(answer);

        return AttachPlaintextFileAsync(startedGeneration, cancellationToken);
    }

    private Task<Result<Unit, AccountAddFailure>> AttachPlaintextFileAsync(int startedGeneration, CancellationToken cancellationToken) =>
        Try.RunAsync(() => Task.Run(() => UnitTask.RunAsync(storage.AttachPlaintextFileAsync), cancellationToken), cancellationToken)
            .ToResultAsync(exception => exception)
            .MatchAsync(_ => AttachedUnlessWithdrawnAsync(startedGeneration), exception => Task.FromResult(PlaintextFileFailed(exception)));

    private Task<Result<Unit, AccountAddFailure>> AttachedUnlessWithdrawnAsync(int startedGeneration) =>
        IsCurrent(startedGeneration)
            ? Task.FromResult(Attached("PlaintextFile"))
            : Try.RunAsync(() => Task.Run(() => UnitTask.RunAsync(storage.DetachPlaintextFileAsync)), CancellationToken.None)
                .MatchAsync(_ => WithdrawnDuringAttempt(), _ => WithdrawnDuringAttempt());

    private Result<Unit, AccountAddFailure> WithdrawnDuringAttempt()
    {
        AccountAddLog.TokenCacheConsent(logger, "WithdrawnDuringAttempt");

        return Result.Failure<Unit, AccountAddFailure>(AccountAddFailureFactory.CreateTokenCacheUnavailable());
    }

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

    private Result<Unit, ErrorResponse> ForgetConsent() =>
        consentStore.Save(PlaintextCacheConsentFactory.CreateDeclined()).Match(ConsentWithdrawn, ConsentNotForgotten);

    private Result<Unit, ErrorResponse> ConsentWithdrawn(Unit forgotten)
    {
        AccountAddLog.PlaintextConsentWithdrawn(logger);

        return Result.Success<Unit, ErrorResponse>(forgotten);
    }

    private Result<Unit, ErrorResponse> ConsentNotForgotten(ErrorResponse error)
    {
        AccountAddLog.PlaintextConsentNotForgotten(logger);

        return Result.Failure<Unit, ErrorResponse>(error);
    }

    private Result<Unit, ErrorResponse> PlaintextFileNotDetached(Exception exception)
    {
        AccountAddLog.PlaintextConsentWithdrawFailed(logger, "the plaintext token file could not be deleted", exception.GetType().Name);

        return Result.Failure<Unit, ErrorResponse>(new ErrorResponse("The plaintext token file could not be deleted."));
    }
}
