using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

[ExcludeFromCodeCoverage]
internal sealed class SyncAttentionEntityConfiguration : IEntityTypeConfiguration<SyncAttentionEntity>
{
    public void Configure(EntityTypeBuilder<SyncAttentionEntity> builder)
    {
        builder.ToTable("SyncAttention");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.AccountId, item.ItemKey }).IsUnique();
        builder.Property(item => item.ItemKey).IsRequired();
        builder.HasMany(item => item.HeldPaths).WithOne().HasForeignKey(path => path.AttentionId).OnDelete(DeleteBehavior.Cascade);
    }
}
