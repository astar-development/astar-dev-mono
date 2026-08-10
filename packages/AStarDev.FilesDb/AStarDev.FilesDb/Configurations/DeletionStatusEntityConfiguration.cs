using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.FilesDb.Configurations;

/// <summary>
/// Represents the configuration for the DeletionStatusEntity in the database context.
/// </summary>
public class DeletionStatusEntityConfiguration : IEntityTypeConfiguration<DeletionStatusEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DeletionStatusEntity> builder)
    {
        builder.Property(d => d.FileEntityId).HasConversion(id => id.Value, value => new FileId(value));

        builder.HasOne(d => d.FileEntity)
            .WithOne()
            .HasForeignKey<DeletionStatusEntity>("FileEntityId")
            .HasPrincipalKey<FileEntity>("Id");
    }
}
