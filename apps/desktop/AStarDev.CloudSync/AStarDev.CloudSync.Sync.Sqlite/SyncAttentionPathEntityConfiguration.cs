using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

[ExcludeFromCodeCoverage]
internal sealed class SyncAttentionPathEntityConfiguration : IEntityTypeConfiguration<SyncAttentionPathEntity>
{
    public void Configure(EntityTypeBuilder<SyncAttentionPathEntity> builder)
    {
        builder.ToTable("SyncAttentionPaths");
        builder.HasKey(path => new { path.AttentionId, path.Path });
        builder.Property(path => path.Path).IsRequired();
    }
}
