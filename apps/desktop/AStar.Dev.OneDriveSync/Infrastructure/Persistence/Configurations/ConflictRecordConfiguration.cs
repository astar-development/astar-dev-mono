using AStar.Dev.Conflict.Resolution.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence.Configurations;

/// <summary>
///     Configures the <see cref="ConflictRecord"/> entity mapping (CR-05, NF-05).
///     All writes are wrapped in SQLite transactions by EF Core.
/// </summary>
internal sealed class ConflictRecordConfiguration : IEntityTypeConfiguration<ConflictRecord>
{
    public void Configure(EntityTypeBuilder<ConflictRecord> builder)
    {
        _ = builder.HasKey(record => record.Id);

        _ = builder.Property(record => record.Id)
            .ValueGeneratedNever();

        _ = builder.Property(record => record.AccountId)
            .IsRequired();

        _ = builder.Property(record => record.FilePath)
            .IsRequired()
            .HasMaxLength(4096);

        _ = builder.Property(record => record.AccountDisplayName)
            .IsRequired(false)
            .HasMaxLength(256);

        _ = builder.Property(record => record.ConflictType)
            .IsRequired()
            .HasMaxLength(32);

        _ = builder.Property(record => record.IsResolved)
            .IsRequired()
            .HasDefaultValue(false);

        _ = builder.Property(record => record.AppliedStrategy)
            .IsRequired(false)
            .HasMaxLength(32);

        _ = builder.HasIndex(record => record.FilePath);
        _ = builder.HasIndex(record => new { record.IsResolved, record.DetectedAt });
    }
}
