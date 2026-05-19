using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Onboarding;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

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
        var (authService, graphService, repository, onboardingService) = BuildMocks();
        var sut = BuildSut(authService, graphService, repository, onboardingService);

        await DriveWizardToCompletionAsync(sut);

        await onboardingService.Received(1).CompleteOnboardingAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_wizard_completes_then_account_is_added_to_accounts_collection()
    {
        var (authService, graphService, repository, onboardingService) = BuildMocks();
        var sut = BuildSut(authService, graphService, repository, onboardingService);

        await DriveWizardToCompletionAsync(sut);

        sut.Accounts.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_wizard_is_cancelled_then_account_onboarding_service_is_not_called()
    {
        var (authService, graphService, repository, onboardingService) = BuildMocks();
        var sut = BuildSut(authService, graphService, repository, onboardingService);

        await DriveWizardToCancellationAsync(sut);

        await onboardingService.DidNotReceive().CompleteOnboardingAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>());
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

    private static (IAuthService AuthService, IGraphService GraphService, IAccountRepository Repository, IAccountOnboardingService OnboardingService) BuildMocks()
    {
        var authService       = Substitute.For<IAuthService>();
        var graphService      = Substitute.For<IGraphService>();
        var repository        = Substitute.For<IAccountRepository>();
        var onboardingService = Substitute.For<IAccountOnboardingService>();

        authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success(AccessToken, AccountIdStr, AccountProfileFactory.Create(DisplayName, Email)));

        graphService.GetRootFoldersAsync(Arg.Any<string>(), AccessToken, Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder(FolderId1, FolderName1), new DriveFolder(FolderId2, FolderName2)]));

        onboardingService.CompleteOnboardingAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<OneDriveAccount>());

        return (authService, graphService, repository, onboardingService);
    }

    private static AccountsViewModel BuildSut(IAuthService authService, IGraphService graphService, IAccountRepository repository, IAccountOnboardingService onboardingService)
        => new(authService, graphService, repository, onboardingService, Substitute.For<ISyncEventAggregator>());
}
