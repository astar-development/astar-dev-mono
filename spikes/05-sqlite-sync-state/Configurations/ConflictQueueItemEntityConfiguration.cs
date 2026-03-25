using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Spikes.SqliteSyncState.Configurations;

public class ConflictQueueItemEntityConfiguration : IEntityTypeConfiguration<ConflictQueueItem>
{
    public void Configure(EntityTypeBuilder<ConflictQueueItem> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.RemotePath).IsRequired().HasMaxLength(1000);
        builder.Property(c => c.LocalPath).IsRequired().HasMaxLength(1000);
        builder.HasIndex(c => new { c.AccountId, c.Resolution }); // query: pending conflicts per account
        builder.Property(c => c.DetectedAt).HasConversion(DateTimeOffsetConverters.ToUnixMs);         // DB-01
        builder.Property(c => c.ResolvedAt).HasConversion(DateTimeOffsetConverters.ToUnixMsNullable); // DB-01
    }
}
