using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

public class AccountEntityConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.Id)
                   .HasConversion(id => id.Id, str => new AccountId(str));
        _ = builder.Property(e => e.LocalSyncPath)
                   .HasConversion(path => path.Value, str => LocalSyncPath.Restore(str));
        _ = builder.HasMany(a => a.SyncFolders)
                   .WithOne(f => f.Account)
                   .HasForeignKey(f => f.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);
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
