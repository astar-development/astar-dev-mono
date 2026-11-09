using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Identity.Client;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public sealed class GivenTheMicrosoftAccountActions : IDisposable
{
    private const string OtherAccountKey = "00000000-0000-0000-9999-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad";

    private readonly RecordingLoggerProvider logs = new();
    private readonly InMemoryAccountStore accountStore = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_same_account_signs_in_again_then_reauthentication_succeeds_with_the_stored_key_and_email_as_hints()
    {
        var accountId = await AddAsync(TestSignIns.AccountKey);
        var hints = new List<(string Key, string LoginHint)>();
        var msal = new FakeMsalClient { Reauthenticate = (key, hint, _) => { hints.Add((key.Value, hint)); return Task.FromResult(TestSignIns.NewAccount); } };

        var result = await Module(msal).Reauthenticator.ReauthenticateAsync(accountId, CancellationToken);

        result.Match(_ => (object)"signed-in", error => error).ShouldBe("signed-in");
        hints.ShouldBe([(TestSignIns.AccountKey, TestSignIns.Email)]);
    }

    [Fact]
    public async Task when_the_same_account_signs_in_with_different_casing_then_it_is_the_same_account()
    {
        var accountId = await AddAsync(TestSignIns.AccountKey.ToUpperInvariant());

        var result = await Module(new FakeMsalClient()).Reauthenticator.ReauthenticateAsync(accountId, CancellationToken);

        result.Match(_ => (object)"signed-in", error => error).ShouldBe("signed-in");
    }

    [Fact]
    public async Task when_a_different_unknown_account_signs_in_then_it_is_rejected_and_its_tokens_are_removed()
    {
        var accountId = await AddAsync(OtherAccountKey);
        var msal = new FakeMsalClient();

        var result = await Module(msal).Reauthenticator.ReauthenticateAsync(accountId, CancellationToken);

        result.Match(_ => (object)"signed-in", error => error).ShouldBe(ReauthFailureFactory.CreateDifferentAccount());
        msal.RemovedKeys.Select(key => key.Value).ShouldBe([TestSignIns.AccountKey]);
        (await accountStore.GetAllAsync(CancellationToken)).Match(accounts => accounts.Single().Registration.Key.Value, _ => string.Empty).ShouldBe(OtherAccountKey);
    }

    [Fact]
    public async Task when_a_different_account_that_is_also_stored_signs_in_then_it_is_rejected_and_its_tokens_are_kept()
    {
        var accountId = await AddAsync(OtherAccountKey);
        _ = await AddAsync(TestSignIns.AccountKey);
        var msal = new FakeMsalClient();

        var result = await Module(msal).Reauthenticator.ReauthenticateAsync(accountId, CancellationToken);

        result.Match(_ => (object)"signed-in", error => error).ShouldBe(ReauthFailureFactory.CreateDifferentAccount());
        msal.RemovedKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_user_closes_the_sign_in_window_then_it_is_cancelled()
    {
        var accountId = await AddAsync(TestSignIns.AccountKey);
        var msal = new FakeMsalClient { Reauthenticate = (_, _, _) => throw new MsalClientException(MsalError.AuthenticationCanceledError, "Cancelled by the user.") };

        var result = await Module(msal).Reauthenticator.ReauthenticateAsync(accountId, CancellationToken);

        result.Match(_ => (object)"signed-in", error => error).ShouldBe(ReauthFailureFactory.CreateCancelled());
    }

    [Theory]
    [InlineData("service", "Transient")]
    [InlineData("other", "Unexpected")]
    public async Task when_the_sign_in_fails_then_the_provider_error_is_reported_and_logged_without_personal_data(string failure, string expected)
    {
        var accountId = await AddAsync(TestSignIns.AccountKey);
        Exception thrown = failure == "service" ? new MsalServiceException("temporarily_unavailable", $"Down for {TestSignIns.Email}") : new InvalidOperationException($"Broken for {TestSignIns.Email}");
        var msal = new FakeMsalClient { Reauthenticate = (_, _, _) => throw thrown };

        var result = await Module(msal).Reauthenticator.ReauthenticateAsync(accountId, CancellationToken);

        result.Match(_ => "signed-in", error => error.ShouldBeOfType<ReauthFailure.Provider>().Error.GetType().Name).ShouldBe(expected);
        logs.Logger.Messages.ShouldAllBe(message => !message.Contains(TestSignIns.Email, StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_the_account_to_reauthenticate_is_unknown_then_it_is_not_found()
    {
        var result = await Module(new FakeMsalClient()).Reauthenticator.ReauthenticateAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);

        result.Match(_ => (object)"signed-in", error => error).ShouldBe(ReauthFailureFactory.CreateProvider(ProviderErrorFactory.CreateNotFound()));
    }

    [Fact]
    public async Task when_the_token_cache_cannot_be_connected_then_reauthentication_fails_without_a_sign_in_window()
    {
        var accountId = await AddAsync(TestSignIns.AccountKey);
        var opened = 0;
        var msal = new FakeMsalClient { Reauthenticate = (_, _, _) => { opened++; return Task.FromResult(TestSignIns.NewAccount); } };
        var storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true, PlaintextFileFails = true };

        var result = await new OneDriveModule(new OneDriveFakes { Msal = msal, Storage = storage, AccountStore = accountStore, Logs = logs }).Reauthenticator.ReauthenticateAsync(accountId, CancellationToken);

        (result.Match(_ => "signed-in", error => error.GetType().Name), opened).ShouldBe(("Provider", 0));
    }

    [Fact]
    public async Task when_the_accounts_cannot_be_read_then_reauthentication_and_sign_out_are_transient()
    {
        var failing = new FakeAccountStore { All = () => new ErrorResponse("Database locked.") };
        var module = new OneDriveModule(new OneDriveFakes { Msal = new FakeMsalClient(), AccountStore = failing, Logs = logs });

        var reauth = await module.Reauthenticator.ReauthenticateAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);
        var signOut = await module.SignOut.SignOutAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);

        (reauth.Match(_ => (object)"ok", error => error), signOut.Match(_ => (object)"ok", error => error)).ShouldBe((ReauthFailureFactory.CreateProvider(ProviderErrorFactory.CreateTransient()), ProviderErrorFactory.CreateTransient()));
    }

    [Fact]
    public async Task when_a_stored_account_signs_out_then_its_tokens_are_removed()
    {
        var accountId = await AddAsync(TestSignIns.AccountKey);
        var msal = new FakeMsalClient();

        var result = await Module(msal).SignOut.SignOutAsync(accountId, CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
        msal.RemovedKeys.Select(key => key.Value).ShouldBe([TestSignIns.AccountKey]);
    }

    [Fact]
    public async Task when_an_unknown_account_signs_out_then_it_succeeds_and_nothing_is_removed()
    {
        var msal = new FakeMsalClient();

        var result = await Module(msal).SignOut.SignOutAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);

        (result.Match(_ => true, _ => false), msal.RemovedKeys.Count).ShouldBe((true, 0));
    }

    [Theory]
    [InlineData("acquired", "SignedIn")]
    [InlineData("ui-required", "SignInRequired")]
    [InlineData("network", "none")]
    [InlineData("no-cached-account", "SignInRequired")]
    public async Task when_the_sign_in_is_probed_then_only_ui_required_or_a_missing_cached_account_needs_sign_in(string silent, string expected)
    {
        var accountId = await AddAsync(TestSignIns.AccountKey);
        var msal = new FakeMsalClient
        {
            SilentToken = (_, _) => silent switch
            {
                "acquired" => Task.FromResult(Option.Some(TestSignIns.AccessToken)),
                "ui-required" => throw new MsalUiRequiredException(MsalError.InvalidGrantError, "Expired."),
                "network" => throw new HttpRequestException("Offline."),
                _ => Task.FromResult(Option.None<string>()),
            },
        };

        var state = await Module(msal).SignInProbe.ProbeAsync(accountId, CancellationToken);

        state.Match(found => found.GetType().Name, () => "none").ShouldBe(expected);
    }

    public void Dispose() => logs.Dispose();

    private OneDriveModule Module(FakeMsalClient msal) => new(new OneDriveFakes { Msal = msal, AccountStore = accountStore, Logs = logs });

    private async Task<AccountId> AddAsync(string key)
    {
        var providerKey = ProviderAccountKeyFactory.Create(key).Match(value => value, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(providerKey, CloudProviderFactory.CreateOneDrive(), TestSignIns.DisplayName, TestSignIns.Email, TestSignIns.DriveId, TestSignIns.Storage);

        return (await accountStore.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
    }
}
