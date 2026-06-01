using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Authentication;

public sealed class GivenAnAuthService
{
    private const string HomeAccountIdentifier = "obj.tenant";
    private const string ObjectId = "obj";
    private const string TenantId = "tenant";
    private const string Username = "user@outlook.com";
    private const string ClientId = "test-client-id";
    private const string RedirectUri = "http://localhost";
    private const string Authority = "https://login.microsoftonline.com/consumers";

    private static readonly IReadOnlyList<string> Scopes = ["Files.ReadWrite", "offline_access", "User.Read"];

    private static IOptions<EntraIdConfiguration> BuildOptions() =>
        Options.Create(new EntraIdConfiguration
        {
            ClientId = ClientId,
            RedirectUri = RedirectUri,
            Scopes = Scopes,
            AuthorityForMicrosoftAccountsOnly = Authority
        });

    private static IAccount BuildMockAccount(string homeAccountIdentifier, string username)
    {
        var mockAccount = Substitute.For<IAccount>();
        var msalAccountId = new Microsoft.Identity.Client.AccountId(homeAccountIdentifier, ObjectId, TenantId);
        mockAccount.HomeAccountId.Returns(msalAccountId);
        mockAccount.Username.Returns(username);

        return mockAccount;
    }

    private static AuthService BuildSut(IPublicClientApplication app, ITokenCacheService cacheService) =>
        new(app, cacheService, BuildOptions(), Substitute.For<ILogger<AuthService>>());

