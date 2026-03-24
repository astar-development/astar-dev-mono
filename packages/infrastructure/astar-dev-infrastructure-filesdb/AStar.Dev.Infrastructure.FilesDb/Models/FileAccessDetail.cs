using AStar.Dev.Utilities;

namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
/// </summary>
public sealed class FileAccessDetail
{
    /// <summary>
    ///     Gets or sets The ID of the <see href="FileAccessDetail"></see>. I know, shocking...
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the date the file details were last updated. I know, shocking...
    /// </summary>
    public DateTime? DetailsLastUpdated { get; set; }

    /// <summary>
    ///     Gets or sets the date the file was last viewed. I know, shocking...
    /// </summary>
    public DateTime? LastViewed { get; set; }

    /// <summary>
    ///     Gets or sets whether the file has been marked as 'needs to move'. I know, shocking...
    /// </summary>
    public bool MoveRequired { get; set; }

    /// <summary>
    ///     Returns this object in JSON format
    /// </summary>
    /// <returns>
    ///     This object serialized as a JSON object
    /// </returns>
    public override string ToString()
        => this.ToJson();
}