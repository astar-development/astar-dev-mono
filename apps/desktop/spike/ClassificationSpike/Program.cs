using AStar.Dev.OneDrive.Sync.Client.Data;
using ClassificationSpike;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = Spike.BuildServiceProvider();
using var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

var items = dbContext.SyncedItems.Where(x => !x.IsFolder).Take(10).ToList();

items.ForEach((item) => Console.WriteLine($"Synced Item {items.IndexOf(item)}: {item.Id}, {item.RemotePath}"));

items.ForEach(item =>
{
    string[] pathSegments = [.. item.RemotePath.Split(['/', ' '], StringSplitOptions.RemoveEmptyEntries).ToList().Distinct()];
    for (int i = 0; i < pathSegments.Length; i++)
    {
        Console.WriteLine($"Segment {i}: {pathSegments[i]}");
    }
});

Console.WriteLine(new string('-', 50));

Spike.DisplayFileCount(dbContext);
Console.WriteLine(new string('-', 50));

Spike.ReadAndDisplayMappings(dbContext);
Console.WriteLine(new string('-', 50));

Spike.DisplayFileClassificationRules(dbContext);
