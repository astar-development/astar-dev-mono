using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure;
using AStar.Dev.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = BuildServiceProvider();
using var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

dbContext.SyncedItems.Where(x => !x.IsFolder).Take(10).ToList()
    .ForEach(item => Console.WriteLine($"Synced Item: {item.Id}, {item.RemotePath}"));

DisplayFileCount(dbContext);

static void ConfigureDbContext(DbContextOptionsBuilder builder)
{
    string dbPath = ApplicationMetadata.ApplicationNameHyphenated.ApplicationDirectory().CombinePath($"{ApplicationMetadata.ApplicationNameHyphenated}.db");
    _ = builder.UseSqlite($"Data Source={dbPath}");
}

static ServiceProvider BuildServiceProvider()
{
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddDbContext<AppDbContext>(options => ConfigureDbContext(options));
    var serviceProvider = serviceCollection.BuildServiceProvider();
    return serviceProvider;
}

static void DisplayFileCount(AppDbContext dbContext)
{
    int count = dbContext.SyncedItems.Count(x => !x.IsFolder);
    Console.WriteLine($"Count of non-folder synced items: {count}");
}
