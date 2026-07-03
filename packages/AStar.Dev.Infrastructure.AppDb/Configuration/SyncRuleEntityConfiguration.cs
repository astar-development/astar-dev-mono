using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

public class SyncRuleEntityConfiguration : IEntityTypeConfiguration<SyncRuleEntity>
{
    public void Configure(EntityTypeBuilder<SyncRuleEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.AccountId)
                   .HasConversion(id => id.Id, str => new AccountId(str));
        _ = builder.Property(e => e.RemoteItemId)
                   .HasConversion(SqliteTypeConverters.OptionStringToNullableString)
                   .IsRequired(false);
        _ = builder.HasIndex(e => new { e.AccountId, e.RemotePath }).IsUnique();
        _ = builder.HasOne(e => e.Account)
                   .WithMany()
                   .HasForeignKey(e => e.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);
    }
}
