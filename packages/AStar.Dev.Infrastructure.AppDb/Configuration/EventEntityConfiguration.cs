using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.AppDb.Configuration;

/// <summary>EF Core configuration for <see cref="EventEntity"/>.</summary>
public sealed class EventEntityConfiguration : IEntityTypeConfiguration<EventEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EventEntity> builder)
    {
        _ = builder.ToTable("Event");
        _ = builder.HasKey(@event => @event.Id);
        _ = builder.Property(@event => @event.Id).HasConversion(eventId => eventId.Id, guid => new FileEventId(guid));

        _ = builder.Property(@event => @event.FileName).HasMaxLength(256);
        _ = builder.Property(@event => @event.DirectoryName).HasMaxLength(256);
        _ = builder.Property(@event => @event.Handle).HasMaxLength(256);
        _ = builder.Property(@event => @event.UpdatedBy).HasMaxLength(30);
    }
}
