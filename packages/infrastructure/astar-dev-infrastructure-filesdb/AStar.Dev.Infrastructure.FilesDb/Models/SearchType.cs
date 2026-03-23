namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///     The <see href="SearchType"></see> enumeration defining the available search types
/// </summary>
public enum SearchType
{
    /// <summary>
    ///     Search for images only
    /// </summary>
    Images,

    /// <summary>
    ///     Search for all file types
    /// </summary>
    All,

    /// <summary>
    ///     Search for duplicates - file type is ignored
    /// </summary>
    Duplicates,

    /// <summary>
    ///     Search for duplicate images
    /// </summary>
    DuplicateImages
}