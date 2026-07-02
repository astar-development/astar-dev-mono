using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

/// <summary>EF Core configuration for <see cref="ModelToIgnoreEntity"/>.</summary>
public sealed class ModelToIgnoreEntityConfiguration : IEntityTypeConfiguration<ModelToIgnoreEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ModelToIgnoreEntity> builder)
    {
        _ = builder.ToTable("ModelToIgnore");
        _ = builder.HasKey(model => model.Id);
        _ = builder.Property(model => model.Id).HasConversion(modelId => modelId.Id, guid => new ModelId(guid));
        _ = builder.Property(model => model.Value).HasMaxLength(300);
    }
}
