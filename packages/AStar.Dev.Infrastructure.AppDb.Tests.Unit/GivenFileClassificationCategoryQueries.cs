using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Infrastructure.AppDb.Tests.Unit;

public sealed class GivenFileClassificationCategoryQueries : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"file-classification-category-queries-{Guid.NewGuid():N}.db");
    private readonly AppDbContext context;

    public GivenFileClassificationCategoryQueries()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new AppDbContext(options);
        context.Database.Migrate();
    }

    [Fact]
    public async Task when_no_unclassified_root_exists_then_one_is_created_and_the_category_is_added_beneath_it()
    {
        await context.EnsureCategoriesExistAsync(["Nature"], TestContext.Current.CancellationToken);

        var root = await context.Set<FileClassificationCategoryEntity>()
            .SingleAsync(category => category.Level == 1 && category.Name == "Unclassified", TestContext.Current.CancellationToken);
        var category = await context.Set<FileClassificationCategoryEntity>()
            .SingleAsync(category => category.Name == "Nature", TestContext.Current.CancellationToken);

        category.ParentId.ShouldBe(root.Id);
        category.Level.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_unclassified_root_already_exists_then_it_is_reused_rather_than_duplicated()
    {
        await context.EnsureCategoriesExistAsync(["Nature"], TestContext.Current.CancellationToken);

        await context.EnsureCategoriesExistAsync(["Abstract"], TestContext.Current.CancellationToken);

        var roots = await context.Set<FileClassificationCategoryEntity>()
            .Where(category => category.Level == 1 && category.Name == "Unclassified")
            .ToListAsync(TestContext.Current.CancellationToken);

        roots.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_category_already_exists_beneath_unclassified_then_it_is_not_duplicated()
    {
        await context.EnsureCategoriesExistAsync(["Nature"], TestContext.Current.CancellationToken);

        await context.EnsureCategoriesExistAsync(["Nature"], TestContext.Current.CancellationToken);

        var categories = await context.Set<FileClassificationCategoryEntity>()
            .Where(category => category.Name == "Nature")
            .ToListAsync(TestContext.Current.CancellationToken);

        categories.Count.ShouldBe(1);
    }

    public void Dispose()
    {
        context.Dispose();

        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
