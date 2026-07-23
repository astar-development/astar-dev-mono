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
