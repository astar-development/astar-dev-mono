using System.Text.Json;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.ScrapeCategoryImportExportTesting;
using AStar.Dev.Utilities;
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite("Data Source=/home/jbarden/.config/astar-dev-onedrive-sync/astar-dev-onedrive-sync.db")
    .Options;
var context = new AppDbContext(options);
await context.Database.MigrateAsync();

ExportPlaying(context);

await ImportPlaying(context);

return;

static void ExportPlaying(AppDbContext context)
{
#pragma warning disable CS8602 // Dereference of a possibly null reference.
    var categories = context.FileClassificationCategories
    .Include(c => c.Parent)
    .OrderBy(c => c.ParentId).ThenBy(c => c.Level).ThenBy(c => c.Name)
    .Select(c => new CategoryNodeRecord
    (
        c.Name,
        c.Level,
        c.IsFamous,
        c.IsInternet,
        c.Parent.Name ?? null, c.CreatedAt, c.UpdatedAt
    )).ToHashSet();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    if (categories.Count <= 1) return;

    string categoriesJson = categories.ToJson();
    File.WriteAllText("/home/jbarden/Documents/Scraper/FileClassifications.json", categoriesJson);
}

static async Task ImportPlaying(AppDbContext context)
{
    string categoriesFromFile = File.ReadAllText("/home/jbarden/Documents/Scraper/FileClassifications.json");
    var categoriesFromJson = categoriesFromFile.FromJson<IList<CategoryNodeRecord>>(new(JsonSerializerDefaults.Web)).ToHashSet();
    categoriesFromJson.ForEach(Console.WriteLine);
    var existingCategories = await context.FileClassificationCategories.ToListAsync();

    foreach (var category in categoriesFromJson.Where(c => c.ParentName is null))
    {
        var existing = existingCategories.FirstOrDefault(c => c.Name == category.Name && c.Level == category.Level);
        if (existing is null)
        {
            var newCategory = new FileClassificationCategoryEntity
            {
                Name = category.Name,
                Level = category.Level,
                IsFamous = category.IsFamous,
                IsInternet = category.IsInternet,
                CreatedAt = category.CreatedAt,
                IncludeInSearch = true,
            };
            context.FileClassificationCategories.Add(newCategory);
        }
        else
        {
            existing.IsFamous = category.IsFamous;
            existing.IsInternet = category.IsInternet;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.ParentId = (existingCategories.FirstOrDefault(c => c.Name == category.ParentName))?.Id;
        }
    }
    try
    {
        await context.SaveChangesAsync();
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }

    foreach (var category in categoriesFromJson.Where(c => c.ParentName is not null))
    {
        var existing = existingCategories.FirstOrDefault(c => c.Name == category.Name && c.Level == category.Level);
        if (existing is null)
        {
            var newCategory = new FileClassificationCategoryEntity
            {
                Name = category.Name,
                Level = category.Level,
                IsFamous = category.IsFamous,
                IsInternet = category.IsInternet,
                ParentId = existing?.Id,
                IncludeInSearch = true,
            };
            context.FileClassificationCategories.Add(newCategory);
        }
        else
        {
            existing.IsFamous = category.IsFamous;
            existing.IsInternet = category.IsInternet;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
    try
    {
        await context.SaveChangesAsync();
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
}
