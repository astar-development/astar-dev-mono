using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Maps <see cref="SyncBaselineEntity"/> to the <c>SyncBaseline</c> table: one row per account and path key, which matches paths ignoring case.</summary>
[ExcludeFromCodeCoverage]
internal sealed class SyncBaselineEntityConfiguration : IEntityTypeConfiguration<SyncBaselineEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncBaselineEntity> builder)
    {
        builder.ToTable("SyncBaseline");
        builder.HasKey(item => new { item.AccountId, item.PathKey });
        builder.Property(item => item.Path).IsRequired();
        builder.Property(item => item.PathKey).IsRequired();
        builder.Property(item => item.RemoteItemId).IsRequired();
    }
}
