using AStarDev.CloudSync.Core.Overview;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.CloudSync.Domain.Common;
using AStarDev.CloudSync.Domain.Conflicts;
using AStarDev.CloudSync.Domain.Overview;
using AStarDev.CloudSync.Domain.SyncPairs;
using AStarDev.CloudSync.Infrastructure.Placeholder;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Overview;

public class GivenASyncOverviewViewModel
{
    [Fact]
    public async Task when_loaded_then_every_account_and_sync_pair_is_exposed()
    {
        var overview = await TestData.CreateOverviewAsync();

        var viewModel = await TestData.CreateLoadedOverviewViewModelAsync();

        viewModel.Accounts.Count.ShouldBe(overview.Accounts.Count);
        viewModel.SyncPairs.Count.ShouldBe(overview.SyncPairs.Count);
    }

    [Fact]
    public async Task when_loaded_then_the_conflicts_name_their_sync_pair()
    {
        var viewModel = await TestData.CreateLoadedOverviewViewModelAsync();

        viewModel.Conflicts.ShouldNotBeEmpty();
        viewModel.Conflicts.ShouldAllBe(conflict => conflict.Detail.StartsWith("Holiday photos"));
    }

    [Fact]
    public async Task when_loaded_then_the_summary_tiles_count_accounts_providers_folders_and_conflicts()
    {
        var overview = await TestData.CreateOverviewAsync();

        var viewModel = await TestData.CreateLoadedOverviewViewModelAsync();

        viewModel.Stats.Select(tile => tile.Label).ShouldBe(["accounts", "provider", "folders synced", "conflicts", "overall status"]);
        viewModel.Stats[0].Value.ShouldBe(overview.Accounts.Count.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("en-GB")));
        viewModel.Stats[1].Value.ShouldBe("1");
        viewModel.Stats[3].Value.ShouldBe(overview.Conflicts.Count.ToString(System.Globalization.CultureInfo.GetCultureInfo("en-GB")));
    }

    [Fact]
    public async Task when_loaded_with_one_of_each_then_the_summary_tile_labels_are_singular()
    {
        var account = CloudAccountFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Work", "work@example.com", CloudProviderFactory.CreateOneDrive(), StorageUsageFactory.Create(1, 2), 1, 1, SyncStatusFactory.CreateHasConflicts(1), Option.None<DateTimeOffset>());
        var conflict = SyncConflictFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), "report.docx", TestData.Now);
        var viewModel = await CreateLoadedViewModelAsync(SyncOverviewFactory.Create([account], [], [], [conflict], [], 0, Option.None<DateTimeOffset>()));

        viewModel.Stats.Select(tile => tile.Label).ShouldBe(["account", "provider", "folder synced", "conflict", "overall status"]);
    }

    [Fact]
    public async Task when_loaded_with_more_than_one_of_each_then_the_summary_tile_labels_are_plural()
    {
        var work = CloudAccountFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Work", "work@example.com", CloudProviderFactory.CreateOneDrive(), StorageUsageFactory.Create(1, 2), 2, 5, SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var personal = CloudAccountFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Personal", "personal@example.com", CloudProviderFactory.CreateOneDrive(), StorageUsageFactory.Create(1, 2), 3, 5, SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var firstConflict = SyncConflictFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), "report.docx", TestData.Now);
        var secondConflict = SyncConflictFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), "budget.xlsx", TestData.Now);
        var viewModel = await CreateLoadedViewModelAsync(SyncOverviewFactory.Create([work, personal], [], [], [firstConflict, secondConflict], [], 0, Option.None<DateTimeOffset>()));

        viewModel.Stats.Select(tile => tile.Label).Take(4).ShouldBe(["accounts", "provider", "folders synced", "conflicts"]);
    }

    [Fact]
    public async Task when_loaded_with_no_accounts_then_the_summary_tile_labels_are_plural()
    {
        var viewModel = await CreateLoadedViewModelAsync(SyncOverviewFactory.Create([], [], [], [], [], 0, Option.None<DateTimeOffset>()));

        viewModel.Stats.Select(tile => tile.Label).Take(4).ShouldBe(["accounts", "providers", "folders synced", "conflicts"]);
    }

    [Fact]
    public async Task when_any_account_is_syncing_then_the_overall_status_is_syncing()
    {
        var viewModel = await TestData.CreateLoadedOverviewViewModelAsync();

        viewModel.Stats[4].Value.ShouldBe("Syncing");
    }

    [Fact]
    public async Task when_loaded_with_conflicts_then_the_conflicts_tile_is_warn_and_the_overall_status_tile_is_accent()
    {
        var viewModel = await TestData.CreateLoadedOverviewViewModelAsync();

        viewModel.Stats.Select(tile => tile.Tone).ShouldBe([StatTileToneFactory.CreateNormal(), StatTileToneFactory.CreateNormal(), StatTileToneFactory.CreateNormal(), StatTileToneFactory.CreateWarn(), StatTileToneFactory.CreateAccent()]);
    }

    [Fact]
    public async Task when_loaded_without_conflicts_then_the_conflicts_tile_is_normal()
    {
        var overview = SyncOverviewFactory.Create([TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>())], [], [], [], [], 0, Option.None<DateTimeOffset>());
        var viewModel = new SyncOverviewViewModel(new FixedSyncOverviewSource(overview), new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.Stats[3].Tone.ShouldBeOfType<StatTileTone.Normal>();
        viewModel.Stats[4].Tone.ShouldBeOfType<StatTileTone.Accent>();
    }

    [Fact]
    public async Task when_loaded_then_the_last_checked_time_is_exposed_relative_to_now()
    {
        var viewModel = await TestData.CreateLoadedOverviewViewModelAsync();

        viewModel.LastChecked.ShouldBe(Option.Some(TestData.Now.AddMinutes(-10)));
        viewModel.LastCheckedText.ShouldBe("10 min ago");
    }

    [Fact]
    public async Task when_loaded_then_the_activity_feed_is_newest_first()
    {
        var viewModel = await TestData.CreateLoadedOverviewViewModelAsync();

        viewModel.Activity.ShouldNotBeEmpty();
        viewModel.Activity[0].WhenText.ShouldBe("Just now");
    }

    [Fact]
    public async Task when_loaded_then_the_throughput_is_exposed()
    {
        var viewModel = await TestData.CreateLoadedOverviewViewModelAsync();

        viewModel.Throughput.Values.ShouldNotBeEmpty();
    }

    [Fact]
    public void when_not_yet_loaded_then_the_collections_are_empty()
    {
        var viewModel = new SyncOverviewViewModel(new AStarDev.CloudSync.Infrastructure.Placeholder.PlaceholderSyncOverviewSource(TestData.CreateTime()), new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);

        viewModel.Accounts.ShouldBeEmpty();
        viewModel.SyncPairs.ShouldBeEmpty();
        viewModel.Conflicts.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_loaded_then_property_changed_is_raised_for_the_collections()
    {
        var viewModel = new SyncOverviewViewModel(new AStarDev.CloudSync.Infrastructure.Placeholder.PlaceholderSyncOverviewSource(TestData.CreateTime()), new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        changed.ShouldContain(nameof(SyncOverviewViewModel.Accounts));
        changed.ShouldContain(nameof(SyncOverviewViewModel.SyncPairs));
    }

    [Fact]
    public void when_not_yet_loaded_then_the_load_state_is_not_loaded_and_there_is_no_error_text()
    {
        var viewModel = new SyncOverviewViewModel(new AStarDev.CloudSync.Infrastructure.Placeholder.PlaceholderSyncOverviewSource(TestData.CreateTime()), new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);

        viewModel.LoadState.ShouldBeOfType<SyncOverviewLoadState.NotLoaded>();
        viewModel.LoadErrorText.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_loaded_then_the_load_state_is_loaded_and_there_is_no_error_text()
    {
        var viewModel = await TestData.CreateLoadedOverviewViewModelAsync();

        viewModel.LoadState.ShouldBeOfType<SyncOverviewLoadState.Loaded>();
        viewModel.LoadErrorText.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_source_throws_then_loading_completes_and_the_load_state_is_failed()
    {
        var viewModel = new SyncOverviewViewModel(new ThrowingSyncOverviewSource(new InvalidOperationException("provider unreachable")), new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);

        await Should.NotThrowAsync(() => viewModel.LoadAsync(TestContext.Current.CancellationToken));

        viewModel.LoadState.ShouldBeOfType<SyncOverviewLoadState.Failed>();
        viewModel.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_source_throws_then_the_localised_error_text_is_exposed()
    {
        var viewModel = new SyncOverviewViewModel(new ThrowingSyncOverviewSource(new InvalidOperationException("provider unreachable")), new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.LoadErrorText.ShouldBe("Could not load your accounts and sync pairs. The details have been logged.");
    }

    [Fact]
    public async Task when_the_source_throws_then_the_exception_is_logged_as_an_error()
    {
        var failure = new InvalidOperationException("provider unreachable");
        var logger = new RecordingLogger<SyncOverviewViewModel>();
        var viewModel = new SyncOverviewViewModel(new ThrowingSyncOverviewSource(failure), new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), logger);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Error);
        entry.Exception.ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task when_the_source_throws_then_property_changed_is_raised_for_the_load_state_and_error_text()
    {
        var viewModel = new SyncOverviewViewModel(new ThrowingSyncOverviewSource(new InvalidOperationException("provider unreachable")), new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        changed.ShouldContain(nameof(SyncOverviewViewModel.LoadState));
        changed.ShouldContain(nameof(SyncOverviewViewModel.LoadErrorText));
    }

    [Fact]
    public async Task when_loading_is_cancelled_then_the_cancellation_is_not_swallowed()
    {
        var viewModel = new SyncOverviewViewModel(new ThrowingSyncOverviewSource(new OperationCanceledException()), new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);

        await Should.ThrowAsync<OperationCanceledException>(() => viewModel.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_the_source_reports_a_failure_then_the_load_state_is_failed_and_an_error_without_the_failure_text_is_logged()
    {
        var logger = new RecordingLogger<SyncOverviewViewModel>();
        var viewModel = new SyncOverviewViewModel(new FailingSyncOverviewSource("database /home/user/cloudsync.db is locked"), new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), logger);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.LoadState.ShouldBeOfType<SyncOverviewLoadState.Failed>();
        viewModel.LoadErrorText.ShouldBe("Could not load your accounts and sync pairs. The details have been logged.");
        logger.Entries.ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Error);
        logger.Messages.ShouldAllBe(message => !message.Contains("/home/user"));
    }

    private static async Task<SyncOverviewViewModel> CreateLoadedViewModelAsync(SyncOverview overview)
    {
        var viewModel = new SyncOverviewViewModel(new FixedSyncOverviewSource(overview), new RecordingSyncActions(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        return viewModel;
    }
}
