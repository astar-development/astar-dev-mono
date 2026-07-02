namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///     Defines the ModelId
/// </summary>
/// <param name="Value">The value of the Model Id</param>
public readonly record struct ModelId(Guid Value)
{
    /// <summary>
    ///    Creates a new instance of the ModelId struct with a new Guid value
     ///
    /// </summary>
    /// <returns>A new ModelId instance</returns>
    public static ModelId CreateNew() => new(Guid.CreateVersion7());
}
