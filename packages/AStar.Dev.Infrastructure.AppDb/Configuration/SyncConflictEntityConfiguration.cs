using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Infrastructure.AppDb.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

public class SyncConflictEntityConfiguration : IEntityTypeConfiguration<SyncConflictEntity>
{
    public void Configure(EntityTypeBuilder<SyncConflictEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.AccountId)
                   .HasConversion(id => id.Id, str => new AccountId(str));
        _ = builder.Property(e => e.FolderId)
                   .HasConversion(id => id.Id, str => new OneDriveFolderId(str));
        _ = builder.Property(e => e.RemoteItemId)
                   .HasConversion(id => id.Id, str => new OneDriveItemId(str));
        _ = builder.Property(e => e.Resolution)
                   .HasConversion(SqliteTypeConverters.OptionConflictPolicyToNullableInt)
                   .IsRequired(false);
        _ = builder.Property(e => e.ResolvedAt)
                   .HasConversion(SqliteTypeConverters.OptionDateTimeOffsetToNullableTicks)
                   .IsRequired(false);
        _ = builder.HasIndex(c => new { c.AccountId, c.State });
    }
}
