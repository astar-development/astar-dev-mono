using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Maps <see cref="ClassificationStateEntity"/> to the <c>ClassificationState</c> table: at most one row, which holds the applied seed version.</summary>
[ExcludeFromCodeCoverage]
internal sealed class ClassificationStateEntityConfiguration : IEntityTypeConfiguration<ClassificationStateEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ClassificationStateEntity> builder)
    {
        builder.ToTable("ClassificationState", table => table.HasCheckConstraint("CK_ClassificationState_SingleRow", "\"Id\" = 1"));
        builder.HasKey(state => state.Id);
        builder.Property(state => state.Id).ValueGeneratedNever();
        builder.Property(state => state.SeedVersion).IsRequired();
    }
}
