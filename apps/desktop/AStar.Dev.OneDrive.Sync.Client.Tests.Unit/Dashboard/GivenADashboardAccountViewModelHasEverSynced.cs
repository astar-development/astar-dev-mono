using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Dashboard;

public sealed class GivenADashboardAccountViewModelHasEverSynced
{
    private static DashboardAccountViewModel CreateSut(OneDriveAccount account) => new(account, Substitute.For<ISyncScheduler>(), Substitute.For<IAccountRepository>(), Substitute.For<ILocalizationService>(), Substitute.For<IActivityItemViewModelFactory>());

    [Fact]
    public void when_account_has_no_last_synced_at_then_has_ever_synced_is_false()
    {
        var account = new OneDriveAccount { Id = new AccountId("acc-1"), LastSyncedAt = Option.None<DateTimeOffset>() };

        var sut = CreateSut(account);

        sut.HasEverSynced.ShouldBeFalse();
    }

    [Fact]
    public void when_account_has_last_synced_at_value_then_has_ever_synced_is_true()
    {
        var account = new OneDriveAccount { Id = new AccountId("acc-2"), LastSyncedAt = Option.Some(DateTimeOffset.UtcNow.AddHours(-1)) };

        var sut = CreateSut(account);

        sut.HasEverSynced.ShouldBeTrue();
    }
}
