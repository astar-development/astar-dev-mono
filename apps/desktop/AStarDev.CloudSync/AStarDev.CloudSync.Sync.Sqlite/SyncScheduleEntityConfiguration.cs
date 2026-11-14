using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

[ExcludeFromCodeCoverage]
internal sealed class SyncScheduleEntityConfiguration : IEntityTypeConfiguration<SyncScheduleEntity>
{
    public void Configure(EntityTypeBuilder<SyncScheduleEntity> builder)
    {
        builder.ToTable("SyncSchedule");
        builder.HasKey(schedule => schedule.Id);
        builder.Property(schedule => schedule.Id).ValueGeneratedNever();
    }
}
