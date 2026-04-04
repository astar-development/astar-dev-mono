using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Conflict.Resolution.Domain;
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

    /// <summary>Conflict queue — all detected sync conflicts (CR-05, NF-05).</summary>
    DbSet<ConflictRecord> ConflictRecords { get; }

    /// <summary>Saves pending changes to database.</summary>
    int SaveChanges();

    /// <summary>Saves pending changes to database asynchronously.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
