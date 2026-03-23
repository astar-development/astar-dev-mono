using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Technical.Debt.Reporting;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Infrastructure.FilesDb.Data;

/// <summary>
/// </summary>
[Refactor(1, 1, "I think this is now defunct too")]
public static class FilesContextExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="files">
    ///     The list of files to filter.
    /// </param>
    /// <param name="startingFolder">
    ///     The starting folder for the filter to be applied from.
    /// </param>
    /// <param name="recursive">
    ///     A boolean to control whether the filter is applied recursively or not.
    /// </param>
    /// <param name="searchType">
    ///     A string representation of the required Search Type.
    /// </param>
    /// <param name="includeSoftDeleted">
    ///     A boolean to control whether the filter includes files marked as Soft Deleted or not.
    /// </param>
    /// <param name="includeMarkedForDeletion">
    ///     A boolean to control whether the filter includes files marked for Deletion or not.
    /// </param>
    /// <param name="excludeViewed">
    ///     A boolean to control whether the filter includes files recently viewed or not.
    /// </param>
    /// <param name="cancellationToken">
    ///     An instance of <see href="CancellationToken"></see> to cancel the filter when requested.
    /// </param>
    /// <returns>
    ///     The original list of files for further filtering.
    /// </returns>
    public static IEnumerable<FileDetail> GetMatchingFiles(this DbSet<FileDetail> files,
                                                           string                 startingFolder,
                                                           bool                   recursive,
                                                           string                 searchType,
                                                           bool                   includeSoftDeleted,
                                                           bool                   includeMarkedForDeletion,
                                                           bool                   excludeViewed,
                                                           CancellationToken      cancellationToken)
    {
        var filesToReturn = files.Include(fileDetail => fileDetail.FileAccessDetail).AsNoTracking().AsQueryable();

        if(cancellationToken.IsCancellationRequested)
        {
            return [];
        }

        filesToReturn = recursive
                            ? filesToReturn.Where(file => file.DirectoryName.Value.StartsWith(startingFolder))
                            : filesToReturn.Where(file => file.DirectoryName.Equals(startingFolder));

        if(cancellationToken.IsCancellationRequested)
        {
            return [];
        }

        filesToReturn = includeSoftDeleted
                            ? filesToReturn
                            : filesToReturn.Where(file => file.DeletionStatus.SoftDeleted == null);

        if(cancellationToken.IsCancellationRequested)
        {
            return [];
        }

        if(!includeMarkedForDeletion)
        {
            filesToReturn = filesToReturn.Where(file => file.DeletionStatus.SoftDeletePending != null && file.DeletionStatus.HardDeletePending != null);
        }

        if(cancellationToken.IsCancellationRequested)
        {
            return [];
        }

        if(searchType == "Images")
        {
            filesToReturn = filesToReturn.Where(file => file.FileName.Value.EndsWith("jpg")
                                                        || file.FileName.Value.EndsWith("jpeg")
                                                        || file.FileName.Value.EndsWith("bmp")
                                                        || file.FileName.Value.EndsWith("png")
                                                        || file.FileName.Value.EndsWith("jfif")
                                                        || file.FileName.Value.EndsWith("jif")
                                                        || file.FileName.Value.EndsWith("gif"));
        }

        if(cancellationToken.IsCancellationRequested)
        {
            return [];
        }

        if(excludeViewed)
        {
            filesToReturn = filesToReturn.Where(file => file.FileAccessDetail.LastViewed == null ||
                                                        file.FileAccessDetail.LastViewed <=
                                                        DateTime.UtcNow.AddDays(-7));
        }

        return cancellationToken.IsCancellationRequested ? [] : [.. filesToReturn];
    }
}