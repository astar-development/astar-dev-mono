using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

[ExcludeFromCodeCoverage]
internal sealed class AccountLogLevelEntityConfiguration : IEntityTypeConfiguration<AccountLogLevelEntity>
{
    public void Configure(EntityTypeBuilder<AccountLogLevelEntity> builder)
    {
        builder.ToTable("AccountLogLevels");
        builder.HasKey(setting => setting.AccountId);
        builder.Property(setting => setting.Verbose).IsRequired();
    }
}
