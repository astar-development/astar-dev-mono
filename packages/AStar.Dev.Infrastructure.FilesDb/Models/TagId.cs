namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///     Defines the TagId
/// </summary>
/// <param name="Value">The value of the Tag Id</param>
public readonly record struct TagId(Guid Value)
{
    /// <summary>
    ///    Creates a new instance of the TagId struct with a new Guid value
     ///
    /// </summary>
    /// <returns>A new TagId instance</returns>
    public static TagId CreateNew() => new(Guid.CreateVersion7());
}
