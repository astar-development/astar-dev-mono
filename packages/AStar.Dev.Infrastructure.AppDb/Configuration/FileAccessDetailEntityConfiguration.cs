using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="FileAccessDetailEntity"/>.</summary>
public sealed class FileAccessDetailEntityConfiguration : IEntityTypeConfiguration<FileAccessDetailEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileAccessDetailEntity> builder)
    {
        _ = builder.ToTable("FileAccessDetail");
        _ = builder.HasKey(detail => detail.Id);
    }
}
