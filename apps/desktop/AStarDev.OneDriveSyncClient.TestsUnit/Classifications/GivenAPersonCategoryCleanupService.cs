using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Classifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Classifications;

public sealed class GivenAPersonCategoryCleanupService : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext seedingContext;
    private readonly IDbContextFactory<AppDbContext> factory;
    private readonly PersonCategoryCleanupService sut;
    private int fileCounter;
    private bool disposed;

    public GivenAPersonCategoryCleanupService()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        seedingContext = new AppDbContext(options);
        _ = seedingContext.Database.EnsureCreated();
        seedingContext.Accounts.Add(new AccountEntity { Id = new AccountId("account-1"), Profile = AccountProfileFactory.Create("Test User", "test@test.com") });
        seedingContext.SaveChanges();
        factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));
        sut = new PersonCategoryCleanupService(factory, new RuleBasedFileAutoCategorisor());
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (!disposing)
            return;

        seedingContext.Dispose();
        connection.Dispose();
    }

    private async Task<int> AddCategoryAsync(string name, int level, int? parentId)
    {
        var category = new FileClassificationCategoryEntity { Name = name, Level = level, ParentId = parentId };
        seedingContext.FileClassificationCategories.Add(category);
        await seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return category.Id;
    }

    private async Task LinkFileAsync(int categoryId, string remotePath)
    {
        fileCounter++;
        var fileDetail = new FileDetailEntity
        {
            FileName = new FileName($"file-{fileCounter}.bin"),
            DirectoryName = new DirectoryName("/local"),
            FileHandle = new FileHandle($"handle-{fileCounter}"),
            FileAccessDetail = new FileAccessDetailEntity(),
            ImageDetail = new ImageDetailEntity(),
            DeletionStatus = new DeletionStatusEntity()
        };
        seedingContext.Files.Add(fileDetail);
        await seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        seedingContext.SyncedItems.Add(new SyncedItemEntity { AccountId = new AccountId("account-1"), RemoteItemId = new OneDriveItemId($"item-{fileCounter}"), RemotePath = remotePath, LocalPath = $"/local/file-{fileCounter}.bin", FileDetailId = fileDetail.Id });
        seedingContext.FileClassifications.Add(new FileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = categoryId });
        await seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task when_a_person_child_is_linked_only_to_files_that_no_longer_yield_that_name_then_the_category_is_removed()
    {
        int personId = await AddCategoryAsync("Person", 1, null);
        int childId = await AddCategoryAsync("Add Azadapppermission", 2, personId);
        await LinkFileAsync(childId, "/a/b/c/d/e/f/g/Add-AzADAppPermission.ps1");

        await sut.RemoveInvalidPersonCategoriesAsync(TestContext.Current.CancellationToken);

        seedingContext.FileClassificationCategories.AsNoTracking().Any(category => category.Id == childId).ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_person_child_is_removed_then_its_file_links_are_removed()
    {
        int personId = await AddCategoryAsync("Person", 1, null);
        int childId = await AddCategoryAsync("Add Azadapppermission", 2, personId);
        await LinkFileAsync(childId, "/a/b/c/d/e/f/g/Add-AzADAppPermission.ps1");

        await sut.RemoveInvalidPersonCategoriesAsync(TestContext.Current.CancellationToken);

        seedingContext.FileClassifications.AsNoTracking().Any(link => link.CategoryId == childId).ShouldBeFalse();
    }

    [Fact]
    public async Task when_person_children_are_removed_then_the_count_removed_is_returned()
    {
        int personId = await AddCategoryAsync("Person", 1, null);
        int firstChildId = await AddCategoryAsync("Add Azadapppermission", 2, personId);
        int secondChildId = await AddCategoryAsync("Get Azadgroup", 2, personId);
        await LinkFileAsync(firstChildId, "/a/b/c/d/e/f/g/Add-AzADAppPermission.ps1");
        await LinkFileAsync(secondChildId, "/a/b/c/d/e/f/g/Get-AzADGroup.ps1");

        var result = await sut.RemoveInvalidPersonCategoriesAsync(TestContext.Current.CancellationToken);

        result.Match(removed => removed, _ => -1).ShouldBe(2);
    }

    [Fact]
    public async Task when_a_person_child_is_linked_to_a_file_that_still_yields_that_name_then_the_category_is_kept()
    {
        int personId = await AddCategoryAsync("Person", 1, null);
        int childId = await AddCategoryAsync("John Smith", 2, personId);
        await LinkFileAsync(childId, "/a/b/c/d/e/f/g/John-Smith.jpg");

        await sut.RemoveInvalidPersonCategoriesAsync(TestContext.Current.CancellationToken);

        seedingContext.FileClassificationCategories.AsNoTracking().Any(category => category.Id == childId).ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_person_child_has_no_linked_files_then_the_category_is_kept()
    {
        int personId = await AddCategoryAsync("Person", 1, null);
        int childId = await AddCategoryAsync("Added By Hand", 2, personId);

        await sut.RemoveInvalidPersonCategoriesAsync(TestContext.Current.CancellationToken);

        seedingContext.FileClassificationCategories.AsNoTracking().Any(category => category.Id == childId).ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_child_of_another_parent_is_linked_to_a_non_matching_file_then_the_category_is_kept()
    {
        int colourId = await AddCategoryAsync("Colour", 1, null);
        int childId = await AddCategoryAsync("Add Azadapppermission", 2, colourId);
        await LinkFileAsync(childId, "/a/b/c/d/e/f/g/Add-AzADAppPermission.ps1");

        await sut.RemoveInvalidPersonCategoriesAsync(TestContext.Current.CancellationToken);

        seedingContext.FileClassificationCategories.AsNoTracking().Any(category => category.Id == childId).ShouldBeTrue();
    }

    [Fact]
    public async Task when_there_is_no_person_category_then_zero_is_returned()
    {
        var result = await sut.RemoveInvalidPersonCategoriesAsync(TestContext.Current.CancellationToken);

        result.Match(removed => removed, _ => -1).ShouldBe(0);
    }
}
