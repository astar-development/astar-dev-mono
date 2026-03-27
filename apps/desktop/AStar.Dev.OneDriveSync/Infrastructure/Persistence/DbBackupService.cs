using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     Copies <c>data.db</c> to <c>data.db.bak</c> before any sync mutation begins (AC EH-07).
///
///     The backup is triggered on explicit call only — never on construction, never periodically.
/// </summary>
public sealed class DbBackupService(IAppDataPathProvider pathProvider) : IDbBackupService
{
    private const string DatabaseFileName = "data.db";
    private const string BackupFileName   = "data.db.bak";

    /// <inheritdoc />
    public async Task<Result<bool, ErrorResponse>> BackupAsync()
    {
        var dataFilePath = Path.Combine(pathProvider.AppDataDirectory, DatabaseFileName);

        if (!File.Exists(dataFilePath))
        {

            return new Result<bool, ErrorResponse>.Error(
                new ErrorResponse($"Database file not found at '{dataFilePath}'. Cannot create backup."));
        }

        var backupFilePath = Path.Combine(pathProvider.AppDataDirectory, BackupFileName);

        await using var source      = File.OpenRead(dataFilePath);
        await using var destination = File.Open(backupFilePath, FileMode.Create, FileAccess.Write);
        await source.CopyToAsync(destination).ConfigureAwait(false);

        return new Result<bool, ErrorResponse>.Ok(true);
    }
}
