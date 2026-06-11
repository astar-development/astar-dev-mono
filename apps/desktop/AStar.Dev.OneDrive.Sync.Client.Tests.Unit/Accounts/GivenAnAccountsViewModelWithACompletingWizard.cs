using AStar.Dev.OneDrive.Sync.Client.Onboarding;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Onboarding;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountsViewModelWithACompletingWizard
{
    private const string AccessToken  = "token-123";
    private const string AccountIdStr = "account-1";
    private const string DisplayName  = "Test User";
    private const string Email        = "test@outlook.com";
    private const string FolderId1    = "f1";
    private const string FolderName1  = "Documents";
    private const string FolderId2    = "f2";
    private const string FolderName2  = "Desktop";

    [Fact]
    public async Task when_wizard_completes_then_account_onboarding_service_is_called()
    {
        var (authService, graphService, repository, onboardingService, quotaRefreshService) = BuildMocks();
        var sut = BuildSut(authService, graphService, repository, onboardingService, quotaRefreshService);

        await DriveWizardToCompletionAsync(sut);

        await onboardingService.Received(1).CompleteOnboardingAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_wizard_completes_then_account_is_added_to_accounts_collection()
    {
        var (authService, graphService, repository, onboardingService, quotaRefreshService) = BuildMocks();
        var sut = BuildSut(authService, graphService, repository, onboardingService, quotaRefreshService);

        await DriveWizardToCompletionAsync(sut);

        sut.Accounts.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_wizard_completes_then_quota_refresh_service_is_called_for_new_account()
    {
        var (authService, graphService, repository, onboardingService, quotaRefreshService) = BuildMocks();
        var sut = BuildSut(authService, graphService, repository, onboardingService, quotaRefreshService);

        await DriveWizardToCompletionAsync(sut);

        await quotaRefreshService.Received(1).TryRefreshAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_wizard_is_cancelled_then_account_onboarding_service_is_not_called()
    {
        var (authService, graphService, repository, onboardingService, quotaRefreshService) = BuildMocks();
        var sut = BuildSut(authService, graphService, repository, onboardingService, quotaRefreshService);

        await DriveWizardToCancellationAsync(sut);

        await onboardingService.DidNotReceive().CompleteOnboardingAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_wizard_is_cancelled_then_quota_refresh_service_is_not_called()
    {
        var (authService, graphService, repository, onboardingService, quotaRefreshService) = BuildMocks();
        var sut = BuildSut(authService, graphService, repository, onboardingService, quotaRefreshService);

        await DriveWizardToCancellationAsync(sut);

        await quotaRefreshService.DidNotReceive().TryRefreshAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>());
    }

    private static async Task DriveWizardToCompletionAsync(AccountsViewModel sut)
    {
        sut.AddAccount();
        var wizard = sut.Wizard!;

        await wizard.OpenBrowserCommand.ExecuteAsync(null);
        await wizard.NextCommand.ExecuteAsync(null);
        await wizard.NextCommand.ExecuteAsync(null);
        await wizard.NextCommand.ExecuteAsync(null);

        await Task.Delay(200);
    }

    private static async Task DriveWizardToCancellationAsync(AccountsViewModel sut)
    {
        sut.AddAccount();
        var wizard = sut.Wizard!;

        await wizard.OpenBrowserCommand.ExecuteAsync(null);
        await wizard.CancelCommand.ExecuteAsync(null);

        await Task.Delay(50);
    }

    private static (IAuthService AuthService, IGraphService GraphService, IAccountRepository Repository, IAccountOnboardingService OnboardingService, IQuotaRefreshService QuotaRefreshService) BuildMocks()
    {
        var authService          = Substitute.For<IAuthService>();
        var graphService         = Substitute.For<IGraphService>();
        var repository           = Substitute.For<IAccountRepository>();
        var onboardingService    = Substitute.For<IAccountOnboardingService>();
        var quotaRefreshService  = Substitute.For<IQuotaRefreshService>();

        authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success(AccessToken, AccountIdStr, AccountProfileFactory.Create(DisplayName, Email)));

        graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder(FolderId1, FolderName1, Option.None<string>()), new DriveFolder(FolderId2, FolderName2, Option.None<string>())]));

        onboardingService.CompleteOnboardingAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<OneDriveAccount>());

        return (authService, graphService, repository, onboardingService, quotaRefreshService);
    }

    private static AccountsViewModel BuildSut(IAuthService authService, IGraphService graphService, IAccountRepository repository, IAccountOnboardingService onboardingService, IQuotaRefreshService quotaRefreshService)
        => new(authService, graphService, repository, onboardingService, quotaRefreshService, Substitute.For<ISyncEventAggregator>(), new AddAccountWizardViewModelFactory(authService, graphService, Substitute.For<ILocalizationService>()), new AccountCardViewModelFactory(Substitute.For<ILocalizationService>()), Substitute.For<ILogger<AccountsViewModel>>());
}
