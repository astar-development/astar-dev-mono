using System.Reactive.Linq;
using System.Windows.Input;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using Testably.Abstractions.Testing;
using RxUnit = System.Reactive.Unit;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Configuration.EntityEditor;

public sealed class GivenEntityEditorViewModel : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;
    private readonly MockFileSystem fileSystem = new();
    private readonly DbContextOptions<AppDbContext> options;

    public GivenEntityEditorViewModel()
    {
        connection.Open();
        options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var migrationContext = new AppDbContext(options))
        {
            migrationContext.Database.Migrate();
        }

        dbContextFactory = new TestDbContextFactory(options);
        fileSystem.Directory.CreateDirectory("/exports");
    }

    [Fact]
    public void when_constructed_then_items_are_loaded_from_the_database()
    {
        SeedTag("existing-tag");

        var sut = CreateSut();

        sut.Items.Count.ShouldBe(1);
        ((TagToIgnoreEntity)sut.Items[0]!).Value.ShouldBe("existing-tag");
    }

    [Fact]
    public async Task when_a_row_is_added_and_saved_then_it_is_persisted_to_the_database()
    {
        var sut = CreateSut();

        await Command(sut.AddCommand).Execute();
        await Command(sut.SaveCommand).Execute();

        await using var context = new AppDbContext(options);
        var rows = await context.Set<TagToIgnoreEntity>().ToListAsync(TestContext.Current.CancellationToken);
        rows.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_an_existing_row_is_edited_in_place_and_saved_then_the_change_is_persisted()
    {
        SeedTag("original");
        var sut = CreateSut();
        var tracked = (TagToIgnoreEntity)sut.Items[0]!;

        tracked.Value = "updated";
        await Command(sut.SaveCommand).Execute();

        await using var context = new AppDbContext(options);
        var row = await context.Set<TagToIgnoreEntity>().SingleAsync(TestContext.Current.CancellationToken);
        row.Value.ShouldBe("updated");
    }

    [Fact]
    public async Task when_a_row_is_selected_and_deleted_and_saved_then_it_is_removed_from_the_database()
    {
        SeedTag("to-delete");
        var sut = CreateSut();
        sut.SelectedItem = sut.Items[0];

        await Command(sut.DeleteCommand).Execute();
        await Command(sut.SaveCommand).Execute();

        await using var context = new AppDbContext(options);
        var rows = await context.Set<TagToIgnoreEntity>().ToListAsync(TestContext.Current.CancellationToken);
        rows.ShouldBeEmpty();
    }

    [Fact]
    public void when_the_descriptor_disallows_add_and_remove_and_no_row_exists_then_the_add_command_can_execute()
    {
        var sut = CreateSingleRowSut();

        Command(sut.AddCommand).CanExecute.FirstAsync().Wait().ShouldBeTrue();
        sut.CanAddRemove.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_descriptor_disallows_add_and_remove_and_a_row_already_exists_then_the_add_command_cannot_execute()
    {
        var sut = CreateSingleRowSut();
        await Command(sut.AddCommand).Execute();

        Command(sut.AddCommand).CanExecute.FirstAsync().Wait().ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_descriptor_disallows_add_and_remove_and_a_row_is_selected_then_the_delete_command_can_execute()
    {
        var sut = CreateSingleRowSut();
        await Command(sut.AddCommand).Execute();
        sut.SelectedItem = sut.Items[0];

        Command(sut.DeleteCommand).CanExecute.FirstAsync().Wait().ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_descriptor_disallows_add_and_remove_and_the_only_row_is_deleted_then_the_add_command_can_execute_again()
    {
        var sut = CreateSingleRowSut();
        await Command(sut.AddCommand).Execute();
        sut.SelectedItem = sut.Items[0];
        await Command(sut.DeleteCommand).Execute();

        Command(sut.AddCommand).CanExecute.FirstAsync().Wait().ShouldBeTrue();
    }

    [Fact]
    public void when_no_row_is_selected_then_the_delete_command_cannot_execute()
    {
        var sut = CreateSut();

        Command(sut.DeleteCommand).CanExecute.FirstAsync().Wait().ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_export_command_runs_then_the_current_items_are_written_as_json_to_the_export_directory()
    {
        SeedTag("exported-tag");
        var sut = CreateSut();

        await Command(sut.ExportCommand).Execute();

        fileSystem.File.Exists("/exports/TagToIgnore.json").ShouldBeTrue();
        var exported = fileSystem.File.ReadAllText("/exports/TagToIgnore.json").FromJson<List<TagToIgnoreEntity>>(Constants.WebDeserialisationSettings);
        exported.Count.ShouldBe(1);
        exported[0].Value.ShouldBe("exported-tag");
    }

    [Fact]
    public async Task when_the_import_command_runs_then_items_are_replaced_from_json_but_the_database_is_untouched_until_saved()
    {
        SeedTag("original-tag");
        var sut = CreateSut();
        fileSystem.File.WriteAllText("/exports/TagToIgnore.json", new List<TagToIgnoreEntity> { new() { Value = "imported-tag" } }.ToJson());

        await Command(sut.ImportCommand).Execute();

        sut.Items.Count.ShouldBe(1);
        ((TagToIgnoreEntity)sut.Items[0]!).Value.ShouldBe("imported-tag");
        await using var context = new AppDbContext(options);
        var row = await context.Set<TagToIgnoreEntity>().SingleAsync(TestContext.Current.CancellationToken);
        row.Value.ShouldBe("original-tag");
    }

    [Fact]
    public async Task when_the_import_command_runs_and_is_then_saved_then_the_database_reflects_the_imported_rows_only()
    {
        SeedTag("original-tag");
        var sut = CreateSut();
        fileSystem.File.WriteAllText("/exports/TagToIgnore.json", new List<TagToIgnoreEntity> { new() { Value = "imported-tag" } }.ToJson());

        await Command(sut.ImportCommand).Execute();
        await Command(sut.SaveCommand).Execute();

        await using var context = new AppDbContext(options);
        var rows = await context.Set<TagToIgnoreEntity>().ToListAsync(TestContext.Current.CancellationToken);
        rows.Count.ShouldBe(1);
        rows[0].Value.ShouldBe("imported-tag");
    }

    [Fact]
    public async Task when_the_import_command_runs_and_the_export_file_is_missing_then_the_status_message_reports_the_failure_without_throwing()
    {
        var sut = CreateSut();

        await Command(sut.ImportCommand).Execute();

        sut.StatusMessage.ShouldBe("Import failed: /exports/TagToIgnore.json was not found.");
    }

    [Fact]
    public async Task when_the_save_command_succeeds_then_the_status_message_reports_the_saved_row_count()
    {
        var sut = CreateSut();

        await Command(sut.AddCommand).Execute();
        await Command(sut.SaveCommand).Execute();

        sut.StatusMessage.ShouldBe("Saved 1 row(s).");
    }

    [Fact]
    public async Task when_the_save_command_fails_then_the_status_message_reports_the_failure()
    {
        var sut = CreateOnBeforeAddSut((_, _, _) => throw new InvalidOperationException("The before-add hook failed."));

        await Command(sut.AddCommand).Execute();
        await Command(sut.SaveCommand).Execute();

        sut.StatusMessage.ShouldBe("Save failed: The before-add hook failed.");
    }

    [Fact]
    public async Task when_the_export_command_succeeds_then_the_status_message_reports_the_exported_row_count_and_path()
    {
        SeedTag("exported-tag");
        var sut = CreateSut();

        await Command(sut.ExportCommand).Execute();

        sut.StatusMessage.ShouldBe("Exported 1 row(s) to /exports/TagToIgnore.json.");
    }

    [Fact]
    public async Task when_the_export_command_fails_then_the_status_message_reports_the_failure()
    {
        fileSystem.Directory.CreateDirectory("/exports/TagToIgnore.json");
        var sut = CreateSut();

        await Command(sut.ExportCommand).Execute();

        sut.StatusMessage.ShouldStartWith("Export failed: ");
    }

    [Fact]
    public async Task when_the_import_command_succeeds_then_the_status_message_reports_the_imported_row_count_and_path()
    {
        var sut = CreateSut();
        fileSystem.File.WriteAllText("/exports/TagToIgnore.json", new List<TagToIgnoreEntity> { new() { Value = "imported-tag" } }.ToJson());

        await Command(sut.ImportCommand).Execute();

        sut.StatusMessage.ShouldBe("Imported 1 row(s) from /exports/TagToIgnore.json. Click Save to persist.");
    }

    [Fact]
    public async Task when_the_import_command_reads_invalid_json_then_the_status_message_reports_the_failure()
    {
        var sut = CreateSut();
        fileSystem.File.WriteAllText("/exports/TagToIgnore.json", "this is not valid json");

        await Command(sut.ImportCommand).Execute();

        sut.StatusMessage.ShouldStartWith("Import failed: ");
    }

    [Fact]
    public void when_the_descriptor_has_a_sort_key_then_items_are_loaded_in_case_insensitive_alphabetical_order()
    {
        SeedTag("zebra");
        SeedTag("Apple");
        SeedTag("mango");

        var sut = CreateSortedSut();

        ((TagToIgnoreEntity)sut.Items[0]!).Value.ShouldBe("Apple");
        ((TagToIgnoreEntity)sut.Items[1]!).Value.ShouldBe("mango");
        ((TagToIgnoreEntity)sut.Items[2]!).Value.ShouldBe("zebra");
    }

    [Fact]
    public async Task when_the_descriptor_has_a_sort_key_then_imported_items_are_sorted()
    {
        var sut = CreateSortedSut();
        fileSystem.File.WriteAllText("/exports/TagToIgnore.json", new List<TagToIgnoreEntity> { new() { Value = "zebra" }, new() { Value = "Apple" } }.ToJson());

        await Command(sut.ImportCommand).Execute();

        ((TagToIgnoreEntity)sut.Items[0]!).Value.ShouldBe("Apple");
        ((TagToIgnoreEntity)sut.Items[1]!).Value.ShouldBe("zebra");
    }

    [Fact]
    public void when_the_descriptor_has_no_custom_action_then_no_custom_action_is_exposed()
    {
        var sut = CreateSut();

        sut.HasCustomAction.ShouldBeFalse();
        sut.CustomActionCommand.ShouldBeNull();
        sut.CustomActionLabel.ShouldBeNull();
    }

    [Fact]
    public void when_the_descriptor_has_a_custom_action_then_the_label_and_command_are_exposed()
    {
        var sut = CreateCustomActionSut((_, _) => Task.FromResult("done"));

        sut.HasCustomAction.ShouldBeTrue();
        sut.CustomActionCommand.ShouldNotBeNull();
        sut.CustomActionLabel.ShouldBe("Run Custom Action");
    }

    [Fact]
    public async Task when_the_custom_action_runs_then_it_receives_the_editors_context_and_its_result_becomes_the_status_message()
    {
        var sut = CreateCustomActionSut(async (context, token) =>
        {
            context.Add(new TagToIgnoreEntity { Value = "added-by-action" });
            await context.SaveChangesAsync(token);

            return "Custom action completed.";
        });

        await Command(sut.CustomActionCommand!).Execute();

        sut.StatusMessage.ShouldBe("Custom action completed.");
        await using var context = new AppDbContext(options);
        var row = await context.Set<TagToIgnoreEntity>().SingleAsync(TestContext.Current.CancellationToken);
        row.Value.ShouldBe("added-by-action");
    }

    [Fact]
    public async Task when_the_custom_action_throws_then_the_status_message_reports_the_failure()
    {
        var sut = CreateCustomActionSut((_, _) => throw new InvalidOperationException("The custom action failed."));

        await Command(sut.CustomActionCommand!).Execute();

        sut.StatusMessage.ShouldBe("Run Custom Action failed: The custom action failed.");
    }

    [Fact]
    public async Task when_the_descriptor_has_an_on_before_add_hook_then_it_runs_for_newly_added_rows_during_save()
    {
        var sut = CreateOnBeforeAddSut((_, entity, _) =>
        {
            entity.Value = "stamped-by-hook";

            return Task.CompletedTask;
        });

        await Command(sut.AddCommand).Execute();
        await Command(sut.SaveCommand).Execute();

        await using var context = new AppDbContext(options);
        var row = await context.Set<TagToIgnoreEntity>().SingleAsync(TestContext.Current.CancellationToken);
        row.Value.ShouldBe("stamped-by-hook");
    }

    public void Dispose() =>
        connection.Dispose();

    private void SeedTag(string value)
    {
        using var context = new AppDbContext(options);
        context.Add(new TagToIgnoreEntity { Value = value });
        context.SaveChanges();
    }

    private EntityEditorViewModel<TagToIgnoreEntity> CreateSut()
    {
        var descriptor = new EntityEditorDescriptor<TagToIgnoreEntity>(
            DisplayName: "Tag to Ignore",
            TableName: "TagToIgnore",
            CreateNew: () => new TagToIgnoreEntity(),
            AllowAddRemove: true,
            ExcludedColumns: [nameof(AuditableEntity.CreatedAt), nameof(AuditableEntity.UpdatedAt)],
            ReadOnlyColumns: [nameof(TagToIgnoreEntity.Id)]);

        return new EntityEditorViewModel<TagToIgnoreEntity>(dbContextFactory, descriptor, fileSystem, "/exports");
    }

    private EntityEditorViewModel<TagToIgnoreEntity> CreateSortedSut()
    {
        var descriptor = new EntityEditorDescriptor<TagToIgnoreEntity>(
            DisplayName: "Tag to Ignore",
            TableName: "TagToIgnore",
            CreateNew: () => new TagToIgnoreEntity(),
            AllowAddRemove: true,
            ExcludedColumns: [nameof(AuditableEntity.CreatedAt), nameof(AuditableEntity.UpdatedAt)],
            ReadOnlyColumns: [nameof(TagToIgnoreEntity.Id)],
            OrderItemsBy: tag => tag.Value);

        return new EntityEditorViewModel<TagToIgnoreEntity>(dbContextFactory, descriptor, fileSystem, "/exports");
    }

    private EntityEditorViewModel<TagToIgnoreEntity> CreateCustomActionSut(Func<AppDbContext, CancellationToken, Task<string>> customActionAsync)
    {
        var descriptor = new EntityEditorDescriptor<TagToIgnoreEntity>(
            DisplayName: "Tag to Ignore",
            TableName: "TagToIgnore",
            CreateNew: () => new TagToIgnoreEntity(),
            AllowAddRemove: true,
            ExcludedColumns: [nameof(AuditableEntity.CreatedAt), nameof(AuditableEntity.UpdatedAt)],
            ReadOnlyColumns: [nameof(TagToIgnoreEntity.Id)],
            CustomActionLabel: "Run Custom Action",
            CustomActionAsync: customActionAsync);

        return new EntityEditorViewModel<TagToIgnoreEntity>(dbContextFactory, descriptor, fileSystem, "/exports");
    }

    private EntityEditorViewModel<TagToIgnoreEntity> CreateSingleRowSut()
    {
        var descriptor = new EntityEditorDescriptor<TagToIgnoreEntity>(
            DisplayName: "Tag to Ignore",
            TableName: "TagToIgnore",
            CreateNew: () => new TagToIgnoreEntity(),
            AllowAddRemove: false,
            ExcludedColumns: [nameof(AuditableEntity.CreatedAt), nameof(AuditableEntity.UpdatedAt)],
            ReadOnlyColumns: [nameof(TagToIgnoreEntity.Id)]);

        return new EntityEditorViewModel<TagToIgnoreEntity>(dbContextFactory, descriptor, fileSystem, "/exports");
    }

    private EntityEditorViewModel<TagToIgnoreEntity> CreateOnBeforeAddSut(Func<AppDbContext, TagToIgnoreEntity, CancellationToken, Task> onBeforeAddAsync)
    {
        var descriptor = new EntityEditorDescriptor<TagToIgnoreEntity>(
            DisplayName: "Tag to Ignore",
            TableName: "TagToIgnore",
            CreateNew: () => new TagToIgnoreEntity(),
            AllowAddRemove: true,
            ExcludedColumns: [nameof(AuditableEntity.CreatedAt), nameof(AuditableEntity.UpdatedAt)],
            ReadOnlyColumns: [nameof(TagToIgnoreEntity.Id)],
            OnBeforeAddAsync: onBeforeAddAsync);

        return new EntityEditorViewModel<TagToIgnoreEntity>(dbContextFactory, descriptor, fileSystem, "/exports");
    }

    private static ReactiveCommand<RxUnit, RxUnit> Command(ICommand command) =>
        (ReactiveCommand<RxUnit, RxUnit>)command;

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() =>
            new(options);
    }
}
