using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

[ExcludeFromCodeCoverage]
internal sealed class ThroughputSampleEntityConfiguration : IEntityTypeConfiguration<ThroughputSampleEntity>
{
    public void Configure(EntityTypeBuilder<ThroughputSampleEntity> builder)
    {
        builder.ToTable("ThroughputSamples");
        builder.HasKey(sample => sample.Id);
        builder.Property(sample => sample.Id).ValueGeneratedOnAdd();
        builder.HasIndex(sample => sample.AtUtcTicks);
    }
}
