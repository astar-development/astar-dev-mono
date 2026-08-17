using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

internal sealed class SearchConfigurationConfiguration : IEntityTypeConfiguration<SearchConfiguration>
{
    public void Configure(EntityTypeBuilder<SearchConfiguration> builder)
    {
        builder.ToTable("SearchConfiguration");

        builder.HasKey(searchConfiguration => searchConfiguration.Id);

        builder.HasIndex(searchConfiguration => searchConfiguration.ScrapeConfigurationEntityId)
               .IsUnique();

        builder.Property(searchConfiguration => searchConfiguration.ScrapeConfigurationEntityId)
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.BaseUrl)
               .HasColumnType("nvarchar(256)")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.ApiKey)
               .HasColumnType("nvarchar(256)")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.SearchString)
               .HasColumnType("nvarchar(256)")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.TopWallpapers)
               .HasColumnType("nvarchar(256)")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.SearchStringPrefix)
               .HasColumnType("nvarchar(256)")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.SearchStringSuffix)
               .HasColumnType("nvarchar(256)")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.Subscriptions)
               .HasColumnType("nvarchar(256)")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.ImagePauseInSeconds)
               .HasColumnType("int")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.StartingPageNumber)
               .HasColumnType("int")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.TotalPages)
               .HasColumnType("int")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.SubscriptionsStartingPageNumber)
               .HasColumnType("int")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.SubscriptionsTotalPages)
               .HasColumnType("int")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.TopWallpapersStartingPageNumber)
               .HasColumnType("int")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.TopWallpapersTotalPages)
               .HasColumnType("int")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.LoginUrl)
               .HasColumnType("nvarchar(256)")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.UseHeadless)
               .HasColumnType("bit")
               .IsRequired();

        builder.Property(searchConfiguration => searchConfiguration.SlowMotionDelay)
               .HasColumnType("real");

        builder.HasMany(searchConfiguration => searchConfiguration.SearchCategories)
               .WithOne(searchCategory => searchCategory.SearchConfiguration)
               .HasForeignKey(searchCategory => searchCategory.SearchConfigurationId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);
    }
}
