using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.CloudSync.Infrastructure.OneDrive;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Identity.Client;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public class GivenAnAccountAddAttemptLog
{
    private readonly RecordingLogger<MicrosoftAccountAdder> logger = new();
    private readonly RecordingLogger<TokenCacheInitialiser> cacheLogger = new();
    private readonly FakeTimeProvider time = TestData.CreateTime();
    private FakeMsalTokenCacheStorage storage = new();
    private FakeTokenCacheConsent consent = new();
    private readonly InMemoryPlaintextCacheConsentStore consentStore = new();
    private FakeMsalClient msal = new();
    private FakeDriveLookup driveLookup = new();
    private FakeAccountStore accountStore = new();

    [Fact]
    public async Task when_a_new_account_is_added_then_every_step_is_logged_in_order()
    {
        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        logger.EventNames.ShouldBe(["AttemptStarted", "BrowserLaunched", "AuthSucceeded", "DriveFetched", "Persisted", "AttemptCompleted"]);
    }

    [Fact]
    public async Task when_an_attempt_runs_then_its_events_share_a_scope_with_the_attempt_id_and_provider()
    {
        var adder = CreateAdder();

        _ = await adder.AddAsync(TestContext.Current.CancellationToken);
        _ = await adder.AddAsync(TestContext.Current.CancellationToken);

        logger.Scopes.Count.ShouldBe(2);
        logger.Scopes.ShouldAllBe(scope => (string?)scope["Provider"] == "OneDrive" && scope["AccountAddAttemptId"] is Guid && (Guid)scope["AccountAddAttemptId"]! != Guid.Empty);
        logger.Scopes[0]["AccountAddAttemptId"].ShouldNotBe(logger.Scopes[1]["AccountAddAttemptId"]);
    }

    [Fact]
    public async Task when_sign_in_succeeds_then_its_duration_scopes_and_a_hashed_account_key_are_logged()
    {
        msal = new FakeMsalClient { SignIn = _ => { time.Advance(TimeSpan.FromSeconds(2)); return Task.FromResult(TestSignIns.NewAccount); } };

        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        var values = logger.ValuesOf("AuthSucceeded");
        values["DurationMs"].ShouldBe(2000d);
        values["Scopes"].ShouldBe("User.Read Files.ReadWrite.All");
        var keyHash = values["AccountKeyHash"].ShouldBeOfType<string>();
        keyHash.Length.ShouldBe(16);
        keyHash.ShouldAllBe(character => Uri.IsHexDigit(character));
        TestSignIns.AccountKey.ShouldNotContain(keyHash);
    }

    [Fact]
    public async Task when_the_same_account_signs_in_with_different_key_casing_then_the_key_hash_is_the_same()
    {
        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);
        msal = new FakeMsalClient { SignIn = _ => Task.FromResult(MicrosoftSignInFactory.Create(TestSignIns.AccountKey.ToUpperInvariant(), TestSignIns.Email, TestSignIns.DisplayName, TestSignIns.AccessToken, TestSignIns.GrantedScopes)) };
        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        logger.Events.Where(recorded => recorded.Name == "AuthSucceeded").Select(recorded => recorded.Values["AccountKeyHash"]).Distinct().Count().ShouldBe(1);
    }

    [Fact]
    public async Task when_the_drive_is_read_and_the_account_saved_then_both_durations_are_logged()
    {
        driveLookup = new FakeDriveLookup { Lookup = _ => { time.Advance(TimeSpan.FromMilliseconds(300)); return Task.FromResult(Result.Success<MicrosoftDrive, AccountAddFailure>(TestSignIns.Drive)); } };
        accountStore = new FakeAccountStore { Save = (_, _) => { time.Advance(TimeSpan.FromMilliseconds(40)); return Task.FromResult(AccountAddOutcomeFactory.CreateAdded(TestSignIns.AccountId)); } };

        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        (logger.ValuesOf("DriveFetched")["DurationMs"], logger.ValuesOf("Persisted")["DurationMs"]).ShouldBe((300d, 40d));
    }

    [Fact]
    public async Task when_the_attempt_completes_then_the_outcome_and_total_duration_are_logged()
    {
        msal = new FakeMsalClient { SignIn = _ => { time.Advance(TimeSpan.FromSeconds(5)); return Task.FromResult(TestSignIns.NewAccount); } };

        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        var values = logger.ValuesOf("AttemptCompleted");
        (values["Outcome"], values["DurationMs"]).ShouldBe(("Added", 5000d));
    }

    [Fact]
    public async Task when_the_account_was_already_added_then_a_duplicate_is_logged_and_the_outcome_is_re_authenticated()
    {
        accountStore = new FakeAccountStore { Save = (_, _) => Task.FromResult(AccountAddOutcomeFactory.CreateReAuthenticated(TestSignIns.AccountId)) };

        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        logger.EventNames.ShouldContain("DuplicateDetected");
        logger.ValuesOf("AttemptCompleted")["Outcome"].ShouldBe("ReAuthenticated");
    }

    [Fact]
    public async Task when_the_user_closes_the_browser_then_auth_cancelled_is_logged()
    {
        msal = new FakeMsalClient { SignIn = _ => Task.FromException<MicrosoftSignIn>(new MsalClientException(MsalError.AuthenticationCanceledError, "The user closed the browser.")) };

        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        logger.EventNames.ShouldContain("AuthCancelled");
        logger.ValuesOf("AttemptCompleted")["Outcome"].ShouldBe("Cancelled");
    }

    [Fact]
    public async Task when_microsoft_rejects_the_sign_in_then_the_msal_error_code_and_correlation_id_are_logged()
    {
        var rejection = new MsalServiceException("invalid_grant", $"AADSTS50126: {TestSignIns.Email} could not sign in.") { CorrelationId = "6f1c2b3a-0d9e-4f8a-b7c6-5d4e3f2a1b0c" };
        msal = new FakeMsalClient { SignIn = _ => Task.FromException<MicrosoftSignIn>(rejection) };

        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        var values = logger.ValuesOf("AuthFailed");
        (values["ErrorCode"], values["CorrelationId"], values["ExceptionType"]).ShouldBe(("invalid_grant", "6f1c2b3a-0d9e-4f8a-b7c6-5d4e3f2a1b0c", nameof(MsalServiceException)));
        logger.ValuesOf("AttemptCompleted")["Outcome"].ShouldBe("Failed:AuthRejected");
    }

    [Fact]
    public async Task when_a_new_account_is_rolled_back_then_rollback_performed_is_logged()
    {
        driveLookup = new FakeDriveLookup { Lookup = _ => Task.FromResult(Result.Failure<MicrosoftDrive, AccountAddFailure>(AccountAddFailureFactory.CreateDriveUnavailable())) };

        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        logger.EventNames.ShouldContain("RollbackPerformed");
        logger.ValuesOf("AttemptCompleted")["Outcome"].ShouldBe("Failed:DriveUnavailable");
    }

    [Fact]
    public async Task when_a_failed_account_was_already_signed_in_then_rollback_skipped_is_logged()
    {
        msal = new FakeMsalClient { CachedAccountKeys = [TestSignIns.AccountKey] };
        accountStore = new FakeAccountStore { Save = (_, _) => Task.FromResult(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreatePersistFailed())) };

        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        logger.EventNames.ShouldContain("RollbackSkipped");
        logger.EventNames.ShouldNotContain("RollbackPerformed");
    }

    [Fact]
    public async Task when_the_secure_store_is_used_then_the_token_cache_mode_is_logged()
    {
        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        cacheLogger.ValuesOf("TokenCacheInitialised")["Mode"].ShouldBe("SecureStore");
    }

    [Theory]
    [InlineData(true, "Granted", "PlaintextFile")]
    [InlineData(false, "Declined", null)]
    public async Task when_the_user_answers_the_plaintext_question_then_the_answer_and_mode_are_logged(bool agrees, string answer, string? mode)
    {
        storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        consent = new FakeTokenCacheConsent { Answer = agrees ? PlaintextCacheConsentFactory.CreateGranted() : PlaintextCacheConsentFactory.CreateDeclined() };

        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        cacheLogger.ValuesOf("TokenCacheConsent")["Answer"].ShouldBe(answer);
        cacheLogger.Events.Where(recorded => recorded.Name == "TokenCacheInitialised").Select(recorded => (string?)recorded.Values["Mode"]).SingleOrDefault().ShouldBe(mode);
    }

    [Fact]
    public async Task when_consent_was_given_on_an_earlier_run_then_the_remembered_answer_is_logged()
    {
        storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        consentStore.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());

        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        cacheLogger.ValuesOf("TokenCacheConsent")["Answer"].ShouldBe("Remembered");
    }

    [Fact]
    public async Task when_attempts_succeed_and_fail_then_no_email_name_token_or_account_key_is_logged()
    {
        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);
        msal = new FakeMsalClient { SignIn = _ => Task.FromException<MicrosoftSignIn>(new MsalServiceException("invalid_grant", $"AADSTS50126: {TestSignIns.Email} / {TestSignIns.DisplayName}")) };
        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        string[] secrets = [TestSignIns.Email, TestSignIns.DisplayName, TestSignIns.AccessToken, TestSignIns.AccountKey];
        logger.Messages.Concat(cacheLogger.Messages).ShouldAllBe(message => secrets.All(secret => !message.Contains(secret, StringComparison.OrdinalIgnoreCase)));
        logger.Events.SelectMany(recorded => recorded.Values.Values).Concat(logger.Scopes.SelectMany(scope => scope.Values)).Select(value => $"{value}").ShouldAllBe(value => secrets.All(secret => !value.Contains(secret, StringComparison.OrdinalIgnoreCase)));
        logger.Entries.ShouldAllBe(entry => entry.Exception == null);
    }

    private MicrosoftAccountAdder CreateAdder() => new(new TokenCacheInitialiser(storage, consent, consentStore, cacheLogger), msal, driveLookup, accountStore, time, logger);
}
