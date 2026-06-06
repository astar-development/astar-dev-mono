using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Integration.FileClassification;

public sealed class GivenAFileClassificationRepository(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        var factory = fixture.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        context.FileClassificationKeywords.RemoveRange(context.FileClassificationKeywords);
        context.FileClassificationCategories.RemoveRange(context.FileClassificationCategories);
        await context.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task when_adding_a_category_then_it_can_be_retrieved()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        var placeholder = new FileClassificationCategoryId(0);
        var category = FileClassificationCategoryFactory.Create(placeholder, "Finance", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));

        await repository.AddCategoryAsync(category, ct);

        var categories = await repository.GetAllCategoriesAsync(ct);
        categories.ShouldNotBeEmpty();
        categories.ShouldContain(c => c.Name == "Finance");
    }

    [Fact]
    public async Task when_adding_a_keyword_to_a_leaf_category_then_it_can_be_retrieved()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        var placeholder = new FileClassificationCategoryId(0);
        var category = FileClassificationCategoryFactory.Create(placeholder, "Finance", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var categoryId = await repository.AddCategoryAsync(category, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var keyword = FileClassificationKeywordFactory.Create("invoice", Option.None<bool>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));

        await repository.AddKeywordAsync(categoryId, keyword, ct);

        var keywords = await repository.GetKeywordsForCategoryAsync(categoryId, ct);
        keywords.ShouldNotBeEmpty();
        keywords.ShouldContain(k => k.Value == "invoice");
    }

    [Fact]
    public async Task when_keywords_exist_then_get_all_keyword_mappings_returns_flat_projections()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        var placeholder = new FileClassificationCategoryId(0);
        var category = FileClassificationCategoryFactory.Create(placeholder, "Finance", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var categoryId = await repository.AddCategoryAsync(category, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var keyword = FileClassificationKeywordFactory.Create("invoice", Option.None<bool>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        await repository.AddKeywordAsync(categoryId, keyword, ct);

        var mappings = await repository.GetAllKeywordMappingsAsync(ct);

        mappings.ShouldNotBeEmpty();
        mappings.ShouldContain(m => m.Keyword == "invoice" && m.Level1 == "Finance");
    }

    [Fact]
    public async Task when_adding_a_keyword_to_a_non_leaf_category_then_an_error_is_returned()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        var placeholder = new FileClassificationCategoryId(0);
        var parentCategory = FileClassificationCategoryFactory.Create(placeholder, "Finance", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var parentId = await repository.AddCategoryAsync(parentCategory, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var childCategory = FileClassificationCategoryFactory.Create(placeholder, "Invoices", 2, Option.Some(parentId))
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        await repository.AddCategoryAsync(childCategory, ct);
        var keyword = FileClassificationKeywordFactory.Create("invoice", Option.None<bool>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));

        await repository.AddKeywordAsync(parentId, keyword, ct)
            .MatchAsync(
                _ => throw new InvalidOperationException("Expected error but got success."),
                err => { err.ShouldNotBeNullOrWhiteSpace(); return err; });
    }

    [Fact]
    public async Task when_updating_a_keyword_on_a_non_leaf_category_then_an_error_is_returned()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        var placeholder = new FileClassificationCategoryId(0);
        var parentCategory = FileClassificationCategoryFactory.Create(placeholder, "Finance", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var parentId = await repository.AddCategoryAsync(parentCategory, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var keyword = FileClassificationKeywordFactory.Create("invoice", Option.None<bool>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var keywordId = await repository.AddKeywordAsync(parentId, keyword, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var childCategory = FileClassificationCategoryFactory.Create(placeholder, "Invoices", 2, Option.Some(parentId))
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        await repository.AddCategoryAsync(childCategory, ct);
        var updatedKeyword = FileClassificationKeywordFactory.Create("receipt", Option.None<bool>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));

        await repository.UpdateKeywordAsync(keywordId, updatedKeyword, ct)
            .MatchAsync(
                _ => throw new InvalidOperationException("Expected error but got success."),
                err => { err.ShouldNotBeNullOrWhiteSpace(); return err; });
    }

    [Fact]
    public async Task when_deleting_a_category_then_its_keywords_are_also_deleted()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        var placeholder = new FileClassificationCategoryId(0);
        var category = FileClassificationCategoryFactory.Create(placeholder, "Finance", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var categoryId = await repository.AddCategoryAsync(category, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var keyword = FileClassificationKeywordFactory.Create("invoice", Option.None<bool>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        await repository.AddKeywordAsync(categoryId, keyword, ct);

        await repository.DeleteCategoryAsync(categoryId, ct);

        var categories = await repository.GetAllCategoriesAsync(ct);
        categories.ShouldNotContain(c => c.Id == categoryId);
        var keywords = await repository.GetKeywordsForCategoryAsync(categoryId, ct);
        keywords.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_updating_a_category_name_then_the_new_name_is_persisted()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        var placeholder = new FileClassificationCategoryId(0);
        var category = FileClassificationCategoryFactory.Create(placeholder, "Finance", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var categoryId = await repository.AddCategoryAsync(category, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var updatedCategory = FileClassificationCategoryFactory.Create(categoryId, "Accounts", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));

        await repository.UpdateCategoryAsync(categoryId, updatedCategory, ct);

        var categories = await repository.GetAllCategoriesAsync(ct);
        categories.ShouldContain(c => c.Name == "Accounts");
        categories.ShouldNotContain(c => c.Name == "Finance");
    }

    [Fact]
    public async Task when_deleting_a_keyword_then_it_is_removed()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        var placeholder = new FileClassificationCategoryId(0);
        var category = FileClassificationCategoryFactory.Create(placeholder, "Finance", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var categoryId = await repository.AddCategoryAsync(category, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var keywordA = FileClassificationKeywordFactory.Create("invoice", Option.None<bool>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var keywordB = FileClassificationKeywordFactory.Create("receipt", Option.None<bool>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var keywordIdA = await repository.AddKeywordAsync(categoryId, keywordA, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        await repository.AddKeywordAsync(categoryId, keywordB, ct);

        await repository.DeleteKeywordAsync(keywordIdA, ct);

        var keywords = await repository.GetKeywordsForCategoryAsync(categoryId, ct);
        keywords.ShouldNotContain(k => k.Value == "invoice");
        keywords.ShouldContain(k => k.Value == "receipt");
    }
}
