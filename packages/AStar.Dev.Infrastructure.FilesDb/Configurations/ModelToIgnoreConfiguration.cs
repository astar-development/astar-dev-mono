using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

/// <summary>
/// </summary>
public sealed class ModelToIgnoreConfiguration : IEntityTypeConfiguration<ModelToIgnore>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ModelToIgnore> builder)
    {
        _ = builder
           .ToTable(nameof(ModelToIgnore), Constants.SchemaName)
           .HasKey(model => model.Id);

        _ = builder.Property(model => model.Id)
                   .HasConversion(modelId => modelId.Value, modelId => new ModelId(modelId));

        builder.Property(model => model.Value).HasMaxLength(300);
    }
}
