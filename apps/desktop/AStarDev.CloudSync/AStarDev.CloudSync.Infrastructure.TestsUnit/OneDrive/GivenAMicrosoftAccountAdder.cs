using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.CloudSync.Infrastructure.OneDrive;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Identity.Client;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public class GivenAMicrosoftAccountAdder
{
    private readonly RecordingLogger<MicrosoftAccountAdder> logger = new();
    private FakeMsalTokenCacheStorage storage = new();
    private FakeTokenCacheConsent consent = new();
    private FakeMsalClient msal = new();
    private FakeDriveLookup driveLookup = new();
    private FakeAccountStore accountStore = new();
    private InMemoryPlaintextCacheConsentStore consentStore = new();

    [Fact]
    public async Task when_a_new_account_signs_in_then_it_is_saved_with_its_profile_drive_and_quota_and_added()
    {
        var outcome = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateAdded(TestSignIns.AccountId));
        accountStore.SavedRegistrations.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            registration => registration.Key.Value.ShouldBe(TestSignIns.AccountKey),
            registration => registration.Provider.ShouldBe(CloudProviderFactory.CreateOneDrive()),
            registration => registration.Email.ShouldBe(TestSignIns.Email),
            registration => registration.DisplayName.ShouldBe(TestSignIns.DisplayName),
            registration => registration.DriveId.ShouldBe(TestSignIns.Drive.DriveId),
            registration => registration.Storage.ShouldBe(TestSignIns.Drive.Storage));
    }

    [Fact]
    public async Task when_the_account_signs_in_then_the_drive_is_read_with_its_access_token()
    {
        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        driveLookup.ReceivedTokens.ShouldBe([TestSignIns.AccessToken]);
    }

    [Fact]
    public async Task when_the_account_was_already_added_then_the_outcome_is_re_authenticated()
    {
        accountStore = new FakeAccountStore { Save = (_, _) => Task.FromResult(AccountAddOutcomeFactory.CreateReAuthenticated(TestSignIns.AccountId)) };

        var outcome = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateReAuthenticated(TestSignIns.AccountId));
        msal.RemovedKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_token_cache_is_unavailable_and_the_user_declines_then_the_add_fails_before_sign_in()
    {
        storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateDeclined() };

        var outcome = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateTokenCacheUnavailable()));
        msal.SignInAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_user_cancels_during_sign_in_then_the_outcome_is_cancelled_and_nothing_is_saved()
    {
        using var cancellation = new CancellationTokenSource();
        msal = new FakeMsalClient { SignIn = async token => { await cancellation.CancelAsync(); token.ThrowIfCancellationRequested(); return TestSignIns.NewAccount; } };

        var outcome = await CreateAdder().AddAsync(cancellation.Token);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateCancelled());
        accountStore.SavedRegistrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_attempt_is_cancelled_before_it_starts_then_the_outcome_is_cancelled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var outcome = await CreateAdder().AddAsync(cancellation.Token);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateCancelled());
        msal.SignInAttempts.ShouldBe(0);
    }

    public static TheoryData<Exception, AccountAddOutcome> SignInExceptionsAndOutcomes() => new()
    {
        { new MsalClientException(MsalError.AuthenticationCanceledError, "The user closed the browser."), AccountAddOutcomeFactory.CreateCancelled() },
        { new MsalClientException(MsalError.LinuxXdgOpen, "xdg-open failed."), AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateBrowserUnavailable()) },
        { new MsalServiceException("access_denied", "The user denied consent."), AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateAuthRejected()) },
        { new MsalClientException(MsalError.RequestTimeout, "The request timed out."), AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork()) },
        { new MsalServiceException("service_unavailable", "Could not reach the service.", new HttpRequestException("No route to host")), AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork()) },
        { new HttpRequestException("No route to host"), AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork()) },
        { new InvalidOperationException("Something unexpected."), AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateAuthRejected()) },
    };

    [Theory]
    [MemberData(nameof(SignInExceptionsAndOutcomes), DisableDiscoveryEnumeration = true)]
    public async Task when_sign_in_throws_then_the_exception_becomes_the_matching_outcome(Exception exception, AccountAddOutcome expectedOutcome)
    {
        msal = new FakeMsalClient { SignIn = _ => Task.FromException<MicrosoftSignIn>(exception) };

        var outcome = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        outcome.ShouldBe(expectedOutcome);
        accountStore.SavedRegistrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_sign_in_returns_no_account_key_then_the_add_fails_as_auth_rejected()
    {
        msal = new FakeMsalClient { SignIn = _ => Task.FromResult(MicrosoftSignInFactory.Create(" ", TestSignIns.Email, TestSignIns.DisplayName, TestSignIns.AccessToken)) };

        var outcome = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateAuthRejected()));
    }

    [Fact]
    public async Task when_the_drive_lookup_fails_for_a_new_account_then_the_msal_account_is_removed_and_nothing_is_saved()
    {
        driveLookup = new FakeDriveLookup { Lookup = _ => Task.FromResult(Result.Failure<MicrosoftDrive, AccountAddFailure>(AccountAddFailureFactory.CreateDriveUnavailable())) };

        var outcome = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateDriveUnavailable()));
        msal.RemovedKeys.ShouldHaveSingleItem().Value.ShouldBe(TestSignIns.AccountKey);
        accountStore.SavedRegistrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_save_fails_for_a_new_account_then_the_msal_account_is_removed()
    {
        accountStore = new FakeAccountStore { Save = (_, _) => Task.FromResult(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreatePersistFailed())) };

        var outcome = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreatePersistFailed()));
        msal.RemovedKeys.ShouldHaveSingleItem().Value.ShouldBe(TestSignIns.AccountKey);
    }

    [Fact]
    public async Task when_the_save_fails_for_an_account_already_in_the_token_cache_then_the_msal_account_is_kept()
    {
        msal = new FakeMsalClient { CachedAccountKeys = [TestSignIns.AccountKey.ToUpperInvariant()] };
        accountStore = new FakeAccountStore { Save = (_, _) => Task.FromResult(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreatePersistFailed())) };

        var outcome = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreatePersistFailed()));
        msal.RemovedKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_user_cancels_during_the_drive_lookup_of_a_new_account_then_the_outcome_is_cancelled_and_the_msal_account_is_removed()
    {
        using var cancellation = new CancellationTokenSource();
        driveLookup = new FakeDriveLookup { Lookup = async token => { await cancellation.CancelAsync(); token.ThrowIfCancellationRequested(); return Result.Success<MicrosoftDrive, AccountAddFailure>(TestSignIns.Drive); } };

        var outcome = await CreateAdder().AddAsync(cancellation.Token);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateCancelled());
        msal.RemovedKeys.ShouldHaveSingleItem().Value.ShouldBe(TestSignIns.AccountKey);
        accountStore.SavedRegistrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_sign_in_times_out_without_the_user_cancelling_then_the_failure_is_network()
    {
        msal = new FakeMsalClient { SignIn = async _ => { await Task.Yield(); throw new TaskCanceledException("The request timed out."); } };

        var outcome = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork()));
    }

    [Fact]
    public async Task when_the_drive_lookup_times_out_without_the_user_cancelling_then_the_failure_is_network_and_the_new_msal_account_is_removed()
    {
        driveLookup = new FakeDriveLookup { Lookup = async _ => { await Task.Yield(); throw new TaskCanceledException("The request timed out."); } };

        var outcome = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork()));
        msal.RemovedKeys.ShouldHaveSingleItem().Value.ShouldBe(TestSignIns.AccountKey);
    }

    [Fact]
    public async Task when_the_user_cancels_during_the_save_of_a_new_account_then_the_outcome_is_cancelled_and_the_msal_account_is_removed()
    {
        using var cancellation = new CancellationTokenSource();
        accountStore = new FakeAccountStore { Save = async (_, token) => { await cancellation.CancelAsync(); token.ThrowIfCancellationRequested(); return AccountAddOutcomeFactory.CreateAdded(TestSignIns.AccountId); } };

        var outcome = await CreateAdder().AddAsync(cancellation.Token);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateCancelled());
        msal.RemovedKeys.ShouldHaveSingleItem().Value.ShouldBe(TestSignIns.AccountKey);
    }

    [Fact]
    public async Task when_the_consent_question_fails_then_the_add_fails_with_token_cache_unavailable_instead_of_throwing()
    {
        storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };
        consent = new FakeTokenCacheConsent { Fails = true };

        var outcome = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        outcome.ShouldBe(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateTokenCacheUnavailable()));
    }

    [Fact]
    public async Task when_the_save_fails_for_an_account_that_is_stored_but_not_in_the_token_cache_then_the_msal_account_is_removed()
    {
        accountStore = new FakeAccountStore { Save = (_, _) => Task.FromResult(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreatePersistFailed())) };

        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        msal.RemovedKeys.ShouldHaveSingleItem().Value.ShouldBe(TestSignIns.AccountKey);
    }

    [Fact]
    public async Task when_the_add_fails_then_the_log_contains_no_email_name_or_token()
    {
        msal = new FakeMsalClient { SignIn = _ => Task.FromException<MicrosoftSignIn>(new MsalServiceException("invalid_grant", $"AADSTS50126: {TestSignIns.Email} could not sign in.")) };

        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        logger.Messages.ShouldNotBeEmpty();
        logger.Messages.ShouldAllBe(message => !message.Contains(TestSignIns.Email) && !message.Contains(TestSignIns.DisplayName) && !message.Contains(TestSignIns.AccessToken));
        logger.Entries.ShouldAllBe(entry => entry.Exception == null);
    }

    [Fact]
    public async Task when_the_save_fails_then_the_log_contains_no_account_key()
    {
        accountStore = new FakeAccountStore { Save = (_, _) => Task.FromResult(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreatePersistFailed())) };

        _ = await CreateAdder().AddAsync(TestContext.Current.CancellationToken);

        logger.Messages.ShouldAllBe(message => !message.Contains(TestSignIns.AccountKey, StringComparison.OrdinalIgnoreCase));
    }

    private MicrosoftAccountAdder CreateAdder() => new(new TokenCacheInitialiser(storage, consent, consentStore, new RecordingLogger<TokenCacheInitialiser>()), msal, driveLookup, accountStore, logger);
}
