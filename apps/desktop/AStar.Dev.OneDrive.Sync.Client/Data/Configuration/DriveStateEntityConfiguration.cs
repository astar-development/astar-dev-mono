using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

public class DriveStateEntityConfiguration : IEntityTypeConfiguration<DriveStateEntity>
{
    public void Configure(EntityTypeBuilder<DriveStateEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.AccountId)
                   .HasConversion(id => id.Id, str => new AccountId(str));
        _ = builder.HasIndex(e => e.AccountId).IsUnique();
        _ = builder.HasOne(e => e.Account)
                   .WithOne()
                   .HasForeignKey<DriveStateEntity>(e => e.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);
    }
}
