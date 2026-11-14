using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

[ExcludeFromCodeCoverage]
internal sealed class ActivityLogEntityConfiguration : IEntityTypeConfiguration<ActivityLogEntity>
{
    public void Configure(EntityTypeBuilder<ActivityLogEntity> builder)
    {
        builder.ToTable("ActivityLog");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedOnAdd();
        builder.Property(entry => entry.Kind).IsRequired();
        builder.HasIndex(entry => new { entry.AccountId, entry.Id });
        builder.HasIndex(entry => entry.OccurredAtUtcTicks);
    }
}
