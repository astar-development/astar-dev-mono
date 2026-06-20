using System.Collections.Concurrent;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Jobs;

public sealed class GivenADownloadJobBuilder
{
    private const string BasePath = "/sync-root";

    private readonly ISyncedItemRegistrar _syncedItemRegistrar = Substitute.For<ISyncedItemRegistrar>();

    private DownloadJobBuilder CreateSut(MockFileSystem mockFileSystem) => new(_syncedItemRegistrar, mockFileSystem, Substitute.For<ILogger<DownloadJobBuilder>>());

    private static OneDriveAccount CreateAccount(ConflictPolicy policy = ConflictPolicy.Ignore) => new()
    {
        Id = new AccountId("user-1"),
        Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
        SyncConfig = Option.Some(AccountSyncConfigFactory.Create(policy, LocalSyncPath.Restore(BasePath))),
        SelectedFolderIds = []
    };

    private static AccountSyncConfig CreateSyncConfig(ConflictPolicy policy = ConflictPolicy.Ignore)
        => AccountSyncConfigFactory.Create(policy, LocalSyncPath.Restore(BasePath));

    private static SyncRuleEntity IncludeRule(string remotePath)
        => new() { RemotePath = remotePath, RuleType = RuleType.Include };

    private static FileDeltaItem FileItem(string id, string relativePath, Option<string> etag = default, Option<DateTimeOffset> lastModified = default)
        => DeltaItemFactory.CreateFile(new OneDriveItemId(id), new DriveId("drive-1"), Option.None<OneDriveFolderId>(), ItemPathFactory.Create(id, relativePath), 100L, lastModified ?? Option.None<DateTimeOffset>(), Option.None<string>(), VersionInfoFactory.Create(etag ?? Option.None<string>(), Option.None<string>()));

    private static FolderDeltaItem FolderItem(string id, string relativePath)
        => DeltaItemFactory.CreateFolder(new OneDriveItemId(id), new DriveId("drive-1"), Option.None<OneDriveFolderId>(), ItemPathFactory.Create(id, relativePath), VersionInfoFactory.Create(Option.None<string>(), Option.None<string>()));

