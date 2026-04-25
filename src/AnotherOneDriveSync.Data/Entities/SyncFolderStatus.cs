using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnotherOneDriveSync.Data.Entities;

public enum SyncStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}

public class SyncFolderStatus
{
    public int Id { get; set; }
    public int SyncFolderId { get; set; }
    public SyncFolder SyncFolder { get; set; } = null!;
    public SyncStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class SyncFolderStatusConfiguration : IEntityTypeConfiguration<SyncFolderStatus>
{
    public void Configure(EntityTypeBuilder<SyncFolderStatus> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasOne(e => e.SyncFolder)
               .WithMany()
               .HasForeignKey(e => e.SyncFolderId);
        builder.Property(e => e.Status).HasConversion<string>();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
    }
}
