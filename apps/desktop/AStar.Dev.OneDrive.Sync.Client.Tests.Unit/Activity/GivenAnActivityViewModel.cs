using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Activity;

public sealed class GivenAnActivityViewModel
{
    private const string AccountId = "acc-1";
    private const string CurrentFile = "Documents/report.pdf";

    private readonly ISyncService _syncService = Substitute.For<ISyncService>();
    private readonly ISyncRepository _syncRepository = Substitute.For<ISyncRepository>();
    private readonly ISyncEventAggregator _syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly ILocalizationService _loc = Substitute.For<ILocalizationService>();
    private readonly IUiDispatcher _dispatcher = new InlineUiDispatcher();

    public GivenAnActivityViewModel() =>
        _syncRepository.GetPendingConflictsAsync(Arg.Any<AccountId>()).Returns([]);

    private ActivityViewModel CreateSut() => new(_syncService, _syncRepository, _syncEventAggregator, _loc, _dispatcher);

    [Fact]
    public void when_sync_progress_fires_with_total_zero_and_current_file_then_info_item_added_to_log()
    {
        var sut = CreateSut();
        sut.SubscribeToSyncEvents();
        var args = new SyncProgressEventArgs(AccountId, "folder-1", 0, 0, CurrentFile, SyncState.Syncing);

        _syncEventAggregator.SyncProgressChanged += Raise.EventWith(args);

        sut.LogItems.ShouldHaveSingleItem();
        sut.LogItems[0].Type.ShouldBe(ActivityItemType.Info);
    }

    [Fact]
    public void when_sync_progress_fires_with_total_zero_and_current_file_then_item_account_id_matches_event()
    {
        var sut = CreateSut();
        sut.SubscribeToSyncEvents();
        var args = new SyncProgressEventArgs(AccountId, "folder-1", 0, 0, CurrentFile, SyncState.Syncing);

        _syncEventAggregator.SyncProgressChanged += Raise.EventWith(args);

        sut.LogItems[0].AccountId.ShouldBe(AccountId);
    }

    [Fact]
    public void when_sync_progress_fires_with_total_zero_and_current_file_then_item_file_name_matches_current_file()
    {
        var sut = CreateSut();
        sut.SubscribeToSyncEvents();
        var args = new SyncProgressEventArgs(AccountId, "folder-1", 0, 0, CurrentFile, SyncState.Syncing);

        _syncEventAggregator.SyncProgressChanged += Raise.EventWith(args);

        sut.LogItems[0].FileName.ShouldBe(CurrentFile);
    }

    [Fact]
    public void when_sync_progress_fires_with_total_greater_than_zero_then_no_item_added()
    {
        var sut = CreateSut();
        sut.SubscribeToSyncEvents();
        var args = new SyncProgressEventArgs(AccountId, "folder-1", 1, 10, CurrentFile, SyncState.Syncing);

        _syncEventAggregator.SyncProgressChanged += Raise.EventWith(args);

        sut.LogItems.ShouldBeEmpty();
    }

    [Fact]
    public void when_sync_progress_fires_with_empty_current_file_then_no_item_added()
    {
        var sut = CreateSut();
        sut.SubscribeToSyncEvents();
        var args = new SyncProgressEventArgs(AccountId, "folder-1", 0, 0, string.Empty, SyncState.Syncing);

        _syncEventAggregator.SyncProgressChanged += Raise.EventWith(args);

        sut.LogItems.ShouldBeEmpty();
    }
}
