using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;
using Microsoft.Extensions.Logging.Abstractions;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Detection;

public sealed class GivenALocalDeletionDetector
{
    private const string BaseDir = "/sync-root";
    private const string LocalFile = $"{BaseDir}/file.txt";

    private readonly IGraphService _graphService = Substitute.For<IGraphService>();
    private readonly ISyncedItemRepository _syncedItemRepository = Substitute.For<ISyncedItemRepository>();
    private readonly AccountId _accountId = new("user-1");

    public GivenALocalDeletionDetector()
        => _graphService.DeleteItemAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<System.Reactive.Unit, string>.Ok(System.Reactive.Unit.Default));

    private LocalDeletionDetector CreateSut(MockFileSystem mockFileSystem) => new(_graphService, _syncedItemRepository, mockFileSystem, NullLogger<LocalDeletionDetector>.Instance);

    [Fact]
    public async Task when_local_file_still_exists_then_graph_delete_is_not_called()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(LocalFile).Which(m => m.HasStringContent("data"));
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/file.txt", LocalPath = LocalFile, IsFolder = false }
        };
        var sut = CreateSut(mockFileSystem);
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");

        await sut.DetectAndApplyAsync(_accountId, tokenFactory, syncedItems, TestContext.Current.CancellationToken);

        await _graphService.DidNotReceive().DeleteItemAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_local_file_is_missing_then_graph_delete_is_called()
    {
        var mockFileSystem = new MockFileSystem();
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/deleted.txt", LocalPath = $"{BaseDir}/deleted.txt", IsFolder = false }
        };
        var sut = CreateSut(mockFileSystem);
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");

        await sut.DetectAndApplyAsync(_accountId, tokenFactory, syncedItems, TestContext.Current.CancellationToken);

        await _graphService.Received(1).DeleteItemAsync(Arg.Is("user-1"), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Is("item-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_local_file_is_missing_then_synced_item_repository_delete_is_called()
    {
        var mockFileSystem = new MockFileSystem();
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/deleted.txt", LocalPath = $"{BaseDir}/deleted.txt", IsFolder = false }
        };
        var sut = CreateSut(mockFileSystem);
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");

        await sut.DetectAndApplyAsync(_accountId, tokenFactory, syncedItems, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).DeleteByRemoteIdAsync(Arg.Is(_accountId), Arg.Is<OneDriveItemId>(id => id.Id == "item-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_synced_item_is_a_folder_then_it_is_skipped()
    {
        var mockFileSystem = new MockFileSystem();
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["folder-1"] = new() { RemoteItemId = new OneDriveItemId("folder-1"), RemotePath = "/SubDir", LocalPath = $"{BaseDir}/SubDir", IsFolder = true }
        };
        var sut = CreateSut(mockFileSystem);
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");

        await sut.DetectAndApplyAsync(_accountId, tokenFactory, syncedItems, TestContext.Current.CancellationToken);

        await _graphService.DidNotReceive().DeleteItemAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_graph_delete_throws_then_exception_is_caught_and_remaining_items_are_processed()
    {
        var mockFileSystem = new MockFileSystem();
        _graphService.When(x => x.DeleteItemAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Is("item-fail"), Arg.Any<CancellationToken>()))
            .Throw(new HttpRequestException("network error"));
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-fail"] = new() { RemoteItemId = new OneDriveItemId("item-fail"), RemotePath = "/fail.txt", LocalPath = $"{BaseDir}/fail.txt", IsFolder = false },
            ["item-ok"] = new() { RemoteItemId = new OneDriveItemId("item-ok"), RemotePath = "/ok.txt", LocalPath = $"{BaseDir}/ok.txt", IsFolder = false }
        };
        var sut = CreateSut(mockFileSystem);
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");

        await sut.DetectAndApplyAsync(_accountId, tokenFactory, syncedItems, TestContext.Current.CancellationToken);

        await _graphService.Received(1).DeleteItemAsync(Arg.Is("user-1"), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Is("item-ok"), Arg.Any<CancellationToken>());
    }
}
