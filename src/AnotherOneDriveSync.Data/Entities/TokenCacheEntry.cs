using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnotherOneDriveSync.Data.Entities;

public class TokenCacheEntry
{
    public string Key { get; set; } = null!;
    public byte[] Value { get; set; } = null!;
}

public class TokenCacheEntryConfiguration : IEntityTypeConfiguration<TokenCacheEntry>
{
    public void Configure(EntityTypeBuilder<TokenCacheEntry> builder)
    {
        builder.HasKey(e => e.Key);
        builder.Property(e => e.Key).IsRequired();
        builder.Property(e => e.Value).IsRequired();
    }
}
