using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Rules;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountFilesViewModelFactory
{
    private static AccountFilesViewModelFactory CreateSut() => new(Substitute.For<IAuthService>(), Substitute.For<IGraphService>(), Substitute.For<ISyncRuleService>(), Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>(), Substitute.For<ILogger<AccountFilesViewModel>>(), Substitute.For<IFolderTreeNodeViewModelFactory>(), Substitute.For<ILocalizationService>());

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
