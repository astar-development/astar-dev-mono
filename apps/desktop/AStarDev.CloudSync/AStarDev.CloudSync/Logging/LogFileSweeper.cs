using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Logging;

/// <summary>Deletes log files that are older than the retention age, so the log folder does not grow for ever (#1411). A file that cannot be deleted is skipped, because tidying logs must never stop the app.</summary>
/// <param name="timeProvider">Supplies the clock the age is measured against.</param>
public sealed class LogFileSweeper(TimeProvider timeProvider)
{
    /// <summary>Deletes the folder's <c>{prefix}-*.log</c> files whose last write is more than <paramref name="age"/> ago. Other files are left alone.</summary>
    /// <param name="folder">The log folder. A folder that does not exist has nothing to delete.</param>
    /// <param name="fileNamePrefix">The prefix of the app's log file names, for example <c>cloudsync</c>.</param>
    /// <param name="age">How old a file may get.</param>
    /// <returns>How many files were deleted.</returns>
    public int DeleteOlderThan(IDirectoryInfo folder, string fileNamePrefix, TimeSpan age)
    {
        folder.Refresh();
        if (!folder.Exists)
            return 0;

        var cutoff = timeProvider.GetUtcNow().UtcDateTime - age;

        return folder.EnumerateFiles($"{fileNamePrefix}-*.log").Where(file => file.LastWriteTimeUtc < cutoff).Count(DeleteQuietly);
    }

    private static bool DeleteQuietly(IFileInfo file) => Try.Run(() =>
    {
        file.Delete();

        return true;
    }).Match(deleted => deleted, _ => false);
}
