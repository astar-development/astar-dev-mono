using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Infrastructure.AppDb.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

public class AccountEntityConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.Id)
                   .HasConversion(id => id.Id, str => new AccountId(str));
        _ = builder.Property(e => e.LastSyncedAt)
                   .HasConversion(SqliteTypeConverters.OptionDateTimeOffsetToNullableTicks)
                   .IsRequired(false);
        _ = builder.ComplexProperty(e => e.Profile, p =>
        {
            _ = p.Property(prof => prof.DisplayName).HasColumnName("DisplayName");
            _ = p.Property(prof => prof.Email).HasColumnName("Email");
        });
        _ = builder.ComplexProperty(e => e.Quota, q =>
        {
            _ = q.Property(s => s.TotalBytes).HasColumnName("QuotaTotal");
            _ = q.Property(s => s.UsedBytes).HasColumnName("QuotaUsed");
        });
        _ = builder.ComplexProperty(e => e.SyncConfig, s =>
        {
            _ = s.Property(cfg => cfg.ConflictPolicy).HasColumnName("ConflictPolicy");
            _ = s.Property(cfg => cfg.LocalSyncPath)
                 .HasConversion(path => path.Value, str => LocalSyncPath.Restore(str))
                 .HasColumnName("LocalSyncPath");
        });
        _ = builder.HasMany<SyncConflictEntity>()
                   .WithOne(c => c.Account)
                   .HasForeignKey(c => c.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);
        _ = builder.HasMany<SyncJobEntity>()
                   .WithOne(j => j.Account)
                   .HasForeignKey(j => j.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);
        _ = builder.HasOne<DriveStateEntity>()
                   .WithOne(d => d.Account)
                   .HasForeignKey<DriveStateEntity>(d => d.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);
        _ = builder.HasMany<SyncRuleEntity>()
                   .WithOne(r => r.Account)
                   .HasForeignKey(r => r.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);
        _ = builder.HasMany<SyncedItemEntity>()
                   .WithOne(i => i.Account)
                   .HasForeignKey(i => i.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);
    }
}
