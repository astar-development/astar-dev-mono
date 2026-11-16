using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Maps <see cref="CategoryEntity"/> to the <c>Categories</c> table: one row per name key, which matches names ignoring case.</summary>
[ExcludeFromCodeCoverage]
internal sealed class CategoryEntityConfiguration : IEntityTypeConfiguration<CategoryEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CategoryEntity> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();
        builder.Property(category => category.Name).IsRequired();
        builder.Property(category => category.NameKey).IsRequired();
        builder.HasIndex(category => category.NameKey).IsUnique();
    }
}
