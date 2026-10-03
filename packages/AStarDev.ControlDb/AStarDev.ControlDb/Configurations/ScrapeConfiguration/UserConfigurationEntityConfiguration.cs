using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.EFCoreSqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.ScrapeConfiguration;

/// <summary>Provides the configuration for the <see cref="UserConfigurationEntity"/> entity.</summary>
public sealed class UserConfigurationEntityConfiguration : IEntityTypeConfiguration<UserConfigurationEntity>
{
    ///<inheritdoc/>
    public void Configure(EntityTypeBuilder<UserConfigurationEntity> builder)
    {
        _ = builder.ToTable("UserConfigurations");

        _ = builder.HasKey(sc => sc.Id);

        _ = builder.Property(sc => sc.Id).ValueGeneratedOnAdd();
        _ = builder.Property(d => d.Id).HasConversion(id => id.Value, value => new UserConfigurationId(value));
        _ = builder.Property(sc => sc.Id).HasValueGenerator((_, _) => new StrongGuidIdValueGenerator<UserConfigurationId>(value => new UserConfigurationId(value)));
        _ = builder.HasOne<ScrapeConfigurationEntity>()
            .WithOne(scrapeConfiguration => scrapeConfiguration.UserConfiguration)
            .HasForeignKey<UserConfigurationEntity>(userConfiguration => userConfiguration.ScrapeConfigurationEntityId)
            .HasPrincipalKey<ScrapeConfigurationEntity>(scrapeConfiguration => scrapeConfiguration.Id);
    }
}
