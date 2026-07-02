using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

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
