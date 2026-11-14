using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

[ExcludeFromCodeCoverage]
internal sealed class SyncRunEntityConfiguration : IEntityTypeConfiguration<SyncRunEntity>
{
    public void Configure(EntityTypeBuilder<SyncRunEntity> builder)
    {
        builder.ToTable("SyncRuns");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.Id).ValueGeneratedNever();
        builder.Property(run => run.Trigger).IsRequired();
        builder.HasIndex(run => new { run.AccountId, run.StartedAtUtcTicks });
        builder.HasIndex(run => run.FinishedAtUtcTicks);
    }
}
