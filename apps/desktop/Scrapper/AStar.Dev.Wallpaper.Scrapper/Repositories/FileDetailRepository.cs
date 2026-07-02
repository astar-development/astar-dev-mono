using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Repositories;

public sealed class FileDetailRepository(IDbContextFactory<FilesContext> contextFactory) : IFileDetailRepository
{
    public async Task<bool> ExistsAsync(string fileName)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Files.FirstOrDefaultAsync(f => f.FileName.Value.Contains(fileName)) != null;
    }

    public async Task AddAsync(FileDetail fileDetail)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var handle = FileHandle.Create(fileDetail.FileName.Value ?? fileDetail.FileHandle.Value);
        int existingCount = await context.Files.AsAsyncEnumerable().CountAsync(f => f.FileHandle.Value == handle.Value);
        if (existingCount > 0)
            handle = FileHandle.Create($"{handle}-{++existingCount}");
        fileDetail.FileHandle = handle;

        _ = await context.Files.AddAsync(fileDetail);
        _ = await context.SaveChangesAsync();
    }
}
