using System.Net;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Identity.Client;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public sealed class GivenAnAccountAddAttemptLog : IDisposable
{
    private readonly RecordingLoggerProvider logs = new();
    private readonly FakeTimeProvider time = TestClock.CreateTime();
    private FakeMsalTokenCacheStorage storage = new();
    private FakeTokenCacheConsent consent = new();
    private readonly ConsentFile consentFile = new();
    private FakeMsalClient msal = new();
    private FakeGraphHandler graph = new();
    private FakeAccountStore accountStore = new();

    [Fact]
    public async Task when_a_new_account_is_added_then_every_step_is_logged_in_order()
    {
        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        logs.Logger.EventNames.ShouldBe(["AttemptStarted", "TokenCacheInitialised", "BrowserLaunched", "AuthSucceeded", "DriveFetched", "Persisted", "AttemptCompleted"]);
    }

    [Fact]
    public async Task when_an_attempt_runs_then_its_events_share_a_scope_with_the_attempt_id_and_provider()
    {
        var adder = CreateModule();

        _ = await adder.AddAsync(TestContext.Current.CancellationToken);
        _ = await adder.AddAsync(TestContext.Current.CancellationToken);

        logs.Logger.Scopes.Count.ShouldBe(2);
        logs.Logger.Scopes.ShouldAllBe(scope => (string?)scope["Provider"] == "OneDrive" && scope["AccountAddAttemptId"] is Guid && (Guid)scope["AccountAddAttemptId"]! != Guid.Empty);
        logs.Logger.Scopes[0]["AccountAddAttemptId"].ShouldNotBe(logs.Logger.Scopes[1]["AccountAddAttemptId"]);
    }

    [Fact]
    public async Task when_sign_in_succeeds_then_its_duration_scopes_and_a_hashed_account_key_are_logged()
    {
        msal = new FakeMsalClient { SignIn = _ => { time.Advance(TimeSpan.FromSeconds(2)); return Task.FromResult(TestSignIns.NewAccount); } };

        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        var values = logs.Logger.ValuesOf("AuthSucceeded");
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
        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);
        msal = new FakeMsalClient { SignIn = _ => Task.FromResult(MicrosoftSignInFactory.Create(TestSignIns.AccountKey.ToUpperInvariant(), TestSignIns.Email, TestSignIns.DisplayName, TestSignIns.AccessToken, TestSignIns.GrantedScopes)) };
        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        logs.Logger.Events.Where(recorded => recorded.Name == "AuthSucceeded").Select(recorded => recorded.Values["AccountKeyHash"]).Distinct().Count().ShouldBe(1);
    }

    [Fact]
    public async Task when_the_drive_is_read_and_the_account_saved_then_both_durations_are_logged()
    {
        graph = new FakeGraphHandler { Respond = (_, _) => { time.Advance(TimeSpan.FromMilliseconds(300)); return Task.FromResult(FakeGraphHandler.Json(HttpStatusCode.OK, FakeGraphHandler.DriveJson)); } };
        accountStore = new FakeAccountStore { Save = (_, _) => { time.Advance(TimeSpan.FromMilliseconds(40)); return Task.FromResult(AccountAddOutcomeFactory.CreateAdded(TestSignIns.AccountId)); } };

        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        (logs.Logger.ValuesOf("DriveFetched")["DurationMs"], logs.Logger.ValuesOf("Persisted")["DurationMs"]).ShouldBe((300d, 40d));
    }

    [Fact]
    public async Task when_the_attempt_completes_then_the_outcome_and_total_duration_are_logged()
    {
        msal = new FakeMsalClient { SignIn = _ => { time.Advance(TimeSpan.FromSeconds(5)); return Task.FromResult(TestSignIns.NewAccount); } };

        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        var values = logs.Logger.ValuesOf("AttemptCompleted");
        (values["Outcome"], values["DurationMs"]).ShouldBe(("Added", 5000d));
    }

    [Fact]
    public async Task when_the_account_was_already_added_then_a_duplicate_is_logged_and_the_outcome_is_re_authenticated()
    {
        accountStore = new FakeAccountStore { Save = (_, _) => Task.FromResult(AccountAddOutcomeFactory.CreateReAuthenticated(TestSignIns.AccountId)) };

        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        logs.Logger.EventNames.ShouldContain("DuplicateDetected");
        logs.Logger.ValuesOf("AttemptCompleted")["Outcome"].ShouldBe("ReAuthenticated");
    }

    [Fact]
    public async Task when_the_user_closes_the_browser_then_auth_cancelled_is_logged()
    {
        msal = new FakeMsalClient { SignIn = _ => Task.FromException<MicrosoftSignIn>(new MsalClientException(MsalError.AuthenticationCanceledError, "The user closed the browser.")) };

        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        logs.Logger.EventNames.ShouldContain("AuthCancelled");
        logs.Logger.ValuesOf("AttemptCompleted")["Outcome"].ShouldBe("Cancelled");
    }

    [Fact]
    public async Task when_microsoft_rejects_the_sign_in_then_the_msal_error_code_and_correlation_id_are_logged()
    {
        var rejection = new MsalServiceException("invalid_grant", $"AADSTS50126: {TestSignIns.Email} could not sign in.") { CorrelationId = "6f1c2b3a-0d9e-4f8a-b7c6-5d4e3f2a1b0c" };
        msal = new FakeMsalClient { SignIn = _ => Task.FromException<MicrosoftSignIn>(rejection) };

        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        var values = logs.Logger.ValuesOf("AuthFailed");
        (values["ErrorCode"], values["CorrelationId"], values["ExceptionType"]).ShouldBe(("invalid_grant", "6f1c2b3a-0d9e-4f8a-b7c6-5d4e3f2a1b0c", nameof(MsalServiceException)));
        logs.Logger.ValuesOf("AttemptCompleted")["Outcome"].ShouldBe("Failed:AuthRejected");
    }

    [Fact]
    public async Task when_a_new_account_is_rolled_back_then_rollback_performed_is_logged()
    {
        graph = new FakeGraphHandler { Respond = (_, _) => Task.FromResult(FakeGraphHandler.Json(HttpStatusCode.InternalServerError, """{ "error": { "code": "generalException", "message": "The drive is unavailable." } }""")) };

        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        logs.Logger.EventNames.ShouldContain("RollbackPerformed");
        logs.Logger.ValuesOf("AttemptCompleted")["Outcome"].ShouldBe("Failed:DriveUnavailable");
    }

    [Fact]
    public async Task when_a_failed_account_was_already_signed_in_then_rollback_skipped_is_logged()
    {
        msal = new FakeMsalClient { CachedAccountKeys = [TestSignIns.AccountKey] };
        accountStore = new FakeAccountStore { Save = (_, _) => Task.FromResult(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreatePersistFailed())) };

        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        logs.Logger.EventNames.ShouldContain("RollbackSkipped");
        logs.Logger.EventNames.ShouldNotContain("RollbackPerformed");
    }

    [Fact]
    public async Task when_the_secure_store_is_used_then_the_token_cache_mode_is_logged()
    {
        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        logs.Logger.ValuesOf("TokenCacheInitialised")["Mode"].ShouldBe("SecureStore");
    }

    [Theory]
    [InlineData(true, "Granted", "PlaintextFile")]
    [InlineData(false, "Declined", null)]
    public async Task when_the_user_answers_the_plaintext_question_then_the_answer_and_mode_are_logged(bool agrees, string answer, string? mode)
    {
        storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        consent = new FakeTokenCacheConsent { Answer = agrees ? PlaintextCacheConsentFactory.CreateGranted() : PlaintextCacheConsentFactory.CreateDeclined() };

        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        logs.Logger.ValuesOf("TokenCacheConsent")["Answer"].ShouldBe(answer);
        logs.Logger.Events.Where(recorded => recorded.Name == "TokenCacheInitialised").Select(recorded => (string?)recorded.Values["Mode"]).SingleOrDefault().ShouldBe(mode);
    }

    [Fact]
    public async Task when_consent_was_given_on_an_earlier_run_then_the_remembered_answer_is_logged()
    {
        storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        consentFile.Remembered = Option.Some(PlaintextCacheConsentFactory.CreateGranted());

        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        logs.Logger.ValuesOf("TokenCacheConsent")["Answer"].ShouldBe("Remembered");
    }

    [Fact]
    public async Task when_attempts_succeed_and_fail_then_no_email_name_token_or_account_key_is_logged()
    {
        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);
        msal = new FakeMsalClient { SignIn = _ => Task.FromException<MicrosoftSignIn>(new MsalServiceException("invalid_grant", $"AADSTS50126: {TestSignIns.Email} / {TestSignIns.DisplayName}")) };
        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        string[] secrets = [TestSignIns.Email, TestSignIns.DisplayName, TestSignIns.AccessToken, TestSignIns.AccountKey];
        logs.Logger.Messages.ShouldAllBe(message => secrets.All(secret => !message.Contains(secret, StringComparison.OrdinalIgnoreCase)));
        logs.Logger.Events.SelectMany(recorded => recorded.Values.Values).Concat(logs.Logger.Scopes.SelectMany(scope => scope.Values)).Select(value => $"{value}").ShouldAllBe(value => secrets.All(secret => !value.Contains(secret, StringComparison.OrdinalIgnoreCase)));
        logs.Logger.Entries.ShouldAllBe(entry => entry.Exception == null);
    }

    [Fact]
    public async Task when_sign_in_times_out_then_the_timed_out_step_is_logged()
    {
        msal = new FakeMsalClient { SignIn = async _ => { await Task.Yield(); throw new TaskCanceledException("The request timed out."); } };

        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        logs.Logger.ValuesOf("StepTimedOut")["Step"].ShouldBe("sign-in");
        logs.Logger.ValuesOf("AttemptCompleted")["Outcome"].ShouldBe("Failed:Network");
    }

    [Fact]
    public async Task when_the_drive_lookup_times_out_then_the_timed_out_step_is_logged()
    {
        graph = new FakeGraphHandler { Respond = async (_, _) => { await Task.Yield(); throw new TaskCanceledException("The request timed out."); } };

        _ = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        logs.Logger.ValuesOf("StepTimedOut")["Step"].ShouldBe("drive lookup and save");
    }

    [Fact]
    public async Task when_the_user_cancels_through_the_token_then_the_attempt_completes_as_cancelled_without_a_failure_event()
    {
        using var cancellation = new CancellationTokenSource();
        msal = new FakeMsalClient { SignIn = async token => { await cancellation.CancelAsync(); token.ThrowIfCancellationRequested(); return TestSignIns.NewAccount; } };

        _ = await CreateModule().AddAsync(cancellation.Token);

        logs.Logger.ValuesOf("AttemptCompleted")["Outcome"].ShouldBe("Cancelled");
        logs.Logger.EventNames.ShouldNotContain("AuthFailed");
        logs.Logger.EventNames.ShouldNotContain("StepTimedOut");
    }

    [Fact]
    public async Task when_a_step_throws_unexpectedly_then_the_attempt_still_completes_with_a_failure_and_the_exception_type_is_logged()
    {
        accountStore = new FakeAccountStore { Save = (_, _) => throw new InvalidOperationException($"{TestSignIns.Email} could not be saved") };

        var outcome = await CreateModule().AddAsync(TestContext.Current.CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateAuthRejected()));
        logs.Logger.ValuesOf("AttemptFaulted")["ExceptionType"].ShouldBe(nameof(InvalidOperationException));
        logs.Logger.ValuesOf("AttemptCompleted")["Outcome"].ShouldBe("Failed:AuthRejected");
        logs.Logger.Messages.ShouldAllBe(message => !message.Contains(TestSignIns.Email));
    }

    [Fact]
    public async Task when_a_later_attempt_finds_the_token_cache_attached_then_it_logs_already_attached()
    {
        var adder = CreateModule();

        _ = await adder.AddAsync(TestContext.Current.CancellationToken);
        _ = await adder.AddAsync(TestContext.Current.CancellationToken);

        logs.Logger.Events.Where(recorded => recorded.Name == "TokenCacheInitialised").Select(recorded => recorded.Values["Mode"]).ShouldBe(["SecureStore", "AlreadyAttached"]);
    }

    private OneDriveModule CreateModule() => new(new OneDriveFakes { Storage = storage, Consent = consent, Msal = msal, Graph = graph, AccountStore = accountStore, ConsentFile = consentFile, Time = time, Logs = logs });

    public void Dispose()
    {
        logs.Dispose();
        graph.Dispose();
    }
}
