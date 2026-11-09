using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Maps <see cref="SyncRootEntity"/> to the <c>SyncRoots</c> table: one row per account, and no two accounts with the same path, ignoring case.</summary>
[ExcludeFromCodeCoverage]
internal sealed class SyncRootEntityConfiguration : IEntityTypeConfiguration<SyncRootEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncRootEntity> builder)
    {
        builder.ToTable("SyncRoots", table => table.HasCheckConstraint("CK_SyncRoots_Path_NotBlank", "length(trim(\"Path\")) > 0"));
        builder.HasKey(root => root.AccountId);
        builder.Property(root => root.Path).IsRequired().UseCollation("NOCASE");
        builder.HasIndex(root => root.Path).IsUnique();
    }
}
