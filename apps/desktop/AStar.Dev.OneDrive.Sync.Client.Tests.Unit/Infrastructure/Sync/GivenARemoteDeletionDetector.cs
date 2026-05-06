using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenARemoteDeletionDetector
{
    private const string BaseDir   = "/sync-root";
    private const string LocalFile = $"{BaseDir}/file.txt";

    private readonly ISyncedItemRepository _syncedItemRepository = Substitute.For<ISyncedItemRepository>();
    private readonly AccountId             _accountId            = new("user-1");

    private RemoteDeletionDetector CreateSut(MockFileSystem mockFs) => new(_syncedItemRepository, mockFs);

    private static List<SyncRuleEntity> IncludeRules(params string[] paths)
        => paths.Select(p => new SyncRuleEntity { RemotePath = p, RuleType = RuleType.Include }).ToList();

    [Fact]
    public async Task when_remote_id_is_in_seen_set_then_local_file_is_not_deleted()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile(LocalFile, new MockFileData("data"));
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/file.txt", LocalPath = LocalFile, IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "item-1" };
        var sut = CreateSut(mockFs);

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/file.txt"), TestContext.Current.CancellationToken);

        mockFs.File.Exists(LocalFile).ShouldBeTrue();
    }

    [Fact]
    public async Task when_remote_id_absent_and_local_file_exists_then_file_is_deleted()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile(LocalFile, new MockFileData("data"));
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/file.txt", LocalPath = LocalFile, IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut(mockFs);

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/file.txt"), TestContext.Current.CancellationToken);

        mockFs.File.Exists(LocalFile).ShouldBeFalse();
    }

    [Fact]
    public async Task when_remote_id_absent_and_local_file_does_not_exist_then_no_exception_is_thrown()
    {
        var mockFs = new MockFileSystem();
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/gone.txt", LocalPath = $"{BaseDir}/gone.txt", IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut(mockFs);

        var act = async () => await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/gone.txt"), TestContext.Current.CancellationToken);

        await act.ShouldNotThrowAsync();
    }

    [Fact]
    public async Task when_remote_id_absent_and_item_is_folder_then_directory_is_deleted_recursively()
    {
        const string localDir = $"{BaseDir}/subfolder";
        const string childFile = $"{localDir}/child.txt";
        var mockFs = new MockFileSystem();
        mockFs.AddDirectory(localDir);
        mockFs.AddFile(childFile, new MockFileData("data"));
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["folder-1"] = new() { RemoteItemId = new OneDriveItemId("folder-1"), RemotePath = "/subfolder", LocalPath = localDir, IsFolder = true }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut(mockFs);

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/subfolder"), TestContext.Current.CancellationToken);

        mockFs.Directory.Exists(localDir).ShouldBeFalse();
    }

    [Fact]
    public async Task when_remote_id_absent_then_synced_item_repository_delete_is_called()
    {
        var mockFs = new MockFileSystem();
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/file.txt", LocalPath = $"{BaseDir}/file.txt", IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut(mockFs);

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/file.txt"), TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).DeleteByRemoteIdAsync(Arg.Is(_accountId), Arg.Is<OneDriveItemId>(id => id.Id == "item-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_remote_path_does_not_match_include_rules_then_item_is_not_treated_as_deleted()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddFile(LocalFile, new MockFileData("data"));
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/Other/other.txt", LocalPath = LocalFile, IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut(mockFs);

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/Documents"), TestContext.Current.CancellationToken);

        mockFs.File.Exists(LocalFile).ShouldBeTrue();
        await _syncedItemRepository.DidNotReceive().DeleteByRemoteIdAsync(Arg.Any<AccountId>(), Arg.Any<OneDriveItemId>(), Arg.Any<CancellationToken>());
    }
}
