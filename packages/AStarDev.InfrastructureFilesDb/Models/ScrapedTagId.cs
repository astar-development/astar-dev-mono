namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///     Defines the ScrapedTagId
/// </summary>
/// <param name="Value">The value of the Scraped Tag Id</param>
public readonly record struct ScrapedTagId(Guid Value)
{
    /// <summary>
    ///     Creates a new instance of <see cref="ScrapedTagId" /> with a new Guid value
    /// </summary>
    /// <returns>A new <see cref="ScrapedTagId" /> instance</returns>
    public static ScrapedTagId CreateNew() => new(Guid.CreateVersion7());
}
