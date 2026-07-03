namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>An audit record of a change made to a scraped file.</summary>
public sealed class EventEntity
{
    /// <summary>Primary key.</summary>
    public FileEventId Id { get; set; } = new(Guid.CreateVersion7());

    /// <summary>The kind of change this event records.</summary>
    public EventType Type { get; set; } = EventType.Update;

    /// <summary>The date and time the event occurred.</summary>
    public DateTimeOffset EventOccurredAt { get; set; }

    /// <summary>The name of the file at the time of the event.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>The directory containing the file at the time of the event.</summary>
    public string DirectoryName { get; set; } = string.Empty;

    /// <summary>The file's handle at the time of the event.</summary>
    public string Handle { get; set; } = string.Empty;

    /// <summary>The width of the file in pixels, if it was an image.</summary>
    public int? Width { get; set; }

    /// <summary>The height of the file in pixels, if it was an image.</summary>
    public int? Height { get; set; }

    /// <summary>The file size in bytes at the time of the event.</summary>
    public long FileSize { get; set; }

    /// <summary>The date and time the file was created.</summary>
    public DateTimeOffset FileCreated { get; set; }

    /// <summary>The date and time the file was last modified.</summary>
    public DateTimeOffset FileLastModified { get; set; }

    /// <summary>Identifies who or what made the change.</summary>
    public string UpdatedBy { get; set; } = string.Empty;
}
