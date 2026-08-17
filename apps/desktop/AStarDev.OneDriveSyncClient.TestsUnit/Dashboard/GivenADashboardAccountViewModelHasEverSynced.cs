using AStar.Dev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Activity;
using AStarDev.OneDriveSyncClient.Dashboard;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Dashboard;

public sealed class GivenADashboardAccountViewModelHasEverSynced
{
    private static DashboardAccountViewModel CreateSut(OneDriveAccount account) => new(account, Substitute.For<ISyncScheduler>(), Substitute.For<IAccountRepository>(), Substitute.For<ILocalizationService>(), Substitute.For<IActivityItemViewModelFactory>(), Substitute.For<ILogger<DashboardAccountViewModel>>());

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
