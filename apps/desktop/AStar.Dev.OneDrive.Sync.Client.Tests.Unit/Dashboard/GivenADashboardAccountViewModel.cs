using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Dashboard;

public sealed class GivenADashboardAccountViewModel
{
    private static DashboardAccountViewModel CreateSut(ISyncScheduler scheduler)
        => new(
            new OneDriveAccount { Id = new AccountId("test-account") },
            scheduler,
            Substitute.For<IAccountRepository>(),
            Substitute.For<ILocalizationService>());

    [Fact]
    public async Task when_cancel_sync_command_invoked_then_scheduler_cancel_account_called_with_correct_id()
    {
        var mockScheduler = Substitute.For<ISyncScheduler>();
        var sut = CreateSut(mockScheduler);

        await ((IAsyncRelayCommand)sut.CancelSyncCommand).ExecuteAsync(null);

        await mockScheduler.Received(1).CancelAccountAsync("test-account");
    }
}
