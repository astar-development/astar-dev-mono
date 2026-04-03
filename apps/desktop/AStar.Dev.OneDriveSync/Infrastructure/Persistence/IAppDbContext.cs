using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDriveSync.Features.Accounts;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     Abstraction for AppDbContext to enable testing and mocking.
/// </summary>
public interface IAppDbContext
{
    /// <summary>Accounts — the sole PII-bearing table.</summary>
    DbSet<Account> Accounts { get; }

    /// <summary>File metadata for accounts with AM-12 enabled.</summary>
    DbSet<SyncedFileMetadata> SyncedFileMetadata { get; }

    /// <summary>Application settings (singleton row).</summary>
    DbSet<AppSettings> AppSettings { get; }

    /// <summary>Saves pending changes to database.</summary>
    int SaveChanges();

    /// <summary>Saves pending changes to database asynchronously.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
