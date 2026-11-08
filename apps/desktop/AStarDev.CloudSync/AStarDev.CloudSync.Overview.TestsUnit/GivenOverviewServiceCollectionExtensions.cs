using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Core.Persistence;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.TestHelpers.Accounts;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Overview.TestsUnit;

public class GivenOverviewServiceCollectionExtensions
{
    [Fact]
    public void when_the_sync_overview_is_added_then_one_source_is_shared_by_every_caller()
    {
        using var provider = new ServiceCollection()
            .AddSingleton<ICloudSyncDatabase>(new FakeCloudSyncDatabase())
            .AddSingleton<IAccountStore>(new FakeAccountStore())
            .AddSyncOverview()
            .BuildServiceProvider();

        provider.GetRequiredService<ISyncOverviewSource>().ShouldBeSameAs(provider.GetRequiredService<ISyncOverviewSource>());
    }
}
