using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Spikes.SqliteSyncState;

public class SyncDbContext(DbContextOptions<SyncDbContext> options) : DbContext(options)
{
    public DbSet<AccountConfiguration> Accounts      => Set<AccountConfiguration>();
    public DbSet<SyncDeltaToken>       DeltaTokens   => Set<SyncDeltaToken>();
    public DbSet<ConflictQueueItem>    ConflictQueue => Set<ConflictQueueItem>();
    public DbSet<SyncSession>          SyncSessions  => Set<SyncSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        // Each entity has its own IEntityTypeConfiguration<T> in Configurations/.
        // ApplyConfigurationsFromAssembly discovers and applies all of them automatically. See DB-02.
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(SyncDbContext).Assembly);
}
