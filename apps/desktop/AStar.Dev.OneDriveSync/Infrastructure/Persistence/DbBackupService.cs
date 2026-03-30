using AStar.Dev.Functional.Extensions;
using Microsoft.Extensions.Logging;

// Disambiguate ILogger: both Serilog and Microsoft.Extensions.Logging declare ILogger.
// All usages in this file intend the MEL interface.
using MelILogger = Microsoft.Extensions.Logging.ILogger;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     Copies <c>file-data.db</c> to <c>file-data.db.bak</c> before any sync mutation begins (AC EH-07).
///
///     The backup is triggered on explicit call only — never on construction, never periodically.
/// </summary>
public sealed partial class DbBackupService(
    IAppDataPathProvider pathProvider,
    ILogger<DbBackupService> logger) : IDbBackupService
{
    private const string DatabaseFileName = "file-data.db";
    private const string BackupFileName = "file-data.db.bak";

    /// <inheritdoc />
    public async Task<Result<bool, ErrorResponse>> BackupAsync(CancellationToken cancellationToken = default)
    {
        var dataFilePath = Path.Combine(pathProvider.AppDataDirectory, DatabaseFileName);

        if (!File.Exists(dataFilePath))
        {
            LogBackupFileMissing(logger, dataFilePath);

            return new Result<bool, ErrorResponse>.Error(
                new ErrorResponse($"Database file not found at '{dataFilePath}'. Cannot create backup."));
        }

        var backupFilePath = Path.Combine(pathProvider.AppDataDirectory, BackupFileName);

        await using FileStream source = File.OpenRead(dataFilePath);
        await using FileStream destination = File.Open(backupFilePath, FileMode.Create, FileAccess.Write);
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);

        LogBackupComplete(logger, backupFilePath);

        return new Result<bool, ErrorResponse>.Ok(true);
    }

    // Source-generated log methods — zero-allocation, structured, CA1848-compliant.
    [LoggerMessage(Level = LogLevel.Error, Message = "Backup failed: database file not found at '{Path}'")]
    private static partial void LogBackupFileMissing(MelILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database backed up to '{BackupPath}'")]
    private static partial void LogBackupComplete(MelILogger logger, string backupPath);
}
