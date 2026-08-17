using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

internal sealed class SearchCategoriesConfiguration : IEntityTypeConfiguration<SearchCategories>
{
    public void Configure(EntityTypeBuilder<SearchCategories> builder)
    {
        builder.ToTable("SearchCategories");

        builder.HasKey(searchCategories => new { searchCategories.SearchConfigurationId, searchCategories.Id });

        builder.Property(searchCategories => searchCategories.SearchConfigurationId)
               .IsRequired();

        builder.Property(searchCategories => searchCategories.Id)
               .HasColumnType("nvarchar(128)")
               .IsRequired();

        builder.Property(searchCategories => searchCategories.Name)
               .HasColumnType("nvarchar(256)")
               .IsRequired();

        builder.Property(searchCategories => searchCategories.LastKnownImageCount)
               .HasColumnType("int")
               .IsRequired();

        builder.Property(searchCategories => searchCategories.LastPageVisited)
               .HasColumnType("int")
               .IsRequired();

        builder.Property(searchCategories => searchCategories.TotalPages)
               .HasColumnType("int")
               .IsRequired();

        builder.HasOne(searchCategories => searchCategories.SearchConfiguration)
               .WithMany(searchConfiguration => searchConfiguration.SearchCategories)
               .HasForeignKey(searchCategories => searchCategories.SearchConfigurationId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);
    }
}
