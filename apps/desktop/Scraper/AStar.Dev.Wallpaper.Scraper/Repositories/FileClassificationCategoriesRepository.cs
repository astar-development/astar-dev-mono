using System.Globalization;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using ScrapedTagDto = AStar.Dev.Wallpaper.Scraper.DTOs.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scraper.Repositories;

public sealed class FileClassificationCategoriesRepository(IDbContextFactory<AppDbContext> contextFactory) : IFileClassificationCategoriesRepository
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
        var parentCategories = await context.FileClassificationCategories.ToListAsync();

        foreach (var tag in titleCasedTags)
        {
            var parentCategory = parentCategories.FirstOrDefault(c => c.Name == tag.Category);
            if (parentCategories.Any(t => t.Name == tag.Value && parentCategory != null && t.ParentId == parentCategory.Id)) continue;

            tag.Level = parentCategory?.Level + 1 ?? 1;
            tag.Category = parentCategory?.Name ?? tag.Value;
            _ = await context.FileClassificationCategories.AddAsync(tag.ToDomain());
        }

        _ = await context.SaveChangesAsync();
    }

    public async Task<List<FileClassificationCategoryEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.FileClassificationCategories.ToListAsync(cancellationToken);
    }

    public async Task UpsertAsync(IReadOnlyList<FileClassificationCategoryEntity> tags, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var parentCategories = await context.FileClassificationCategories.ToListAsync(cancellationToken);

        foreach (var tag in tags)
        {
            var parentCategory = parentCategories.FirstOrDefault(c => c.Id == tag.Id);
            var existing = parentCategories.FirstOrDefault(t => t.Name == tag.Name && t.ParentId == parentCategory?.Id);
            if (existing is not null)
            {
                existing.IncludeInSearch = tag.IncludeInSearch;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
                context.FileClassificationCategories.Add(tag);
        }

        _ = await context.SaveChangesAsync(cancellationToken);
    }
}
