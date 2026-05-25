using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

public sealed class FileClassificationRuleEntityConfiguration : IEntityTypeConfiguration<FileClassificationRuleEntity>
{
    public void Configure(EntityTypeBuilder<FileClassificationRuleEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.Property(e => e.Keywords).IsRequired();
        _ = builder.Property(e => e.Level1).IsRequired();
        _ = builder.Property(e => e.Level2).IsRequired(false);
        _ = builder.Property(e => e.Level3).IsRequired(false);
    }
}
