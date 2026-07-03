using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

public class DriveStateEntityConfiguration : IEntityTypeConfiguration<DriveStateEntity>
{
    public void Configure(EntityTypeBuilder<DriveStateEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.AccountId)
                   .HasConversion(id => id.Id, str => new AccountId(str));
        _ = builder.Property(e => e.DeltaLink)
                   .HasConversion(SqliteTypeConverters.OptionStringToNullableString)
                   .IsRequired(false);
        _ = builder.Property(e => e.LastSyncStartedAt)
                   .HasConversion(SqliteTypeConverters.OptionDateTimeOffsetToNullableTicks)
                   .IsRequired(false);
        _ = builder.HasIndex(e => e.AccountId).IsUnique();
        _ = builder.HasOne(e => e.Account)
                   .WithOne()
                   .HasForeignKey<DriveStateEntity>(e => e.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);
    }
}
