using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

/// <summary>
/// </summary>
public sealed class FileAccessDetailConfiguration : IEntityTypeConfiguration<FileAccessDetail>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileAccessDetail> builder)
        => _ = builder
              .ToTable(nameof(FileAccessDetail), Constants.SchemaName)
              .HasKey(fileAccessDetail => fileAccessDetail.Id);
}
