using AStarDev.WallpaperScraper.Startup;
using AStarDev.WallpaperScraper.TestsUnit.Fakes;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace AStarDev.WallpaperScraper.TestsUnit.Startup;

public sealed class GivenADatabaseMigrator
{
    [Fact]
    public async Task when_the_migration_fails_then_a_critical_failure_is_logged_by_type_only_and_rethrown()
    {
        var logger = new CapturingLogger<GivenADatabaseMigrator>();
        var failure = new InvalidOperationException("cannot open /home/user/.local/share/scraper.db");

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => DatabaseMigrator.MigrateAsync(new ThrowingDbContextFactory(failure), logger));

        thrown.ShouldBeSameAs(failure);
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Critical);
        logger.Events.ShouldContain(recorded => recorded.Name == "DatabaseMigrationFailed" && Equals(recorded.Values["ErrorType"], "System.InvalidOperationException"));
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains("/home/user"));
    }
}
