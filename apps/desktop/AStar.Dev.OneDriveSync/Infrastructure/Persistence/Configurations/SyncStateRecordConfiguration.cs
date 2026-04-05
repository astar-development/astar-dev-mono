using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence.Configurations;

/// <summary>Configures the <see cref="SyncStateRecord"/> entity mapping (EH-04, EH-05, EH-06).</summary>
internal sealed class SyncStateRecordConfiguration : IEntityTypeConfiguration<SyncStateRecord>
{
    public void Configure(EntityTypeBuilder<SyncStateRecord> builder)
    {
        _ = builder.HasKey(record => record.AccountId);

        _ = builder.Property(record => record.AccountId)
            .IsRequired()
            .HasMaxLength(36)
            .ValueGeneratedNever();

        _ = builder.Property(record => record.State)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        _ = builder.Property(record => record.CheckpointJson)
            .IsRequired(false);
    }
}
