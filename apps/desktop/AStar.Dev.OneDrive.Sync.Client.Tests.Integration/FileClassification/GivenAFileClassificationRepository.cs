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
        context.SyncedItemFileClassifications.RemoveRange(context.SyncedItemFileClassifications);
        context.SyncedItems.RemoveRange(context.SyncedItems);
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
        keywords.ShouldContain(k => k.Keyword.Value == "Invoice");
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
        mappings.ShouldContain(m => m.Keyword == "Invoice" && m.Level1 == "Finance");
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
        keywords.ShouldNotContain(k => k.Keyword.Value == "invoice");
        keywords.ShouldContain(k => k.Keyword.Value == "Receipt");
    }

    [Fact]
    public async Task when_delete_all_called_with_data_then_all_categories_removed()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        var placeholder = new FileClassificationCategoryId(0);
        var category = FileClassificationCategoryFactory.Create(placeholder, "Finance", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        await repository.AddCategoryAsync(category, ct);

        await repository.DeleteAllAsync(ct);

        var categories = await repository.GetAllCategoriesAsync(ct);
        categories.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_delete_all_called_with_data_then_all_keywords_removed()
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

        await repository.DeleteAllAsync(ct);

        var keywords = await repository.GetKeywordsForCategoryAsync(categoryId, ct);
        keywords.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_delete_all_called_with_empty_db_then_no_error_thrown()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRepository>();

        var exception = await Record.ExceptionAsync(() => repository.DeleteAllAsync(ct));

        exception.ShouldBeNull();
    }

    [Fact]
    public async Task when_adding_a_keyword_with_is_special_override_true_then_retrieved_keyword_has_is_special_override_some_true()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        var placeholder = new FileClassificationCategoryId(0);
        var category = FileClassificationCategoryFactory.Create(placeholder, "Finance", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var categoryId = await repository.AddCategoryAsync(category, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var keyword = FileClassificationKeywordFactory.Create("invoice", Option.Some(true))
            .Match(ok => ok, err => throw new InvalidOperationException(err));

        await repository.AddKeywordAsync(categoryId, keyword, ct);

        var keywords = await repository.GetKeywordsForCategoryAsync(categoryId, ct);
        keywords.ShouldContain(k => k.Keyword.IsSpecialOverride == Option.Some(true));
    }

    [Fact]
    public async Task when_adding_a_keyword_with_is_special_override_false_then_retrieved_keyword_has_is_special_override_some_false()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        var placeholder = new FileClassificationCategoryId(0);
        var category = FileClassificationCategoryFactory.Create(placeholder, "Finance", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var categoryId = await repository.AddCategoryAsync(category, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var keyword = FileClassificationKeywordFactory.Create("receipt", Option.Some(false))
            .Match(ok => ok, err => throw new InvalidOperationException(err));

        await repository.AddKeywordAsync(categoryId, keyword, ct);

        var keywords = await repository.GetKeywordsForCategoryAsync(categoryId, ct);
        keywords.ShouldContain(k => k.Keyword.IsSpecialOverride == Option.Some(false));
    }

    [Fact]
    public async Task when_adding_a_keyword_with_is_special_override_none_then_retrieved_keyword_has_is_special_override_some_false()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        var placeholder = new FileClassificationCategoryId(0);
        var category = FileClassificationCategoryFactory.Create(placeholder, "Finance", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var categoryId = await repository.AddCategoryAsync(category, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var keyword = FileClassificationKeywordFactory.Create("statement", Option.None<bool>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));

        await repository.AddKeywordAsync(categoryId, keyword, ct);

        var keywords = await repository.GetKeywordsForCategoryAsync(categoryId, ct);
        keywords.ShouldContain(k => k.Keyword.IsSpecialOverride == Option.Some(false));
    }

    [Fact]
    public async Task when_updating_a_keyword_to_is_special_override_true_then_retrieved_keyword_has_is_special_override_some_true()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        var placeholder = new FileClassificationCategoryId(0);
        var category = FileClassificationCategoryFactory.Create(placeholder, "Finance", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var categoryId = await repository.AddCategoryAsync(category, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var keyword = FileClassificationKeywordFactory.Create("invoice", Option.Some(false))
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var keywordId = await repository.AddKeywordAsync(categoryId, keyword, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var updatedKeyword = FileClassificationKeywordFactory.Create("invoice", Option.Some(true))
            .Match(ok => ok, err => throw new InvalidOperationException(err));

        await repository.UpdateKeywordAsync(keywordId, updatedKeyword, ct);

        var keywords = await repository.GetKeywordsForCategoryAsync(categoryId, ct);
        keywords.ShouldContain(k => k.Keyword.IsSpecialOverride == Option.Some(true));
    }

    [Fact]
    public async Task when_updating_a_keyword_to_is_special_override_false_then_retrieved_keyword_has_is_special_override_some_false()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        var placeholder = new FileClassificationCategoryId(0);
        var category = FileClassificationCategoryFactory.Create(placeholder, "Finance", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var categoryId = await repository.AddCategoryAsync(category, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var keyword = FileClassificationKeywordFactory.Create("invoice", Option.Some(true))
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var keywordId = await repository.AddKeywordAsync(categoryId, keyword, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var updatedKeyword = FileClassificationKeywordFactory.Create("invoice", Option.Some(false))
            .Match(ok => ok, err => throw new InvalidOperationException(err));

        await repository.UpdateKeywordAsync(keywordId, updatedKeyword, ct);

        var keywords = await repository.GetKeywordsForCategoryAsync(categoryId, ct);
        keywords.ShouldContain(k => k.Keyword.IsSpecialOverride == Option.Some(false));
    }

    [Fact]
    public async Task when_searching_synced_items_by_tag_using_junction_table_then_items_with_matching_category_are_returned()
    {
        var ct = TestContext.Current.CancellationToken;
        var classificationRepository = fixture.Services.GetRequiredService<IFileClassificationRepository>();
        var syncedItemRepository = fixture.Services.GetRequiredService<ISyncedItemRepository>();
        var accountId = new AccountId("search-user-tag-junction");
        await SeedAccountAsync(accountId, ct);
        var placeholder = new FileClassificationCategoryId(0);
        var category = FileClassificationCategoryFactory.Create(placeholder, "Photos", 1, Option.None<FileClassificationCategoryId>())
            .Match(ok => ok, err => throw new InvalidOperationException(err));
        var categoryId = await classificationRepository.AddCategoryAsync(category, ct)
            .MatchAsync(ok => ok, err => throw new InvalidOperationException(err));
        var photoItem = new SyncedItemEntity { AccountId = accountId, RemoteItemId = new OneDriveItemId(Guid.NewGuid().ToString()), RemotePath = "/photo.jpg", LocalPath = "/local/photo.jpg", IsFolder = false, RemoteModifiedAt = DateTimeOffset.UtcNow, SizeInBytes = 1024 };
        var untaggedItem = new SyncedItemEntity { AccountId = accountId, RemoteItemId = new OneDriveItemId(Guid.NewGuid().ToString()), RemotePath = "/doc.txt", LocalPath = "/local/doc.txt", IsFolder = false, RemoteModifiedAt = DateTimeOffset.UtcNow, SizeInBytes = 512 };
        var photoItemId = await syncedItemRepository.UpsertAsync(photoItem, ct);
        _ = await syncedItemRepository.UpsertAsync(untaggedItem, ct);
        await syncedItemRepository.UpsertFileClassificationsAsync(photoItemId, [categoryId.Id], ct);
        var criteria = SyncedItemSearchCriteriaFactory.Create(accountId, tags: ["Photos"]);

        var results = await syncedItemRepository.SearchAsync(criteria, ct);

        results.Count.ShouldBe(1);
        results[0].RemotePath.ShouldBe("/photo.jpg");
    }

    private async Task SeedAccountAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var factory = fixture.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        context.Set<AccountEntity>().Add(new AccountEntity { Id = accountId });
        await context.SaveChangesAsync(cancellationToken);
    }
}
