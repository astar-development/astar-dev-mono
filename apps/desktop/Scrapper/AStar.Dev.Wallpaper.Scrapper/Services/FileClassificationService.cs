using System.Globalization;
using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public sealed class FileClassificationService(IDbContextFactory<FilesContext> contextFactory, TimeProvider timeProvider)
{
    private static readonly TextInfo TitleCaseInfo = new CultureInfo("en-GB", false).TextInfo;

    public async Task<PageClassificationData> LoadPageClassificationDataAsync(string categoryId, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        var searchable = await context.FileClassifications
            .Include(fc => fc.FileNameParts)
            .Where(fc => fc.IncludeInSearch)
            .ToListAsync(token)
            .ConfigureAwait(false);

        var categoryClassification = await ResolveCategoryClassificationAsync(context, categoryId, token).ConfigureAwait(false);

        var includedTags = await context.ScrapedTags
            .Where(t => t.IncludeInSearch)
            .ToListAsync(token)
            .ConfigureAwait(false);

        return PageClassificationDataFactory.Create(searchable, categoryClassification, includedTags);
    }

    public async Task ClassifyAsync(FileDetail fileDetail, PageClassificationData pageData, IReadOnlyList<string> imageTags, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        var matched = new List<FileClassification>();

        CollectFileNameMatches(pageData.SearchableClassifications, fileDetail, matched);
        if (pageData.CategoryClassification is not null)
            matched.Add(pageData.CategoryClassification);
        await CollectTagMatchesAsync(context, pageData.IncludedTags, imageTags, matched, token).ConfigureAwait(false);

        var distinct = matched.DistinctBy(c => c.Name).ToList();

        await context.SaveChangesAsync(token).ConfigureAwait(false);

        foreach (var classification in distinct)
            context.DownloadedFileClassifications.Add(new DownloadedFileClassification
            {
                FileDetailId         = fileDetail.Id,
                FileClassificationId = classification.Id
            });

        await context.SaveChangesAsync(token).ConfigureAwait(false);
    }

    internal async Task<List<FileClassification>> ExportClassificationsAsync(CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        return await context.FileClassifications
            .Include(fc => fc.FileNameParts)
            .ToListAsync(token)
            .ConfigureAwait(false);
    }

    internal async Task<object> ImportClassificationsAsync(List<FileClassification> classifications, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        foreach (var classification in classifications)
        {
            var existing = await context.FileClassifications
                .Include(fc => fc.FileNameParts)
                .FirstOrDefaultAsync(fc => fc.Name == classification.Name, token)
                .ConfigureAwait(false);

            if (existing is null)
            {
                classification.CreatedAt = timeProvider.GetUtcNow();
                classification.UpdatedAt = timeProvider.GetUtcNow();
                context.FileClassifications.Add(classification);
            }
            else
            {
                existing.IncludeInSearch = classification.IncludeInSearch;
                existing.UpdatedAt       = timeProvider.GetUtcNow();

                var existingParts = existing.FileNameParts.ToList();
                foreach (var part in classification.FileNameParts)
                {
                    if (!existingParts.Any(ep => ep.Text.Equals(part.Text, StringComparison.OrdinalIgnoreCase)))
                    {
                        existing.FileNameParts.Add(new FileNamePart { Text = part.Text });
                        existing.UpdatedAt = timeProvider.GetUtcNow();
                    }
                }
            }
        }

        await context.SaveChangesAsync(token).ConfigureAwait(false);

        return new { Success = true, Count = classifications.Count };
    }

    private async Task<FileClassification?> ResolveCategoryClassificationAsync(FilesContext context, string categoryId, CancellationToken token)
    {
        if (string.IsNullOrEmpty(categoryId)) return null;

        var searchConfig = await context.SearchConfigurations
            .Include(sc => sc.SearchCategories)
            .OrderByDescending(sc => sc.Id)
            .FirstOrDefaultAsync(token)
            .ConfigureAwait(false);

        if (searchConfig is null) return null;

        var category = searchConfig.SearchCategories.FirstOrDefault(c => c.Id == categoryId && c.IncludeInSearch);
        if (category is null) return null;

        var classification = await FindOrCreateClassificationAsync(context, category.Name, token).ConfigureAwait(false);
        await context.SaveChangesAsync(token).ConfigureAwait(false);

        return classification;
    }

    private static void CollectFileNameMatches(IReadOnlyList<FileClassification> searchable, FileDetail fileDetail, List<FileClassification> matched)
        => matched.AddRange(searchable.Where(fc =>
            fc.FileNameParts.Any(fnp => fileDetail.FullNameWithPath.Contains(fnp.Text, StringComparison.OrdinalIgnoreCase))));

    private async Task CollectTagMatchesAsync(FilesContext context, IReadOnlyList<ScrapedTag> includedTags, IReadOnlyList<string> imageTags, List<FileClassification> matched, CancellationToken token)
    {
        if (imageTags.Count == 0) return;

        var tagSet = new HashSet<string>(imageTags, StringComparer.OrdinalIgnoreCase);

        foreach (var tag in includedTags.Where(t => tagSet.Contains(t.Value)))
            matched.Add(await FindOrCreateClassificationAsync(context, tag.Value, token).ConfigureAwait(false));
    }

    private async Task<FileClassification> FindOrCreateClassificationAsync(FilesContext context, string name, CancellationToken token)
    {
        var normalizedName = TitleCaseInfo.ToTitleCase(name);
        var tracked = context.ChangeTracker.Entries<FileClassification>()
            .FirstOrDefault(e => e.Entity.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase))?.Entity;
        if (tracked is not null) return tracked;

        var existing = await context.FileClassifications
            .FirstOrDefaultAsync(fc => fc.Name == normalizedName, token)
            .ConfigureAwait(false);
        if (existing is not null) return existing;

        var now     = timeProvider.GetUtcNow();
        var created = new FileClassification { Name = normalizedName, CreatedAt = now, UpdatedAt = now };
        context.FileClassifications.Add(created);

        return created;
    }
}
