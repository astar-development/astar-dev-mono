namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///     Defines the EventId
/// </summary>
/// <param name="Value">The value of the Event Id</param>
public readonly record struct EventId(Guid Value)
{
    /// <summary>
    ///    Creates a new instance of the EventId struct with a new Guid value
     ///
    /// </summary>
    /// <returns>A new FileId instance</returns>
    public static EventId CreateNew() => new(Guid.CreateVersion7());
}
