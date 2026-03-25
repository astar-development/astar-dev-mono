using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AStar.Dev.Spikes.SqliteSyncState.Configurations;

public class AccountConfigurationEntityConfiguration : IEntityTypeConfiguration<AccountConfiguration>
{
    public void Configure(EntityTypeBuilder<AccountConfiguration> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.LocalSyncPath).IsUnique(); // AM-06: unique sync paths
        builder.Property(a => a.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.LocalSyncPath).IsRequired();
        builder.Property(a => a.NextSyncAt).HasConversion(DateTimeOffsetConverters.ToUnixMs); // DB-01
    }
}
