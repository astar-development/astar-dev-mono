using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Spikes.SqliteSyncState.Configurations;

public class SyncDeltaTokenEntityConfiguration : IEntityTypeConfiguration<SyncDeltaToken>
{
    public void Configure(EntityTypeBuilder<SyncDeltaToken> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => new { t.AccountId, t.FolderPath }).IsUnique(); // one token per account/folder
        builder.Property(t => t.Token).IsRequired();
        builder.Property(t => t.FolderPath).IsRequired().HasMaxLength(1000);
        builder.Property(t => t.UpdatedAt).HasConversion(DateTimeOffsetConverters.ToUnixMs); // DB-01
    }
}
