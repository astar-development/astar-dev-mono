using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public static class DataSeed
{
    private static readonly string[] TagsToIgnoreCompletelyValues =
    [
        "Vladislava Shelygina", "Bianca Beauchamp", "Uy Uy", "CGI", "Functions",
        "hairy armpits", "Beau D", "Lucie Wilde", "Brooke Adams", "erotic art",
        "concept art", "2D", "foot fetishism", "curvy", "Big Areola", "big areolae",
        "cartoon", "artwork", "Jana Defi", "Piper Perri", "Dakota Pink", "saggy boobs",
        "Sarah Jay", "Sara Jay", "fan art"
    ];

    public static async Task SeedTagsToIgnoreAsync(Logger logger, AppDbContext dbContext)
    {
        if (!dbContext.TagsToIgnore.Any(t => t.IgnoreImage))
        {
            logger.Information("Seeding tags to ignore completely...");
            dbContext.TagsToIgnore.AddRange(
                TagsToIgnoreCompletelyValues.Distinct().Select(tag => new TagToIgnoreEntity { Value = tag, IgnoreImage = true }));
            await dbContext.SaveChangesAsync();
        }
    }

    // TODO(#697): rewrite against the FileClassificationCategoryEntity/FileClassificationKeywordEntity
    // hierarchy - the CSV's flat "DatabaseMapping"/"Celebrity" columns need real hierarchy placement
    // decisions this phase doesn't make. Stubbed as a no-op during #696's mechanical AppDbContext migration.
    public static Task SeedFileClassificationsAsync(string csvPath, Logger logger, AppDbContext dbContext)
        => Task.CompletedTask;
}
