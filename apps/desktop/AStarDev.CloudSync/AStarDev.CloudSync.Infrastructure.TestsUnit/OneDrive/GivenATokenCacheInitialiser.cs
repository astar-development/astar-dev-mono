using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.CloudSync.Infrastructure.OneDrive;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public class GivenATokenCacheInitialiser
{
    private readonly RecordingLogger<TokenCacheInitialiser> logger = new();
    private readonly InMemoryPlaintextCacheConsentStore consentStore = new();

    [Fact]
    public async Task when_the_secure_store_is_available_then_the_cache_is_attached_without_asking_the_user()
    {
        var storage = new FakeMsalTokenCacheStorage();
        var consent = new FakeTokenCacheConsent();

        var attached = await CreateInitialiser(storage, consent).EnsureAsync(TestContext.Current.CancellationToken).MatchAsync(_ => true, _ => false);

        (attached, consent.QuestionsAsked, storage.PlaintextFileAttempts).ShouldBe((true, 0, 0));
    }

    [Fact]
    public async Task when_the_secure_store_is_unavailable_and_the_user_agrees_then_the_plaintext_file_is_attached()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        var consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted() };

        var attached = await CreateInitialiser(storage, consent).EnsureAsync(TestContext.Current.CancellationToken).MatchAsync(_ => true, _ => false);

        (attached, consent.QuestionsAsked, storage.PlaintextFileAttempts).ShouldBe((true, 1, 1));
    }

    [Fact]
    public async Task when_the_secure_store_is_unavailable_and_the_user_declines_then_the_token_cache_is_unavailable_and_no_plaintext_file_is_used()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        var consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateDeclined() };

        var failure = await CreateInitialiser(storage, consent).EnsureAsync(TestContext.Current.CancellationToken).MatchAsync(attached => (object)attached, failure => failure);

        (failure, storage.PlaintextFileAttempts).ShouldBe((AccountAddFailureFactory.CreateTokenCacheUnavailable(), 0));
    }

    [Fact]
    public async Task when_the_plaintext_file_cannot_be_used_then_the_token_cache_is_unavailable_and_a_warning_is_logged()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true, PlaintextFileFails = true };
        var consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted() };

        var failure = await CreateInitialiser(storage, consent).EnsureAsync(TestContext.Current.CancellationToken).MatchAsync(attached => (object)attached, failure => failure);

        failure.ShouldBe(AccountAddFailureFactory.CreateTokenCacheUnavailable());
        logger.Entries.Count(entry => entry.Level >= LogLevel.Warning).ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task when_the_cache_is_already_attached_then_a_second_call_does_not_attach_it_again()
    {
        var storage = new FakeMsalTokenCacheStorage();
        var initialiser = CreateInitialiser(storage, new FakeTokenCacheConsent());

        _ = await initialiser.EnsureAsync(TestContext.Current.CancellationToken);
        _ = await initialiser.EnsureAsync(TestContext.Current.CancellationToken);

        storage.SecureStoreAttempts.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_user_declined_earlier_then_the_next_call_asks_again()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        var consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateDeclined() };
        var initialiser = CreateInitialiser(storage, consent);

        _ = await initialiser.EnsureAsync(TestContext.Current.CancellationToken);
        consent.Answer = PlaintextCacheConsentFactory.CreateGranted();
        var attached = await initialiser.EnsureAsync(TestContext.Current.CancellationToken).MatchAsync(_ => true, _ => false);

        (attached, consent.QuestionsAsked, storage.PlaintextFileAttempts).ShouldBe((true, 2, 1));
    }

    [Fact]
    public async Task when_many_attempts_start_together_then_the_cache_is_attached_once()
    {
        var storage = new FakeMsalTokenCacheStorage { AttachDelay = TimeSpan.FromMilliseconds(50) };
        var initialiser = CreateInitialiser(storage, new FakeTokenCacheConsent());

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => initialiser.EnsureAsync(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken)));

        (results.All(result => result.Match(_ => true, _ => false)), storage.SecureStoreAttempts).ShouldBe((true, 1));
    }

    [Fact]
    public async Task when_the_secure_store_is_unavailable_then_the_log_does_not_contain_the_exception_message()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true, PlaintextFileFails = true };

        _ = await CreateInitialiser(storage, new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted() }).EnsureAsync(TestContext.Current.CancellationToken);

        logger.Messages.ShouldAllBe(message => !message.Contains("keyring is locked") && !message.Contains("read-only"));
        logger.Entries.ShouldAllBe(entry => entry.Exception == null);
    }

    [Fact]
    public async Task when_the_user_cancels_the_consent_question_then_the_attempt_is_cancelled_and_the_next_call_asks_again()
    {
        using var cancellation = new CancellationTokenSource();
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        var consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted(), CancelOnFirstQuestion = cancellation };
        var initialiser = CreateInitialiser(storage, consent);

        await Should.ThrowAsync<OperationCanceledException>(() => initialiser.EnsureAsync(cancellation.Token));
        var attached = await initialiser.EnsureAsync(TestContext.Current.CancellationToken).MatchAsync(_ => true, _ => false);

        (attached, consent.QuestionsAsked).ShouldBe((true, 2));
    }

    [Fact]
    public async Task when_the_consent_question_fails_then_the_token_cache_is_unavailable_and_the_failure_message_is_not_logged()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };

        var failure = await CreateInitialiser(storage, new FakeTokenCacheConsent { Fails = true }).EnsureAsync(TestContext.Current.CancellationToken).MatchAsync(attached => (object)attached, failure => failure);

        (failure, storage.PlaintextFileAttempts).ShouldBe((AccountAddFailureFactory.CreateTokenCacheUnavailable(), 0));
        logger.Messages.ShouldAllBe(message => !message.Contains("could not open"));
    }

    [Fact]
    public async Task when_the_user_agrees_then_the_consent_is_remembered()
    {
        _ = await CreateInitialiser(new FakeMsalTokenCacheStorage { SecureStoreFails = true }, new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted() }).EnsureAsync(TestContext.Current.CancellationToken);

        consentStore.SavedAnswers.ShouldBe([PlaintextCacheConsentFactory.CreateGranted()]);
    }

    [Fact]
    public async Task when_the_user_declines_then_no_consent_is_remembered()
    {
        _ = await CreateInitialiser(new FakeMsalTokenCacheStorage { SecureStoreFails = true }, new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateDeclined() }).EnsureAsync(TestContext.Current.CancellationToken);

        consentStore.Load().Match(_ => "remembered", () => "none").ShouldBe("none");
    }

    [Fact]
    public async Task when_the_user_agreed_on_an_earlier_run_then_the_plaintext_file_is_used_without_asking_again()
    {
        consentStore.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        var consent = new FakeTokenCacheConsent();

        var attached = await CreateInitialiser(storage, consent).EnsureAsync(TestContext.Current.CancellationToken).MatchAsync(_ => true, _ => false);

        (attached, consent.QuestionsAsked, storage.PlaintextFileAttempts).ShouldBe((true, 0, 1));
    }

    [Fact]
    public async Task when_the_secure_store_works_then_a_remembered_consent_is_not_used()
    {
        consentStore.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());
        var storage = new FakeMsalTokenCacheStorage();

        _ = await CreateInitialiser(storage, new FakeTokenCacheConsent()).EnsureAsync(TestContext.Current.CancellationToken);

        storage.PlaintextFileAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_secure_store_works_and_an_old_plaintext_file_exists_then_its_tokens_are_moved_and_the_consent_is_forgotten()
    {
        consentStore.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());
        var storage = new FakeMsalTokenCacheStorage { PlaintextFileOnDisk = true };

        var attached = await CreateInitialiser(storage, new FakeTokenCacheConsent()).EnsureAsync(TestContext.Current.CancellationToken).MatchAsync(_ => true, _ => false);

        (attached, storage.Moves, storage.PlaintextFileOnDisk, consentStore.Remembered.Match(_ => true, () => false)).ShouldBe((true, 1, false, false));
        logger.EventNames.ShouldContain("PlaintextTokensMoved");
    }

    [Fact]
    public async Task when_the_secure_store_works_and_no_plaintext_file_exists_then_nothing_is_moved_and_no_move_is_logged()
    {
        var storage = new FakeMsalTokenCacheStorage();

        _ = await CreateInitialiser(storage, new FakeTokenCacheConsent()).EnsureAsync(TestContext.Current.CancellationToken);

        storage.Moves.ShouldBe(1);
        logger.EventNames.ShouldNotContain("PlaintextTokensMoved");
    }

    [Fact]
    public async Task when_moving_the_plaintext_tokens_fails_then_the_cache_is_still_attached_and_the_consent_is_kept()
    {
        consentStore.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());
        var storage = new FakeMsalTokenCacheStorage { PlaintextFileOnDisk = true, MoveFails = true };

        var attached = await CreateInitialiser(storage, new FakeTokenCacheConsent()).EnsureAsync(TestContext.Current.CancellationToken).MatchAsync(_ => true, _ => false);

        (attached, consentStore.Remembered.Match(_ => true, () => false)).ShouldBe((true, true));
        logger.ValuesOf("PlaintextTokenMoveFailed")["ExceptionType"].ShouldBe(nameof(IOException));
    }

    [Fact]
    public async Task when_the_secure_store_is_unavailable_then_no_plaintext_tokens_are_moved()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true, PlaintextFileOnDisk = true };

        _ = await CreateInitialiser(storage, new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted() }).EnsureAsync(TestContext.Current.CancellationToken);

        (storage.Moves, storage.PlaintextFileOnDisk).ShouldBe((0, true));
    }

    [Fact]
    public void when_a_consent_is_remembered_then_the_current_consent_is_granted()
    {
        consentStore.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());

        CreateInitialiser(new FakeMsalTokenCacheStorage(), new FakeTokenCacheConsent()).Current().ShouldBeOfType<PlaintextCacheConsent.Granted>();
    }

    [Fact]
    public void when_no_consent_is_remembered_then_the_current_consent_is_declined()
    {
        CreateInitialiser(new FakeMsalTokenCacheStorage(), new FakeTokenCacheConsent()).Current().ShouldBeOfType<PlaintextCacheConsent.Declined>();
    }

    [Fact]
    public async Task when_the_consent_is_withdrawn_then_the_plaintext_file_is_detached_and_deleted_and_the_consent_is_forgotten()
    {
        consentStore.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());
        var storage = new FakeMsalTokenCacheStorage { PlaintextFileOnDisk = true };
        var initialiser = CreateInitialiser(storage, new FakeTokenCacheConsent());

        var withdrawn = await initialiser.WithdrawAsync().MatchAsync(_ => true, _ => false);

        (withdrawn, storage.Detaches, storage.PlaintextFileOnDisk).ShouldBe((true, 1, false));
        initialiser.Current().ShouldBeOfType<PlaintextCacheConsent.Declined>();
        logger.EventNames.ShouldContain("PlaintextConsentWithdrawn");
    }

    [Fact]
    public async Task when_the_consent_is_withdrawn_after_the_plaintext_file_was_attached_then_the_next_attempt_asks_the_user_again()
    {
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        var consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted() };
        var initialiser = CreateInitialiser(storage, consent);
        _ = await initialiser.EnsureAsync(TestContext.Current.CancellationToken);

        _ = await initialiser.WithdrawAsync();
        _ = await initialiser.EnsureAsync(TestContext.Current.CancellationToken);

        (consent.QuestionsAsked, storage.PlaintextFileAttempts).ShouldBe((2, 2));
    }

    [Fact]
    public async Task when_the_plaintext_file_cannot_be_detached_then_the_withdrawal_fails_the_consent_is_kept_and_a_warning_is_logged()
    {
        consentStore.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());
        var initialiser = CreateInitialiser(new FakeMsalTokenCacheStorage { DetachFails = true }, new FakeTokenCacheConsent());

        var withdrawn = await initialiser.WithdrawAsync().MatchAsync(_ => true, _ => false);

        withdrawn.ShouldBeFalse();
        initialiser.Current().ShouldBeOfType<PlaintextCacheConsent.Granted>();
        logger.ValuesOf("PlaintextConsentWithdrawFailed")["ExceptionType"].ShouldBe(nameof(IOException));
    }

    [Fact]
    public async Task when_the_consent_is_withdrawn_then_the_log_holds_no_path()
    {
        consentStore.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());

        _ = await CreateInitialiser(new FakeMsalTokenCacheStorage(), new FakeTokenCacheConsent()).WithdrawAsync();

        logger.Messages.ShouldAllBe(message => !message.Contains('/') && !message.Contains("msalcache") && !message.Contains(".json"));
    }

    private TokenCacheInitialiser CreateInitialiser(FakeMsalTokenCacheStorage storage, FakeTokenCacheConsent consent) => new(storage, consent, consentStore, logger);
}
