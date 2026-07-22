using System.Reactive.Linq;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using Testably.Abstractions.Testing;
using RxUnit = System.Reactive.Unit;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Configuration.EntityEditor;

public sealed class GivenEntityEditorFactory : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly DbContextOptions<AppDbContext> options;
    private readonly EntityEditorFactory sut;

    public GivenEntityEditorFactory()
    {
        connection.Open();
        options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var migrationContext = new AppDbContext(options))
        {
            migrationContext.Database.Migrate();
            migrationContext.RemoveRange(migrationContext.FileClassificationCategories);
            migrationContext.SaveChanges();
        }

        sut = new EntityEditorFactory(new TestDbContextFactory(options), new MockFileSystem(), ExportDirectoryFactory.Create("/exports").Match(exportDirectory => exportDirectory, error => throw new InvalidOperationException(error)));
    }

    [Fact]
    public void when_creating_the_connection_strings_editor_then_audit_columns_are_hidden_and_add_remove_is_disabled()
    {
        var editor = sut.CreateConnectionStringsEditor();

        editor.CanAddRemove.ShouldBeFalse();
        editor.IsColumnVisible(nameof(AuditableEntity.CreatedAt)).ShouldBeFalse();
        editor.IsColumnVisible(nameof(AuditableEntity.UpdatedAt)).ShouldBeFalse();
        editor.IsColumnVisible(nameof(ConnectionStringsEntity.Sqlite)).ShouldBeTrue();
        editor.IsColumnReadOnly(nameof(ConnectionStringsEntity.Id)).ShouldBeTrue();
    }

    [Fact]
    public void when_creating_the_file_classification_categories_editor_then_the_parent_navigation_is_hidden_and_add_remove_is_enabled()
    {
        var editor = sut.CreateFileClassificationCategoriesEditor();

        editor.CanAddRemove.ShouldBeTrue();
        editor.IsColumnVisible(nameof(FileClassificationCategoryEntity.Parent)).ShouldBeFalse();
        editor.IsColumnVisible(nameof(FileClassificationCategoryEntity.Name)).ShouldBeTrue();
        editor.IsColumnReadOnly(nameof(FileClassificationCategoryEntity.Id)).ShouldBeTrue();
    }

    [Fact]
    public void when_creating_the_search_configuration_editor_then_the_search_categories_collection_is_hidden_and_add_remove_is_disabled()
    {
        var editor = sut.CreateSearchConfigurationEditor();

        editor.CanAddRemove.ShouldBeFalse();
        editor.IsColumnVisible(nameof(SearchConfigurationEntity.SearchCategories)).ShouldBeFalse();
        editor.IsColumnVisible(nameof(AuditableEntity.CreatedAt)).ShouldBeFalse();
    }

    [Fact]
    public void when_creating_the_model_to_ignore_editor_then_add_remove_is_enabled()
    {
        var editor = sut.CreateModelToIgnoreEditor();

        editor.CanAddRemove.ShouldBeTrue();
        editor.IsColumnReadOnly(nameof(ModelToIgnoreEntity.Id)).ShouldBeTrue();
    }

    [Fact]
    public void when_creating_the_scrape_directories_editor_then_add_remove_is_disabled()
    {
        var editor = sut.CreateScrapeDirectoriesEditor();

        editor.CanAddRemove.ShouldBeFalse();
    }

    [Fact]
    public void when_creating_the_tag_to_ignore_editor_then_add_remove_is_enabled()
    {
        var editor = sut.CreateTagToIgnoreEditor();

        editor.CanAddRemove.ShouldBeTrue();
        editor.IsColumnReadOnly(nameof(TagToIgnoreEntity.Id)).ShouldBeTrue();
    }

    [Fact]
    public void when_creating_the_user_configuration_editor_then_add_remove_is_disabled()
    {
        var editor = sut.CreateUserConfigurationEditor();

        editor.CanAddRemove.ShouldBeFalse();
    }

    [Fact]
    public void when_creating_the_search_categories_editor_then_the_foreign_key_column_is_hidden_but_add_remove_is_enabled()
    {
        var editor = sut.CreateSearchCategoriesEditor();

        editor.CanAddRemove.ShouldBeTrue();
        editor.IsColumnVisible(nameof(SearchCategoryEntity.SearchConfiguration)).ShouldBeFalse();
        editor.IsColumnVisible(nameof(SearchCategoryEntity.SearchConfigurationId)).ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_new_search_category_row_is_added_and_saved_then_it_is_stamped_with_the_single_search_configurations_id()
    {
        int searchConfigurationId;
        await using (var seedContext = new AppDbContext(options))
        {
            var searchConfiguration = new SearchConfigurationEntity();
            seedContext.Add(searchConfiguration);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            searchConfigurationId = searchConfiguration.Id;
        }

        var editor = sut.CreateSearchCategoriesEditor();

        await ((ReactiveCommand<RxUnit, RxUnit>)editor.AddCommand).Execute();
        await ((ReactiveCommand<RxUnit, RxUnit>)editor.SaveCommand).Execute();

        await using var context = new AppDbContext(options);
        var row = await context.Set<SearchCategoryEntity>().SingleAsync(TestContext.Current.CancellationToken);
        row.SearchConfigurationId.ShouldBe(searchConfigurationId);
    }

    [Fact]
    public void when_creating_the_file_classification_categories_editor_then_categories_are_listed_alphabetically_by_name()
    {
        SeedClassification("Zebra", includeInSearch: true);
        SeedClassification("apple", includeInSearch: true);
        SeedClassification("Mango", includeInSearch: true);

        var editor = sut.CreateFileClassificationCategoriesEditor();

        ((FileClassificationCategoryEntity)editor.Items[0]!).Name.ShouldBe("apple");
        ((FileClassificationCategoryEntity)editor.Items[1]!).Name.ShouldBe("Mango");
        ((FileClassificationCategoryEntity)editor.Items[2]!).Name.ShouldBe("Zebra");
    }

    [Fact]
    public void when_creating_the_file_classification_categories_editor_then_the_sync_tags_to_ignore_action_is_exposed()
    {
        var editor = sut.CreateFileClassificationCategoriesEditor();

        editor.HasCustomAction.ShouldBeTrue();
        editor.CustomActionLabel.ShouldBe("Sync Tags to Ignore");
    }

    [Fact]
    public async Task when_syncing_tags_to_ignore_and_a_classification_is_excluded_from_search_then_its_name_is_added_to_the_tags_to_ignore()
    {
        SeedClassification("Sunsets", includeInSearch: false);
        var editor = sut.CreateFileClassificationCategoriesEditor();

        await ((ReactiveCommand<RxUnit, RxUnit>)editor.CustomActionCommand!).Execute();

        await using var context = new AppDbContext(options);
        var tag = await context.Set<TagToIgnoreEntity>().SingleAsync(TestContext.Current.CancellationToken);
        tag.Value.ShouldBe("Sunsets");
    }

    [Fact]
    public async Task when_syncing_tags_to_ignore_and_a_classification_is_included_in_search_then_its_matching_tag_is_removed_case_insensitively()
    {
        SeedClassification("Sunsets", includeInSearch: true);
        SeedTag("sunsets");
        var editor = sut.CreateFileClassificationCategoriesEditor();

        await ((ReactiveCommand<RxUnit, RxUnit>)editor.CustomActionCommand!).Execute();

        await using var context = new AppDbContext(options);
        var tags = await context.Set<TagToIgnoreEntity>().ToListAsync(TestContext.Current.CancellationToken);
        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_syncing_tags_to_ignore_and_an_excluded_classification_already_has_a_matching_tag_then_no_duplicate_is_added()
    {
        SeedClassification("Sunsets", includeInSearch: false);
        SeedTag("SUNSETS");
        var editor = sut.CreateFileClassificationCategoriesEditor();

        await ((ReactiveCommand<RxUnit, RxUnit>)editor.CustomActionCommand!).Execute();

        await using var context = new AppDbContext(options);
        var tag = await context.Set<TagToIgnoreEntity>().SingleAsync(TestContext.Current.CancellationToken);
        tag.Value.ShouldBe("SUNSETS");
    }

    [Fact]
    public async Task when_syncing_tags_to_ignore_then_unsaved_include_in_search_edits_are_persisted_and_honoured()
    {
        SeedClassification("Sunsets", includeInSearch: false);
        SeedTag("Sunsets");
        var editor = sut.CreateFileClassificationCategoriesEditor();
        ((FileClassificationCategoryEntity)editor.Items[0]!).IncludeInSearch = true;

        await ((ReactiveCommand<RxUnit, RxUnit>)editor.CustomActionCommand!).Execute();

        await using var context = new AppDbContext(options);
        var classification = await context.Set<FileClassificationCategoryEntity>().SingleAsync(TestContext.Current.CancellationToken);
        classification.IncludeInSearch.ShouldBeTrue();
        var tags = await context.Set<TagToIgnoreEntity>().ToListAsync(TestContext.Current.CancellationToken);
        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_syncing_tags_to_ignore_and_a_classification_has_a_blank_name_then_no_tag_is_added_for_it()
    {
        SeedClassification("", includeInSearch: false);
        var editor = sut.CreateFileClassificationCategoriesEditor();

        await ((ReactiveCommand<RxUnit, RxUnit>)editor.CustomActionCommand!).Execute();

        await using var context = new AppDbContext(options);
        var tags = await context.Set<TagToIgnoreEntity>().ToListAsync(TestContext.Current.CancellationToken);
        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_syncing_tags_to_ignore_then_the_status_message_reports_the_added_and_removed_counts()
    {
        SeedClassification("Sunsets", includeInSearch: false);
        SeedClassification("Mountains", includeInSearch: true);
        SeedTag("Mountains");
        var editor = sut.CreateFileClassificationCategoriesEditor();

        await ((ReactiveCommand<RxUnit, RxUnit>)editor.CustomActionCommand!).Execute();

        editor.StatusMessage.ShouldBe("Synced tags to ignore: 1 added, 1 removed.");
    }

    public void Dispose() =>
        connection.Dispose();

    private void SeedClassification(string name, bool includeInSearch)
    {
        using var context = new AppDbContext(options);
        context.Add(new FileClassificationCategoryEntity { Name = name, IncludeInSearch = includeInSearch });
        context.SaveChanges();
    }

    private void SeedTag(string value)
    {
        using var context = new AppDbContext(options);
        context.Add(new TagToIgnoreEntity { Value = value });
        context.SaveChanges();
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() =>
            new(options);
    }
}
