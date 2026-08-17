using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

internal sealed class UserConfigurationConfiguration : IEntityTypeConfiguration<UserConfiguration>
{
    public void Configure(EntityTypeBuilder<UserConfiguration> builder)
    {
        builder.ToTable("UserConfiguration");

        builder.HasKey(userConfiguration => userConfiguration.Id);

        builder.HasIndex(userConfiguration => userConfiguration.ScrapeConfigurationEntityId)
               .IsUnique();

        builder.Property(userConfiguration => userConfiguration.ScrapeConfigurationEntityId)
               .IsRequired();

        builder.Property(userConfiguration => userConfiguration.Username)
               .HasColumnType("nvarchar(256)")
               .IsRequired();

        builder.Property(userConfiguration => userConfiguration.LoginEmailAddress)
               .HasColumnType("nvarchar(256)")
               .IsRequired();

        builder.Property(userConfiguration => userConfiguration.Password)
               .HasColumnType("nvarchar(256)")
               .IsRequired();

        builder.Property(userConfiguration => userConfiguration.SessionCookie)
               .HasColumnType("nvarchar(256)")
               .IsRequired();
    }
}
