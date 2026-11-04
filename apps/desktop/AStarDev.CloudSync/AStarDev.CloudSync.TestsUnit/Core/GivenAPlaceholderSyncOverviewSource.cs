using AStarDev.CloudSync.Core;

namespace AStarDev.CloudSync.TestsUnit.Core;

public class GivenAPlaceholderSyncOverviewSource
{
    private const string OneDriveLocalRoot = "/home/user/CloudSync/OneDrive";

    [Fact]
    public async Task when_read_then_every_account_is_a_onedrive_account()
    {
        var overview = await TestData.CreateOverviewAsync();

        overview.Accounts.ShouldNotBeEmpty();
        overview.Accounts.ShouldAllBe(account => account.Provider is CloudProvider.OneDrive);
    }

    [Fact]
    public async Task when_read_then_every_sync_pair_lives_under_the_onedrive_local_root()
    {
        var overview = await TestData.CreateOverviewAsync();

        overview.SyncPairs.ShouldNotBeEmpty();
        overview.SyncPairs.ShouldAllBe(pair => pair.LocalFolder.FullName.StartsWith(OneDriveLocalRoot + "/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_read_then_no_two_sync_pairs_share_a_local_folder()
    {
        var overview = await TestData.CreateOverviewAsync();

        overview.SyncPairs.Select(pair => pair.LocalFolder.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(overview.SyncPairs.Count);
    }

    [Fact]
    public async Task when_read_then_every_sync_pair_belongs_to_a_known_account()
    {
        var overview = await TestData.CreateOverviewAsync();

        var accountIds = overview.Accounts.Select(account => account.Id).ToHashSet();
        overview.SyncPairs.ShouldAllBe(pair => accountIds.Contains(pair.AccountId));
    }

    [Fact]
    public async Task when_read_then_every_conflict_belongs_to_a_known_sync_pair()
    {
        var overview = await TestData.CreateOverviewAsync();

        var pairIds = overview.SyncPairs.Select(pair => pair.Id).ToHashSet();
        overview.Conflicts.ShouldNotBeEmpty();
        overview.Conflicts.ShouldAllBe(conflict => pairIds.Contains(conflict.SyncPairId));
    }

    [Fact]
    public async Task when_read_then_activity_and_throughput_are_supplied()
    {
        var overview = await TestData.CreateOverviewAsync();

        overview.Activity.ShouldNotBeEmpty();
        overview.Throughput.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task when_read_then_some_bytes_were_moved_today()
    {
        var overview = await TestData.CreateOverviewAsync();

        overview.BytesMovedToday.ShouldBeGreaterThan(0);
    }
}
