using AStarDev.ControlDb.ScrapeConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.ScrapeConfiguration;

/// <summary>
/// Provides the configuration for the <see cref="UserConfigurationEntity"/> entity.
/// </summary>
public sealed class UserConfigurationEntityConfiguration : IEntityTypeConfiguration<UserConfigurationEntity>
{
    ///<inheritdoc/>
    public void Configure(EntityTypeBuilder<UserConfigurationEntity> builder)
    {
        builder.ToTable("UserConfigurations");

        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.Id).ValueGeneratedOnAdd();
        builder.Property(d => d.Id).HasConversion(id => id.Value, value => new UserConfigurationId(value));
        builder.HasOne<ScrapeConfigurationEntity>()
            .WithOne(scrapeConfiguration => scrapeConfiguration.UserConfiguration)
            .HasForeignKey<UserConfigurationEntity>(userConfiguration => userConfiguration.ScrapeConfigurationEntityId)
            .HasPrincipalKey<ScrapeConfigurationEntity>(scrapeConfiguration => scrapeConfiguration.Id);
    }
}
