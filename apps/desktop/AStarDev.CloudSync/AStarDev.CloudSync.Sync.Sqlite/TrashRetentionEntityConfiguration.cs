using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

[ExcludeFromCodeCoverage]
internal sealed class TrashRetentionEntityConfiguration : IEntityTypeConfiguration<TrashRetentionEntity>
{
    public void Configure(EntityTypeBuilder<TrashRetentionEntity> builder)
    {
        builder.ToTable("TrashRetention");
        builder.HasKey(retention => retention.Id);
        builder.Property(retention => retention.Id).ValueGeneratedNever();
    }
}
