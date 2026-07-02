using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
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

    public static async Task SeedTagsToIgnoreAsync(Logger logger, FilesContext dbContext)
    {
        if (!dbContext.TagsToIgnore.Any(t => t.IgnoreImage))
        {
            logger.Information("Seeding tags to ignore completely...");
            dbContext.TagsToIgnore.AddRange(
                TagsToIgnoreCompletelyValues.Distinct().Select(tag => new TagToIgnore { Value = tag, IgnoreImage = true }));
            await dbContext.SaveChangesAsync();
        }
    }

    public static async Task SeedFileClassificationsAsync(string csvPath, Logger logger, FilesContext dbContext)
    {
        if (!File.Exists(csvPath)) return;

        if (dbContext.FileClassifications.Any()) return;

        logger.Information("Seeding file classifications from {CsvPath}...", csvPath);

        string[] lines = await File.ReadAllLinesAsync(csvPath);
        var rows = lines.Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(','))
            .Where(parts => parts.Length >= 4)
            .Select(parts => new
            {
                FileNameContains = parts[0],
                DatabaseMapping = parts[1].Trim(),
                Celebrity = parts[2].Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                Searchable = parts[3].Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase)
            })
            .ToList();

        foreach (var group in rows.GroupBy(r => r.DatabaseMapping))
        {
            var first = group.First();
            var classification = new FileClassification
            {
                Name = group.Key,
                Celebrity = first.Celebrity,
                IncludeInSearch = first.Searchable
            };

            foreach (var part in group.Select(r => new FileNamePart { Text = r.FileNameContains, IncludeInSearch = r.Searchable }))
            {
                classification.FileNameParts.Add(part);
            }

            dbContext.FileClassifications.Add(classification);
        }

        await dbContext.SaveChangesAsync();
    }
}
