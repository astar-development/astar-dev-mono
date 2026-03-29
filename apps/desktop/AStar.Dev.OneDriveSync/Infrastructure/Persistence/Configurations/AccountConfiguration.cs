using AStar.Dev.OneDriveSync.Features.Accounts;
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
        _ = builder.HasKey(a => a.Id);

        // Synthetic GUID — caller always provides the value; EF must never generate one.
        _ = builder.Property(a => a.Id)
            .ValueGeneratedNever();

        _ = builder.Property(a => a.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        _ = builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(320);

        _ = builder.Property(a => a.MicrosoftAccountId)
            .IsRequired()
            .HasMaxLength(128);
    }
}
