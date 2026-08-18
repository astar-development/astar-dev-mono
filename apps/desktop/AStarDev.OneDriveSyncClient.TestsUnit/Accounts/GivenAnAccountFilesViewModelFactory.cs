using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Rules;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Accounts;

public sealed class GivenAnAccountFilesViewModelFactory
{
    private static AccountFilesViewModelFactory CreateSut()
    {
        var fileSystemServices = new FileSystemServices(Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>());
        return new(Substitute.For<IAuthService>(), Substitute.For<IGraphService>(), Substitute.For<ISyncRuleService>(), fileSystemServices, Substitute.For<ILogger<AccountFilesViewModel>>(), Substitute.For<IFolderTreeNodeViewModelFactory>(), Substitute.For<ILocalizationService>());
    }

    [Fact]
    public void when_create_is_called_then_the_view_model_targets_the_account()
    {
        var sut = CreateSut();
        var account = new OneDriveAccount { Id = new AccountId("account-1"), Profile = AccountProfileFactory.Create("Test User", "user@example.com") };

        var viewModel = sut.Create(account);

        viewModel.AccountId.ShouldBe("account-1");
        viewModel.Email.ShouldBe("user@example.com");
    }

    [Fact]
    public void when_create_is_called_twice_then_distinct_view_models_are_returned()
    {
        var sut = CreateSut();
        var account = new OneDriveAccount { Id = new AccountId("account-1") };

        var firstViewModel = sut.Create(account);
        var secondViewModel = sut.Create(account);

        firstViewModel.ShouldNotBeSameAs(secondViewModel);
    }
}
