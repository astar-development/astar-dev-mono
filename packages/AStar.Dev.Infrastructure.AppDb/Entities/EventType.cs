namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>The kind of change recorded by an <see cref="EventEntity"/>.</summary>
public enum EventType
{
    /// <summary>A new record was created.</summary>
    Add = 1,

    /// <summary>An existing record was modified.</summary>
    Update = 2,

    /// <summary>A record was soft-deleted.</summary>
    SoftDelete = 3,

    /// <summary>A record was permanently removed.</summary>
    HardDelete = 4
}
