using AStar.Dev.OneDrive.Client.Authentication;
using Microsoft.Identity.Client;
using NSubstitute.ExceptionExtensions;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Authentication;

[TestSubject(typeof(TokenManager))]
public class TokenManagerShould
{
    private readonly IMsalClient _msalClient = Substitute.For<IMsalClient>();
    private readonly TokenManager _sut;

    public TokenManagerShould() => _sut = new TokenManager(_msalClient);

    [Fact]
    public async Task ReturnCachedToken_WhenSilentAcquisitionSucceeds()
    {
        var account = Substitute.For<IAccount>();
        _msalClient.GetAccountsAsync().Returns(new[] { account }.AsEnumerable());
        _msalClient.AcquireTokenSilentAsync(
                       Arg.Any<IEnumerable<string>>(), account, Arg.Any<CancellationToken>())
                   .Returns("cached-access-token");

        var token = await _sut.AcquireAccessTokenAsync(["User.Read"], TestContext.Current.CancellationToken);

        token.ShouldBe("cached-access-token");
    }

    [Fact]
    public async Task FallBackToInteractiveAuthentication_WhenSilentAcquisitionRequiresUserInteraction()
    {
        var account = Substitute.For<IAccount>();
        _msalClient.GetAccountsAsync().Returns(new[] { account }.AsEnumerable());
        _msalClient.AcquireTokenSilentAsync(
                       Arg.Any<IEnumerable<string>>(), account, Arg.Any<CancellationToken>())
                   .ThrowsAsync(new MsalUiRequiredException("interaction_required", "Silent acquisition failed."));
        _msalClient.AcquireTokenInteractiveAsync(
                       Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
                   .Returns("interactive-access-token");

        var token = await _sut.AcquireAccessTokenAsync(["User.Read"], TestContext.Current.CancellationToken);

        token.ShouldBe("interactive-access-token");
    }

    [Fact]
    public async Task AttemptSilentAcquisitionFirst_BeforeFallingBackToInteractive()
    {
        var account = Substitute.For<IAccount>();
        _msalClient.GetAccountsAsync().Returns(new[] { account }.AsEnumerable());
        _msalClient.AcquireTokenSilentAsync(
                       Arg.Any<IEnumerable<string>>(), account, Arg.Any<CancellationToken>())
                   .ThrowsAsync(new MsalUiRequiredException("interaction_required", "Silent acquisition failed."));
        _msalClient.AcquireTokenInteractiveAsync(
                       Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
                   .Returns("interactive-access-token");

        await _sut.AcquireAccessTokenAsync(["User.Read"], TestContext.Current.CancellationToken);

        await _msalClient.Received(1)
                         .AcquireTokenSilentAsync(
                             Arg.Any<IEnumerable<string>>(), account, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PassNullAccount_WhenNoCachedAccountExists()
    {
        _msalClient.GetAccountsAsync().Returns(Enumerable.Empty<IAccount>());
        _msalClient.AcquireTokenSilentAsync(
                       Arg.Any<IEnumerable<string>>(), null, Arg.Any<CancellationToken>())
                   .Returns("token");

        await _sut.AcquireAccessTokenAsync(["User.Read"], TestContext.Current.CancellationToken);

        await _msalClient.Received(1)
                         .AcquireTokenSilentAsync(
                             Arg.Any<IEnumerable<string>>(), null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAllCachedAccounts_WhenSigningOut()
    {
        var account1 = Substitute.For<IAccount>();
        var account2 = Substitute.For<IAccount>();
        _msalClient.GetAccountsAsync().Returns(new[] { account1, account2 }.AsEnumerable());

        await _sut.SignOutAsync(TestContext.Current.CancellationToken);

        await _msalClient.Received(1).RemoveAccountAsync(account1);
        await _msalClient.Received(1).RemoveAccountAsync(account2);
    }
}
