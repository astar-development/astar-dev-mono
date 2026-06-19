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

public sealed class GivenSyncedItemRegistrar_WhenRegisteringPhantomFiles(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task when_registering_a_phantom_file_then_a_synced_item_entity_is_persisted()
    {
        var registrar = fixture.Services.GetRequiredService<ISyncedItemRegistrar>();
        var repository = fixture.Services.GetRequiredService<ISyncedItemRepository>();
        var accountId = new AccountId("account-phantom-persist");
        await SeedAccountAsync(accountId);
        var mappings = await GetMappingsAsync();

        await registrar.RegisterPhantomAsync(accountId, BuildFileItem("phantom-id-persist"), "Documents/test.txt", "/local/phantom-persist/test.txt", [], mappings, CancellationToken.None);

        var items = await repository.GetAllByAccountAsync(accountId, CancellationToken.None);
        items.ShouldContainKey("phantom-id-persist");
    }

    [Fact]
    public async Task when_registering_a_phantom_file_then_classifications_are_persisted()
    {
        var registrar = fixture.Services.GetRequiredService<ISyncedItemRegistrar>();
        var repository = fixture.Services.GetRequiredService<ISyncedItemRepository>();
        var accountId = new AccountId("account-phantom-classifications");
        await SeedAccountAsync(accountId);
        var mappings = await GetMappingsAsync();

        await registrar.RegisterPhantomAsync(accountId, BuildFileItem("phantom-id-classifications"), "Documents/test.txt", "/local/phantom-class/test.txt", [], mappings, CancellationToken.None);

        var items = await repository.GetAllByAccountAsync(accountId, CancellationToken.None);
        var categoryNames = await GetFileClassificationCategoryNamesAsync(items["phantom-id-classifications"].Id);
        categoryNames.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task when_registering_a_phantom_file_then_file_auto_categorisation_result_is_included()
    {
        var registrar = fixture.Services.GetRequiredService<ISyncedItemRegistrar>();
        var repository = fixture.Services.GetRequiredService<ISyncedItemRepository>();
        var categorisor = fixture.Services.GetRequiredService<IFileAutoCategorisor>();
        var accountId = new AccountId("account-phantom-autocategorise");
        const string remotePath = "Documents/test.txt";
        await SeedAccountAsync(accountId);
        var mappings = await GetMappingsAsync();

        await registrar.RegisterPhantomAsync(accountId, BuildFileItem("phantom-id-autocategorise"), remotePath, "/local/phantom-autocat/test.txt", [], mappings, CancellationToken.None);

        string expectedLevel1 = categorisor.Categorise(remotePath).Match(c => c.Level1, () => "Unclassified");
        var items = await repository.GetAllByAccountAsync(accountId, CancellationToken.None);
        var categoryNames = await GetFileClassificationCategoryNamesAsync(items["phantom-id-autocategorise"].Id);
        categoryNames.ShouldContain(name => name == expectedLevel1);
    }

    private async Task<IReadOnlyList<FileClassificationCategory>> GetMappingsAsync()
    {
        var classificationRepository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        return await classificationRepository.GetAllCategoriesAsync(CancellationToken.None);
    }

    private async Task SeedAccountAsync(AccountId accountId)
    {
        var factory = fixture.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        context.Set<AccountEntity>().Add(new AccountEntity { Id = accountId });
        await context.SaveChangesAsync();
    }

    private async Task<List<string>> GetFileClassificationCategoryNamesAsync(int syncedItemId)
    {
        var factory = fixture.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await factory.CreateDbContextAsync();

        return await context.SyncedItemFileClassifications
            .Include(c => c.Category)
            .Where(c => c.SyncedItemId == syncedItemId)
            .AsNoTracking()
            .Select(c => c.Category!.Name)
            .ToListAsync();
    }

    private static FileDeltaItem BuildFileItem(string itemId) => DeltaItemFactory.CreateFile(
        new OneDriveItemId(itemId),
        new DriveId("test-drive-id"),
        Option.None<OneDriveFolderId>(),
        ItemPathFactory.Create("test.txt"),
        1024,
        Option.None<DateTimeOffset>(),
        Option.None<string>(),
        VersionInfoFactory.Create(Option.None<string>(), Option.None<string>()));
}
