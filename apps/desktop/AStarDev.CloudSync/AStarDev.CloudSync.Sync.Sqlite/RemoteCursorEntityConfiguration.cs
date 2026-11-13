using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

[ExcludeFromCodeCoverage]
internal sealed class RemoteCursorEntityConfiguration : IEntityTypeConfiguration<RemoteCursorEntity>
{
    public void Configure(EntityTypeBuilder<RemoteCursorEntity> builder)
    {
        builder.ToTable("RemoteCursors");
        builder.HasKey(cursor => cursor.AccountId);
    }
}
