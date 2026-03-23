using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Data.Configurations;

internal sealed class EventTypeConfiguration : IComplexPropertyConfiguration<EventType>
{
    public void Configure(ComplexPropertyBuilder<EventType> builder)
    {
        _ = builder.Property(eventType => eventType.Value).HasColumnName("EventType").IsRequired();
        _ = builder.Property(eventType => eventType.Name).HasColumnName("EventName").IsRequired();
    }
}