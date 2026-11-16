using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Maps <see cref="FileLinkEntity"/> to the <c>FileLinks</c> table: one row per account, path key and term, with an index on the term so the files of a term and the file counts are read without a scan.</summary>
[ExcludeFromCodeCoverage]
internal sealed class FileLinkEntityConfiguration : IEntityTypeConfiguration<FileLinkEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileLinkEntity> builder)
    {
        builder.ToTable("FileLinks");
        builder.HasKey(link => new { link.AccountId, link.PathKey, link.TermId });
        builder.Property(link => link.Path).IsRequired();
        builder.Property(link => link.PathKey).IsRequired();
        builder.Property(link => link.Origin).IsRequired();
        builder.HasOne<TermEntity>().WithMany().HasForeignKey(link => link.TermId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(link => link.TermId);
    }
}
