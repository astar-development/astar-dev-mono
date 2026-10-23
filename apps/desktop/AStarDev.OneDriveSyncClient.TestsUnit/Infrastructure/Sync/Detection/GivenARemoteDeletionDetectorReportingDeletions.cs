using System.Collections.Concurrent;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;
using Microsoft.Extensions.Logging.Abstractions;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Detection;

public sealed class GivenARemoteDeletionDetectorReportingDeletions
{
    private const string BaseDir = "/sync-root";

    private readonly ISyncedItemRepository _syncedItemRepository = Substitute.For<ISyncedItemRepository>();
    private readonly AccountId _accountId = new("user-1");
    private readonly IReadOnlySet<string> _nothingSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private RemoteDeletionDetector CreateSut() => new(_syncedItemRepository, new MockFileSystem(), NullLogger<RemoteDeletionDetector>.Instance);

    private static SyncedItemEntity Item(string id, string remotePath, bool isFolder)
        => new() { RemoteItemId = new OneDriveItemId(id), RemotePath = remotePath, LocalPath = $"{BaseDir}{remotePath}", IsFolder = isFolder };

    private static List<SyncRuleEntity> IncludeRoot() => [new SyncRuleEntity { RemotePath = "/", RuleType = RuleType.Include }];

    [Fact]
    public async Task when_remote_file_is_absent_then_the_deleted_file_is_reported()
    {
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity> { ["file-1"] = Item("file-1", "/file.txt", false) };

        var deleted = await CreateSut().DetectAndApplyAsync(_accountId, syncedItems, _nothingSeen, IncludeRoot(), TestContext.Current.CancellationToken);

        deleted.Select(item => item.RemoteItemId.Value).ShouldBe(["file-1"]);
    }

    [Fact]
    public async Task when_remote_folder_with_children_is_absent_then_only_the_folder_is_reported()
    {
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>
        {
            ["folder-1"] = Item("folder-1", "/SubDir", true),
            ["file-1"] = Item("file-1", "/SubDir/file.txt", false)
        };

        var deleted = await CreateSut().DetectAndApplyAsync(_accountId, syncedItems, _nothingSeen, IncludeRoot(), TestContext.Current.CancellationToken);

        deleted.Select(item => item.RemoteItemId.Value).ShouldBe(["folder-1"]);
    }

    [Fact]
    public async Task when_remote_item_is_still_seen_then_nothing_is_reported()
    {
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity> { ["file-1"] = Item("file-1", "/file.txt", false) };
        IReadOnlySet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "file-1" };

        var deleted = await CreateSut().DetectAndApplyAsync(_accountId, syncedItems, seen, IncludeRoot(), TestContext.Current.CancellationToken);

        deleted.ShouldBeEmpty();
    }
}
