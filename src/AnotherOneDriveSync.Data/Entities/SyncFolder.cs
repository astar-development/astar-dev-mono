using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnotherOneDriveSync.Data.Entities;

public class SyncFolder
{
    public int Id { get; set; }
    public string DriveId { get; set; } = null!;
    public string FolderId { get; set; } = null!;
    public string LocalPath { get; set; } = null!;
    public DateTimeOffset? LastSyncTime { get; set; }
    public string? ETag { get; set; }
}

public class SyncFolderConfiguration : IEntityTypeConfiguration<SyncFolder>
{
    public void Configure(EntityTypeBuilder<SyncFolder> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DriveId).IsRequired();
        builder.Property(e => e.FolderId).IsRequired();
        builder.Property(e => e.LocalPath).IsRequired();
    }
}
