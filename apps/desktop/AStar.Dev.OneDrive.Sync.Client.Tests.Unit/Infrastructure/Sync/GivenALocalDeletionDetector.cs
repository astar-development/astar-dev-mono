using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenALocalDeletionDetector : IDisposable
{
    private readonly IGraphService         _graphService         = Substitute.For<IGraphService>();
    private readonly ISyncedItemRepository _syncedItemRepository = Substitute.For<ISyncedItemRepository>();
    private readonly string                _tempBase             = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly AccountId             _accountId            = new("user-1");

    public GivenALocalDeletionDetector() => Directory.CreateDirectory(_tempBase);

    public void Dispose()
    {
        if(Directory.Exists(_tempBase))
            Directory.Delete(_tempBase, recursive: true);
    }

    private LocalDeletionDetector CreateSut() => new(_graphService, _syncedItemRepository);

    [Fact]
    public async Task when_local_file_still_exists_then_graph_delete_is_not_called()
    {
        string localFile = Path.Combine(_tempBase, "file.txt");
        File.WriteAllText(localFile, "data");

        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/file.txt", LocalPath = localFile, IsFolder = false }
        };
        var sut = CreateSut();

        await sut.DetectAndApplyAsync(_accountId, "token", syncedItems, TestContext.Current.CancellationToken);

        await _graphService.DidNotReceive().DeleteItemAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_local_file_is_missing_then_graph_delete_is_called()
    {
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/deleted.txt", LocalPath = Path.Combine(_tempBase, "deleted.txt"), IsFolder = false }
        };
        var sut = CreateSut();

        await sut.DetectAndApplyAsync(_accountId, "token", syncedItems, TestContext.Current.CancellationToken);

        await _graphService.Received(1).DeleteItemAsync(Arg.Is("token"), Arg.Is("item-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_local_file_is_missing_then_synced_item_repository_delete_is_called()
    {
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/deleted.txt", LocalPath = Path.Combine(_tempBase, "deleted.txt"), IsFolder = false }
        };
        var sut = CreateSut();

        await sut.DetectAndApplyAsync(_accountId, "token", syncedItems, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).DeleteByRemoteIdAsync(Arg.Is(_accountId), Arg.Is<OneDriveItemId>(id => id.Id == "item-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_synced_item_is_a_folder_then_it_is_skipped()
    {
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["folder-1"] = new() { RemoteItemId = new OneDriveItemId("folder-1"), RemotePath = "/SubDir", LocalPath = Path.Combine(_tempBase, "SubDir"), IsFolder = true }
        };
        var sut = CreateSut();

        await sut.DetectAndApplyAsync(_accountId, "token", syncedItems, TestContext.Current.CancellationToken);

        await _graphService.DidNotReceive().DeleteItemAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_graph_delete_throws_then_exception_is_caught_and_remaining_items_are_processed()
    {
        _graphService.DeleteItemAsync(Arg.Any<string>(), Arg.Is("item-fail"), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("network error")));

        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-fail"] = new() { RemoteItemId = new OneDriveItemId("item-fail"), RemotePath = "/fail.txt", LocalPath = Path.Combine(_tempBase, "fail.txt"), IsFolder = false },
            ["item-ok"]   = new() { RemoteItemId = new OneDriveItemId("item-ok"),   RemotePath = "/ok.txt",   LocalPath = Path.Combine(_tempBase, "ok.txt"),   IsFolder = false }
        };
        var sut = CreateSut();

        await sut.DetectAndApplyAsync(_accountId, "token", syncedItems, TestContext.Current.CancellationToken);

        await _graphService.Received(1).DeleteItemAsync(Arg.Is("token"), Arg.Is("item-ok"), Arg.Any<CancellationToken>());
    }
}
