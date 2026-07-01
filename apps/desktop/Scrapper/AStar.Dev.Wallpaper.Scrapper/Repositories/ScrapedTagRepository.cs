using System.Globalization;
using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using ScrapedTagDto = AStar.Dev.Wallpaper.Scrapper.DTOs.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scrapper.Repositories;

public sealed class ScrapedTagRepository(IDbContextFactory<FilesContext> contextFactory) : IScrapedTagRepository
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
            if (!await context.ScrapedTags.AnyAsync(t => t.Value == tag.Value && tag.Category == t.Category))
                _ = await context.ScrapedTags.AddAsync(tag.ToDomain());
        }

        _ = await context.SaveChangesAsync();
    }

    public async Task<List<ScrapedTag>> GetAllAsync(CancellationToken ct)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        return await context.ScrapedTags.AsNoTracking().ToListAsync(ct);
    }

    public async Task UpsertAsync(IReadOnlyList<ScrapedTag> tags, CancellationToken ct)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var values = tags.Select(t => t.Value).ToList();
        var existingMap = await context.ScrapedTags
            .Where(t => values.Contains(t.Value))
            .ToListAsync(ct);

        foreach (var tag in tags)
        {
            var existing = existingMap.FirstOrDefault(t => t.Value == tag.Value && t.Category == tag.Category);
            if (existing is not null)
            {
                existing.IncludeInSearch = tag.IncludeInSearch;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
                context.ScrapedTags.Add(tag);
        }

        _ = await context.SaveChangesAsync(ct);
    }
}
