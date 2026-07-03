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
    }
}
