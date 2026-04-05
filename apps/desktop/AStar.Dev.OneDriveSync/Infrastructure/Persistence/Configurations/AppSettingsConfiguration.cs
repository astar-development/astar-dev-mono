using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence.Configurations;

internal sealed class AppSettingsConfiguration : IEntityTypeConfiguration<AppSettings>
{
    public void Configure(EntityTypeBuilder<AppSettings> builder)
    {
        _ = builder.ToTable("AppSettings");
        _ = builder.HasKey(s => s.Id);
        _ = builder.Property(s => s.Id).ValueGeneratedNever();
        _ = builder.Property(s => s.ThemeMode).IsRequired().HasMaxLength(20);
        _ = builder.Property(s => s.Locale).IsRequired().HasMaxLength(10).HasDefaultValue("en-GB");
        _ = builder.Property(s => s.UserType).IsRequired().HasMaxLength(20).HasDefaultValue("Casual");
        _ = builder.Property(s => s.NotificationsEnabled).IsRequired().HasDefaultValue(true);
    }
}
