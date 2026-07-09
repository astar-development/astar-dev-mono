using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="DeletionStatusEntity"/>.</summary>
public sealed class DeletionStatusEntityConfiguration : IEntityTypeConfiguration<DeletionStatusEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DeletionStatusEntity> builder)
    {
        _ = builder.ToTable("DeletionStatus");
        _ = builder.HasKey(status => status.Id);
        _ = builder.Property(status => status.FileDetailId).HasConversion(fileId => fileId.Id, guid => new FileId(guid));

        _ = builder.HasOne(status => status.FileDetail)
            .WithOne(file => file.DeletionStatus)
            .HasForeignKey<DeletionStatusEntity>(status => status.FileDetailId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
