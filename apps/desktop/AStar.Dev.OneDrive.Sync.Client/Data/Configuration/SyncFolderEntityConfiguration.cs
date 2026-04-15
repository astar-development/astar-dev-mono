using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

public class SyncFolderEntityConfiguration : IEntityTypeConfiguration<SyncFolderEntity>
{
    public void Configure(EntityTypeBuilder<SyncFolderEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.AccountId)
                   .HasConversion(id => id.Id, str => new AccountId(str));
        _ = builder.Property(e => e.FolderId)
                   .HasConversion(id => id.Id, str => new OneDriveFolderId(str));
        _ = builder.HasIndex(f => new { f.AccountId, f.FolderId }).IsUnique();
        _ = builder.HasOne(f => f.Account)
                   .WithMany(a => a.SyncFolders)
                   .HasForeignKey(f => f.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);
    }
}
