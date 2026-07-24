using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Persists downloaded wallpapers and their tag classifications.
/// </summary>
public sealed class WallpaperFileClassificationRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IWallpaperFileClassificationRepository
{
    /// <inheritdoc />
    public async Task<bool> IsAlreadyDownloadedAsync(string fileNameContains, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Files.AnyAsync(file => file.FileName.Value.Contains(fileNameContains), cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordAsync(IReadOnlyList<TagData> tags, string imageUrl, string directoryPath, long sizeBytes, ImageDimensions dimensions, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var tagNames = tags.Select(tag => tag.Tag).ToList();

        foreach (string? tagName in tagNames)
        {
            int categoryId = await FindCategoryIdAsync(context, tagName, cancellationToken);

            context.FileClassifications.Add(CreateClassification(categoryId, imageUrl, directoryPath, sizeBytes, dimensions));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static Task<int> FindCategoryIdAsync(AppDbContext context, string tagName, CancellationToken cancellationToken) =>
        context.FileClassificationCategories.Where(category => category.Name == tagName).Select(category => category.Id).FirstAsync(cancellationToken);

    private static FileClassificationEntity CreateClassification(int categoryId, string imageUrl, string directoryPath, long sizeBytes, ImageDimensions dimensions) => new()
    {
        FileDetailId = new FileId(Guid.CreateVersion7()),
        CategoryId = categoryId,
        FileDetail = CreateFileDetail(imageUrl, directoryPath, sizeBytes, dimensions),
    };

    private static FileDetailEntity CreateFileDetail(string imageUrl, string directoryPath, long sizeBytes, ImageDimensions dimensions) => new()
    {
        FileName = new FileName(Path.GetFileName(imageUrl)),
        DirectoryName = new DirectoryName(directoryPath),
        FileHandle = new FileHandle(Guid.NewGuid().ToString()),
        FileSize = sizeBytes,
        IsImage = true,
        ImageDetail = new ImageDetailEntity
        {
            Width = dimensions.Width,
            Height = dimensions.Height,
        },
    };
}
