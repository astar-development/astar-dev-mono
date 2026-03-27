using AStar.Dev.OneDriveSync.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence.Configurations;

/// <summary>
///     Configures the <see cref="Account" /> entity mapping (AC DB-02).
///
///     Enforces the PII isolation rule: <c>DisplayName</c>, <c>Email</c>, and
///     <c>MicrosoftAccountId</c> exist only in the <c>Accounts</c> table.
///     All other entities reference this table via the synthetic <see cref="Account.Id"/>
///     Guid foreign key.
/// </summary>
internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(a => a.MicrosoftAccountId)
            .IsRequired()
            .HasMaxLength(128);
    }
}
