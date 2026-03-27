using AStar.Dev.OneDrive.Client.Authentication;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using NSubstitute.ExceptionExtensions;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Authentication;

[TestSubject(typeof(TokenCacheInitializer))]
public class TokenCacheInitializerShould
{
    private readonly IConsentStore _consentStore = Substitute.For<IConsentStore>();
    private readonly IConsentPrompt _consentPrompt = Substitute.For<IConsentPrompt>();
    private readonly IPublicClientApplication _app = Substitute.For<IPublicClientApplication>();
    private readonly ITokenCache _tokenCache = Substitute.For<ITokenCache>();

    private readonly AuthenticationOptions _options = new() { ClientId = "test-client-id" };

    public TokenCacheInitializerShould()
        => _app.UserTokenCache.Returns(_tokenCache);

    [Fact]
    public async Task SkipConsentPrompt_WhenKeychainCacheRegistersSuccessfully()
    {
        Func<StorageCreationProperties, ITokenCache, Task> successfulKeychain =
            (_, _) => Task.CompletedTask;

        var sut = new TokenCacheInitializer(_consentStore, _consentPrompt, successfulKeychain);

        await sut.InitializeAsync(_app, _options, "account-123", TestContext.Current.CancellationToken);

        await _consentPrompt.DidNotReceive()
                            .RequestConsentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestConsent_WhenKeychainUnavailableAndNoConsentRecorded()
    {
        Func<StorageCreationProperties, ITokenCache, Task> failingKeychain =
            (_, _) => Task.FromException(new InvalidOperationException("Keychain unavailable."));
        _consentStore.HasConsented("account-123").Returns(false);
        _consentPrompt.RequestConsentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var sut = new TokenCacheInitializer(_consentStore, _consentPrompt, failingKeychain);

        await sut.InitializeAsync(_app, _options, "account-123", TestContext.Current.CancellationToken);

        await _consentPrompt.Received(1)
                            .RequestConsentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipConsentPrompt_WhenConsentAlreadyGrantedForAccount()
    {
        Func<StorageCreationProperties, ITokenCache, Task> failingKeychain =
            (_, _) => Task.FromException(new InvalidOperationException("Keychain unavailable."));
        _consentStore.HasConsented("account-123").Returns(true);

        var sut = new TokenCacheInitializer(_consentStore, _consentPrompt, failingKeychain);

        await sut.InitializeAsync(_app, _options, "account-123", TestContext.Current.CancellationToken);

        await _consentPrompt.DidNotReceive()
                            .RequestConsentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PersistConsentDecision_WhenUserRespondsToPrompt()
    {
        Func<StorageCreationProperties, ITokenCache, Task> failingKeychain =
            (_, _) => Task.FromException(new InvalidOperationException("Keychain unavailable."));
        _consentStore.HasConsented("account-123").Returns(false);
        _consentPrompt.RequestConsentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var sut = new TokenCacheInitializer(_consentStore, _consentPrompt, failingKeychain);

        await sut.InitializeAsync(_app, _options, "account-123", TestContext.Current.CancellationToken);

        _consentStore.Received(1).RecordConsent("account-123", true);
    }

    [Fact]
    public async Task ThrowInvalidOperationException_WhenUserDeniesConsentForInsecureFallback()
    {
        Func<StorageCreationProperties, ITokenCache, Task> failingKeychain =
            (_, _) => Task.FromException(new InvalidOperationException("Keychain unavailable."));
        _consentStore.HasConsented("account-123").Returns(false);
        _consentPrompt.RequestConsentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var sut = new TokenCacheInitializer(_consentStore, _consentPrompt, failingKeychain);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.InitializeAsync(_app, _options, "account-123", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UseSystemAdminAccountId_WhenNoAccountIdProvided()
    {
        Func<StorageCreationProperties, ITokenCache, Task> failingKeychain =
            (_, _) => Task.FromException(new InvalidOperationException("Keychain unavailable."));
        _consentStore.HasConsented(AuthenticationOptions.SystemAdminAccountId).Returns(false);
        _consentPrompt.RequestConsentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var sut = new TokenCacheInitializer(_consentStore, _consentPrompt, failingKeychain);

        await sut.InitializeAsync(_app, _options, cancellationToken: TestContext.Current.CancellationToken);

        _consentStore.Received(1).RecordConsent(AuthenticationOptions.SystemAdminAccountId, true);
    }

    [Fact]
    public async Task RegisterInsecureCacheCallbacks_WhenConsentGrantedAndKeychainUnavailable()
    {
        Func<StorageCreationProperties, ITokenCache, Task> failingKeychain =
            (_, _) => Task.FromException(new InvalidOperationException("Keychain unavailable."));
        _consentStore.HasConsented("account-123").Returns(true);

        var sut = new TokenCacheInitializer(_consentStore, _consentPrompt, failingKeychain);

        await sut.InitializeAsync(_app, _options, "account-123", TestContext.Current.CancellationToken);

        _tokenCache.Received(1).SetBeforeAccess(Arg.Any<TokenCacheCallback>());
        _tokenCache.Received(1).SetAfterAccess(Arg.Any<TokenCacheCallback>());
    }
}
