using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

[ExcludeFromCodeCoverage]
internal sealed class TransferConcurrencyEntityConfiguration : IEntityTypeConfiguration<TransferConcurrencyEntity>
{
    public void Configure(EntityTypeBuilder<TransferConcurrencyEntity> builder)
    {
        builder.ToTable("TransferConcurrencies");
        builder.HasKey(setting => setting.AccountId);
        builder.Property(setting => setting.MaxConcurrentTransfers).IsRequired();
    }
}
