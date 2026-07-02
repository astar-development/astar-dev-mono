namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///     Defines the FileId
/// </summary>
/// <param name="Value">The value of the File Id</param>
public readonly record struct FileId(Guid Value)
{
    /// <summary>
    ///    Creates a new instance of the FileId struct with a new Guid value
     ///
    /// </summary>
    /// <returns>A new FileId instance</returns>
    public static FileId CreateNew() => new(Guid.CreateVersion7());
}
