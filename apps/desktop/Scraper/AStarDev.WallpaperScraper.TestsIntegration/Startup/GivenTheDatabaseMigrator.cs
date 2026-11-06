using AStarDev.ControlDb;
using AStarDev.WallpaperScraper.Startup;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace AStarDev.WallpaperScraper.TestsIntegration.Startup;

/// <summary>Runs the real startup migration against a temp SQLite file, so a model change without a matching migration fails here rather than at application start.</summary>
public sealed class GivenTheDatabaseMigrator : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-WallpaperScraper-migrator-{Guid.CreateVersion7():N}.db");
    private readonly ServiceProvider serviceProvider;
    private bool disposed;

    public GivenTheDatabaseMigrator() =>
        serviceProvider = new ServiceCollection().AddDataServices(databasePath).BuildServiceProvider();

    [Fact]
    public async Task when_a_new_database_is_migrated_then_it_succeeds()
    {
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ControlDbContext>>();

        await Should.NotThrowAsync(() => DatabaseMigrator.MigrateAsync(factory, NullLogger.Instance));
    }

    [Fact]
    public async Task when_a_new_database_is_migrated_then_the_model_has_no_pending_changes()
    {
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ControlDbContext>>();
        await DatabaseMigrator.MigrateAsync(factory, NullLogger.Instance);

        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_database_with_data_is_migrated_to_the_latest_version_then_the_data_is_kept()
    {
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ControlDbContext>>();
        await using (var older = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await MigrateWithoutSeedingAsync(older, "20260929073110_AddPersonCategories", TestContext.Current.CancellationToken);
            await older.Database.ExecuteSqlRawAsync("ALTER TABLE UserConfigurations DROP COLUMN Password", TestContext.Current.CancellationToken);
            await older.Database.ExecuteSqlRawAsync("ALTER TABLE UserConfigurations ADD COLUMN Password TEXT NOT NULL DEFAULT 'old-secret'", TestContext.Current.CancellationToken);
            await SeedOlderSchemaAsync(older, TestContext.Current.CancellationToken);
            await older.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await DatabaseMigrator.MigrateAsync(factory, NullLogger.Instance);

        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var configuration = await context.ScrapeConfigurations.Include(scrapeConfiguration => scrapeConfiguration.SearchConfiguration).ThenInclude(search => search.SearchCategories).Include(scrapeConfiguration => scrapeConfiguration.SearchConfiguration).ThenInclude(search => search.PersonCategories).SingleAsync(TestContext.Current.CancellationToken);
        configuration.LoginUrl.ShouldBe("https://example.com/login");
        configuration.SearchConfiguration.SearchCategories.Count.ShouldBe(3);
        configuration.SearchConfiguration.PersonCategories.Count.ShouldBe(5);
    }

    [Fact]
    public async Task when_files_exist_before_the_last_updated_column_is_added_then_it_is_back_filled_from_the_access_detail_and_zero_when_there_is_none()
    {
        const string withDetail = "11111111-1111-1111-1111-111111111111";
        const string withoutDetail = "22222222-2222-2222-2222-222222222222";
        const long detailsLastUpdatedTicks = 639000000000000000;
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ControlDbContext>>();
        await using (var older = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await MigrateWithoutSeedingAsync(older, "20260929214238_LoginUrlIsFreeText", TestContext.Current.CancellationToken);
            await older.Database.ExecuteSqlRawAsync($"INSERT INTO FileDetail (Id, FileHandle, FileSize, IsImage, DirectoryName, FileName, FileType) VALUES ('{withDetail}', 'handle-1', 1, 1, 'dir', 'one.jpg', 'image/jpeg'), ('{withoutDetail}', 'handle-2', 1, 1, 'dir', 'two.jpg', 'image/jpeg')", TestContext.Current.CancellationToken);
            await older.Database.ExecuteSqlRawAsync($"INSERT INTO FileAccessDetail (Id, FileId, DetailsLastUpdated_Ticks, MoveRequired) VALUES ('33333333-3333-3333-3333-333333333333', '{withDetail}', {detailsLastUpdatedTicks}, 0)", TestContext.Current.CancellationToken);
        }

        await DatabaseMigrator.MigrateAsync(factory, NullLogger.Instance);

        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var backFilled = await context.Database.SqlQueryRaw<long>("SELECT LastUpdated_Ticks AS Value FROM FileDetail ORDER BY FileHandle").ToListAsync(TestContext.Current.CancellationToken);
        string.Join(",", backFilled).ShouldBe($"{detailsLastUpdatedTicks},0");
    }

    [Fact]
    public async Task when_a_database_with_a_stored_password_is_migrated_then_the_password_column_is_gone_and_the_rest_of_the_user_configuration_is_kept()
    {
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ControlDbContext>>();
        await using (var older = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await MigrateWithoutSeedingAsync(older, "20260930085048_AddFileLastUpdated", TestContext.Current.CancellationToken);
            await older.Database.ExecuteSqlRawAsync("ALTER TABLE UserConfigurations DROP COLUMN Password", TestContext.Current.CancellationToken);
            await older.Database.ExecuteSqlRawAsync("ALTER TABLE UserConfigurations ADD COLUMN Password TEXT NOT NULL DEFAULT 'stored-secret'", TestContext.Current.CancellationToken);
            await SeedOlderSchemaAsync(older, TestContext.Current.CancellationToken);
        }

        await DatabaseMigrator.MigrateAsync(factory, NullLogger.Instance);

        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var passwordColumns = await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM pragma_table_info('UserConfigurations') WHERE name = 'Password'").SingleAsync(TestContext.Current.CancellationToken);
        var user = (await context.ScrapeConfigurations.Include(configuration => configuration.UserConfiguration).SingleAsync(TestContext.Current.CancellationToken)).UserConfiguration;
        (passwordColumns, user.EmailAddress, user.Username).ShouldBe((0, "user@example.com", "user"));
    }

    [Fact]
    public async Task when_tags_exist_before_the_famous_column_is_added_then_person_name_tags_are_flagged_famous_and_the_rest_are_not()
    {
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ControlDbContext>>();
        await using (var older = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await MigrateWithoutSeedingAsync(older, "20261001140613_AddTagIsName", TestContext.Current.CancellationToken);
            await SeedOlderSchemaAsync(older, TestContext.Current.CancellationToken);
            await older.Database.ExecuteSqlRawAsync("INSERT INTO Tags (Id, WallhavenTagId, Name, Alias, CategoryId, Category, Purity, IgnoreImage, IsName) VALUES ('a1', 1, 'Emma Watson', 'emma', 1, 'celebrities', 'sfw', 0, 0), ('a2', 2, 'finger pointing', 'finger', 1, 'Celebrities', 'sfw', 0, 0), ('a3', 3, 'Formula 1', 'f1', 1, 'Sports', 'sfw', 0, 0)", TestContext.Current.CancellationToken);
        }

        await DatabaseMigrator.MigrateAsync(factory, NullLogger.Instance);

        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var famous = await context.Database.SqlQueryRaw<string>("SELECT Name AS Value FROM Tags WHERE IsFamous = 1 ORDER BY Name").ToListAsync(TestContext.Current.CancellationToken);
        string.Join(",", famous).ShouldBe("Emma Watson");
    }

    [Fact]
    public async Task when_a_configuration_exists_before_the_hot_wallpapers_columns_are_added_then_it_gets_the_default_url_and_zero_pages()
    {
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ControlDbContext>>();
        await using (var older = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await MigrateWithoutSeedingAsync(older, "20261001150610_AddTagIsFamous", TestContext.Current.CancellationToken);
            await SeedOlderSchemaAsync(older, TestContext.Current.CancellationToken);
        }

        await DatabaseMigrator.MigrateAsync(factory, NullLogger.Instance);

        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var configuration = await context.ScrapeConfigurations.SingleAsync(TestContext.Current.CancellationToken);
        (configuration.HotWallpapers, configuration.HotWallpapersStartingPageNumber, configuration.HotWallpapersTotalPages).ShouldBe(("HotWallpapers", 0, 0));
    }

    /// <summary>Seeds with the current model, which also writes the hot wallpapers columns: add them for the insert, then drop them so the schema is as old as the migration it was built to.</summary>
    private static async Task SeedOlderSchemaAsync(ControlDbContext context, CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE ScrapeConfigurations ADD COLUMN HotWallpapers TEXT NOT NULL DEFAULT ''", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE ScrapeConfigurations ADD COLUMN HotWallpapersStartingPageNumber INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE ScrapeConfigurations ADD COLUMN HotWallpapersTotalPages INTEGER NOT NULL DEFAULT 0", cancellationToken);

        await Seeder.SeedAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await context.Database.ExecuteSqlRawAsync("ALTER TABLE ScrapeConfigurations DROP COLUMN HotWallpapers", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE ScrapeConfigurations DROP COLUMN HotWallpapersStartingPageNumber", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE ScrapeConfigurations DROP COLUMN HotWallpapersTotalPages", cancellationToken);
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "The text is the migrator's own generated script for a fixed migration name; no user input.")]
    private static async Task MigrateWithoutSeedingAsync(ControlDbContext context, string targetMigration, CancellationToken cancellationToken)
    {
        // IMigrator.MigrateAsync also runs the (current-model) seeder, which cannot insert into an older schema; apply the generated script instead.
        var script = context.GetService<IMigrator>().GenerateScript(toMigration: targetMigration);
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = script;
        _ = await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        serviceProvider.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }
}
