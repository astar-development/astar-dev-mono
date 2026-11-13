using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

[ExcludeFromCodeCoverage]
internal sealed class RemoteItemEntityConfiguration : IEntityTypeConfiguration<RemoteItemEntity>
{
    public void Configure(EntityTypeBuilder<RemoteItemEntity> builder)
    {
        builder.ToTable("RemoteItems");
        builder.HasKey(item => new { item.AccountId, item.RemoteItemId });
        builder.HasIndex(item => new { item.AccountId, item.ParentId });
        builder.Property(item => item.RemoteItemId).IsRequired();
        builder.Property(item => item.Name).IsRequired();
        builder.Property(item => item.ETag).IsRequired();
    }
}
