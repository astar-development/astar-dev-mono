using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Utilities;

namespace AStar.Dev.Infrastructure.FilesDb.Data;

/// <summary>
/// </summary>
public static class FileDetailDirectoryNameExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="files"></param>
    /// <param name="directoryName"></param>
    /// <param name="includeSubDirectories"></param>
    /// <returns></returns>
    public static IQueryable<FileDetail> WhereDirectoryNameMatches(this IQueryable<FileDetail> files, string directoryName, bool includeSubDirectories)
        => includeSubDirectories
               ? files.Where(file => file.DirectoryName.Value.Contains(directoryName.RemoveTrailing(@"\")))
               : files.Where(file => file.DirectoryName.Value == directoryName.RemoveTrailing(@"\"));
}