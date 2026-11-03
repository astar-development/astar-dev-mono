using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="SyncThroughputSampleEntity"/>.</summary>
public class SyncThroughputSampleEntityConfiguration : IEntityTypeConfiguration<SyncThroughputSampleEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncThroughputSampleEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.AccountId)
                   .HasConversion(id => id.Value, str => new AccountId(str));
        _ = builder.HasIndex(e => e.CompletedAt);
        _ = builder.HasOne(e => e.Account)
                   .WithMany()
                   .HasForeignKey(e => e.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);
    }
}