    [Fact]
    public async Task when_acquire_token_silent_is_called_with_no_cached_accounts_then_result_is_error()
    {
        var mockApp = Substitute.For<IPublicClientApplication>();
        mockApp.GetAccountsAsync().Returns(Task.FromResult(Enumerable.Empty<IAccount>()));
        var sut = BuildSut(mockApp, Substitute.For<ITokenCacheService>());

        var result = await sut.AcquireTokenSilentAsync(HomeAccountIdentifier, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<AuthResult, AuthError>.Error>();
    }

    [Fact]
    public async Task when_acquire_token_silent_is_called_with_no_cached_accounts_then_error_reason_is_re_auth_required()
    {
        var mockApp = Substitute.For<IPublicClientApplication>();
        mockApp.GetAccountsAsync().Returns(Task.FromResult(Enumerable.Empty<IAccount>()));
        var sut = BuildSut(mockApp, Substitute.For<ITokenCacheService>());

        var result = (Result<AuthResult, AuthError>.Error)await sut.AcquireTokenSilentAsync(HomeAccountIdentifier, TestContext.Current.CancellationToken);

        result.Reason.ShouldBeOfType<AuthReAuthRequiredError>();
    }

    [Fact]
    public async Task when_acquire_token_silent_is_called_with_no_cached_accounts_then_re_auth_error_code_is_no_account()
    {
        var mockApp = Substitute.For<IPublicClientApplication>();
        mockApp.GetAccountsAsync().Returns(Task.FromResult(Enumerable.Empty<IAccount>()));
        var sut = BuildSut(mockApp, Substitute.For<ITokenCacheService>());

        var result = (Result<AuthResult, AuthError>.Error)await sut.AcquireTokenSilentAsync(HomeAccountIdentifier, TestContext.Current.CancellationToken);
        var reAuthError = (AuthReAuthRequiredError)result.Reason;

        reAuthError.ErrorCode.ShouldBe("no_account");
    }

    [Fact]
    public async Task when_acquire_token_silent_matches_account_by_home_account_identifier_then_result_is_not_the_no_account_re_auth_error()
    {
        var mockApp = Substitute.For<IPublicClientApplication>();
        var mockAccount = BuildMockAccount(HomeAccountIdentifier, Username);
        mockApp.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        mockApp.When(app => app.AcquireTokenSilent(Arg.Any<IEnumerable<string>>(), Arg.Any<IAccount>()))
            .Throw(new MsalUiRequiredException("interaction_required", "User interaction required"));
        var sut = BuildSut(mockApp, Substitute.For<ITokenCacheService>());

        var result = (Result<AuthResult, AuthError>.Error)await sut.AcquireTokenSilentAsync(HomeAccountIdentifier, TestContext.Current.CancellationToken);
        var reAuthError = (AuthReAuthRequiredError)result.Reason;

        reAuthError.ErrorCode.ShouldNotBe("no_account");
    }

    [Fact]
    public async Task when_acquire_token_silent_matches_account_by_username_only_then_result_is_not_the_no_account_re_auth_error()
    {
        var mockApp = Substitute.For<IPublicClientApplication>();
        var mockAccount = BuildMockAccount("different-identifier.tenant", Username);
        mockApp.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        mockApp.When(app => app.AcquireTokenSilent(Arg.Any<IEnumerable<string>>(), Arg.Any<IAccount>()))
            .Throw(new MsalUiRequiredException("interaction_required", "User interaction required"));
        var sut = BuildSut(mockApp, Substitute.For<ITokenCacheService>());

        var result = (Result<AuthResult, AuthError>.Error)await sut.AcquireTokenSilentAsync(Username, TestContext.Current.CancellationToken);
        var reAuthError = (AuthReAuthRequiredError)result.Reason;

        reAuthError.ErrorCode.ShouldNotBe("no_account");
    }

    [Fact]
    public async Task when_msal_throws_ui_required_exception_then_result_is_error()
    {
        var mockApp = Substitute.For<IPublicClientApplication>();
        var mockAccount = BuildMockAccount(HomeAccountIdentifier, Username);
        mockApp.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        mockApp.When(app => app.AcquireTokenSilent(Arg.Any<IEnumerable<string>>(), Arg.Any<IAccount>()))
            .Throw(new MsalUiRequiredException("interaction_required", "User interaction required"));
        var sut = BuildSut(mockApp, Substitute.For<ITokenCacheService>());

        var result = await sut.AcquireTokenSilentAsync(HomeAccountIdentifier, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<AuthResult, AuthError>.Error>();
    }

    [Fact]
    public async Task when_msal_throws_ui_required_exception_then_error_reason_is_re_auth_required()
    {
        var mockApp = Substitute.For<IPublicClientApplication>();
        var mockAccount = BuildMockAccount(HomeAccountIdentifier, Username);
        mockApp.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        mockApp.When(app => app.AcquireTokenSilent(Arg.Any<IEnumerable<string>>(), Arg.Any<IAccount>()))
            .Throw(new MsalUiRequiredException("interaction_required", "User interaction required"));
        var sut = BuildSut(mockApp, Substitute.For<ITokenCacheService>());

        var result = (Result<AuthResult, AuthError>.Error)await sut.AcquireTokenSilentAsync(HomeAccountIdentifier, TestContext.Current.CancellationToken);

        result.Reason.ShouldBeOfType<AuthReAuthRequiredError>();
    }

    [Fact]
    public async Task when_msal_throws_ui_required_exception_then_re_auth_error_carries_the_msal_error_code()
    {
        var mockApp = Substitute.For<IPublicClientApplication>();
        var mockAccount = BuildMockAccount(HomeAccountIdentifier, Username);
        mockApp.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        mockApp.When(app => app.AcquireTokenSilent(Arg.Any<IEnumerable<string>>(), Arg.Any<IAccount>()))
            .Throw(new MsalUiRequiredException("interaction_required", "User interaction required"));
        var sut = BuildSut(mockApp, Substitute.For<ITokenCacheService>());

        var result = (Result<AuthResult, AuthError>.Error)await sut.AcquireTokenSilentAsync(HomeAccountIdentifier, TestContext.Current.CancellationToken);
        var reAuthError = (AuthReAuthRequiredError)result.Reason;

        reAuthError.ErrorCode.ShouldBe("interaction_required");
    }

    [Fact]
    public async Task when_operation_is_cancelled_then_result_is_error()
    {
        var mockApp = Substitute.For<IPublicClientApplication>();
        var mockAccount = BuildMockAccount(HomeAccountIdentifier, Username);
        mockApp.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        mockApp.When(app => app.AcquireTokenSilent(Arg.Any<IEnumerable<string>>(), Arg.Any<IAccount>()))
            .Throw(new OperationCanceledException());
        var sut = BuildSut(mockApp, Substitute.For<ITokenCacheService>());

        var result = await sut.AcquireTokenSilentAsync(HomeAccountIdentifier, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<AuthResult, AuthError>.Error>();
    }

    [Fact]
    public async Task when_operation_is_cancelled_then_error_reason_is_auth_cancelled_error()
    {
        var mockApp = Substitute.For<IPublicClientApplication>();
        var mockAccount = BuildMockAccount(HomeAccountIdentifier, Username);
        mockApp.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        mockApp.When(app => app.AcquireTokenSilent(Arg.Any<IEnumerable<string>>(), Arg.Any<IAccount>()))
            .Throw(new OperationCanceledException());
        var sut = BuildSut(mockApp, Substitute.For<ITokenCacheService>());

        var result = (Result<AuthResult, AuthError>.Error)await sut.AcquireTokenSilentAsync(HomeAccountIdentifier, TestContext.Current.CancellationToken);

        result.Reason.ShouldBeOfType<AuthCancelledError>();
    }
}
