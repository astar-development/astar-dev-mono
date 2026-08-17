using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="SearchConfigurationEntity"/>.</summary>
public sealed class SearchConfigurationEntityConfiguration : IEntityTypeConfiguration<SearchConfigurationEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SearchConfigurationEntity> builder)
    {
        _ = builder.ToTable("SearchConfiguration");
        _ = builder.HasKey(searchConfiguration => searchConfiguration.Id);
        _ = builder.HasIndex(searchConfiguration => searchConfiguration.ScrapeConfigurationEntityId).IsUnique();
        _ = builder.Property(searchConfiguration => searchConfiguration.ScrapeConfigurationEntityId).IsRequired();

        _ = builder.Property(searchConfiguration => searchConfiguration.BaseUrl)
                   .HasConversion(uri => uri.ToString(), value => new Uri(value))
                   .HasMaxLength(256)
                   .IsRequired();

        _ = builder.Property(searchConfiguration => searchConfiguration.LoginUrl)
                   .HasConversion(uri => uri.ToString(), value => new Uri(value))
                   .HasMaxLength(256)
                   .IsRequired();

        _ = builder.Property(searchConfiguration => searchConfiguration.ApiKey).HasMaxLength(256).IsRequired();
        _ = builder.Property(searchConfiguration => searchConfiguration.SearchString).HasMaxLength(256).IsRequired();
        _ = builder.Property(searchConfiguration => searchConfiguration.TopWallpapers).HasMaxLength(256).IsRequired();
        _ = builder.Property(searchConfiguration => searchConfiguration.SearchStringPrefix).HasMaxLength(256).IsRequired();
        _ = builder.Property(searchConfiguration => searchConfiguration.SearchStringSuffix).HasMaxLength(256).IsRequired();
        _ = builder.Property(searchConfiguration => searchConfiguration.Subscriptions).HasMaxLength(256).IsRequired();

        _ = builder.HasMany(searchConfiguration => searchConfiguration.SearchCategories)
                   .WithOne(category => category.SearchConfiguration)
                   .HasForeignKey(category => category.SearchConfigurationId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);
    }
}
