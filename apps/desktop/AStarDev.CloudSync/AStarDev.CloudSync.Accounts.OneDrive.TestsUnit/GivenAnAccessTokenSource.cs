using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Identity.Client;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public sealed class GivenAnAccessTokenSource : IDisposable
{
    private readonly RecordingLoggerProvider logs = new();
    private readonly InMemoryAccountStore accountStore = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_stored_account_has_a_cached_token_then_the_token_is_acquired()
    {
        var accountId = await AddStoredAccountAsync();

        var outcome = await CreateModule(new FakeMsalClient()).TokenSource.GetAccessTokenAsync(accountId, CancellationToken);

        outcome.ShouldBe(AccessTokenOutcomeFactory.CreateAcquired(TestSignIns.AccessToken));
    }

    [Fact]
    public async Task when_a_token_is_requested_then_msal_is_asked_for_the_stored_accounts_key()
    {
        var accountId = await AddStoredAccountAsync();
        var msal = new FakeMsalClient();

        await CreateModule(msal).TokenSource.GetAccessTokenAsync(accountId, CancellationToken);

        msal.SilentTokenKeys.Select(key => key.Value).ShouldBe([TestSignIns.AccountKey]);
    }

    [Fact]
    public async Task when_the_token_cache_holds_the_key_in_another_casing_then_msal_is_asked_for_the_cached_casing()
    {
        var accountId = await AddStoredAccountAsync();
        var msal = new FakeMsalClient { CachedAccountKeys = [TestSignIns.AccountKey.ToUpperInvariant()] };

        await CreateModule(msal).TokenSource.GetAccessTokenAsync(accountId, CancellationToken);

        msal.SilentTokenKeys.Select(key => key.Value).ShouldBe([TestSignIns.AccountKey.ToUpperInvariant()]);
    }

    [Fact]
    public async Task when_no_stored_account_has_the_id_then_the_account_is_unknown_and_msal_is_not_asked()
    {
        await AddStoredAccountAsync();
        var msal = new FakeMsalClient();

        var outcome = await CreateModule(msal).TokenSource.GetAccessTokenAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);

        outcome.ShouldBe(AccessTokenOutcomeFactory.CreateUnknownAccount());
        msal.SilentTokenKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_token_cache_has_no_such_account_then_the_account_is_not_cached_rather_than_needing_sign_in()
    {
        var accountId = await AddStoredAccountAsync();
        var msal = new FakeMsalClient { SilentToken = (_, _) => Task.FromResult(Option.None<string>()) };

        var outcome = await CreateModule(msal).TokenSource.GetAccessTokenAsync(accountId, CancellationToken);

        outcome.ShouldBe(AccessTokenOutcomeFactory.CreateAccountNotCached());
    }

    [Fact]
    public async Task when_msal_needs_the_user_to_sign_in_again_then_sign_in_is_required()
    {
        var accountId = await AddStoredAccountAsync();
        var msal = new FakeMsalClient { SilentToken = (_, _) => throw new MsalUiRequiredException(MsalError.InvalidGrantError, "The refresh token has expired.") };

        var outcome = await CreateModule(msal).TokenSource.GetAccessTokenAsync(accountId, CancellationToken);

        outcome.ShouldBe(AccessTokenOutcomeFactory.CreateSignInRequired());
    }

    [Fact]
    public async Task when_msal_fails_then_the_token_is_unavailable_and_an_error_is_logged()
    {
        var accountId = await AddStoredAccountAsync();
        var msal = new FakeMsalClient { SilentToken = (_, _) => throw new MsalServiceException("service_unavailable", "Graph is down.") };

        var outcome = await CreateModule(msal).TokenSource.GetAccessTokenAsync(accountId, CancellationToken);

        outcome.ShouldBe(AccessTokenOutcomeFactory.CreateUnavailable());
        logs.Logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error && entry.Exception == null);
    }

    [Fact]
    public async Task when_msal_fails_with_a_message_holding_personal_data_then_the_logs_do_not_contain_it()
    {
        var accountId = await AddStoredAccountAsync();
        var msal = new FakeMsalClient { SilentToken = (_, _) => throw new MsalServiceException("invalid_request", $"Rejected {TestSignIns.Email} with {TestSignIns.AccessToken} for {TestSignIns.AccountKey}") };

        await CreateModule(msal).TokenSource.GetAccessTokenAsync(accountId, CancellationToken);

        logs.Logger.Messages.ShouldNotBeEmpty();
        logs.Logger.Messages.ShouldAllBe(message => !message.Contains(TestSignIns.Email) && !message.Contains(TestSignIns.AccessToken) && !message.Contains(TestSignIns.AccountKey));
    }

    [Fact]
    public async Task when_the_request_times_out_without_the_caller_cancelling_then_the_token_is_unavailable()
    {
        var accountId = await AddStoredAccountAsync();
        var msal = new FakeMsalClient { SilentToken = (_, _) => throw new TaskCanceledException("The request timed out.") };

        var outcome = await CreateModule(msal).TokenSource.GetAccessTokenAsync(accountId, CancellationToken);

        outcome.ShouldBe(AccessTokenOutcomeFactory.CreateUnavailable());
    }

    [Fact]
    public async Task when_the_caller_cancels_then_the_request_is_cancelled()
    {
        var accountId = await AddStoredAccountAsync();
        using var cancellation = new CancellationTokenSource();
        var msal = new FakeMsalClient
        {
            SilentToken = async (_, token) =>
            {
                await cancellation.CancelAsync();
                token.ThrowIfCancellationRequested();

                return Option.Some(TestSignIns.AccessToken);
            },
        };

        await Should.ThrowAsync<OperationCanceledException>(() => CreateModule(msal).TokenSource.GetAccessTokenAsync(accountId, cancellation.Token));
    }

    [Fact]
    public async Task when_the_accounts_cannot_be_read_then_the_token_is_unavailable()
    {
        var failingStore = new FakeAccountStore { All = () => new ErrorResponse("The accounts could not be read.") };

        var outcome = await new OneDriveModule(new OneDriveFakes { Msal = new FakeMsalClient(), AccountStore = failingStore, Logs = logs }).TokenSource.GetAccessTokenAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);

        outcome.ShouldBe(AccessTokenOutcomeFactory.CreateUnavailable());
    }

    [Fact]
    public async Task when_the_token_cache_cannot_be_connected_then_the_token_cache_is_unavailable_and_msal_is_not_asked()
    {
        var accountId = await AddStoredAccountAsync();
        var msal = new FakeMsalClient();
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true, PlaintextFileFails = true };

        var outcome = await new OneDriveModule(new OneDriveFakes { Msal = msal, Storage = storage, AccountStore = accountStore, Logs = logs }).TokenSource.GetAccessTokenAsync(accountId, CancellationToken);

        outcome.ShouldBe(AccessTokenOutcomeFactory.CreateTokenCacheUnavailable());
        msal.SilentTokenKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_token_cache_needs_plaintext_consent_then_the_user_is_not_asked_and_the_token_cache_is_unavailable()
    {
        var accountId = await AddStoredAccountAsync();
        var consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted() };
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true };

        var outcome = await new OneDriveModule(new OneDriveFakes { Msal = new FakeMsalClient(), Storage = storage, Consent = consent, AccountStore = accountStore, Logs = logs }).TokenSource.GetAccessTokenAsync(accountId, CancellationToken);

        outcome.ShouldBe(AccessTokenOutcomeFactory.CreateTokenCacheUnavailable());
        consent.QuestionsAsked.ShouldBe(0);
    }

    [Fact]
    public async Task when_an_account_is_added_while_a_token_request_is_connecting_the_cache_then_the_user_is_still_asked_for_consent()
    {
        var consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted() };
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true, AttachDelay = TimeSpan.FromMilliseconds(200) };
        var module = new OneDriveModule(new OneDriveFakes { Msal = new FakeMsalClient(), Storage = storage, Consent = consent, Logs = logs });

        var tokenRequest = module.TokenSource.GetAccessTokenAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);
        var added = await module.AddAsync(CancellationToken);
        await tokenRequest;

        added.ShouldBeOfType<AccountAddOutcome.Added>();
        consent.QuestionsAsked.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_caller_cancels_and_msal_reports_it_as_a_failure_then_the_request_is_cancelled()
    {
        var accountId = await AddStoredAccountAsync();
        using var cancellation = new CancellationTokenSource();
        var msal = new FakeMsalClient
        {
            SilentToken = async (_, _) =>
            {
                await cancellation.CancelAsync();

                throw new MsalClientException(MsalError.AuthenticationCanceledError, "The request was cancelled.");
            },
        };

        await Should.ThrowAsync<OperationCanceledException>(() => CreateModule(msal).TokenSource.GetAccessTokenAsync(accountId, cancellation.Token));
    }

    [Fact]
    public async Task when_many_tokens_are_requested_at_once_then_each_is_acquired()
    {
        var accountId = await AddStoredAccountAsync();
        var tokenSource = CreateModule(new FakeMsalClient()).TokenSource;

        var outcomes = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => tokenSource.GetAccessTokenAsync(accountId, CancellationToken), CancellationToken)));

        outcomes.ShouldAllBe(outcome => outcome is AccessTokenOutcome.Acquired);
    }

    [Fact]
    public void when_an_acquired_outcome_is_printed_then_the_token_is_redacted()
    {
        var outcome = AccessTokenOutcomeFactory.CreateAcquired(TestSignIns.AccessToken);

        outcome.ToString().ShouldNotContain(TestSignIns.AccessToken);
    }

    [Fact]
    public void when_an_acquired_outcome_is_created_without_a_token_then_the_token_is_empty()
    {
        var outcome = AccessTokenOutcomeFactory.CreateAcquired(null);

        outcome.ShouldBeOfType<AccessTokenOutcome.Acquired>().Token.ShouldBeEmpty();
    }

    public void Dispose() => logs.Dispose();

    private async Task<AccountId> AddStoredAccountAsync()
    {
        var key = ProviderAccountKeyFactory.Create(TestSignIns.AccountKey).Match(value => value, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(key, CloudProviderFactory.CreateOneDrive(), TestSignIns.DisplayName, TestSignIns.Email, TestSignIns.DriveId, TestSignIns.Storage);

        return (await accountStore.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
    }

    private OneDriveModule CreateModule(FakeMsalClient msal) => new(new OneDriveFakes { Msal = msal, AccountStore = accountStore, Logs = logs });
}
