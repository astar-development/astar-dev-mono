using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.ViewModels;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.TestsUnit.ViewModels;

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

        viewModel.Stats.Select(tile => tile.Label).ShouldBe(["accounts", "providers", "folders synced", "conflicts", "overall status"]);
        viewModel.Stats[0].Value.ShouldBe(overview.Accounts.Count.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("en-GB")));
        viewModel.Stats[1].Value.ShouldBe("1");
        viewModel.Stats[3].Value.ShouldBe(overview.Conflicts.Count.ToString(System.Globalization.CultureInfo.GetCultureInfo("en-GB")));
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

        viewModel.Stats.Select(tile => tile.Tone).ShouldBe([StatTileToneFactory.CreateDefault(), StatTileToneFactory.CreateDefault(), StatTileToneFactory.CreateDefault(), StatTileToneFactory.CreateWarn(), StatTileToneFactory.CreateAccent()]);
    }

    [Fact]
    public async Task when_loaded_without_conflicts_then_the_conflicts_tile_is_default()
    {
        var overview = SyncOverviewFactory.Create([TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>())], [], [], [], []);
        var viewModel = new SyncOverviewViewModel(new FixedSyncOverviewSource(overview), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.Stats[3].Tone.ShouldBeOfType<StatTileTone.Default>();
        viewModel.Stats[4].Tone.ShouldBeOfType<StatTileTone.Accent>();
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
        var viewModel = new SyncOverviewViewModel(new AStarDev.CloudSync.Core.Placeholder.PlaceholderSyncOverviewSource(TestData.CreateTime()), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);

        viewModel.Accounts.ShouldBeEmpty();
        viewModel.SyncPairs.ShouldBeEmpty();
        viewModel.Conflicts.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_loaded_then_property_changed_is_raised_for_the_collections()
    {
        var viewModel = new SyncOverviewViewModel(new AStarDev.CloudSync.Core.Placeholder.PlaceholderSyncOverviewSource(TestData.CreateTime()), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        changed.ShouldContain(nameof(SyncOverviewViewModel.Accounts));
        changed.ShouldContain(nameof(SyncOverviewViewModel.SyncPairs));
    }

    [Fact]
    public void when_not_yet_loaded_then_the_load_state_is_not_loaded_and_there_is_no_error_text()
    {
        var viewModel = new SyncOverviewViewModel(new AStarDev.CloudSync.Core.Placeholder.PlaceholderSyncOverviewSource(TestData.CreateTime()), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);

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
        var viewModel = new SyncOverviewViewModel(new ThrowingSyncOverviewSource(new InvalidOperationException("provider unreachable")), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);

        await Should.NotThrowAsync(() => viewModel.LoadAsync(TestContext.Current.CancellationToken));

        viewModel.LoadState.ShouldBeOfType<SyncOverviewLoadState.Failed>();
        viewModel.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_source_throws_then_the_localised_error_text_is_exposed()
    {
        var viewModel = new SyncOverviewViewModel(new ThrowingSyncOverviewSource(new InvalidOperationException("provider unreachable")), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.LoadErrorText.ShouldBe("Could not load your accounts and sync pairs. The details have been logged.");
    }

    [Fact]
    public async Task when_the_source_throws_then_the_exception_is_logged_as_an_error()
    {
        var failure = new InvalidOperationException("provider unreachable");
        var logger = new RecordingLogger<SyncOverviewViewModel>();
        var viewModel = new SyncOverviewViewModel(new ThrowingSyncOverviewSource(failure), TestData.CreateLocalization(), TestData.CreateTime(), logger);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Error);
        entry.Exception.ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task when_the_source_throws_then_property_changed_is_raised_for_the_load_state_and_error_text()
    {
        var viewModel = new SyncOverviewViewModel(new ThrowingSyncOverviewSource(new InvalidOperationException("provider unreachable")), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        changed.ShouldContain(nameof(SyncOverviewViewModel.LoadState));
        changed.ShouldContain(nameof(SyncOverviewViewModel.LoadErrorText));
    }

    [Fact]
    public async Task when_loading_is_cancelled_then_the_cancellation_is_not_swallowed()
    {
        var viewModel = new SyncOverviewViewModel(new ThrowingSyncOverviewSource(new OperationCanceledException()), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);

        await Should.ThrowAsync<OperationCanceledException>(() => viewModel.LoadAsync(TestContext.Current.CancellationToken));
    }
}
