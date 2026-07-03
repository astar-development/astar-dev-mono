using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Repositories;

public sealed class FileDetailRepository(IDbContextFactory<AppDbContext> contextFactory) : IFileDetailRepository
{
    public async Task<bool> ExistsAsync(string fileName)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Files.FirstOrDefaultAsync(f => f.FileName.Value.Contains(fileName)) != null;
    }

    public async Task AddAsync(FileDetailEntity fileDetail)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var handle = FileHandleFactory.Create(fileDetail.FileName.Value ?? fileDetail.FileHandle.Value);
        int existingCount = await context.Files.AsAsyncEnumerable().CountAsync(f => f.FileHandle.Value == handle.Value);
        if (existingCount > 0)
            handle = FileHandleFactory.Create($"{handle}-{++existingCount}");
        fileDetail.FileHandle = handle;

        _ = await context.Files.AddAsync(fileDetail);
        _ = await context.SaveChangesAsync();
    }
}
