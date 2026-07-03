using AStar.Dev.Infrastructure.AppDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenAFileClassificationKeywordEntity : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;

    public GivenAFileClassificationKeywordEntity()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        context = new AppDbContext(options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        context.Dispose();
        connection.Dispose();
    }

    [Fact]
    public async Task when_a_keyword_is_added_for_a_category_then_it_can_be_retrieved()
    {
        var category = new FileClassificationCategoryEntity { Name = "Animals", Level = 3 };
        context.FileClassificationCategories.Add(category);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.FileClassificationKeywords.Add(new FileClassificationKeywordEntity { Keyword = "cat", CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.FileClassificationKeywords.Include(keyword => keyword.Category).First();

        retrieved.Keyword.ShouldBe("cat");
        retrieved.Category!.Name.ShouldBe("Animals");
    }

    [Fact]
    public async Task when_the_owning_category_is_deleted_then_its_keywords_are_also_deleted()
    {
        var category = new FileClassificationCategoryEntity { Name = "Animals", Level = 3 };
        context.FileClassificationCategories.Add(category);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.FileClassificationKeywords.Add(new FileClassificationKeywordEntity { Keyword = "cat", CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.FileClassificationCategories.Remove(category);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.FileClassificationKeywords.Any().ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_same_keyword_and_category_pair_is_added_twice_then_save_fails()
    {
        var category = new FileClassificationCategoryEntity { Name = "Animals", Level = 3 };
        context.FileClassificationCategories.Add(category);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.FileClassificationKeywords.Add(new FileClassificationKeywordEntity { Keyword = "cat", CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.FileClassificationKeywords.Add(new FileClassificationKeywordEntity { Keyword = "cat", CategoryId = category.Id });

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void when_instantiated_then_keyword_defaults_to_empty_string() =>
        new FileClassificationKeywordEntity().Keyword.ShouldBe(string.Empty);

    [Fact]
    public void when_instantiated_then_category_id_defaults_to_zero() =>
        new FileClassificationKeywordEntity().CategoryId.ShouldBe(0);

    [Fact]
    public void when_instantiated_then_category_navigation_is_null() =>
        new FileClassificationKeywordEntity().Category.ShouldBeNull();

    [Fact]
    public void when_category_id_is_set_then_it_reflects_in_the_property()
    {
        var entity = new FileClassificationKeywordEntity
        {
            CategoryId = 7
        };

        entity.CategoryId.ShouldBe(7);
    }

    [Fact]
    public void when_keyword_is_set_then_it_reflects_in_the_property()
    {
        var entity = new FileClassificationKeywordEntity
        {
            Keyword = "holiday"
        };

        entity.Keyword.ShouldBe("holiday");
    }
}
