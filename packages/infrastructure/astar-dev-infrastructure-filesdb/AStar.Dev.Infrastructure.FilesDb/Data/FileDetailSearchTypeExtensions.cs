using System.Diagnostics;
using AStar.Dev.Infrastructure.FilesDb.Models;

namespace AStar.Dev.Infrastructure.FilesDb.Data;

/// <summary>
/// </summary>
public static class FileDetailSearchTypeExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="files"></param>
    /// <param name="searchType"></param>
    /// <returns></returns>
    public static IQueryable<FileDetail> OfSearchType(this IQueryable<FileDetail> files, SearchType searchType)
        => searchType switch
           {
               SearchType.All        => files,
               SearchType.Images     => files.Where(f => f.IsImage),
               SearchType.Duplicates => files.Where(f => files.Count(x => x.FileSize == f.FileSize) > 1),
               SearchType.DuplicateImages => files.Where(f => f.IsImage &&
                                                              files.Count(x => x.IsImage                                    &&
                                                                               x.FileSize           == f.FileSize           &&
                                                                               x.ImageDetail.Height == f.ImageDetail.Height &&
                                                                               x.ImageDetail.Width  == f.ImageDetail.Width) > 1),
               _ => throw new UnreachableException("If we reach here, a new SearchType has been added but not included...")
           };
}