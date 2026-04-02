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
        _ = builder.HasKey(account => account.Id);

        _ = builder.Property(account => account.Id)
            .ValueGeneratedNever();

        _ = builder.Property(account => account.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        _ = builder.Property(account => account.Email)
            .IsRequired()
            .HasMaxLength(320);

        _ = builder.Property(account => account.MicrosoftAccountId)
            .IsRequired()
            .HasMaxLength(128);

        _ = builder.Property(account => account.AuthState)
            .IsRequired()
            .HasMaxLength(32)
            .HasDefaultValue("Authenticated");

        _ = builder.Property(account => account.LocalSyncPath)
            .IsRequired()
            .HasMaxLength(4096)
            .HasDefaultValue(string.Empty);

        _ = builder.Property(account => account.SyncIntervalMinutes)
            .HasDefaultValue(15);

        _ = builder.Property(account => account.ConcurrencyLimit)
            .HasDefaultValue(5);

        _ = builder.Property(account => account.StoreFileMetadata)
            .HasDefaultValue(false);

        _ = builder.Property(account => account.IsSyncActive)
            .HasDefaultValue(false);
    }
}
