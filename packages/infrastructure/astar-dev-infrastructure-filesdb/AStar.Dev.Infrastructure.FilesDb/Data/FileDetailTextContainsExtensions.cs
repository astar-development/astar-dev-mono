using AStar.Dev.Infrastructure.FilesDb.Models;

namespace AStar.Dev.Infrastructure.FilesDb.Data;

/// <summary>
/// </summary>
public static class FileDetailTextContainsExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="files"></param>
    /// <param name="searchText"></param>
    /// <returns></returns>
    public static IQueryable<FileDetail> SelectFilesMatching(this IQueryable<FileDetail> files, string? searchText)
        => string.IsNullOrEmpty(searchText)
               ? files
               : files.Where(file => file.DirectoryName.Value.Contains(searchText) || file.FileName.Value.Contains(searchText));
}