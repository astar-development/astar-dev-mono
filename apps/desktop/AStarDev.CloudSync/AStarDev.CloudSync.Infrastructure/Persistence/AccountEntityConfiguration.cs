using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Infrastructure.Persistence;

/// <summary>Maps <see cref="AccountEntity"/> to the <c>Accounts</c> table. One row per provider and key, ignoring the key's case; keys and drive ids must not be blank.</summary>
[ExcludeFromCodeCoverage]
public sealed class AccountEntityConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        builder.ToTable("Accounts", table =>
        {
            table.HasCheckConstraint("CK_Accounts_Provider_Known", "\"Provider\" IN ('OneDrive')");
            table.HasCheckConstraint("CK_Accounts_ProviderAccountKey_NotBlank", "length(trim(\"ProviderAccountKey\")) > 0");
            table.HasCheckConstraint("CK_Accounts_DriveId_NotBlank", "length(trim(\"DriveId\")) > 0");
        });
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Provider).IsRequired();
        builder.Property(account => account.ProviderAccountKey).IsRequired().UseCollation("NOCASE");
        builder.Property(account => account.DisplayName).IsRequired();
        builder.Property(account => account.Email).IsRequired();
        builder.Property(account => account.DriveId).IsRequired();
        builder.HasIndex(account => new { account.Provider, account.ProviderAccountKey }).IsUnique();
    }
}
