using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

/// <summary>EF Core configuration for <see cref="FileNamePartEntity"/>.</summary>
public sealed class FileNamePartEntityConfiguration : IEntityTypeConfiguration<FileNamePartEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileNamePartEntity> builder)
    {
        _ = builder.ToTable("FileNamePart");
        _ = builder.HasKey(part => part.Id);
        _ = builder.Property(part => part.Text).HasMaxLength(150);
    }
}
