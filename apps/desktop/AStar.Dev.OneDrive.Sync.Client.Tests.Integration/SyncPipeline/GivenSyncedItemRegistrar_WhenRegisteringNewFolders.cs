using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Integration.SyncPipeline;

public sealed class GivenSyncedItemRegistrar_WhenRegisteringNewFolders(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task when_registering_a_folder_then_the_folder_is_created_in_the_file_system()
    {
        var registrar = fixture.Services.GetRequiredService<ISyncedItemRegistrar>();
        var accountId = new AccountId("account-fs-test");
        const string localPath = "/local/fs-test/Documents";
        await SeedAccountAsync(accountId);

        await registrar.RegisterFolderAsync(accountId, BuildFolderItem("folder-id-fs"), "Documents", localPath, [], CancellationToken.None);

        fixture.FileSystem.Directory.Exists(localPath).ShouldBeTrue();
    }

    [Fact]
    public async Task when_registering_a_folder_then_a_synced_item_entity_is_persisted()
    {
        var registrar = fixture.Services.GetRequiredService<ISyncedItemRegistrar>();
        var repository = fixture.Services.GetRequiredService<ISyncedItemRepository>();
        var accountId = new AccountId("account-persist-test");
        const string localPath = "/local/persist-test/Documents";
        await SeedAccountAsync(accountId);

        await registrar.RegisterFolderAsync(accountId, BuildFolderItem("folder-id-persist"), "Documents", localPath, [], CancellationToken.None);

        var items = await repository.GetAllByAccountAsync(accountId, CancellationToken.None);
        items.ShouldContainKey("folder-id-persist");
    }

    [Fact]
    public async Task when_registering_the_same_folder_twice_then_only_one_row_exists()
    {
        var registrar = fixture.Services.GetRequiredService<ISyncedItemRegistrar>();
        var repository = fixture.Services.GetRequiredService<ISyncedItemRepository>();
        var accountId = new AccountId("account-upsert-test");
        const string localPath = "/local/upsert-test/Documents";
        var folderItem = BuildFolderItem("folder-id-upsert");
        await SeedAccountAsync(accountId);

        await registrar.RegisterFolderAsync(accountId, folderItem, "Documents", localPath, [], CancellationToken.None);
        await registrar.RegisterFolderAsync(accountId, folderItem, "Documents", localPath, [], CancellationToken.None);

        var items = await repository.GetAllByAccountAsync(accountId, CancellationToken.None);
        items.Count.ShouldBe(1);
    }

    private async Task SeedAccountAsync(AccountId accountId)
    {
        var factory = fixture.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        context.Set<AccountEntity>().Add(new AccountEntity { Id = accountId });
        await context.SaveChangesAsync();
    }

    private static FolderDeltaItem BuildFolderItem(string itemId) => DeltaItemFactory.CreateFolder(
        new OneDriveItemId(itemId),
        new DriveId("test-drive-id"),
        Option.None<OneDriveFolderId>(),
        ItemPathFactory.Create("Documents"),
        VersionInfoFactory.Create(Option.None<string>(), Option.None<string>()));
}
