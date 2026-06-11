using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Dashboard;

public sealed class GivenADashboardAccountViewModelFactory
{
    private static DashboardAccountViewModelFactory CreateSut() => new(Substitute.For<ISyncScheduler>(), Substitute.For<IAccountRepository>(), Substitute.For<ILocalizationService>(), Substitute.For<IActivityItemViewModelFactory>());

    [Fact]
    public void when_create_is_called_then_the_section_targets_the_account()
    {
        var sut = CreateSut();
        var account = new OneDriveAccount { Id = new AccountId("account-1") };

        var section = sut.Create(account);

        section.AccountId.ShouldBe("account-1");
    }

    [Fact]
    public void when_create_is_called_then_the_folder_count_matches_the_account_selection()
    {
        var sut = CreateSut();
        var account = new OneDriveAccount { Id = new AccountId("account-1"), SelectedFolderIds = ["folder-1", "folder-2"] };

        var section = sut.Create(account);

        section.FolderCount.ShouldBe(2);
    }
}
