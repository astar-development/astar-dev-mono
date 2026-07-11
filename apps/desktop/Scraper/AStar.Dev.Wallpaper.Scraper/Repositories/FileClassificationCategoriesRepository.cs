using System.Globalization;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Models;
using Microsoft.EntityFrameworkCore;
using ScrapedTagDto = AStar.Dev.Wallpaper.Scraper.DTOs.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scraper.Repositories;

public sealed class FileClassificationCategoriesRepository(IDbContextFactory<AppDbContext> contextFactory) : IFileClassificationCategoriesRepository
{
    public async Task<Result<Unit, ScrapeError>> SaveAsync(IReadOnlyList<TagData> tags, CancellationToken cancellationToken)
        => (await Try.RunAsync(async () =>
            {
                await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
                var textInfo = new CultureInfo("en-GB", false).TextInfo;
                var titleCasedTags = tags.Select(t => new ScrapedTagDto
                {
                    Category = textInfo.ToTitleCase(t.Category ?? ""),
                    Value = textInfo.ToTitleCase(t.Tag)
                }).ToList();
                var parentCategories = await context.FileClassificationCategories.ToListAsync(cancellationToken);

                foreach (var tag in titleCasedTags)
                {
                    var parentCategory = parentCategories.FirstOrDefault(c => c.Name == tag.Category);
                    if (parentCategories.Any(t => t.Name == tag.Value && parentCategory != null && t.ParentId == parentCategory.Id)) continue;

                    tag.Level = parentCategory?.Level + 1 ?? 1;
                    tag.Category = parentCategory?.Name ?? tag.Value;
                    _ = await context.FileClassificationCategories.AddAsync(tag.ToDomain(), cancellationToken);
                }

                _ = await context.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }).ConfigureAwait(false))
            .ToResult(exception => (ScrapeError)ScrapeErrorFactory.CreateRepositoryOperationFailed(nameof(SaveAsync), exception.Message));

    public async Task<Result<List<FileClassificationCategoryEntity>, ScrapeError>> GetAllAsync(CancellationToken cancellationToken)
        => (await Try.RunAsync(async () =>
            {
                await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

                return await context.FileClassificationCategories.ToListAsync(cancellationToken);
            }).ConfigureAwait(false))
            .ToResult(exception => (ScrapeError)ScrapeErrorFactory.CreateRepositoryOperationFailed(nameof(GetAllAsync), exception.Message));

    public async Task<Result<Unit, ScrapeError>> UpsertAsync(IReadOnlyList<FileClassificationCategoryEntity> tags, CancellationToken cancellationToken)
        => (await Try.RunAsync(async () =>
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

                return Unit.Value;
            }).ConfigureAwait(false))
            .ToResult(exception => (ScrapeError)ScrapeErrorFactory.CreateRepositoryOperationFailed(nameof(UpsertAsync), exception.Message));
}
