using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Helpers;

internal sealed class TestDbContextFactory(AppDbContextFactory inner) : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext() => inner.CreateContextAsync().GetAwaiter().GetResult();

    public async Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        await inner.CreateContextAsync(cancellationToken).ConfigureAwait(false);
}
