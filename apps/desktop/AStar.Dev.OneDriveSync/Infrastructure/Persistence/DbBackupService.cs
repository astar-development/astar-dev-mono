using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Functional.Extensions;
using Microsoft.Extensions.Logging;

using MelILogger = Microsoft.Extensions.Logging.ILogger;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     Copies <c>file-data.db</c> to <c>file-data.db.bak</c> before any sync mutation begins (AC EH-07).
///
///     The backup is triggered on explicit call only — never on construction, never periodically.
/// </summary>
public sealed partial class DbBackupService(IAppDataPathProvider pathProvider, ILogger<DbBackupService> logger, IFileSystem fileSystem) : IDbBackupService
{
    private const string DatabaseFileName = "file-data.db";
    private const string BackupFileName = "file-data.db.bak";

    /// <inheritdoc />
    public async Task<Result<bool, ErrorResponse>> BackupAsync(CancellationToken cancellationToken = default)
    {
        string dataFilePath = Path.Combine(pathProvider.AppDataDirectory, DatabaseFileName);

        if (!fileSystem.File.Exists(dataFilePath))
        {
            LogBackupFileMissing(logger, dataFilePath);

            return new Result<bool, ErrorResponse>.Error(
                new ErrorResponse($"Database file not found at '{dataFilePath}'. Cannot create backup."));
        }

        string backupFilePath = Path.Combine(pathProvider.AppDataDirectory, BackupFileName);

        await using var source = fileSystem.File.OpenRead(dataFilePath);
        await using var destination = fileSystem.File.Open(backupFilePath, FileMode.Create, FileAccess.Write);
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);

        LogBackupComplete(logger, backupFilePath);

        return new Result<bool, ErrorResponse>.Ok(true);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Backup failed: database file not found at '{Path}'")]
    private static partial void LogBackupFileMissing(MelILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database backed up to '{BackupPath}'")]
    private static partial void LogBackupComplete(MelILogger logger, string backupPath);
}
