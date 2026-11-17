using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Maps <see cref="ReclassifyRunEntity"/> to the <c>ReclassifyRun</c> table: at most one row, the last completed reclassify run.</summary>
[ExcludeFromCodeCoverage]
internal sealed class ReclassifyRunEntityConfiguration : IEntityTypeConfiguration<ReclassifyRunEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ReclassifyRunEntity> builder)
    {
        builder.ToTable("ReclassifyRun", table => table.HasCheckConstraint("CK_ReclassifyRun_SingleRow", "\"Id\" = 1"));
        builder.HasKey(run => run.Id);
        builder.Property(run => run.Id).ValueGeneratedNever();
        builder.Property(run => run.EngineVersion).IsRequired();
        builder.Property(run => run.FinishedAt).IsRequired();
    }
}
