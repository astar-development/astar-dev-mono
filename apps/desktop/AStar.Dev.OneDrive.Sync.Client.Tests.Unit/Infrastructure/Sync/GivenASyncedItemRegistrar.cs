using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenASyncedItemRegistrar
{
    private readonly ISyncedItemRepository _syncedItemRepository = Substitute.For<ISyncedItemRepository>();
    private readonly IDirectory _mockDirectory = Substitute.For<IDirectory>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public GivenASyncedItemRegistrar()
        => _fileSystem.Directory.Returns(_mockDirectory);

    private SyncedItemRegistrar CreateSut() => new(_syncedItemRepository, _fileSystem);

    private static DeltaItem FolderItem(string id, string remotePath)
        => DeltaItemFactory.Create(new OneDriveItemId(id), new DriveId("drive-1"), null, ItemPathFactory.Create(id, remotePath), true, false, 0L, null, null, VersionInfoFactory.Create(null, null));

    private static DeltaItem FileItem(string id, string remotePath)
        => DeltaItemFactory.Create(new OneDriveItemId(id), new DriveId("drive-1"), null, ItemPathFactory.Create(id, remotePath), false, false, 100L, DateTimeOffset.UtcNow.AddDays(-1), null, VersionInfoFactory.Create(null, null));

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
}
