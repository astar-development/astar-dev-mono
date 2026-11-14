using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.CloudSync.Sync.Sqlite;

[ExcludeFromCodeCoverage]
internal sealed class UploadSessionEntityConfiguration : IEntityTypeConfiguration<UploadSessionEntity>
{
    public void Configure(EntityTypeBuilder<UploadSessionEntity> builder)
    {
        builder.ToTable("UploadSessions");
        builder.HasKey(session => new { session.AccountId, session.PathKey });
        builder.Property(session => session.PathKey).IsRequired();
        builder.Property(session => session.Url).IsRequired();
        builder.Property(session => session.Target).IsRequired();
    }
}
