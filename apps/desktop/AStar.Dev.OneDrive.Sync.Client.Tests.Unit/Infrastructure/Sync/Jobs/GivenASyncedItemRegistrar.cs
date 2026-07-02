using System.Collections.Concurrent;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Jobs;

public sealed class GivenASyncedItemRegistrar
{
    private const string DownloadLocalPath = "/sync-root/Documents/report.pdf";
    private const string DownloadRelativePath = "Documents/report.pdf";
    private const string DownloadRemotePath = "/Documents/report.pdf";
    private const string UploadLocalPath = "/sync-root/Photos/beach.jpg";
    private const string UploadRelativePath = "Photos/beach.jpg";
    private const string UploadRemotePath = "/Photos/beach.jpg";
    private const string ColourRemotePath = "/Photos/a red car on the road.jpg";
    private const string ColourLocalPath = "/sync-root/Photos/a red car on the road.jpg";

    private readonly ISyncedItemRepository _syncedItemRepository = Substitute.For<ISyncedItemRepository>();
    private readonly IDirectory _mockDirectory = Substitute.For<IDirectory>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IFileAutoCategorisor _fileAutoCategorisor = Substitute.For<IFileAutoCategorisor>();
    private readonly ICategoryResolutionService _categoryResolutionService = Substitute.For<ICategoryResolutionService>();

    public GivenASyncedItemRegistrar()
    {
        _fileSystem.Directory.Returns(_mockDirectory);
        _fileSystem.FileInfo.New(Arg.Any<string>()).Returns(Substitute.For<IFileInfo>());
        _fileAutoCategorisor.Categorise(Arg.Any<string>()).Returns(FileClassificationFactory.Create("Unclassified", Option.None<string>(), Option.None<string>(), false, false));
        _categoryResolutionService.ResolveManyAsync(Arg.Any<IReadOnlyList<FileClassification>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<int>>([]));
        _syncedItemRepository.UpsertWithClassificationsAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
    }

    private SyncedItemRegistrar CreateSut() => new(_syncedItemRepository, _fileSystem, Substitute.For<ILogger<SyncedItemRegistrar>>(), _fileAutoCategorisor, _categoryResolutionService);

    private static FileClassificationCategory KeywordMap(string name, int level)
        => ((Result<FileClassificationCategory, string>.Ok)FileClassificationCategoryFactory.Create(new FileClassificationCategoryId(), name, level, false, false, Option.None<FileClassificationCategoryId>())).Value;

    private static FolderDeltaItem FolderItem(string id, string remotePath)
        => DeltaItemFactory.CreateFolder(new OneDriveItemId(id), new DriveId("drive-1"), Option.None<OneDriveFolderId>(), ItemPathFactory.Create(id, remotePath), VersionInfoFactory.Create(Option.None<string>(), Option.None<string>()));

    private static FileDeltaItem FileItem(string id, string remotePath)
        => DeltaItemFactory.CreateFile(new OneDriveItemId(id), new DriveId("drive-1"), Option.None<OneDriveFolderId>(), ItemPathFactory.Create(id, remotePath), 100L, DateTimeOffset.UtcNow.AddDays(-1), Option.None<string>(), VersionInfoFactory.Create(Option.None<string>(), Option.None<string>()));

    private static SyncJob DownloadJob(string remoteId, string localPath, string relativePath)
    {
        var remote = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId(""), new OneDriveItemId(remoteId));
        var target = SyncFileTargetFactory.Create(localPath, relativePath);
        var metadata = SyncFileMetadataFactory.Create(100L, DateTimeOffset.UtcNow.AddDays(-1));

