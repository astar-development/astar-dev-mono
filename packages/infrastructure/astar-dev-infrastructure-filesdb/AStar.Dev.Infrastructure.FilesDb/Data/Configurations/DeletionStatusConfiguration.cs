using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Data.Configurations;

internal sealed class DeletionStatusConfiguration : IEntityTypeConfiguration<DeletionStatus>
{
    public void Configure(EntityTypeBuilder<DeletionStatus> builder)
    {
        _ = builder
            .ToTable(nameof(DeletionStatus), Constants.SchemaName)
            .HasKey(deletionStatus => deletionStatus.Id);

        _ = builder
            .Property(deletionStatus => deletionStatus.HardDeletePending)
            .HasColumnName("HardDeletePending")
            .HasColumnType("datetimeoffset");

        _ = builder
            .Property(deletionStatus => deletionStatus.SoftDeletePending)
            .HasColumnName("SoftDeletePending")
            .HasColumnType("datetimeoffset");

        _ = builder
            .Property(deletionStatus => deletionStatus.SoftDeleted)
            .HasColumnName("SoftDeleted")
            .HasColumnType("datetimeoffset");
    }
}