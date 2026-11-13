using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

[ExcludeFromCodeCoverage]
internal sealed class AccountPauseEntityConfiguration : IEntityTypeConfiguration<AccountPauseEntity>
{
    public void Configure(EntityTypeBuilder<AccountPauseEntity> builder)
    {
        builder.ToTable("AccountPauses");
        builder.HasKey(pause => pause.AccountId);
    }
}
