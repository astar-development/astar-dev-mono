using System.IO.Abstractions;
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
    private readonly ISyncedItemRepository _syncedItemRepository = Substitute.For<ISyncedItemRepository>();
    private readonly IFileClassificationRepository _classificationRepository = Substitute.For<IFileClassificationRepository>();
    private readonly IDirectory _mockDirectory = Substitute.For<IDirectory>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IFileAutoCategorisor _fileAutoCategorisor = Substitute.For<IFileAutoCategorisor>();

    public GivenASyncedItemRegistrar()
    {
        _fileSystem.Directory.Returns(_mockDirectory);
        _classificationRepository.GetAllKeywordMappingsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<KeywordMapping>>([]));
        _fileAutoCategorisor.Categorise(Arg.Any<string>()).Returns(FileClassificationFactory.Create("Unclassified", Option.None<string>(), Option.None<string>(), false));
    }

    private SyncedItemRegistrar CreateSut() => new(_syncedItemRepository, _classificationRepository, _fileSystem, Substitute.For<ILogger<SyncedItemRegistrar>>(), _fileAutoCategorisor);

    private SyncedItemRegistrar CreateSutWithMappings(IReadOnlyList<KeywordMapping> mappings)
    {
        _classificationRepository.GetAllKeywordMappingsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(mappings));

        return new(_syncedItemRepository, _classificationRepository, _fileSystem, Substitute.For<ILogger<SyncedItemRegistrar>>(), _fileAutoCategorisor);
    }

    private static KeywordMapping KeywordMap(string keyword, string level1)
        => ((Result<KeywordMapping, string>.Ok)KeywordMappingFactory.Create(keyword, level1, Option.None<string>(), Option.None<string>(), false)).Value;

    private static FolderDeltaItem FolderItem(string id, string remotePath)
        => DeltaItemFactory.CreateFolder(new OneDriveItemId(id), new DriveId("drive-1"), Option.None<OneDriveFolderId>(), ItemPathFactory.Create(id, remotePath), VersionInfoFactory.Create(Option.None<string>(), Option.None<string>()));

    private static FileDeltaItem FileItem(string id, string remotePath)
        => DeltaItemFactory.CreateFile(new OneDriveItemId(id), new DriveId("drive-1"), Option.None<OneDriveFolderId>(), ItemPathFactory.Create(id, remotePath), 100L, DateTimeOffset.UtcNow.AddDays(-1), Option.None<string>(), VersionInfoFactory.Create(Option.None<string>(), Option.None<string>()));

    [Fact]
    public async Task when_register_folder_called_then_directory_is_created()
    {
        var sut = CreateSut();
        var item = FolderItem("folder-1", "/Documents/Sub");
        var syncedItems = new Dictionary<string, SyncedItemEntity>();

        await sut.RegisterFolderAsync(new AccountId("user-1"), item, "/Documents/Sub", "/sync-root/Documents/Sub", syncedItems, TestContext.Current.CancellationToken);

        _mockDirectory.Received(1).CreateDirectory("/sync-root/Documents/Sub");
    }

    [Fact]
    public async Task when_register_folder_called_then_synced_item_repository_upsert_is_called()
    {
        var sut = CreateSut();
        var item = FolderItem("folder-1", "/Documents/Sub");
        var syncedItems = new Dictionary<string, SyncedItemEntity>();

        await sut.RegisterFolderAsync(new AccountId("user-1"), item, "/Documents/Sub", "/sync-root/Documents/Sub", syncedItems, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertAsync(Arg.Is<SyncedItemEntity>(e => e.IsFolder && e.RemoteItemId.Id == "folder-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_folder_called_then_synced_items_dict_is_updated()
    {
        var sut = CreateSut();
        var item = FolderItem("folder-1", "/Documents/Sub");
        var syncedItems = new Dictionary<string, SyncedItemEntity>();

        await sut.RegisterFolderAsync(new AccountId("user-1"), item, "/Documents/Sub", "/sync-root/Documents/Sub", syncedItems, TestContext.Current.CancellationToken);

        syncedItems.ShouldContainKey("folder-1");
        syncedItems["folder-1"].IsFolder.ShouldBeTrue();
    }

    [Fact]
    public async Task when_register_phantom_called_then_synced_item_repository_upsert_is_called()
    {
        var sut = CreateSut();
        var item = FileItem("item-phantom", "/Documents/phantom.txt");
        var syncedItems = new Dictionary<string, SyncedItemEntity>();

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/Documents/phantom.txt", "/sync-root/Documents/phantom.txt", syncedItems, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertAsync(Arg.Is<SyncedItemEntity>(e => e.RemoteItemId.Id == "item-phantom"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_phantom_called_then_synced_items_dict_is_updated()
    {
        var sut = CreateSut();
        var item = FileItem("item-phantom", "/Documents/phantom.txt");
        var syncedItems = new Dictionary<string, SyncedItemEntity>();

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/Documents/phantom.txt", "/sync-root/Documents/phantom.txt", syncedItems, TestContext.Current.CancellationToken);

        syncedItems.ShouldContainKey("item-phantom");
    }

    [Fact]
    public async Task when_register_phantom_called_with_matching_mappings_then_upsert_classifications_is_called_with_matched_tags()
    {
        const int entityId = 42;
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(entityId));
        var sut = CreateSutWithMappings([KeywordMap("photos", "Media")]);
        var item = FileItem("file-1", "/photos/beach.jpg");
        var syncedItems = new Dictionary<string, SyncedItemEntity>();

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/photos/beach.jpg", "/sync/photos/beach.jpg", syncedItems, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertClassificationsAsync(
            entityId,
            Arg.Is<IReadOnlyList<FileClassification>>(list => list.Any(c => c.Level1 == "Media")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_phantom_called_with_no_matching_rules_then_upsert_classifications_is_called_with_unclassified()
    {
        const int entityId = 7;
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(entityId));
        var sut = CreateSutWithMappings([KeywordMap("spacecraft", "Science")]);
        var item = FileItem("file-2", "/docs/report.pdf");
        var syncedItems = new Dictionary<string, SyncedItemEntity>();

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/docs/report.pdf", "/sync/docs/report.pdf", syncedItems, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertClassificationsAsync(
            entityId,
            Arg.Is<IReadOnlyList<FileClassification>>(list => list.Any(c => c.TagName == "Unclassified")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_phantom_called_twice_then_upsert_classifications_is_called_each_time()
    {
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        var sut = CreateSutWithMappings([KeywordMap("photos", "Media")]);
        var item = FileItem("file-3", "/photos/sunset.jpg");
        var syncedItems = new Dictionary<string, SyncedItemEntity>();

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/photos/sunset.jpg", "/sync/photos/sunset.jpg", syncedItems, TestContext.Current.CancellationToken);
        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/photos/sunset.jpg", "/sync/photos/sunset.jpg", syncedItems, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(2).UpsertClassificationsAsync(Arg.Any<int>(), Arg.Any<IReadOnlyList<FileClassification>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_folder_called_then_upsert_classifications_is_not_called()
    {
        var sut = CreateSutWithMappings([KeywordMap("photos", "Media")]);
        var item = FolderItem("folder-2", "/photos");
        var syncedItems = new Dictionary<string, SyncedItemEntity>();

        await sut.RegisterFolderAsync(new AccountId("user-1"), item, "/photos", "/sync/photos", syncedItems, TestContext.Current.CancellationToken);

        await _syncedItemRepository.DidNotReceive().UpsertClassificationsAsync(Arg.Any<int>(), Arg.Any<IReadOnlyList<FileClassification>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_phantom_called_then_auto_categorisor_categorise_is_called()
    {
        var sut = CreateSut();
        var item = FileItem("file-auto-1", "/Photos/a red car on the road.jpg");
        var syncedItems = new Dictionary<string, SyncedItemEntity>();

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/Photos/a red car on the road.jpg", "/sync/Photos/a red car on the road.jpg", syncedItems, TestContext.Current.CancellationToken);

        _fileAutoCategorisor.Received(1).Categorise("/Photos/a red car on the road.jpg");
    }

    [Fact]
    public async Task when_register_phantom_called_then_upsert_classifications_includes_auto_derived_classification()
    {
        const int entityId = 99;
        _syncedItemRepository.UpsertAsync(Arg.Any<SyncedItemEntity>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(entityId));
        _fileAutoCategorisor.Categorise(Arg.Any<string>()).Returns(FileClassificationFactory.Create("Color", Option.Some("Red"), Option.None<string>(), false));
        var sut = CreateSut();
        var item = FileItem("file-auto-2", "/Photos/a red car on the road.jpg");
        var syncedItems = new Dictionary<string, SyncedItemEntity>();

        await sut.RegisterPhantomAsync(new AccountId("user-1"), item, "/Photos/a red car on the road.jpg", "/sync/Photos/a red car on the road.jpg", syncedItems, TestContext.Current.CancellationToken);

        await _syncedItemRepository.Received(1).UpsertClassificationsAsync(
            entityId,
            Arg.Is<IReadOnlyList<FileClassification>>(list => list.Any(c => c.Level1 == "Color")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_register_folder_called_then_auto_categorisor_is_not_called()
    {
        var sut = CreateSut();
        var item = FolderItem("folder-auto-1", "/Photos");
        var syncedItems = new Dictionary<string, SyncedItemEntity>();

        await sut.RegisterFolderAsync(new AccountId("user-1"), item, "/Photos", "/sync/Photos", syncedItems, TestContext.Current.CancellationToken);

        _fileAutoCategorisor.DidNotReceive().Categorise(Arg.Any<string>());
    }
}
