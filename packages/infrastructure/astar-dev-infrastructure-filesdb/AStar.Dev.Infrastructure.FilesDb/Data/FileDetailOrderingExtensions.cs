using System.Diagnostics;
using AStar.Dev.Infrastructure.FilesDb.Models;

namespace AStar.Dev.Infrastructure.FilesDb.Data;

/// <summary>
/// </summary>
public static class FileDetailOrderingExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="files"></param>
    /// <param name="sortOrder"></param>
    /// <returns></returns>
    public static IQueryable<FileDetail> OrderResultsBy(this IQueryable<FileDetail> files, SortOrder sortOrder)
        => sortOrder switch
           {
               SortOrder.NameAscending  => files.OrderBy(f => f.FileName.Value),
               SortOrder.NameDescending => files.OrderByDescending(f => f.FileName.Value),
               SortOrder.SizeAscending  => files.OrderBy(f => f.FileSize),
               SortOrder.SizeDescending => files.OrderByDescending(f => f.FileSize),
               _                        => throw new UnreachableException("If we reach here, a new SortOrder has been added but not included...")
           };
}