using AStar.Dev.Infrastructure.FilesDb.Models;

namespace AStar.Dev.Infrastructure.FilesDb.Data;

/// <summary>
/// </summary>
public static class FileDetailPagingExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="files"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    public static IQueryable<FileDetail> GetPage(this IQueryable<FileDetail> files, int pageNumber, int pageSize)
    {
        pageSize = RestrictPageSize(pageSize);

        if(pageNumber < 1)
        {
            pageNumber = 1;
        }

        return files.Skip(pageNumber * pageSize).Take(pageSize);
    }

    private static int RestrictPageSize(int pageSize)
        => pageSize switch
           {
               < 1  => 1,
               > 50 => 50,
               _    => pageSize
           };
}