        return SyncJobFactory.CreateDownload(remote, target, metadata);
    }

    private static UploadSyncJob UploadJob(string remoteId, string localPath, string relativePath)
    {
        var remote = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId(""), new OneDriveItemId(remoteId));
        var target = SyncFileTargetFactory.Create(localPath, relativePath);
        var metadata = SyncFileMetadataFactory.Create(100L, DateTimeOffset.UtcNow.AddDays(-1));

        return (UploadSyncJob)SyncJobFactory.CreateUpload(remote, target, metadata);
    }

    [Fact]
    public async Task when_register_folder_called_then_directory_is_created()
    {
        var sut = CreateSut();
        var item = FolderItem("folder-1", "/Documents/Sub");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();

        await sut.RegisterFolderAsync(new AccountId("user-1"), item, "/Documents/Sub", "/sync-root/Documents/Sub", syncedItems, TestContext.Current.CancellationToken);

        _mockDirectory.Received(1).CreateDirectory("/sync-root/Documents/Sub");
    }

    [Fact]
    public async Task when_register_folder_called_then_synced_item_repository_upsert_is_called()
    {
        var sut = CreateSut();
        var item = FolderItem("folder-1", "/Documents/Sub");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();

        await sut.RegisterFolderAsync(new AccountId("user-1"), item, "/Documents/Sub", "/sync-root/Documents/Sub", syncedItems, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertAsync(Arg.Is<SyncedItemEntity>(e => e.IsFolder && e.RemoteItemId.Id == "folder-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_folder_called_then_synced_items_dict_is_updated()
    {
        var sut = CreateSut();
        var item = FolderItem("folder-1", "/Documents/Sub");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();

        await sut.RegisterFolderAsync(new AccountId("user-1"), item, "/Documents/Sub", "/sync-root/Documents/Sub", syncedItems, TestContext.Current.CancellationToken);

        syncedItems.ShouldContainKey("folder-1");
        syncedItems["folder-1"].IsFolder.ShouldBeTrue();
    }

    [Fact]
    public async Task when_register_phantom_called_then_synced_item_repository_upsert_is_called()
    {
        var sut = CreateSut();
        var item = FileItem("item-phantom", "/Documents/phantom.txt");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/Documents/phantom.txt", "/sync-root/Documents/phantom.txt", syncedItems, [], TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertAsync(Arg.Is<SyncedItemEntity>(e => e.RemoteItemId.Id == "item-phantom"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_phantom_called_then_synced_items_dict_is_updated()
    {
        var sut = CreateSut();
        var item = FileItem("item-phantom", "/Documents/phantom.txt");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/Documents/phantom.txt", "/sync-root/Documents/phantom.txt", syncedItems, [], TestContext.Current.CancellationToken);

        syncedItems.ShouldContainKey("item-phantom");
    }

    [Fact]
    public async Task when_register_phantom_called_then_category_resolution_service_is_called_with_combined_classifications()
    {
        const int entityId = 42;
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(entityId));
        var sut = CreateSut();
        var item = FileItem("file-1", "/photos/beach.jpg");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();
        IReadOnlyList<FileClassificationCategory> mappings = [KeywordMap("photos", 1)];

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/photos/beach.jpg", "/sync/photos/beach.jpg", syncedItems, mappings, TestContext.Current.CancellationToken);

        await _categoryResolutionService.Received(1).ResolveManyAsync(
            Arg.Is<IReadOnlyList<FileClassification>>(list => list.Any(c => c.Level1 == "photos")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_phantom_called_then_upsert_file_classifications_is_called_with_resolved_category_ids()
    {
        const int entityId = 42;
        IReadOnlyList<int> resolvedIds = [10, 20];
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(entityId));
        _categoryResolutionService.ResolveManyAsync(Arg.Any<IReadOnlyList<FileClassification>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(resolvedIds));
        var sut = CreateSut();
        var item = FileItem("file-1", "/photos/beach.jpg");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/photos/beach.jpg", "/sync/photos/beach.jpg", syncedItems, [], TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertFileClassificationsAsync(
            entityId,
            Arg.Is<IReadOnlyList<int>>(ids => ids.Contains(10) && ids.Contains(20)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_phantom_called_with_no_matching_rules_then_upsert_file_classifications_is_called()
    {
        const int entityId = 7;
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(entityId));
        var sut = CreateSut();
        var item = FileItem("file-2", "/docs/report.pdf");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();
        IReadOnlyList<FileClassificationCategory> mappings = [KeywordMap("spacecraft", 1)];

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/docs/report.pdf", "/sync/docs/report.pdf", syncedItems, mappings, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertFileClassificationsAsync(entityId, Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_phantom_called_twice_then_upsert_file_classifications_is_called_each_time()
    {
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        var sut = CreateSut();
        var item = FileItem("file-3", "/photos/sunset.jpg");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();
        IReadOnlyList<FileClassificationCategory> mappings = [KeywordMap("photos", 1)];

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/photos/sunset.jpg", "/sync/photos/sunset.jpg", syncedItems, mappings, TestContext.Current.CancellationToken);
        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/photos/sunset.jpg", "/sync/photos/sunset.jpg", syncedItems, mappings, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(2).UpsertFileClassificationsAsync(Arg.Any<int>(), Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_folder_called_then_upsert_file_classifications_is_not_called()
    {
        var sut = CreateSut();
        var item = FolderItem("folder-2", "/photos");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();

        await sut.RegisterFolderAsync(new AccountId("user-1"), item, "/photos", "/sync/photos", syncedItems, TestContext.Current.CancellationToken);

        await _syncedItemRepository.DidNotReceive().UpsertFileClassificationsAsync(Arg.Any<int>(), Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_phantom_called_then_auto_categorisor_categorise_is_called()
    {
        var sut = CreateSut();
        var item = FileItem("file-auto-1", "/Photos/a red car on the road.jpg");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/Photos/a red car on the road.jpg", "/sync/Photos/a red car on the road.jpg", syncedItems, [], TestContext.Current.CancellationToken);

        _fileAutoCategorisor.Received(1).Categorise("/Photos/a red car on the road.jpg");
    }

    [Fact]
    public async Task when_register_phantom_called_then_upsert_file_classifications_includes_auto_derived_classification_ids()
    {
        const int entityId = 99;
        IReadOnlyList<int> resolvedIds = [7];
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(entityId));
        _fileAutoCategorisor.Categorise(Arg.Any<string>()).Returns(FileClassificationFactory.Create("Color", Option.Some("Red"), Option.None<string>(), false, false));
        _categoryResolutionService.ResolveManyAsync(Arg.Any<IReadOnlyList<FileClassification>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(resolvedIds));
        var sut = CreateSut();
        var item = FileItem("file-auto-2", "/Photos/a red car on the road.jpg");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/Photos/a red car on the road.jpg", "/sync/Photos/a red car on the road.jpg", syncedItems, [], TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertFileClassificationsAsync(
            entityId,
            Arg.Is<IReadOnlyList<int>>(ids => ids.Contains(7)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_folder_called_then_auto_categorisor_is_not_called()
    {
        var sut = CreateSut();
        var item = FolderItem("folder-auto-1", "/Photos");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();

        await sut.RegisterFolderAsync(new AccountId("user-1"), item, "/Photos", "/sync/Photos", syncedItems, TestContext.Current.CancellationToken);

        _fileAutoCategorisor.DidNotReceive().Categorise(Arg.Any<string>());
    }

    [Fact]
    public async Task when_register_phantom_called_with_preloaded_mappings_then_classification_uses_those_mappings()
    {
        const int entityId = 50;
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(entityId));
        var sut = CreateSut();
        var item = FileItem("file-mapping", "/Videos/clip.mp4");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();
        IReadOnlyList<FileClassificationCategory> mappings = [KeywordMap("Videos", 1)];

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/Videos/clip.mp4", "/sync/Videos/clip.mp4", syncedItems, mappings, TestContext.Current.CancellationToken);

        await _categoryResolutionService.Received(1).ResolveManyAsync(
            Arg.Is<IReadOnlyList<FileClassification>>(list => list.Any(c => c.Level1 == "Videos")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_download_called_then_upsert_with_classifications_is_called()
    {
        var job = DownloadJob("item-dl-1", DownloadLocalPath, DownloadRelativePath);
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();
        var sut = CreateSut();

        await sut.RegisterDownloadAsync(new AccountId("user-1"), job, DownloadRemotePath, [], syncedItems, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertWithClassificationsAsync(Arg.Is<SyncedItemEntity>(e => e.RemoteItemId.Id == "item-dl-1"), Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_download_called_then_synced_items_dict_is_updated()
    {
        var job = DownloadJob("item-dl-2", DownloadLocalPath, DownloadRelativePath);
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();
        var sut = CreateSut();

        await sut.RegisterDownloadAsync(new AccountId("user-1"), job, DownloadRemotePath, [], syncedItems, TestContext.Current.CancellationToken);

        syncedItems.ShouldContainKey("item-dl-2");
        syncedItems["item-dl-2"].IsFolder.ShouldBeFalse();
    }

    [Fact]
    public async Task when_register_download_called_then_auto_categoriser_is_called_with_remote_path()
    {
        var job = DownloadJob("item-dl-3", DownloadLocalPath, DownloadRelativePath);
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();
        var sut = CreateSut();

        await sut.RegisterDownloadAsync(new AccountId("user-1"), job, DownloadRemotePath, [], syncedItems, TestContext.Current.CancellationToken);

        _fileAutoCategorisor.Received(1).Categorise(DownloadRemotePath);
    }

    [Fact]
    public async Task when_register_download_called_then_category_resolution_service_is_called()
    {
        var job = DownloadJob("item-dl-4", DownloadLocalPath, DownloadRelativePath);
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();
        var sut = CreateSut();

        await sut.RegisterDownloadAsync(new AccountId("user-1"), job, DownloadRemotePath, [], syncedItems, TestContext.Current.CancellationToken);

        await _categoryResolutionService.Received(1).ResolveManyAsync(Arg.Any<IReadOnlyList<FileClassification>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_download_called_with_matching_rule_then_matched_classification_is_forwarded()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [KeywordMap("Documents", 1)];
        var job = DownloadJob("item-dl-5", DownloadLocalPath, DownloadRelativePath);
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();
        var sut = CreateSut();

        await sut.RegisterDownloadAsync(new AccountId("user-1"), job, DownloadRemotePath, mappings, syncedItems, TestContext.Current.CancellationToken);

        await _categoryResolutionService.Received(1).ResolveManyAsync(
            Arg.Is<IReadOnlyList<FileClassification>>(list => list.Any(c => c.Level1 == "Documents")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_download_called_with_colour_in_path_then_auto_categoriser_classification_is_persisted()
    {
        IReadOnlyList<int> resolvedIds = [5];
        _fileAutoCategorisor.Categorise(ColourRemotePath).Returns(FileClassificationFactory.Create("Color", Option.Some("Red"), Option.None<string>(), false, false));
        _categoryResolutionService.ResolveManyAsync(Arg.Any<IReadOnlyList<FileClassification>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(resolvedIds));
        var job = DownloadJob("item-dl-colour", ColourLocalPath, "Photos/a red car on the road.jpg");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();
        var sut = CreateSut();

        await sut.RegisterDownloadAsync(new AccountId("user-1"), job, ColourRemotePath, [], syncedItems, TestContext.Current.CancellationToken);

        await _categoryResolutionService.Received(1).ResolveManyAsync(
            Arg.Is<IReadOnlyList<FileClassification>>(list => list.Any(c => c.Level1 == "Color")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_upload_called_then_upsert_with_classifications_is_called_with_uploaded_id()
    {
        var job = UploadJob("item-ul-1", UploadLocalPath, UploadRelativePath);
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();
        var sut = CreateSut();

        await sut.RegisterUploadAsync(new AccountId("user-1"), job, "uploaded-remote-id", UploadRemotePath, [], syncedItems, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertWithClassificationsAsync(
            Arg.Is<SyncedItemEntity>(e => e.RemoteItemId.Id == "uploaded-remote-id"),
            Arg.Any<IReadOnlyList<int>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_upload_called_then_synced_items_dict_is_updated_with_uploaded_id()
    {
        var job = UploadJob("item-ul-2", UploadLocalPath, UploadRelativePath);
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();
        var sut = CreateSut();

        await sut.RegisterUploadAsync(new AccountId("user-1"), job, "uploaded-remote-id-2", UploadRemotePath, [], syncedItems, TestContext.Current.CancellationToken);

        syncedItems.ShouldContainKey("uploaded-remote-id-2");
    }

    [Fact]
    public async Task when_register_upload_called_then_auto_categoriser_is_called_with_remote_path()
    {
        var job = UploadJob("item-ul-3", UploadLocalPath, UploadRelativePath);
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();
        var sut = CreateSut();

        await sut.RegisterUploadAsync(new AccountId("user-1"), job, "uploaded-remote-id-3", UploadRemotePath, [], syncedItems, TestContext.Current.CancellationToken);

        _fileAutoCategorisor.Received(1).Categorise(UploadRemotePath);
    }

    [Fact]
    public async Task when_register_upload_called_with_colour_in_path_then_auto_categoriser_classification_is_persisted()
    {
        IReadOnlyList<int> resolvedIds = [9];
        _fileAutoCategorisor.Categorise(ColourRemotePath).Returns(FileClassificationFactory.Create("Color", Option.Some("Red"), Option.None<string>(), false, false));
        _categoryResolutionService.ResolveManyAsync(Arg.Any<IReadOnlyList<FileClassification>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(resolvedIds));
        var job = UploadJob("item-ul-colour", ColourLocalPath, "Photos/a red car on the road.jpg");
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();
        var sut = CreateSut();

        await sut.RegisterUploadAsync(new AccountId("user-1"), job, "uploaded-colour-id", ColourRemotePath, [], syncedItems, TestContext.Current.CancellationToken);

        await _categoryResolutionService.Received(1).ResolveManyAsync(
            Arg.Is<IReadOnlyList<FileClassification>>(list => list.Any(c => c.Level1 == "Color")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_upload_called_with_matching_rule_then_matched_classification_is_forwarded()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [KeywordMap("Photos", 1)];
        var job = UploadJob("item-ul-4", UploadLocalPath, UploadRelativePath);
        var syncedItems = new ConcurrentDictionary<string, SyncedItemEntity>();
        var sut = CreateSut();

        await sut.RegisterUploadAsync(new AccountId("user-1"), job, "uploaded-remote-id-4", UploadRemotePath, mappings, syncedItems, TestContext.Current.CancellationToken);

        await _categoryResolutionService.Received(1).ResolveManyAsync(
            Arg.Is<IReadOnlyList<FileClassification>>(list => list.Any(c => c.Level1 == "Photos")),
            Arg.Any<CancellationToken>());
    }
}
