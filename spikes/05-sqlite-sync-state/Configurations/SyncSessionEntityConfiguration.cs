using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Spikes.SqliteSyncState.Configurations;

public class SyncSessionEntityConfiguration : IEntityTypeConfiguration<SyncSession>
{
    public void Configure(EntityTypeBuilder<SyncSession> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => new { s.AccountId, s.Status }); // query: in-progress session per account
        builder.Property(s => s.StartedAt).HasConversion(DateTimeOffsetConverters.ToUnixMs);         // DB-01
        builder.Property(s => s.CompletedAt).HasConversion(DateTimeOffsetConverters.ToUnixMsNullable); // DB-01
    }
}
