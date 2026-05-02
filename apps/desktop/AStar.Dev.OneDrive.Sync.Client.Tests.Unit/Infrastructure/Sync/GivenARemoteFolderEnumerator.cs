using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Accounts;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenARemoteFolderEnumerator
{
    private const string BasePath = "/sync-root";

    private readonly IGraphService            _graphService            = Substitute.For<IGraphService>();
    private readonly ISyncRuleRepository      _syncRuleRepository      = Substitute.For<ISyncRuleRepository>();
    private readonly ISyncedItemRepository    _syncedItemRepository    = Substitute.For<ISyncedItemRepository>();

    public GivenARemoteFolderEnumerator()
    {
        _syncedItemRepository.GetAllByAccountAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, SyncedItemEntity>());
        _graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("drive-1");
    }

    private RemoteFolderEnumerator CreateSut(MockFileSystem mockFs) => new(_graphService, _syncRuleRepository, _syncedItemRepository, mockFs);

    private static OneDriveAccount CreateAccount() => new()
    {
        Id             = new AccountId("user-1"),
        Email          = "user@outlook.com",
        LocalSyncPath  = LocalSyncPath.Restore(BasePath),
        SelectedFolderIds = []
    };

    private static SyncRuleEntity IncludeRule(string remotePath, string? remoteItemId = null)
        => new() { RemotePath = remotePath, RuleType = RuleType.Include, RemoteItemId = remoteItemId };

    private static DeltaItem FileItem(string id, string name, string? relativePath = null, string? etag = null)
        => new(id, "drive-1", name, null, false, false, 100L, DateTimeOffset.UtcNow.AddDays(-1), null, relativePath ?? name, etag);

    private static DeltaItem FolderItem(string id, string name, string? relativePath = null)
        => new(id, "drive-1", name, null, true, false, 0L, null, null, relativePath ?? name);

    [Fact]
    public async Task when_no_rules_configured_then_result_has_no_rules_flag_set()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = CreateSut(new MockFileSystem());

        var result = await sut.EnumerateAsync(CreateAccount(), "token", _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.HadNoRules.ShouldBeTrue();
    }

    [Fact]
    public async Task when_no_rules_configured_then_graph_drive_id_is_not_requested()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = CreateSut(new MockFileSystem());

        await sut.EnumerateAsync(CreateAccount(), "token", _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _graphService.DidNotReceive().GetDriveIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_no_rules_configured_then_download_jobs_is_empty()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = CreateSut(new MockFileSystem());

        var result = await sut.EnumerateAsync(CreateAccount(), "token", _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.DownloadJobs.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_folder_id_cannot_be_resolved_then_enumerate_folder_is_not_called()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents")]);
        _graphService.GetFolderIdByPathAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);
        var sut = CreateSut(new MockFileSystem());

        await sut.EnumerateAsync(CreateAccount(), "token", _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _graphService.DidNotReceive().EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_folder_id_resolved_from_graph_then_sync_rule_repository_upsert_is_called_to_back_fill()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: null)]);
        _graphService.GetFolderIdByPathAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("folder-resolved");
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = CreateSut(new MockFileSystem());

        await sut.EnumerateAsync(CreateAccount(), "token", _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncRuleRepository.Received(1).UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Is(RuleType.Include), Arg.Is("folder-resolved"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_rule_already_has_matching_remote_item_id_then_sync_rule_upsert_is_not_called()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = CreateSut(new MockFileSystem());

        await sut.EnumerateAsync(CreateAccount(), "token", _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncRuleRepository.DidNotReceive().UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Any<RuleType>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_enumeration_returns_items_then_seen_remote_ids_contains_all_item_ids()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([FileItem("item-a", "a.txt", "/Documents/a.txt"), FileItem("item-b", "b.txt", "/Documents/b.txt")]);
        var sut = CreateSut(new MockFileSystem());

        var result = await sut.EnumerateAsync(CreateAccount(), "token", _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.SeenRemoteIds.ShouldContain("item-a");
        result.SeenRemoteIds.ShouldContain("item-b");
    }

    [Fact]
    public async Task when_item_has_no_known_synced_item_and_no_local_file_then_download_job_is_created()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([FileItem("item-a", "a.txt", "/Documents/a.txt")]);
        var sut = CreateSut(new MockFileSystem());

        var result = await sut.EnumerateAsync(CreateAccount(), "token", _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.DownloadJobs.ShouldHaveSingleItem();
        result.DownloadJobs[0].RemoteItemId.ShouldBe("item-a");
        result.DownloadJobs[0].Direction.ShouldBe(SyncDirection.Download);
    }

    [Fact]
    public async Task when_etag_matches_and_local_file_exists_then_no_download_job_is_created()
    {
        const string localFile = $"{BasePath}/Documents/a.txt";
        var mockFs = new MockFileSystem();
        mockFs.AddFile(localFile, new MockFileData("data"));

        var knownItem = new SyncedItemEntity
        {
            AccountId      = new AccountId("user-1"),
            RemoteItemId   = new OneDriveItemId("item-a"),
            RemotePath     = "/Documents/a.txt",
            LocalPath      = localFile,
            ETag           = "etag-123",
            RemoteModifiedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        _syncedItemRepository.GetAllByAccountAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, SyncedItemEntity> { ["item-a"] = knownItem });
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([FileItem("item-a", "a.txt", "/Documents/a.txt", etag: "etag-123")]);
        var sut = CreateSut(mockFs);

        var result = await sut.EnumerateAsync(CreateAccount(), "token", _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.DownloadJobs.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_local_file_is_newer_than_known_remote_by_more_than_5_seconds_then_on_conflict_callback_is_invoked()
    {
        const string localFile = $"{BasePath}/Documents/a.txt";
        var localWriteTime = DateTime.UtcNow;
        var fileData = new MockFileData("modified locally") { LastWriteTime = localWriteTime };
        var mockFs = new MockFileSystem();
        mockFs.AddFile(localFile, fileData);

        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-10);
        var knownItem = new SyncedItemEntity
        {
            AccountId        = new AccountId("user-1"),
            RemoteItemId     = new OneDriveItemId("item-a"),
            RemotePath       = "/Documents/a.txt",
            LocalPath        = localFile,
            RemoteModifiedAt = remoteModified
        };
        _syncedItemRepository.GetAllByAccountAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, SyncedItemEntity> { ["item-a"] = knownItem });
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([new DeltaItem("item-a", "drive-1", "a.txt", null, false, false, 100L, remoteModified, null, "/Documents/a.txt")]);

        var conflictsDetected = new List<SyncConflict>();
        var sut = CreateSut(mockFs);

        await sut.EnumerateAsync(CreateAccount(), "token", conflict =>
        {
            conflictsDetected.Add(conflict);
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);

        conflictsDetected.ShouldHaveSingleItem();
        conflictsDetected[0].RemoteItemId.ShouldBe("item-a");
    }

    [Fact]
    public async Task when_item_is_a_folder_then_synced_item_repository_upsert_is_called()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([FolderItem("subfolder-1", "Sub", "/Documents/Sub")]);
        var sut = CreateSut(new MockFileSystem());

        await sut.EnumerateAsync(CreateAccount(), "token", _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertAsync(Arg.Is<SyncedItemEntity>(e => e.IsFolder && e.RemoteItemId.Id == "subfolder-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_file_exists_locally_without_synced_item_then_phantom_item_is_upserted()
    {
        const string localFile = $"{BasePath}/Documents/phantom.txt";
        var mockFs = new MockFileSystem();
        mockFs.AddFile(localFile, new MockFileData("phantom"));

        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([FileItem("item-phantom", "phantom.txt", "/Documents/phantom.txt")]);
        var sut = CreateSut(mockFs);

        await sut.EnumerateAsync(CreateAccount(), "token", _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertAsync(Arg.Is<SyncedItemEntity>(e => e.RemoteItemId.Id == "item-phantom"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_result_returned_then_rules_are_included_in_result()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = CreateSut(new MockFileSystem());

        var result = await sut.EnumerateAsync(CreateAccount(), "token", _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.Rules.ShouldHaveSingleItem();
        result.Rules[0].RemotePath.ShouldBe("/Documents");
    }

    [Fact]
    public async Task when_remote_file_item_has_nested_relative_path_then_download_job_local_path_starts_with_base_path()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([FileItem("item-a", "report.txt", "/Documents/2024/report.txt")]);
        var sut = CreateSut(new MockFileSystem());

        var result = await sut.EnumerateAsync(CreateAccount(), "token", _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.DownloadJobs.ShouldHaveSingleItem();
        result.DownloadJobs[0].LocalPath.ShouldStartWith(BasePath);
    }

    [Fact]
    public async Task when_remote_file_item_has_nested_relative_path_then_download_job_local_path_does_not_equal_base_path()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([FileItem("item-a", "report.txt", "/Documents/2024/report.txt")]);
        var sut = CreateSut(new MockFileSystem());

        var result = await sut.EnumerateAsync(CreateAccount(), "token", _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.DownloadJobs.ShouldHaveSingleItem();
        result.DownloadJobs[0].LocalPath.ShouldNotBe(BasePath);
    }
}
