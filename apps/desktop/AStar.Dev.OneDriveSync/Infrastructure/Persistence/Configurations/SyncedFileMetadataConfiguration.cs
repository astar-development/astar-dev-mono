using AStar.Dev.OneDriveSync.Features.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence.Configurations;

/// <summary>
///     Configures the <see cref="SyncedFileMetadata" /> entity mapping (AM-12–AM-15, DB-06).
///
///     Cascade delete on <c>AccountId</c> FK ensures all metadata rows are removed when
///     the owning <see cref="Account" /> is deleted (AM-15, DB-06).
/// </summary>
internal sealed class SyncedFileMetadataConfiguration : IEntityTypeConfiguration<SyncedFileMetadata>
{
    public void Configure(EntityTypeBuilder<SyncedFileMetadata> builder)
    {
        _ = builder.HasKey(m => m.Id);

        _ = builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        _ = builder.Property(m => m.RemoteItemId)
            .IsRequired()
            .HasMaxLength(1024);

        _ = builder.Property(m => m.RelativePath)
            .IsRequired()
            .HasMaxLength(4096);

        _ = builder.Property(m => m.FileName)
            .IsRequired()
            .HasMaxLength(512);

        _ = builder.Property(m => m.Sha256Checksum)
            .IsRequired()
            .HasMaxLength(64);

        _ = builder.HasOne(m => m.Account)
            .WithMany()
            .HasForeignKey(m => m.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
