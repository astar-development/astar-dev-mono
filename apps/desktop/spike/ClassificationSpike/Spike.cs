using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure;
using AStar.Dev.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace ClassificationSpike;

public static class Spike
{

    internal static void ConfigureDbContext(DbContextOptionsBuilder builder)
    {
        string dbPath = ApplicationMetadata.ApplicationNameHyphenated.ApplicationDirectory().CombinePath($"{ApplicationMetadata.ApplicationNameHyphenated}.db");
        _ = builder.UseSqlite($"Data Source={dbPath}");
    }

    internal static ServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDbContext<AppDbContext>(options => ConfigureDbContext(options));
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider;
    }

    internal static void DisplayFileCount(AppDbContext dbContext)
    {
        int count = dbContext.SyncedItems.Count(x => !x.IsFolder);
        Console.WriteLine($"Count of non-folder synced items: {count}");
    }

    internal static void DisplayFileClassificationRules(AppDbContext dbContext)
    {
        var fileClassificationRules = dbContext.FileClassificationRules.ToList();
        foreach (var rule in fileClassificationRules)
        {
            Console.WriteLine($"Rule ID: {rule.Id}, Is Special: {rule.IsSpecial}, Keywords: {rule.Keywords}, Level1: {rule.Level1}, Level2: {rule.Level2}, Level3: {rule.Level3}");
        }
    }

    internal static void ReadAndDisplayMappings()
    {
        string[] fileData = File.ReadAllLines("/home/jbarden/Documents/Mappings.csv");
        foreach (string line in fileData)
        {
            string[] parts = line.Split(',');
            if (parts.Length > 0)
            {
                string fileNameContains = parts[0].Trim();
                string databaseMapping = parts[1].Trim();
                string celebrity = parts[2].Trim();
                string searchable = parts[3].Trim();
                string level1 = parts[4].Trim();
                string level2 = parts[5].Trim();
                string level3 = parts[6].Trim();
                Console.WriteLine($"File Name Contains: {fileNameContains}, Database Mapping: {databaseMapping}, Celebrity: {celebrity}, Searchable: {searchable}, Level 1: {level1}, Level 2: {level2}, Level 3: {level3}");
            }
        }
    }
}
