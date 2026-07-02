namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
/// 
/// </summary>
/// <param name="Value"></param>
public readonly record struct ImageId(Guid Value)
{
    /// <summary>
    /// </summary>
    public static ImageId CreateNew => new(Guid.CreateVersion7());
}