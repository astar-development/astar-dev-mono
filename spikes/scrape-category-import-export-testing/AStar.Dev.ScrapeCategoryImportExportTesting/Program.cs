// See https://aka.ms/new-console-template for more information
using System.Runtime.CompilerServices;
using System.Text.Json;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.ScrapeCategoryImportExportTesting;
using AStar.Dev.Utilities;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("Hello, World!");

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite("Data Source=/home/jasonbarden/.config/astar-dev-onedrive-sync/astar-dev-onedrive-sync.db")
    .Options;
var context = new AppDbContext(options);
await context.Database.MigrateAsync();

ExportPlaying(context);

static void ExportPlaying(AppDbContext context)
{
#pragma warning disable CS8602 // Dereference of a possibly null reference.
    var categories = context.FileClassificationCategories
    .Include(c => c.Parent)
    .OrderBy(c => c.ParentId).ThenBy(c => c.Level).ThenBy(c => c.Name)
    .Select(c => new CategoryNodeRecord
    (
        // c.Id,
        c.Name,
        c.Level,
        c.IsFamous,
        c.IsInternet,
        // c.ParentId,
        c.Parent.Name ?? null, c.CreatedAt, c.UpdatedAt
    )).ToHashSet();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

    // categories.ForEach(category => Console.WriteLine($"Category: {category.Name}, Level: {category.Level}, Id: {category.Id}, ParentId: {category.ParentId}"));

    if (categories.Count > 1)
    {
        string categoriesJson = categories.ToJson();
        File.WriteAllText("/home/jasonbarden/Desktop/classifications-only2.json", categoriesJson);
    }
}

await ImportPlaying(context);

static async Task ImportPlaying(AppDbContext context)
{
    string categoriesFromFile = File.ReadAllText("/home/jasonbarden/Desktop/classifications-only2.json");
    var categoriesFromJson = categoriesFromFile.FromJson<IList<CategoryNodeRecord>>(new(JsonSerializerDefaults.Web));
    categoriesFromJson.ForEach(Console.WriteLine);

    foreach (var category in categoriesFromJson.Where(c => c.ParentName is null))
    {
        var existing = await context.FileClassificationCategories.FirstOrDefaultAsync(c => c.Name == category.Name && c.Level == category.Level);
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
            existing.ParentId = (await context.FileClassificationCategories.FirstOrDefaultAsync(c => c.Name == category.ParentName))?.Id;
        }
    }

    await context.SaveChangesAsync();
    foreach (var category in categoriesFromJson.Where(c => c.ParentName is not null))
    {
        var parentCategory = await context.FileClassificationCategories.FirstOrDefaultAsync(c => c.Name == category.ParentName);
        var existing = await context.FileClassificationCategories.FirstOrDefaultAsync(c => c.Name == category.Name && c.Level == category.Level);
        if (existing is null)
        {
            var newCategory = new FileClassificationCategoryEntity
            {
                Name = category.Name,
                Level = category.Level,
                IsFamous = category.IsFamous,
                IsInternet = category.IsInternet,
                ParentId = parentCategory?.Id,
                IncludeInSearch = true,
            };
            context.FileClassificationCategories.Add(newCategory);
        }
        else
        {
            existing.IsFamous = category.IsFamous;
            existing.IsInternet = category.IsInternet;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.ParentId = parentCategory?.Id;
        }
    }

    await context.SaveChangesAsync();
}
