using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Graph;

public sealed class GivenAQuotaRefreshService
{
    private readonly IGraphService _graphService = Substitute.For<IGraphService>();
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ILogger<QuotaRefreshService> _logger = Substitute.For<ILogger<QuotaRefreshService>>();

    private QuotaRefreshService CreateSut() => new(_graphService, _authService, _accountRepository, _logger);

    private static OneDriveAccount BuildAccount(string id = "acc-1") => new() { Id = new AccountId(id) };

    private static Result<AuthResult, AuthError> BuildSuccessAuthResult(string token = "access-token")
        => AuthResultFactory.Success(token, "acc-1", AccountProfileFactory.Create("Test", "test@test.com"));

    [Fact]
    public async Task when_auth_succeeds_and_quota_fetched_then_account_quota_is_updated()
    {
        var account = BuildAccount();
        _authService.AcquireTokenSilentAsync("acc-1", Arg.Any<CancellationToken>()).Returns(BuildSuccessAuthResult());
        _graphService.GetQuotaAsync("acc-1", Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>()).Returns(new Result<(long Total, long Used), string>.Ok((1_073_741_824L, 536_870_912L)));

        await CreateSut().TryRefreshAsync(account, TestContext.Current.CancellationToken);

        account.Quota.TotalBytes.ShouldBe(1_073_741_824L);
        account.Quota.UsedBytes.ShouldBe(536_870_912L);
    }

    [Fact]
    public async Task when_auth_succeeds_and_quota_fetched_then_quota_is_persisted_to_repository()
    {
        var account = BuildAccount();
        _authService.AcquireTokenSilentAsync("acc-1", Arg.Any<CancellationToken>()).Returns(BuildSuccessAuthResult());
        _graphService.GetQuotaAsync("acc-1", Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>()).Returns(new Result<(long Total, long Used), string>.Ok((1_073_741_824L, 536_870_912L)));

        await CreateSut().TryRefreshAsync(account, TestContext.Current.CancellationToken);

        await _accountRepository.Received(1).UpdateQuotaAsync(new AccountId("acc-1"), StorageQuotaFactory.Create(1_073_741_824L, 536_870_912L), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_auth_fails_then_account_quota_remains_unknown()
    {
        var account = BuildAccount();
        _authService.AcquireTokenSilentAsync("acc-1", Arg.Any<CancellationToken>()).Returns(new Result<AuthResult, AuthError>.Error(new AuthFailedError("silent auth failed")));

        await CreateSut().TryRefreshAsync(account, TestContext.Current.CancellationToken);

        account.Quota.TotalBytes.ShouldBe(0L);
        account.Quota.UsedBytes.ShouldBe(0L);
    }

    [Fact]
    public async Task when_auth_fails_then_graph_service_is_not_called()
    {
        var account = BuildAccount();
        _authService.AcquireTokenSilentAsync("acc-1", Arg.Any<CancellationToken>()).Returns(new Result<AuthResult, AuthError>.Error(new AuthFailedError("silent auth failed")));

        await CreateSut().TryRefreshAsync(account, TestContext.Current.CancellationToken);

        await _graphService.DidNotReceive().GetQuotaAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_quota_fetch_fails_then_account_quota_remains_unknown()
    {
        var account = BuildAccount();
        _authService.AcquireTokenSilentAsync("acc-1", Arg.Any<CancellationToken>()).Returns(BuildSuccessAuthResult());
        _graphService.GetQuotaAsync("acc-1", Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>()).Returns(new Result<(long Total, long Used), string>.Error("Graph API error"));

        await CreateSut().TryRefreshAsync(account, TestContext.Current.CancellationToken);

        account.Quota.TotalBytes.ShouldBe(0L);
        account.Quota.UsedBytes.ShouldBe(0L);
    }

    [Fact]
    public async Task when_quota_fetch_fails_then_repository_is_not_called()
    {
        var account = BuildAccount();
        _authService.AcquireTokenSilentAsync("acc-1", Arg.Any<CancellationToken>()).Returns(BuildSuccessAuthResult());
        _graphService.GetQuotaAsync("acc-1", Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>()).Returns(new Result<(long Total, long Used), string>.Error("Graph API error"));

        await CreateSut().TryRefreshAsync(account, TestContext.Current.CancellationToken);

        await _accountRepository.DidNotReceive().UpdateQuotaAsync(Arg.Any<AccountId>(), Arg.Any<StorageQuota>(), Arg.Any<CancellationToken>());
    }
}
