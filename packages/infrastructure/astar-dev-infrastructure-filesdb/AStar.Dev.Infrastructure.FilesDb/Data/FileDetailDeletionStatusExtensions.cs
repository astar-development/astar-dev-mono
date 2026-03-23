using AStar.Dev.Infrastructure.FilesDb.Models;

namespace AStar.Dev.Infrastructure.FilesDb.Data;

/// <summary>
/// </summary>
public static class FileDetailDeletionStatusExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="files"></param>
    /// <param name="includeDeleted"></param>
    /// <returns></returns>
    public static IQueryable<FileDetail> IncludeDeletedOrDeletePending(this IQueryable<FileDetail> files, bool includeDeleted)
        => includeDeleted
               ? files
               : files.Where(f => f.DeletionStatus.HardDeletePending    == null
                                  && f.DeletionStatus.SoftDeletePending == null
                                  && f.DeletionStatus.SoftDeleted       == null);
}