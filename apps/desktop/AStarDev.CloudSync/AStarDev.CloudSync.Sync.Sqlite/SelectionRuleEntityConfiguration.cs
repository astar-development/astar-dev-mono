using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Maps <see cref="SelectionRuleEntity"/> to the <c>SelectionRules</c> table: one rule per account and path, ignoring case.</summary>
[ExcludeFromCodeCoverage]
internal sealed class SelectionRuleEntityConfiguration : IEntityTypeConfiguration<SelectionRuleEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SelectionRuleEntity> builder)
    {
        builder.ToTable("SelectionRules", table => table.HasCheckConstraint("CK_SelectionRules_Direction_Known", "\"Direction\" IN ('TwoWay', 'UploadOnly', 'DownloadOnly')"));
        builder.HasKey(rule => new { rule.AccountId, rule.Path });
        builder.Property(rule => rule.Path).IsRequired().UseCollation("NOCASE");
        builder.Property(rule => rule.Direction).IsRequired();
    }
}
