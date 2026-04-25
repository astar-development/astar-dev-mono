using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnotherOneDriveSync.Data.Entities;

public class AppSettings
{
    public int Id { get; set; }
    public string LocalRootPath { get; set; } = null!;
}

public class AppSettingsConfiguration : IEntityTypeConfiguration<AppSettings>
{
    public void Configure(EntityTypeBuilder<AppSettings> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.LocalRootPath).IsRequired();
    }
}
