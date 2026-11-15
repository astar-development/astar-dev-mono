using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Activity;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.CloudSync.ViewModels.TestsUnit.Overview;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Activity;

public class GivenAnActivityList
{
    [Fact]
    public async Task when_refreshed_then_the_entries_are_listed_in_the_order_read_with_their_text_and_age()
    {
        var (overview, data) = await LoadOverviewAsync();
        var reader = new FakeActivityLogReader { Entries = Entries(Entry(data.Accounts[0], ActivityKindFactory.CreateDownloaded(3), 5), Entry(data.Accounts[1], ActivityKindFactory.CreateDeleted(1), 30)) };
        var list = CreateFactory(reader).Create(overview, Option.None<AccountId>());

        await list.RefreshCommand.ExecuteAsync(null);

        list.Items.Select(item => (item.Text, item.WhenText)).ShouldBe([("Downloaded 3 files", "5 min ago"), ("Deleted 1 file", "30 min ago")]);
        (list.IsEmpty, list.HasError).ShouldBe((false, false));
    }

    [Fact]
    public async Task when_created_without_an_account_then_every_accounts_activity_is_requested_up_to_the_per_account_limit()
    {
        var (overview, _) = await LoadOverviewAsync();
        var reader = new FakeActivityLogReader();
        var list = CreateFactory(reader).Create(overview, Option.None<AccountId>());

        await list.RefreshCommand.ExecuteAsync(null);

        reader.Requests.ShouldBe([(Option.None<AccountId>(), ActivityLogLimits.EntriesPerAccount)]);
    }

    [Fact]
    public async Task when_created_for_an_account_then_only_that_accounts_activity_is_requested()
    {
        var (overview, data) = await LoadOverviewAsync();
        var reader = new FakeActivityLogReader();
        var list = CreateFactory(reader).Create(overview, Option.Some(data.Accounts[1].Id));

        await list.RefreshCommand.ExecuteAsync(null);

        reader.Requests.ShouldBe([(Option.Some(data.Accounts[1].Id), ActivityLogLimits.EntriesPerAccount)]);
    }

    [Fact]
    public async Task when_an_entry_belongs_to_an_account_that_is_gone_then_it_is_left_out()
    {
        var (overview, data) = await LoadOverviewAsync();
        var gone = AccountIdFactory.Create(Guid.NewGuid());
        var reader = new FakeActivityLogReader { Entries = Entries(ActivityEntryFactory.Create(gone, Option.None<SyncPairId>(), ActivityKindFactory.CreateDownloaded(1), TestData.Now), Entry(data.Accounts[0], ActivityKindFactory.CreateDownloaded(2), 1)) };
        var list = CreateFactory(reader).Create(overview, Option.None<AccountId>());

        await list.RefreshCommand.ExecuteAsync(null);

        list.Items.Select(item => item.Text).ShouldBe(["Downloaded 2 files"]);
    }

    [Fact]
    public async Task when_there_is_no_activity_then_the_list_is_empty_with_the_empty_text()
    {
        var (overview, _) = await LoadOverviewAsync();
        var list = CreateFactory(new FakeActivityLogReader()).Create(overview, Option.None<AccountId>());

        await list.RefreshCommand.ExecuteAsync(null);

        (list.IsEmpty, list.EmptyText, list.TitleText).ShouldBe((true, "No activity yet", "Activity"));
    }

    [Fact]
    public async Task when_the_log_cannot_be_read_then_no_entries_show_and_the_error_says_so_instead_of_the_empty_text()
    {
        var (overview, _) = await LoadOverviewAsync();
        var list = CreateFactory(new FakeActivityLogReader { Entries = Result.Failure<IReadOnlyList<ActivityEntry>, ErrorResponse>(new ErrorResponse("locked")) }).Create(overview, Option.None<AccountId>());

        await list.RefreshCommand.ExecuteAsync(null);

        (list.Items.Count, list.IsEmpty, list.HasError, list.ErrorText).ShouldBe((0, false, true, "The activity could not be read."));
    }

    [Fact]
    public async Task when_a_failed_read_is_followed_by_a_good_one_then_the_error_clears()
    {
        var (overview, data) = await LoadOverviewAsync();
        var reader = new FakeActivityLogReader { Entries = Result.Failure<IReadOnlyList<ActivityEntry>, ErrorResponse>(new ErrorResponse("locked")) };
        var list = CreateFactory(reader).Create(overview, Option.None<AccountId>());
        await list.RefreshCommand.ExecuteAsync(null);
        reader.Entries = Entries(Entry(data.Accounts[0], ActivityKindFactory.CreateDownloaded(1), 1));

        await list.RefreshCommand.ExecuteAsync(null);

        (list.HasError, list.Items.Count).ShouldBe((false, 1));
    }

    [Fact]
    public void when_the_factory_creates_two_lists_then_they_are_independent()
    {
        var overview = new SyncOverviewViewModel(new PlaceholderSyncOverviewSource(TestData.CreateTime()), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var factory = CreateFactory(new FakeActivityLogReader());

        factory.Create(overview, Option.None<AccountId>()).ShouldNotBeSameAs(factory.Create(overview, Option.None<AccountId>()));
    }

    private static async Task<(SyncOverviewViewModel Overview, SyncOverview Data)> LoadOverviewAsync()
    {
        var data = await TestData.CreateOverviewAsync();
        var overview = new SyncOverviewViewModel(new CountingSyncOverviewSource(data), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(TestContext.Current.CancellationToken);

        return (overview, data);
    }

    private static IActivityListFactory CreateFactory(FakeActivityLogReader reader) => TestData.CreateActivityListFactory(reader);

    private static ActivityEntry Entry(CloudAccount account, ActivityKind kind, int minutesAgo) => ActivityEntryFactory.Create(account.Id, Option.None<SyncPairId>(), kind, TestData.Now.AddMinutes(-minutesAgo));

    private static Result<IReadOnlyList<ActivityEntry>, ErrorResponse> Entries(params ActivityEntry[] entries) => Result.Success<IReadOnlyList<ActivityEntry>, ErrorResponse>(entries);
}
