using System.Collections.Concurrent;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Detection;

public sealed class GivenARemoteDeletionDetector
{
    private const string BaseDir = "/sync-root";
    private const string LocalFile = $"{BaseDir}/file.txt";

    private readonly ISyncedItemRepository _syncedItemRepository = Substitute.For<ISyncedItemRepository>();
    private readonly AccountId _accountId = new("user-1");

    private RemoteDeletionDetector CreateSut(MockFileSystem mockFileSystem) => new(_syncedItemRepository, mockFileSystem, Substitute.For<ILogger<RemoteDeletionDetector>>());

    private static List<SyncRuleEntity> IncludeRules(params string[] paths)
        => [.. paths.Select(p => new SyncRuleEntity { RemotePath = p, RuleType = RuleType.Include })];

    [Fact]
    public async Task when_remote_id_is_in_seen_set_then_local_file_is_not_deleted()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(LocalFile).Which(m => m.HasStringContent("data"));
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/file.txt", LocalPath = LocalFile, IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "item-1" };
        var sut = CreateSut(mockFileSystem);

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/file.txt"), TestContext.Current.CancellationToken);

        mockFileSystem.File.Exists(LocalFile).ShouldBeTrue();
    }

    [Fact]
    public async Task when_remote_id_absent_and_local_file_exists_then_file_is_deleted()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(LocalFile).Which(m => m.HasStringContent("data"));
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/file.txt", LocalPath = LocalFile, IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut(mockFileSystem);

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/file.txt"), TestContext.Current.CancellationToken);

        mockFileSystem.File.Exists(LocalFile).ShouldBeFalse();
    }

    [Fact]
    public async Task when_remote_id_absent_and_local_file_does_not_exist_then_no_exception_is_thrown()
    {
        var mockFileSystem = new MockFileSystem();
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/gone.txt", LocalPath = $"{BaseDir}/gone.txt", IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut(mockFileSystem);

        var act = async () => await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/gone.txt"), TestContext.Current.CancellationToken);

        await act.ShouldNotThrowAsync();
    }

    [Fact]
    public async Task when_remote_id_absent_and_item_is_folder_then_directory_is_deleted_recursively()
    {
        const string localDir = $"{BaseDir}/subfolder";
        const string childFile = $"{localDir}/child.txt";
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory(localDir);
        mockFileSystem.Initialize().WithFile(childFile).Which(m => m.HasStringContent("data"));
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>
        {
            ["folder-1"] = new() { RemoteItemId = new OneDriveItemId("folder-1"), RemotePath = "/subfolder", LocalPath = localDir, IsFolder = true }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut(mockFileSystem);

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/subfolder"), TestContext.Current.CancellationToken);

        mockFileSystem.Directory.Exists(localDir).ShouldBeFalse();
    }

    [Fact]
    public async Task when_remote_id_absent_then_batch_delete_is_called_with_that_id()
    {
        var mockFileSystem = new MockFileSystem();
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/file.txt", LocalPath = $"{BaseDir}/file.txt", IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut(mockFileSystem);

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/file.txt"), TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).DeleteManyByRemoteIdAsync(Arg.Is(_accountId), Arg.Is<IReadOnlyList<OneDriveItemId>>(ids => ids.Count == 1 && ids[0].Id == "item-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_multiple_remote_ids_absent_then_batch_delete_is_called_once_with_all_ids()
    {
        var mockFileSystem = new MockFileSystem();
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/file1.txt", LocalPath = $"{BaseDir}/file1.txt", IsFolder = false },
            ["item-2"] = new() { RemoteItemId = new OneDriveItemId("item-2"), RemotePath = "/file2.txt", LocalPath = $"{BaseDir}/file2.txt", IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut(mockFileSystem);

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/file1.txt", "/file2.txt"), TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).DeleteManyByRemoteIdAsync(Arg.Is(_accountId), Arg.Is<IReadOnlyList<OneDriveItemId>>(ids => ids.Count == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_no_remote_ids_are_absent_then_batch_delete_is_not_called()
    {
        var mockFileSystem = new MockFileSystem();
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/file.txt", LocalPath = LocalFile, IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "item-1" };
        var sut = CreateSut(mockFileSystem);

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/file.txt"), TestContext.Current.CancellationToken);

        await _syncedItemRepository.DidNotReceive().DeleteManyByRemoteIdAsync(Arg.Any<AccountId>(), Arg.Any<IReadOnlyList<OneDriveItemId>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_remote_path_does_not_match_include_rules_then_item_is_not_treated_as_deleted()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(LocalFile).Which(m => m.HasStringContent("data"));
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/Other/other.txt", LocalPath = LocalFile, IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut(mockFileSystem);

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/Documents"), TestContext.Current.CancellationToken);

        mockFileSystem.File.Exists(LocalFile).ShouldBeTrue();
        await _syncedItemRepository.DidNotReceive().DeleteManyByRemoteIdAsync(Arg.Any<AccountId>(), Arg.Any<IReadOnlyList<OneDriveItemId>>(), Arg.Any<CancellationToken>());
    }
}
