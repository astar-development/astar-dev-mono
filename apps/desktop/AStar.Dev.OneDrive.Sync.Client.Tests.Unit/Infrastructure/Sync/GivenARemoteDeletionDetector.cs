using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenARemoteDeletionDetector : IDisposable
{
    private readonly ISyncedItemRepository _syncedItemRepository = Substitute.For<ISyncedItemRepository>();
    private readonly string                _tempBase             = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly AccountId             _accountId            = new("user-1");

    public GivenARemoteDeletionDetector() => Directory.CreateDirectory(_tempBase);

    public void Dispose()
    {
        if(Directory.Exists(_tempBase))
            Directory.Delete(_tempBase, recursive: true);
    }

    private RemoteDeletionDetector CreateSut() => new(_syncedItemRepository);

    private static List<SyncRuleEntity> IncludeRules(params string[] paths)
        => paths.Select(p => new SyncRuleEntity { RemotePath = p, RuleType = RuleType.Include }).ToList();

    [Fact]
    public async Task when_remote_id_is_in_seen_set_then_local_file_is_not_deleted()
    {
        string localFile = Path.Combine(_tempBase, "file.txt");
        File.WriteAllText(localFile, "data");

        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/file.txt", LocalPath = localFile, IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "item-1" };
        var sut = CreateSut();

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/file.txt"), TestContext.Current.CancellationToken);

        File.Exists(localFile).ShouldBeTrue();
    }

    [Fact]
    public async Task when_remote_id_absent_and_local_file_exists_then_file_is_deleted()
    {
        string localFile = Path.Combine(_tempBase, "file.txt");
        File.WriteAllText(localFile, "data");

        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/file.txt", LocalPath = localFile, IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut();

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/file.txt"), TestContext.Current.CancellationToken);

        File.Exists(localFile).ShouldBeFalse();
    }

    [Fact]
    public async Task when_remote_id_absent_and_local_file_does_not_exist_then_no_exception_is_thrown()
    {
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/gone.txt", LocalPath = Path.Combine(_tempBase, "gone.txt"), IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut();

        var act = async () => await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/gone.txt"), TestContext.Current.CancellationToken);

        await act.ShouldNotThrowAsync();
    }

    [Fact]
    public async Task when_remote_id_absent_and_item_is_folder_then_directory_is_deleted_recursively()
    {
        string localDir = Path.Combine(_tempBase, "subfolder");
        Directory.CreateDirectory(localDir);
        File.WriteAllText(Path.Combine(localDir, "child.txt"), "data");

        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["folder-1"] = new() { RemoteItemId = new OneDriveItemId("folder-1"), RemotePath = "/subfolder", LocalPath = localDir, IsFolder = true }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut();

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/subfolder"), TestContext.Current.CancellationToken);

        Directory.Exists(localDir).ShouldBeFalse();
    }

    [Fact]
    public async Task when_remote_id_absent_then_synced_item_repository_delete_is_called()
    {
        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/file.txt", LocalPath = Path.Combine(_tempBase, "file.txt"), IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut();

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/file.txt"), TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).DeleteByRemoteIdAsync(Arg.Is(_accountId), Arg.Is<OneDriveItemId>(id => id.Id == "item-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_remote_path_does_not_match_include_rules_then_item_is_not_treated_as_deleted()
    {
        string localFile = Path.Combine(_tempBase, "other.txt");
        File.WriteAllText(localFile, "data");

        var syncedItems = new Dictionary<string, SyncedItemEntity>
        {
            ["item-1"] = new() { RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/Other/other.txt", LocalPath = localFile, IsFolder = false }
        };
        var seenRemoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sut = CreateSut();

        await sut.DetectAndApplyAsync(_accountId, syncedItems, seenRemoteIds, IncludeRules("/Documents"), TestContext.Current.CancellationToken);

        File.Exists(localFile).ShouldBeTrue();
        await _syncedItemRepository.DidNotReceive().DeleteByRemoteIdAsync(Arg.Any<AccountId>(), Arg.Any<OneDriveItemId>(), Arg.Any<CancellationToken>());
    }
}
