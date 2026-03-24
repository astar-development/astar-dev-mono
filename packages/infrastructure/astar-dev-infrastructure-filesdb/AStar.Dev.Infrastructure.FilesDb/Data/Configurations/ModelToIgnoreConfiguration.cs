using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Data.Configurations;

/// <summary>
/// </summary>
public class ModelToIgnoreConfiguration : IEntityTypeConfiguration<ModelToIgnore>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ModelToIgnore> builder)
    {
        _ = builder
            .ToTable(nameof(ModelToIgnore), Constants.SchemaName)
            .HasKey(fileDetail => fileDetail.Id);

        _ = builder.Property(fileDetail => fileDetail.Value).HasMaxLength(300);
    }
}