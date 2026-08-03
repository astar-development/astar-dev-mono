using System.Reactive.Linq;
using System.Windows.Input;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.Utilities;
using AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using Testably.Abstractions.Testing;
using RxUnit = System.Reactive.Unit;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Integration.Configuration.EntityEditor;

public sealed class GivenEntityEditorViewModelImport : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;
    private readonly MockFileSystem fileSystem = new();
    private readonly DbContextOptions<AppDbContext> options;

    public GivenEntityEditorViewModelImport()
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
    public async Task when_multiple_unsaved_new_rows_share_the_default_key_and_the_import_command_runs_then_it_does_not_throw()
    {
        var sut = CreateSut();

        await Command(sut.AddCommand).Execute();
        await Command(sut.AddCommand).Execute();
        fileSystem.File.WriteAllText("/exports/FileClassificationCategories.json", new List<FileClassificationCategoryEntity> { new() { Name = "imported-category" } }.ToJson());

        await Command(sut.ImportCommand).Execute();

        sut.StatusMessage.ShouldStartWith("Imported ");
    }

    [Fact]
    public async Task when_imported_rows_carry_a_duplicated_nested_parent_navigation_then_the_import_command_does_not_throw()
    {
        var sut = CreateSut();
        var root = new FileClassificationCategoryEntity { Id = 78, Name = "Famous", Level = 2, Priority = 78 };

        var imported = new List<FileClassificationCategoryEntity>
        {
            root,
            new()
            {
                Id = 994, Name = "Miss April", Level = 3, Priority = 994, ParentId = 78,
                Parent = new FileClassificationCategoryEntity { Id = 78, Name = "Famous", Level = 2, Priority = 78 }
            },
            new()
            {
                Id = 998, Name = "Miss August", Level = 3, Priority = 998, ParentId = 78,
                Parent = new FileClassificationCategoryEntity { Id = 78, Name = "Famous", Level = 2, Priority = 78 }
            }
        };

        fileSystem.File.WriteAllText("/exports/FileClassificationCategories.json", imported.ToJson());

        await Command(sut.ImportCommand).Execute();

        sut.StatusMessage.ShouldStartWith("Imported ");

        await Command(sut.SaveCommand).Execute();

        sut.StatusMessage.ShouldStartWith("Saved ");
        await using var context = new AppDbContext(options);
        var rows = await context.Set<FileClassificationCategoryEntity>().ToListAsync(TestContext.Current.CancellationToken);
        rows.ShouldContain(row => row.Id == 78 && row.Name == "Famous");
        rows.ShouldContain(row => row.Id == 994 && row.ParentId == 78);
        rows.ShouldContain(row => row.Id == 998 && row.ParentId == 78);
    }

    public void Dispose() =>
        connection.Dispose();

    private EntityEditorViewModel<FileClassificationCategoryEntity> CreateSut()
    {
        var descriptor = new EntityEditorDescriptor<FileClassificationCategoryEntity>(
            DisplayName: "File Classification Categories",
            TableName: "FileClassificationCategories",
            CreateNew: () => new FileClassificationCategoryEntity(),
            AllowAddRemove: true,
            ExcludedColumns: [nameof(FileClassificationCategoryEntity.Parent)],
            ReadOnlyColumns: [nameof(FileClassificationCategoryEntity.Id)]);

        return new EntityEditorViewModel<FileClassificationCategoryEntity>(dbContextFactory, descriptor, fileSystem, "/exports");
    }

    private static ReactiveCommand<RxUnit, RxUnit> Command(ICommand command) =>
        (ReactiveCommand<RxUnit, RxUnit>)command;

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() =>
            new(options);
    }
}
