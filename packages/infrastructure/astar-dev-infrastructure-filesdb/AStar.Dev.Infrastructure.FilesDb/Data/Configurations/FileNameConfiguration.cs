using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Data.Configurations;

internal sealed class FileNameConfiguration : IComplexPropertyConfiguration<FileName>
{
    public void Configure(ComplexPropertyBuilder<FileName> builder)
        => builder.Property(fileName => fileName.Value)
                  .HasColumnName("FileName")
                  .HasColumnType("nvarchar(256)");
}