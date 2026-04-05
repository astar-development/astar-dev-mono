using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Configuration;

public class SyncJobEntityConfiguration : IEntityTypeConfiguration<SyncJobEntity>
{
    public void Configure(EntityTypeBuilder<SyncJobEntity> builder)
    {
        _ = builder.HasKey(e => e.Id);
        _ = builder.HasIndex(j => new { j.AccountId, j.State });
    }
}
