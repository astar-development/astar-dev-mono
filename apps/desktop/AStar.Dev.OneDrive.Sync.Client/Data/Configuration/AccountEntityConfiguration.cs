using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

public class AccountEntityConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
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
    }
}
