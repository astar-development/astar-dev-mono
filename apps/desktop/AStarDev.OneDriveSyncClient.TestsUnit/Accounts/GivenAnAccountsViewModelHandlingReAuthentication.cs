using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Onboarding;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Localization;
using AStarDev.OneDriveSyncClient.Onboarding;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Accounts;

public sealed class GivenAnAccountsViewModelHandlingReAuthentication
{
    [Fact]
    public void when_interactive_sign_in_throws_then_the_exception_does_not_escape_the_handler()
    {
        var authService = Substitute.For<IAuthService>();
        authService.SignInInteractiveAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<Result<AuthResult, AuthError>>(new IOException("keyring unavailable")));
        var sut = BuildSut(authService);
        sut.RestoreAccounts([CreateAccount()]);
        var context = new CapturingSynchronizationContext();
        var original = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            sut.Accounts[0].ReAuthenticateCommand.Execute(null);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }

        context.Exceptions.ShouldBeEmpty();
    }

    private static AccountsViewModel BuildSut(IAuthService authService)
    {
        var graphService = Substitute.For<IGraphService>();

        return new(authService, graphService, Substitute.For<IAccountRepository>(), Substitute.For<IAccountOnboardingService>(), Substitute.For<IQuotaRefreshService>(), Substitute.For<ISyncEventAggregator>(), new AddAccountWizardViewModelFactory(authService, graphService, Substitute.For<ILocalizationService>()), new AccountCardViewModelFactory(Substitute.For<ILocalizationService>()), Substitute.For<ILogger<AccountsViewModel>>());
    }

    private static OneDriveAccount CreateAccount() => new()
    {
        Id = new AccountId("account-1"),
        Profile = AccountProfileFactory.Create("Test User", "test@outlook.com"),
        SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/sync-root")),
        SelectedFolderIds = []
    };

    private sealed class CapturingSynchronizationContext : SynchronizationContext
    {
        public List<Exception> Exceptions { get; } = [];

        public override void Post(SendOrPostCallback d, object? state)
        {
            try
            {
                d(state);
            }
            catch (Exception ex)
            {
                Exceptions.Add(ex);
            }
        }
    }
}
