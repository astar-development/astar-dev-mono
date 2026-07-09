using System.Globalization;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using ScrapedTagDto = AStar.Dev.Wallpaper.Scraper.DTOs.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scraper.Repositories;

public sealed class ScrapedTagRepository(IDbContextFactory<AppDbContext> contextFactory) :  IScrapedTagRepository
{
    public async Task SaveAsync(IReadOnlyList<TagData> tags)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var textInfo = new CultureInfo("en-GB", false).TextInfo;
        var titleCasedTags = tags.Select(t => new ScrapedTagDto
        {
            Category = textInfo.ToTitleCase(t.Category ?? ""),
            Value = textInfo.ToTitleCase(t.Tag)
        }).ToList();

        foreach (var tag in titleCasedTags)
        {
            var parentCategory = await context.FileClassificationCategories.FirstOrDefaultAsync(c => c.Name == tag.Category);
            if (!await context.FileClassificationCategories.AnyAsync(t => t.Name == tag.Value && parentCategory != null && t.ParentId == parentCategory.Id))
                _ = await context.FileClassificationCategories.AddAsync(tag.ToDomain());
        }

        _ = await context.SaveChangesAsync();
    }

    public async Task<List<FileClassificationCategoryEntity>> GetAllAsync(CancellationToken ct)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        return await context.FileClassificationCategories.ToListAsync(ct);
    }

    public async Task UpsertAsync(IReadOnlyList<FileClassificationCategoryEntity> tags, CancellationToken ct)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var values = tags.Select(t => t.Name).ToList();
        var existingMap = await context.FileClassificationCategories
            .Where(t => values.Contains(t.Name))
            .ToListAsync(ct);

        foreach (var tag in tags)
        {
            var parentCategory = await context.FileClassificationCategories.FirstOrDefaultAsync(c => c.Id == tag.ParentId, ct);
            var existing = existingMap.FirstOrDefault(t => t.Name == tag.Name && t.ParentId == parentCategory?.Id);
            if (existing is not null)
            {
                existing.IncludeInSearch = tag.IncludeInSearch;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
                context.FileClassificationCategories.Add(tag);
        }

        _ = await context.SaveChangesAsync(ct);
    }
}