    [Fact]
    public async Task when_item_path_is_not_included_by_rules_then_no_job_is_created()
    {
        var sut = CreateSut(new MockFileSystem());
        var item = FileItem("item-a", "/Other/a.txt");
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildOneAsync(CreateAccount(), CreateSyncConfig(), item, rules, new ConcurrentDictionary<string, SyncedItemEntity>(), _ => Task.CompletedTask, [], TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task when_folder_item_is_encountered_then_register_folder_is_called()
    {
        var sut = CreateSut(new MockFileSystem());
        var item = FolderItem("subfolder-1", "/Documents/Sub");
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        await sut.BuildOneAsync(CreateAccount(), CreateSyncConfig(), item, rules, new ConcurrentDictionary<string, SyncedItemEntity>(), _ => Task.CompletedTask, [], TestContext.Current.CancellationToken);

        await _syncedItemRegistrar.Received(1).RegisterFolderAsync(Arg.Any<AccountId>(), Arg.Is<FolderDeltaItem>(i => i.Id.Id == "subfolder-1"), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_folder_item_is_encountered_then_no_job_is_created()
    {
        var sut = CreateSut(new MockFileSystem());
        var item = FolderItem("subfolder-1", "/Documents/Sub");
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildOneAsync(CreateAccount(), CreateSyncConfig(), item, rules, new ConcurrentDictionary<string, SyncedItemEntity>(), _ => Task.CompletedTask, [], TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task when_file_has_no_known_item_and_no_local_file_then_download_job_is_created()
    {
        var sut = CreateSut(new MockFileSystem());
        var item = FileItem("item-a", "/Documents/a.txt");
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildOneAsync(CreateAccount(), CreateSyncConfig(), item, rules, new ConcurrentDictionary<string, SyncedItemEntity>(), _ => Task.CompletedTask, [], TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<DownloadSyncJob>();
        result.Remote.RemoteItemId.Id.ShouldBe("item-a");
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
            Tags = VersionInfoFactory.Create("etag-123", Option.None<string>()),
            RemoteModifiedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity> { ["item-a"] = knownItem };

        var sut = CreateSut(mockFileSystem);
        var item = FileItem("item-a", "/Documents/a.txt", etag: "etag-123");
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildOneAsync(CreateAccount(), CreateSyncConfig(), item, rules, syncedItems, _ => Task.CompletedTask, [], TestContext.Current.CancellationToken);

        result.ShouldBeNull();
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
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity> { ["item-a"] = knownItem };

        var conflictsDetected = new List<SyncConflict>();
        var sut = CreateSut(mockFileSystem);
        var item = FileItem("item-a", "/Documents/a.txt", lastModified: remoteModified);
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        await sut.BuildOneAsync(CreateAccount(), CreateSyncConfig(), item, rules, syncedItems, conflict =>
        {
            conflictsDetected.Add(conflict);
            return Task.CompletedTask;
        }, [], TestContext.Current.CancellationToken);

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
        var item = FileItem("item-phantom", "/Documents/phantom.txt");
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        await sut.BuildOneAsync(CreateAccount(), CreateSyncConfig(), item, rules, new ConcurrentDictionary<string, SyncedItemEntity>(), _ => Task.CompletedTask, [], TestContext.Current.CancellationToken);

        await _syncedItemRegistrar.Received(1).RegisterPhantomAsync(Arg.Any<AccountId>(), Arg.Is<FileDeltaItem>(i => i.Id.Id == "item-phantom"), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Any<IReadOnlyList<FileClassificationCategory>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_phantom_file_is_detected_then_preloaded_mappings_are_forwarded_to_registrar()
    {
        const string localFile = $"{BasePath}/Documents/phantom.txt";
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(localFile).Which(m => m.HasStringContent("phantom"));

        var sut = CreateSut(mockFileSystem);
        var item = FileItem("item-phantom", "/Documents/phantom.txt");
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };
        IReadOnlyList<FileClassificationCategory> mappings =
        [
            ((Result<FileClassificationCategory, string>.Ok)FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(), "Documents", 1, false, false, Option.None<FileClassificationCategoryId>())).Value
        ];

        await sut.BuildOneAsync(CreateAccount(), CreateSyncConfig(), item, rules, new ConcurrentDictionary<string, SyncedItemEntity>(), _ => Task.CompletedTask, mappings, TestContext.Current.CancellationToken);

        await _syncedItemRegistrar.Received(1).RegisterPhantomAsync(Arg.Any<AccountId>(), Arg.Any<FileDeltaItem>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ConcurrentDictionary<string, SyncedItemEntity>>(), Arg.Is<IReadOnlyList<FileClassificationCategory>>(m => m.Count == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_phantom_file_is_detected_then_no_job_is_created()
    {
        const string localFile = $"{BasePath}/Documents/phantom.txt";
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Initialize().WithFile(localFile).Which(m => m.HasStringContent("phantom"));

        var sut = CreateSut(mockFileSystem);
        var item = FileItem("item-phantom", "/Documents/phantom.txt");
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildOneAsync(CreateAccount(), CreateSyncConfig(), item, rules, new ConcurrentDictionary<string, SyncedItemEntity>(), _ => Task.CompletedTask, [], TestContext.Current.CancellationToken);

        result.ShouldBeNull();
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
            Tags = VersionInfoFactory.Create(Option.None<string>(), Option.None<string>())
        };
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity> { ["item-a"] = knownItem };

        var sut = CreateSut(mockFileSystem);
        var item = FileItem("item-a", "/Documents/a.txt", lastModified: remoteModified);
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildOneAsync(CreateAccount(), CreateSyncConfig(), item, rules, syncedItems, _ => Task.CompletedTask, [], TestContext.Current.CancellationToken);

        result.ShouldBeNull();
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
            Tags = VersionInfoFactory.Create(Option.None<string>(), Option.None<string>())
        };
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity> { ["item-a"] = knownItem };

        var sut = CreateSut(mockFileSystem);
        var item = FileItem("item-a", "/Documents/a.txt", lastModified: newRemoteModified);
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildOneAsync(CreateAccount(), CreateSyncConfig(), item, rules, syncedItems, _ => Task.CompletedTask, [], TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<DownloadSyncJob>();
    }

    [Fact]
    public async Task when_file_item_has_nested_path_then_download_job_local_path_starts_with_base_path()
    {
        var sut = CreateSut(new MockFileSystem());
        var item = FileItem("item-a", "/Documents/2024/report.txt");
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildOneAsync(CreateAccount(), CreateSyncConfig(), item, rules, new ConcurrentDictionary<string, SyncedItemEntity>(), _ => Task.CompletedTask, [], TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Target.LocalPath.ShouldStartWith(BasePath);
    }

    [Fact]
    public async Task when_file_item_has_nested_path_then_download_job_local_path_is_not_equal_to_base_path()
    {
        var sut = CreateSut(new MockFileSystem());
        var item = FileItem("item-a", "/Documents/2024/report.txt");
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildOneAsync(CreateAccount(), CreateSyncConfig(), item, rules, new ConcurrentDictionary<string, SyncedItemEntity>(), _ => Task.CompletedTask, [], TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Target.LocalPath.ShouldNotBe(BasePath);
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
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity> { ["item-a"] = knownItem };

        var sut = CreateSut(mockFileSystem);
        var item = FileItem("item-a", "/Documents/a.txt", lastModified: remoteModified);
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildOneAsync(CreateAccount(ConflictPolicy.RemoteWins), CreateSyncConfig(ConflictPolicy.RemoteWins), item, rules, syncedItems, _ => Task.CompletedTask, [], TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<DownloadSyncJob>();
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
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity> { ["item-a"] = knownItem };

        var sut = CreateSut(mockFileSystem);
        var item = FileItem("item-a", "/Documents/a.txt", lastModified: remoteModified);
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildOneAsync(CreateAccount(ConflictPolicy.LocalWins), CreateSyncConfig(ConflictPolicy.LocalWins), item, rules, syncedItems, _ => Task.CompletedTask, [], TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<UploadSyncJob>();
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
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity> { ["item-a"] = knownItem };

        var sut = CreateSut(mockFileSystem);
        var item = FileItem("item-a", "/Documents/a.txt", lastModified: remoteModified);
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildOneAsync(CreateAccount(ConflictPolicy.Ignore), CreateSyncConfig(ConflictPolicy.Ignore), item, rules, syncedItems, _ => Task.CompletedTask, [], TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task when_download_job_is_created_then_metadata_version_info_is_populated_from_delta_item()
    {
        var sut = CreateSut(new MockFileSystem());
        var item = FileItem("item-a", "/Documents/a.txt", etag: "etag-from-delta");
        var rules = new List<SyncRuleEntity> { IncludeRule("/Documents") };

        var result = await sut.BuildOneAsync(CreateAccount(), CreateSyncConfig(), item, rules, new ConcurrentDictionary<string, SyncedItemEntity>(), _ => Task.CompletedTask, [], TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Metadata.VersionInfo.TryGetValue(out var vi).ShouldBeTrue();
        vi.ETag.TryGetValue(out string? etag).ShouldBeTrue();
        etag.ShouldBe("etag-from-delta");
    }
}
