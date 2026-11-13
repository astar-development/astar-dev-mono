using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Maps <see cref="SyncOutcomeEntity"/> to the SyncOutcomes table.</summary>
[ExcludeFromCodeCoverage]
internal sealed class SyncOutcomeEntityConfiguration : IEntityTypeConfiguration<SyncOutcomeEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncOutcomeEntity> builder)
    {
        builder.ToTable("SyncOutcomes");
        builder.HasKey(outcome => outcome.AccountId);
        builder.Property(outcome => outcome.Kind).IsRequired();
    }
}
