using System.Collections.Concurrent;
using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;
using Microsoft.Extensions.Logging.Abstractions;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Detection;

public sealed class GivenALocalDeletionDetectorReportingDeletions
{
    private const string BaseDir = "/sync-root";

    private readonly IGraphService _graphService = Substitute.For<IGraphService>();
    private readonly ISyncedItemRepository _syncedItemRepository = Substitute.For<ISyncedItemRepository>();
    private readonly AccountId _accountId = new("user-1");

    public GivenALocalDeletionDetectorReportingDeletions()
        => _graphService.DeleteItemAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Ok<System.Reactive.Unit, string>(System.Reactive.Unit.Default));

    private LocalDeletionDetector CreateSut() => new(_graphService, _syncedItemRepository, new MockFileSystem(), NullLogger<LocalDeletionDetector>.Instance);

    private static SyncedItemEntity Item(string id, string remotePath, bool isFolder)
        => new() { RemoteItemId = new OneDriveItemId(id), RemotePath = remotePath, LocalPath = $"{BaseDir}{remotePath}", IsFolder = isFolder };

    [Fact]
    public async Task when_local_folder_is_missing_then_the_deleted_folder_is_reported()
    {
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity> { ["folder-1"] = Item("folder-1", "/SubDir", true) };

        var deleted = await CreateSut().DetectAndApplyAsync(_accountId, _ => Task.FromResult("token"), syncedItems, TestContext.Current.CancellationToken);

        deleted.Select(item => item.RemoteItemId.Value).ShouldBe(["folder-1"]);
    }

    [Fact]
    public async Task when_local_folder_with_children_is_missing_then_only_the_folder_is_reported()
    {
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>
        {
            ["folder-1"] = Item("folder-1", "/SubDir", true),
            ["file-1"] = Item("file-1", "/SubDir/file.txt", false)
        };

        var deleted = await CreateSut().DetectAndApplyAsync(_accountId, _ => Task.FromResult("token"), syncedItems, TestContext.Current.CancellationToken);

        deleted.Select(item => item.RemoteItemId.Value).ShouldBe(["folder-1"]);
    }

    [Fact]
    public async Task when_graph_delete_fails_then_nothing_is_reported()
    {
        _graphService.DeleteItemAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Error<System.Reactive.Unit, string>("boom"));
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity> { ["file-1"] = Item("file-1", "/file.txt", false) };

        var deleted = await CreateSut().DetectAndApplyAsync(_accountId, _ => Task.FromResult("token"), syncedItems, TestContext.Current.CancellationToken);

        deleted.ShouldBeEmpty();
    }
}
