using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="SearchCategoryEntity"/>.</summary>
public sealed class SearchCategoryEntityConfiguration : IEntityTypeConfiguration<SearchCategoryEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SearchCategoryEntity> builder)
    {
        _ = builder.ToTable("SearchCategories");
        _ = builder.HasKey(category => new { category.SearchConfigurationId, category.Id });
        _ = builder.Property(category => category.SearchConfigurationId).IsRequired();
        _ = builder.Property(category => category.Id).HasMaxLength(128).IsRequired();
        _ = builder.Property(category => category.Name).HasMaxLength(256).IsRequired();
    }
}
