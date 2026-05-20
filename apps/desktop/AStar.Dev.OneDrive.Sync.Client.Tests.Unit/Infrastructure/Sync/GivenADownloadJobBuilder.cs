using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using Microsoft.Extensions.Logging;
using Testably.Abstractions.Testing;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenADownloadJobBuilder
{
    private const string BasePath = "/sync-root";

    private readonly ISyncedItemRegistrar _syncedItemRegistrar = Substitute.For<ISyncedItemRegistrar>();

    private DownloadJobBuilder CreateSut(MockFileSystem mockFileSystem) => new(_syncedItemRegistrar, mockFileSystem, Substitute.For<ILogger<DownloadJobBuilder>>());

    private static OneDriveAccount CreateAccount(ConflictPolicy policy = ConflictPolicy.Ignore) => new()
    {
        Id = new AccountId("user-1"),
        Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
        SyncConfig = AccountSyncConfigFactory.Create(policy, LocalSyncPath.Restore(BasePath)),
        SelectedFolderIds = []
    };

    private static SyncRuleEntity IncludeRule(string remotePath)
        => new() { RemotePath = remotePath, RuleType = RuleType.Include };

    private static FileDeltaItem FileItem(string id, string relativePath, string? etag = null, DateTimeOffset? lastModified = null)
        => DeltaItemFactory.CreateFile(new OneDriveItemId(id), new DriveId("drive-1"), null, ItemPathFactory.Create(id, relativePath), 100L, lastModified ?? DateTimeOffset.UtcNow.AddDays(-1), null, VersionInfoFactory.Create(etag, null));

    private static FolderDeltaItem FolderItem(string id, string relativePath)
        => DeltaItemFactory.CreateFolder(new OneDriveItemId(id), new DriveId("drive-1"), null, ItemPathFactory.Create(id, relativePath), VersionInfoFactory.Create(null, null));

    [Fact]
    public async Task when_item_path_is_not_included_by_rules_then_no_job_is_created()
    {
        var sut = CreateSut(new MockFileSystem());
        var items = new List<DeltaItem> { FileItem("item-a", "/Other/a.txt") };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildAsync(CreateAccount(), items, rules, [], _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_folder_item_is_encountered_then_register_folder_is_called()
    {
        var sut = CreateSut(new MockFileSystem());
        var items = new List<DeltaItem> { FolderItem("subfolder-1", "/Documents/Sub") };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        await sut.BuildAsync(CreateAccount(), items, rules, [], _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRegistrar.Received(1).RegisterFolderAsync(Arg.Any<AccountId>(), Arg.Is<FolderDeltaItem>(i => i.Id.Id == "subfolder-1"), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, SyncedItemEntity>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_folder_item_is_encountered_then_no_job_is_created()
    {
        var sut = CreateSut(new MockFileSystem());
        var items = new List<DeltaItem> { FolderItem("subfolder-1", "/Documents/Sub") };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildAsync(CreateAccount(), items, rules, [], _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_file_has_no_known_item_and_no_local_file_then_download_job_is_created()
    {
        var sut = CreateSut(new MockFileSystem());
        var items = new List<DeltaItem> { FileItem("item-a", "/Documents/a.txt") };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildAsync(CreateAccount(), items, rules, [], _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
        result[0].ShouldBeOfType<DownloadSyncJob>();
        result[0].Remote.RemoteItemId.Id.ShouldBe("item-a");
    }

    [Fact]
    public async Task when_etag_matches_and_local_file_exists_then_no_job_is_created()
    {
        const string localFile = $"{BasePath}/Documents/a.txt";
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(localFile).Which(m => m.HasStringContent("data"));

        var knownItem = new SyncedItemEntity
        {
            AccountId = new AccountId("user-1"),
            RemoteItemId = new OneDriveItemId("item-a"),
            RemotePath = "/Documents/a.txt",
            LocalPath = localFile,
            Tags = VersionInfoFactory.Create("etag-123", null),
            RemoteModifiedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        var syncedItems = new Dictionary<string, SyncedItemEntity> { ["item-a"] = knownItem };

        var sut = CreateSut(mockFileSystem);
        var items = new List<DeltaItem> { FileItem("item-a", "/Documents/a.txt", etag: "etag-123") };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildAsync(CreateAccount(), items, rules, syncedItems, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_local_file_is_newer_than_known_remote_then_conflict_callback_is_invoked()
    {
        const string localFile = $"{BasePath}/Documents/a.txt";
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(localFile).Which(m => m.HasStringContent("modified locally"));
        mockFileSystem.File.SetLastWriteTime(localFile, DateTime.UtcNow);

        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-10);
        var knownItem = new SyncedItemEntity
        {
            AccountId = new AccountId("user-1"),
            RemoteItemId = new OneDriveItemId("item-a"),
            RemotePath = "/Documents/a.txt",
            LocalPath = localFile,
            RemoteModifiedAt = remoteModified
        };
        var syncedItems = new Dictionary<string, SyncedItemEntity> { ["item-a"] = knownItem };

        var conflictsDetected = new List<SyncConflict>();
        var sut = CreateSut(mockFileSystem);
        var items = new List<DeltaItem> { FileItem("item-a", "/Documents/a.txt", lastModified: remoteModified) };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        await sut.BuildAsync(CreateAccount(), items, rules, syncedItems, conflict =>
        {
            conflictsDetected.Add(conflict);
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);

        conflictsDetected.ShouldHaveSingleItem();
        conflictsDetected[0].Remote.RemoteItemId.Id.ShouldBe("item-a");
    }

    [Fact]
    public async Task when_phantom_file_is_detected_then_register_phantom_is_called()
    {
        const string localFile = $"{BasePath}/Documents/phantom.txt";
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(localFile).Which(m => m.HasStringContent("phantom"));

        var sut = CreateSut(mockFileSystem);
        var items = new List<DeltaItem> { FileItem("item-phantom", "/Documents/phantom.txt") };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        await sut.BuildAsync(CreateAccount(), items, rules, [], _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        await _syncedItemRegistrar.Received(1).RegisterPhantomAsync(Arg.Any<AccountId>(), Arg.Is<FileDeltaItem>(i => i.Id.Id == "item-phantom"), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, SyncedItemEntity>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_phantom_file_is_detected_then_no_job_is_created()
    {
        const string localFile = $"{BasePath}/Documents/phantom.txt";
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(localFile).Which(m => m.HasStringContent("phantom"));

        var sut = CreateSut(mockFileSystem);
        var items = new List<DeltaItem> { FileItem("item-phantom", "/Documents/phantom.txt") };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildAsync(CreateAccount(), items, rules, [], _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_known_item_exists_with_matching_remote_timestamp_and_null_etag_and_local_file_exists_then_no_job_is_created()
    {
        const string localFile = $"{BasePath}/Documents/a.txt";
        var remoteModified = DateTimeOffset.UtcNow.AddDays(-1);
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(localFile).Which(m => m.HasStringContent("data"));
        mockFileSystem.File.SetLastWriteTimeUtc(localFile, remoteModified.UtcDateTime);

        var knownItem = new SyncedItemEntity
        {
            AccountId = new AccountId("user-1"),
            RemoteItemId = new OneDriveItemId("item-a"),
            RemotePath = "/Documents/a.txt",
            LocalPath = localFile,
            RemoteModifiedAt = remoteModified,
            Tags = VersionInfoFactory.Create(null, null)
        };
        var syncedItems = new Dictionary<string, SyncedItemEntity> { ["item-a"] = knownItem };

        var sut = CreateSut(mockFileSystem);
        var items = new List<DeltaItem> { FileItem("item-a", "/Documents/a.txt", lastModified: remoteModified) };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildAsync(CreateAccount(), items, rules, syncedItems, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_known_item_exists_and_remote_is_newer_than_stored_modified_and_null_etag_then_download_job_is_created()
    {
        const string localFile = $"{BasePath}/Documents/a.txt";
        var storedRemoteModified = DateTimeOffset.UtcNow.AddDays(-2);
        var newRemoteModified = DateTimeOffset.UtcNow.AddDays(-1);
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(localFile).Which(m => m.HasStringContent("data"));
        mockFileSystem.File.SetLastWriteTimeUtc(localFile, storedRemoteModified.UtcDateTime);

        var knownItem = new SyncedItemEntity
        {
            AccountId = new AccountId("user-1"),
            RemoteItemId = new OneDriveItemId("item-a"),
            RemotePath = "/Documents/a.txt",
            LocalPath = localFile,
            RemoteModifiedAt = storedRemoteModified,
            Tags = VersionInfoFactory.Create(null, null)
        };
        var syncedItems = new Dictionary<string, SyncedItemEntity> { ["item-a"] = knownItem };

        var sut = CreateSut(mockFileSystem);
        var items = new List<DeltaItem> { FileItem("item-a", "/Documents/a.txt", lastModified: newRemoteModified) };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildAsync(CreateAccount(), items, rules, syncedItems, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
        result[0].ShouldBeOfType<DownloadSyncJob>();
    }

    [Fact]
    public async Task when_file_item_has_nested_path_then_download_job_local_path_starts_with_base_path()
    {
        var sut = CreateSut(new MockFileSystem());
        var items = new List<DeltaItem> { FileItem("item-a", "/Documents/2024/report.txt") };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildAsync(CreateAccount(), items, rules, [], _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
        result[0].Target.LocalPath.ShouldStartWith(BasePath);
    }

    [Fact]
    public async Task when_file_item_has_nested_path_then_download_job_local_path_is_not_equal_to_base_path()
    {
        var sut = CreateSut(new MockFileSystem());
        var items = new List<DeltaItem> { FileItem("item-a", "/Documents/2024/report.txt") };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildAsync(CreateAccount(), items, rules, [], _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
        result[0].Target.LocalPath.ShouldNotBe(BasePath);
    }

    [Fact]
    public async Task when_conflict_policy_is_use_remote_then_download_job_is_created()
    {
        const string localFile = $"{BasePath}/Documents/a.txt";
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(localFile).Which(m => m.HasStringContent("modified locally"));
        mockFileSystem.File.SetLastWriteTime(localFile, DateTime.UtcNow);

        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-10);
        var knownItem = new SyncedItemEntity
        {
            AccountId = new AccountId("user-1"),
            RemoteItemId = new OneDriveItemId("item-a"),
            RemotePath = "/Documents/a.txt",
            LocalPath = localFile,
            RemoteModifiedAt = remoteModified
        };
        var syncedItems = new Dictionary<string, SyncedItemEntity> { ["item-a"] = knownItem };

        var sut = CreateSut(mockFileSystem);
        var items = new List<DeltaItem> { FileItem("item-a", "/Documents/a.txt", lastModified: remoteModified) };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildAsync(CreateAccount(ConflictPolicy.RemoteWins), items, rules, syncedItems, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
        result[0].ShouldBeOfType<DownloadSyncJob>();
    }

    [Fact]
    public async Task when_conflict_policy_is_use_local_then_upload_job_is_created()
    {
        const string localFile = $"{BasePath}/Documents/a.txt";
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(localFile).Which(m => m.HasStringContent("modified locally"));
        mockFileSystem.File.SetLastWriteTime(localFile, DateTime.UtcNow);

        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-10);
        var knownItem = new SyncedItemEntity
        {
            AccountId = new AccountId("user-1"),
            RemoteItemId = new OneDriveItemId("item-a"),
            RemotePath = "/Documents/a.txt",
            LocalPath = localFile,
            RemoteModifiedAt = remoteModified
        };
        var syncedItems = new Dictionary<string, SyncedItemEntity> { ["item-a"] = knownItem };

        var sut = CreateSut(mockFileSystem);
        var items = new List<DeltaItem> { FileItem("item-a", "/Documents/a.txt", lastModified: remoteModified) };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildAsync(CreateAccount(ConflictPolicy.LocalWins), items, rules, syncedItems, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
        result[0].ShouldBeOfType<UploadSyncJob>();
    }

    [Fact]
    public async Task when_conflict_policy_is_ignore_then_no_job_is_created()
    {
        const string localFile = $"{BasePath}/Documents/a.txt";
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(localFile).Which(m => m.HasStringContent("modified locally"));
        mockFileSystem.File.SetLastWriteTime(localFile, DateTime.UtcNow);

        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-10);
        var knownItem = new SyncedItemEntity
        {
            AccountId = new AccountId("user-1"),
            RemoteItemId = new OneDriveItemId("item-a"),
            RemotePath = "/Documents/a.txt",
            LocalPath = localFile,
            RemoteModifiedAt = remoteModified
        };
        var syncedItems = new Dictionary<string, SyncedItemEntity> { ["item-a"] = knownItem };

        var sut = CreateSut(mockFileSystem);
        var items = new List<DeltaItem> { FileItem("item-a", "/Documents/a.txt", lastModified: remoteModified) };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildAsync(CreateAccount(ConflictPolicy.Ignore), items, rules, syncedItems, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_download_job_is_created_then_metadata_version_info_is_populated_from_delta_item()
    {
        var sut = CreateSut(new MockFileSystem());
        var items = new List<DeltaItem> { FileItem("item-a", "/Documents/a.txt", etag: "etag-from-delta") };
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildAsync(CreateAccount(), items, rules, [], _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem();
        result[0].Metadata.VersionInfo!.ETag.ShouldBe("etag-from-delta");
    }
}