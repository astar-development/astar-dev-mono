using AStarDev.ControlDb.ScrapeConfiguration;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenASeeder : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-seeder-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenASeeder()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public void when_the_context_is_null_then_seeding_throws_an_argument_null_exception()
        => Should.Throw<ArgumentNullException>(() => Seeder.Seed(null!)).ParamName.ShouldBe("context");

    [Fact]
    public void when_the_context_is_null_then_async_seeding_throws_an_argument_null_exception()
        => Should.Throw<ArgumentNullException>(() => Seeder.SeedAsync(null!, TestContext.Current.CancellationToken)).ParamName.ShouldBe("context");

    [Fact]
    public async Task when_the_database_is_seeded_then_the_user_details_are_neutral_placeholders()
    {
        await Seeder.SeedAsync(context, TestContext.Current.CancellationToken);

        var user = (await context.Set<ScrapeConfigurationEntity>().Include(configuration => configuration.UserConfiguration).SingleAsync(TestContext.Current.CancellationToken)).UserConfiguration;

        (user.EmailAddress, user.Username).ShouldBe(("user@example.com", "user"));
    }

    [Fact]
    public async Task when_the_database_is_seeded_then_the_root_directory_is_a_wallhaven_folder_under_the_users_pictures_not_a_developer_specific_path()
    {
        await Seeder.SeedAsync(context, TestContext.Current.CancellationToken);

        var directories = (await context.Set<ScrapeConfigurationEntity>().Include(configuration => configuration.ScrapeDirectories).SingleAsync(TestContext.Current.CancellationToken)).ScrapeDirectories;

        (Path.GetFileName(directories.RootDirectory), Path.IsPathRooted(directories.RootDirectory), directories.RootDirectory.Contains("Tbdrive", StringComparison.Ordinal), Path.GetFileName(directories.RootDirectoryFamous)).ShouldBe(("WallHaven", true, false, "Famous"));
    }

    [Fact]
    public async Task when_the_database_is_seeded_then_the_hot_wallpapers_setting_defaults_to_hot_wallpapers()
    {
        await Seeder.SeedAsync(context, TestContext.Current.CancellationToken);

        var configuration = await context.Set<ScrapeConfigurationEntity>().SingleAsync(TestContext.Current.CancellationToken);

        configuration.HotWallpapers.ShouldBe("HotWallpapers");
    }

    [Fact]
    public async Task when_the_database_is_empty_then_seeding_adds_a_scrape_configuration()
    {
        await Seeder.SeedAsync(context, TestContext.Current.CancellationToken);

        var configurations = await context.Set<ScrapeConfigurationEntity>().ToListAsync(TestContext.Current.CancellationToken);

        configurations.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_database_already_contains_a_scrape_configuration_then_seeding_again_does_not_duplicate_it()
    {
        await Seeder.SeedAsync(context, TestContext.Current.CancellationToken);

        await Seeder.SeedAsync(context, TestContext.Current.CancellationToken);

        var configurations = await context.Set<ScrapeConfigurationEntity>().ToListAsync(TestContext.Current.CancellationToken);
        configurations.Count.ShouldBe(1);
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

        context.Dispose();
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
