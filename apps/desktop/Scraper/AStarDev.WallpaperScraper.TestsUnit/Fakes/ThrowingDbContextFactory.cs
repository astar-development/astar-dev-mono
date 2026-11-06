using AStarDev.ControlDb;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.WallpaperScraper.TestsUnit.Fakes;

internal sealed class ThrowingDbContextFactory(Exception failure) : IDbContextFactory<ControlDbContext>
{
    public ControlDbContext CreateDbContext() => throw failure;

    public Task<ControlDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromException<ControlDbContext>(failure);
}
