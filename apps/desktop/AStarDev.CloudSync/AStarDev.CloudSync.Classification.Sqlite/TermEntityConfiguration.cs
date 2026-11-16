using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Maps <see cref="TermEntity"/> to the <c>Terms</c> table: one row per category and normalised text. A term is never deleted, so a category with terms cannot be deleted either.</summary>
[ExcludeFromCodeCoverage]
internal sealed class TermEntityConfiguration : IEntityTypeConfiguration<TermEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TermEntity> builder)
    {
        builder.ToTable("Terms");
        builder.HasKey(term => term.Id);
        builder.Property(term => term.Id).ValueGeneratedNever();
        builder.Property(term => term.Text).IsRequired();
        builder.Property(term => term.Source).IsRequired();
        builder.HasOne<CategoryEntity>().WithMany().HasForeignKey(term => term.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(term => new { term.CategoryId, term.Text }).IsUnique();
    }
}
