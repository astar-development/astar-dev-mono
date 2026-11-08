using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public sealed class GivenATokenCacheInitialiser : IDisposable
{
    private readonly RecordingLoggerProvider logs = new();
    private readonly ConsentFile consentFile = new();

    [Fact]
    public async Task when_the_secure_store_is_available_then_the_cache_is_attached_without_asking_the_user()
    {
        var storage = new FakeMsalTokenCacheStorage();
        var consent = new FakeTokenCacheConsent();

        var attached = await CreateModule(storage, consent).EnsureTokenCacheAsync(TestContext.Current.CancellationToken).MatchAsync(_ => true, _ => false);

        (attached, consent.QuestionsAsked, storage.PlaintextFileAttempts).ShouldBe((true, 0, 0));
    }

    [Fact]
    public async Task when_the_secure_store_is_unavailable_and_the_user_agrees_then_the_plaintext_file_is_attached()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        var consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted() };

        var attached = await CreateModule(storage, consent).EnsureTokenCacheAsync(TestContext.Current.CancellationToken).MatchAsync(_ => true, _ => false);

        (attached, consent.QuestionsAsked, storage.PlaintextFileAttempts).ShouldBe((true, 1, 1));
    }

    [Fact]
    public async Task when_the_secure_store_is_unavailable_and_the_user_declines_then_the_token_cache_is_unavailable_and_no_plaintext_file_is_used()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        var consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateDeclined() };

        var failure = await CreateModule(storage, consent).EnsureTokenCacheAsync(TestContext.Current.CancellationToken).MatchAsync(attached => (object)attached, failure => failure);

        (failure, storage.PlaintextFileAttempts).ShouldBe((AccountAddFailureFactory.CreateTokenCacheUnavailable(), 0));
    }

    [Fact]
    public async Task when_the_plaintext_file_cannot_be_used_then_the_token_cache_is_unavailable_and_a_warning_is_logged()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true, PlaintextFileFails = true };
        var consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted() };

        var failure = await CreateModule(storage, consent).EnsureTokenCacheAsync(TestContext.Current.CancellationToken).MatchAsync(attached => (object)attached, failure => failure);

        failure.ShouldBe(AccountAddFailureFactory.CreateTokenCacheUnavailable());
        logs.Logger.Entries.Count(entry => entry.Level >= LogLevel.Warning).ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task when_the_cache_is_already_attached_then_a_second_call_does_not_attach_it_again()
    {
        var storage = new FakeMsalTokenCacheStorage();
        var initialiser = CreateModule(storage, new FakeTokenCacheConsent());

        _ = await initialiser.EnsureTokenCacheAsync(TestContext.Current.CancellationToken);
        _ = await initialiser.EnsureTokenCacheAsync(TestContext.Current.CancellationToken);

        storage.SecureStoreAttempts.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_user_declined_earlier_then_the_next_call_asks_again()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        var consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateDeclined() };
        var initialiser = CreateModule(storage, consent);

        _ = await initialiser.EnsureTokenCacheAsync(TestContext.Current.CancellationToken);
        consent.Answer = PlaintextCacheConsentFactory.CreateGranted();
        var attached = await initialiser.EnsureTokenCacheAsync(TestContext.Current.CancellationToken).MatchAsync(_ => true, _ => false);

        (attached, consent.QuestionsAsked, storage.PlaintextFileAttempts).ShouldBe((true, 2, 1));
    }

    [Fact]
    public async Task when_many_attempts_start_together_then_the_cache_is_attached_once()
    {
        var storage = new FakeMsalTokenCacheStorage { AttachDelay = TimeSpan.FromMilliseconds(50) };
        var initialiser = CreateModule(storage, new FakeTokenCacheConsent());

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => initialiser.EnsureTokenCacheAsync(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken)));

        (results.All(result => result.Match(_ => true, _ => false)), storage.SecureStoreAttempts).ShouldBe((true, 1));
    }

    [Fact]
    public async Task when_the_secure_store_is_unavailable_then_the_log_does_not_contain_the_exception_message()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true, PlaintextFileFails = true };

        _ = await CreateModule(storage, new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted() }).EnsureTokenCacheAsync(TestContext.Current.CancellationToken);

        logs.Logger.Messages.ShouldAllBe(message => !message.Contains("keyring is locked") && !message.Contains("read-only"));
        logs.Logger.Entries.ShouldAllBe(entry => entry.Exception == null);
    }

    [Fact]
    public async Task when_the_user_cancels_the_consent_question_then_the_attempt_is_cancelled_and_the_next_call_asks_again()
    {
        using var cancellation = new CancellationTokenSource();
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        var consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted(), CancelOnFirstQuestion = cancellation };
        var initialiser = CreateModule(storage, consent);

        await Should.ThrowAsync<OperationCanceledException>(() => initialiser.EnsureTokenCacheAsync(cancellation.Token));
        var attached = await initialiser.EnsureTokenCacheAsync(TestContext.Current.CancellationToken).MatchAsync(_ => true, _ => false);

        (attached, consent.QuestionsAsked).ShouldBe((true, 2));
    }

    [Fact]
    public async Task when_the_consent_question_fails_then_the_token_cache_is_unavailable_and_the_failure_message_is_not_logged()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };

        var failure = await CreateModule(storage, new FakeTokenCacheConsent { Fails = true }).EnsureTokenCacheAsync(TestContext.Current.CancellationToken).MatchAsync(attached => (object)attached, failure => failure);

        (failure, storage.PlaintextFileAttempts).ShouldBe((AccountAddFailureFactory.CreateTokenCacheUnavailable(), 0));
        logs.Logger.Messages.ShouldAllBe(message => !message.Contains("could not open"));
    }

    [Fact]
    public async Task when_the_user_agrees_then_the_consent_is_remembered()
    {
        _ = await CreateModule(new FakeMsalTokenCacheStorage { SecureStoreFails = true }, new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted() }).EnsureTokenCacheAsync(TestContext.Current.CancellationToken);

        consentFile.Remembered.Match(_ => "remembered", () => "none").ShouldBe("remembered");
    }

    [Fact]
    public async Task when_the_user_declines_then_no_consent_is_remembered()
    {
        _ = await CreateModule(new FakeMsalTokenCacheStorage { SecureStoreFails = true }, new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateDeclined() }).EnsureTokenCacheAsync(TestContext.Current.CancellationToken);

        consentFile.Remembered.Match(_ => "remembered", () => "none").ShouldBe("none");
    }

    [Fact]
    public async Task when_the_user_agreed_on_an_earlier_run_then_the_plaintext_file_is_used_without_asking_again()
    {
        consentFile.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        var consent = new FakeTokenCacheConsent();

        var attached = await CreateModule(storage, consent).EnsureTokenCacheAsync(TestContext.Current.CancellationToken).MatchAsync(_ => true, _ => false);

        (attached, consent.QuestionsAsked, storage.PlaintextFileAttempts).ShouldBe((true, 0, 1));
    }

    [Fact]
    public async Task when_the_secure_store_works_then_a_remembered_consent_is_not_used()
    {
        consentFile.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());
        var storage = new FakeMsalTokenCacheStorage();

        _ = await CreateModule(storage, new FakeTokenCacheConsent()).EnsureTokenCacheAsync(TestContext.Current.CancellationToken);

        storage.PlaintextFileAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_secure_store_works_and_an_old_plaintext_file_exists_then_its_tokens_are_moved_and_the_consent_is_forgotten()
    {
        consentFile.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());
        var storage = new FakeMsalTokenCacheStorage { PlaintextFileOnDisk = true };

        var attached = await CreateModule(storage, new FakeTokenCacheConsent()).EnsureTokenCacheAsync(TestContext.Current.CancellationToken).MatchAsync(_ => true, _ => false);

        (attached, storage.Moves, storage.PlaintextFileOnDisk, consentFile.Remembered.Match(_ => true, () => false)).ShouldBe((true, 1, false, false));
        logs.Logger.EventNames.ShouldContain("PlaintextTokensMoved");
    }

    [Fact]
    public async Task when_the_secure_store_works_and_no_plaintext_file_exists_then_nothing_is_moved_and_no_move_is_logged()
    {
        var storage = new FakeMsalTokenCacheStorage();

        _ = await CreateModule(storage, new FakeTokenCacheConsent()).EnsureTokenCacheAsync(TestContext.Current.CancellationToken);

        storage.Moves.ShouldBe(1);
        logs.Logger.EventNames.ShouldNotContain("PlaintextTokensMoved");
    }

    [Fact]
    public async Task when_moving_the_plaintext_tokens_fails_then_the_cache_is_still_attached_and_the_consent_is_kept()
    {
        consentFile.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());
        var storage = new FakeMsalTokenCacheStorage { PlaintextFileOnDisk = true, MoveFails = true };

        var attached = await CreateModule(storage, new FakeTokenCacheConsent()).EnsureTokenCacheAsync(TestContext.Current.CancellationToken).MatchAsync(_ => true, _ => false);

        (attached, consentFile.Remembered.Match(_ => true, () => false)).ShouldBe((true, true));
        logs.Logger.ValuesOf("PlaintextTokenMoveFailed")["ExceptionType"].ShouldBe(nameof(IOException));
    }

    [Fact]
    public async Task when_the_secure_store_is_unavailable_then_no_plaintext_tokens_are_moved()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true, PlaintextFileOnDisk = true };

        _ = await CreateModule(storage, new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted() }).EnsureTokenCacheAsync(TestContext.Current.CancellationToken);

        (storage.Moves, storage.PlaintextFileOnDisk).ShouldBe((0, true));
    }

    [Fact]
    public void when_a_consent_is_remembered_then_the_current_consent_is_granted()
    {
        consentFile.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());

        CreateModule(new FakeMsalTokenCacheStorage(), new FakeTokenCacheConsent()).Current().ShouldBeOfType<PlaintextCacheConsent.Granted>();
    }

    [Fact]
    public void when_no_consent_is_remembered_then_the_current_consent_is_declined()
    {
        CreateModule(new FakeMsalTokenCacheStorage(), new FakeTokenCacheConsent()).Current().ShouldBeOfType<PlaintextCacheConsent.Declined>();
    }

    [Fact]
    public async Task when_the_consent_is_withdrawn_then_the_plaintext_file_is_detached_and_deleted_and_the_consent_is_forgotten()
    {
        consentFile.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());
        var storage = new FakeMsalTokenCacheStorage { PlaintextFileOnDisk = true };
        var initialiser = CreateModule(storage, new FakeTokenCacheConsent());

        var withdrawn = await initialiser.WithdrawAsync().MatchAsync(_ => true, _ => false);

        (withdrawn, storage.Detaches, storage.PlaintextFileOnDisk).ShouldBe((true, 1, false));
        initialiser.Current().ShouldBeOfType<PlaintextCacheConsent.Declined>();
        logs.Logger.EventNames.ShouldContain("PlaintextConsentWithdrawn");
    }

    [Fact]
    public async Task when_the_consent_is_withdrawn_after_the_plaintext_file_was_attached_then_the_next_attempt_asks_the_user_again()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        var consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted() };
        var initialiser = CreateModule(storage, consent);
        _ = await initialiser.EnsureTokenCacheAsync(TestContext.Current.CancellationToken);

        _ = await initialiser.WithdrawAsync();
        _ = await initialiser.EnsureTokenCacheAsync(TestContext.Current.CancellationToken);

        (consent.QuestionsAsked, storage.PlaintextFileAttempts).ShouldBe((2, 2));
    }

    [Fact]
    public async Task when_the_plaintext_file_cannot_be_detached_then_the_withdrawal_fails_the_consent_is_kept_and_a_warning_is_logged()
    {
        consentFile.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());
        var initialiser = CreateModule(new FakeMsalTokenCacheStorage { DetachFails = true }, new FakeTokenCacheConsent());

        var withdrawn = await initialiser.WithdrawAsync().MatchAsync(_ => true, _ => false);

        withdrawn.ShouldBeFalse();
        initialiser.Current().ShouldBeOfType<PlaintextCacheConsent.Granted>();
        logs.Logger.ValuesOf("PlaintextConsentWithdrawFailed")["ExceptionType"].ShouldBe(nameof(IOException));
    }

    [Fact]
    public async Task when_the_consent_is_withdrawn_then_the_log_holds_no_path()
    {
        consentFile.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());

        _ = await CreateModule(new FakeMsalTokenCacheStorage(), new FakeTokenCacheConsent()).WithdrawAsync();

        logs.Logger.Messages.ShouldAllBe(message => !message.Contains('/') && !message.Contains("msalcache") && !message.Contains(".json"));
    }

    [Fact]
    public async Task when_the_consent_is_withdrawn_while_the_question_is_open_then_a_later_agree_does_not_restore_the_plaintext_file()
    {
        var answer = new TaskCompletionSource<PlaintextCacheConsent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        var initialiser = CreateModule(storage, new FakeTokenCacheConsent { PendingAnswer = answer });
        var attempt = initialiser.EnsureTokenCacheAsync(TestContext.Current.CancellationToken);

        _ = await initialiser.WithdrawAsync();
        answer.SetResult(PlaintextCacheConsentFactory.CreateGranted());
        var failure = await attempt.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).MatchAsync(attached => (object)attached, failure => failure);

        (failure, storage.PlaintextFileAttempts, consentFile.Remembered.Match(_ => true, () => false)).ShouldBe((AccountAddFailureFactory.CreateTokenCacheUnavailable(), 0, false));
    }

    [Fact]
    public async Task when_the_consent_is_withdrawn_while_the_plaintext_file_is_being_attached_then_the_file_is_detached_again()
    {
        consentFile.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        var initialiser = CreateModule(storage, new FakeTokenCacheConsent());
        storage.DuringPlaintextAttach = async () => _ = await initialiser.WithdrawAsync();

        var failure = await initialiser.EnsureTokenCacheAsync(TestContext.Current.CancellationToken).MatchAsync(attached => (object)attached, failure => failure);

        (failure, storage.Detaches, consentFile.Remembered.Match(_ => true, () => false)).ShouldBe((AccountAddFailureFactory.CreateTokenCacheUnavailable(), 2, false));
    }

    [Fact]
    public async Task when_the_consent_cannot_be_forgotten_then_the_withdrawal_fails_and_a_warning_is_logged()
    {
        consentFile.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());
        consentFile.RefuseDeletion();
        var initialiser = CreateModule(new FakeMsalTokenCacheStorage(), new FakeTokenCacheConsent());

        var withdrawn = await initialiser.WithdrawAsync().MatchAsync(_ => true, _ => false);

        withdrawn.ShouldBeFalse();
        logs.Logger.EventNames.ShouldContain("PlaintextConsentNotForgotten");
    }

    private OneDriveModule CreateModule(FakeMsalTokenCacheStorage storage, FakeTokenCacheConsent consent) => new(new OneDriveFakes { Storage = storage, Consent = consent, ConsentFile = consentFile, Logs = logs });

    public void Dispose()
    {
        logs.Dispose();
    }
}
