using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnotherOneDriveSync.Data.Entities;

public class DriveItemMetadata
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? ParentId { get; set; }
    public bool IsFolder { get; set; }
    public long? Size { get; set; }
    public DateTimeOffset? CreatedTime { get; set; }
    public DateTimeOffset? LastModifiedTime { get; set; }
    public string? ETag { get; set; }
    public string? CTag { get; set; }
    public int SyncFolderId { get; set; }
    public SyncFolder SyncFolder { get; set; } = null!;
}

public class DriveItemMetadataConfiguration : IEntityTypeConfiguration<DriveItemMetadata>
{
    public void Configure(EntityTypeBuilder<DriveItemMetadata> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired();
        builder.HasOne(e => e.SyncFolder)
               .WithMany()
               .HasForeignKey(e => e.SyncFolderId);
    }
}
