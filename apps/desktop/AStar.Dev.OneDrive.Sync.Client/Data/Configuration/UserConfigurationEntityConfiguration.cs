using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

/// <summary>EF Core configuration for <see cref="UserConfigurationEntity"/>.</summary>
public sealed class UserConfigurationEntityConfiguration : IEntityTypeConfiguration<UserConfigurationEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserConfigurationEntity> builder)
    {
        _ = builder.ToTable("UserConfiguration");
        _ = builder.HasKey(userConfiguration => userConfiguration.Id);
        _ = builder.HasIndex(userConfiguration => userConfiguration.ScrapeConfigurationEntityId).IsUnique();
        _ = builder.Property(userConfiguration => userConfiguration.ScrapeConfigurationEntityId).IsRequired();
        _ = builder.Property(userConfiguration => userConfiguration.Username).HasMaxLength(256).IsRequired();
        _ = builder.Property(userConfiguration => userConfiguration.LoginEmailAddress).HasMaxLength(256).IsRequired();
        _ = builder.Property(userConfiguration => userConfiguration.Password).HasMaxLength(256).IsRequired();
        _ = builder.Property(userConfiguration => userConfiguration.SessionCookie).HasMaxLength(256).IsRequired();
    }
}
