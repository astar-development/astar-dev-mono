using AStarDev.ControlDb.FileDetail;
using AStarDev.EFCoreSqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.Files;

/// <summary>EF Core configuration for <see cref="DeletionStatusEntity"/>.</summary>
public sealed class DeletionStatusEntityConfiguration : IEntityTypeConfiguration<DeletionStatusEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DeletionStatusEntity> builder)
    {
        _ = builder.ToTable("DeletionStatus");
        _ = builder.HasKey(status => status.Id);
        _ = builder.Property(status => status.Id).ValueGeneratedOnAdd();
        _ = builder.Property(status => status.Id).HasConversion(id => id.Value, guid => new DeletionStatusId(guid));
        _ = builder.Property(status => status.Id).HasValueGenerator((_, _) => new StrongGuidIdValueGenerator<DeletionStatusId>(value => new DeletionStatusId(value)));
        _ = builder.Property(status => status.FileId).HasConversion(fileId => fileId.Value, guid => new FileId(guid));

        _ = builder.HasOne(status => status.FileDetail)
            .WithOne(file => file.DeletionStatus)
            .HasForeignKey<DeletionStatusEntity>(status => status.FileId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
