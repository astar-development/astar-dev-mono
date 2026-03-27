using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Persistence;

/// <summary>
///     Verifies AC EH-07: "<see cref="IDbBackupService" /> implementation copies
///     <c>data.db</c> to <c>data.db.bak</c> before any sync mutation begins".
///
///     These are lightweight file-system integration tests — no database is opened;
///     only the backup copy logic is exercised.
/// </summary>
public sealed class DbBackupServiceShould
{
    private static readonly string TempRoot =
        Path.Combine(Path.GetTempPath(), "AStar.Dev.OneDriveSync.Tests", Guid.NewGuid().ToString());

    [Fact]
    public async Task ReturnSuccessWhenDataFileExistsAndBackupIsCreated()
    {
        // Arrange
        Directory.CreateDirectory(TempRoot);
        var dataFile   = Path.Combine(TempRoot, "data.db");
        var backupFile = Path.Combine(TempRoot, "data.db.bak");
        await File.WriteAllTextAsync(dataFile, "SQLite placeholder");

        try
        {
            var pathProvider = Substitute.For<IAppDataPathProvider>();
            pathProvider.AppDataDirectory.Returns(TempRoot);

            var sut = new DbBackupService(pathProvider);

            // Act
            var result = await sut.BackupAsync();

            // Assert
            result.ShouldBeOfType<Result<bool, ErrorResponse>.Ok>();
            result.Match(ok => ok, _ => false).ShouldBeTrue();
            File.Exists(backupFile).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(TempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task ReturnFailureWhenDataFileDoesNotExist()
    {
        // Arrange — directory exists but data.db is absent
        Directory.CreateDirectory(TempRoot);
        try
        {
            var pathProvider = Substitute.For<IAppDataPathProvider>();
            pathProvider.AppDataDirectory.Returns(TempRoot);

            var sut = new DbBackupService(pathProvider);

            // Act
            var result = await sut.BackupAsync();

            // Assert
            result.ShouldBeOfType<Result<bool, ErrorResponse>.Error>();
        }
        finally
        {
            Directory.Delete(TempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task OverwriteExistingBackupFile()
    {
        // Arrange — stale .bak file already exists; backup must replace it
        Directory.CreateDirectory(TempRoot);
        var dataFile   = Path.Combine(TempRoot, "data.db");
        var backupFile = Path.Combine(TempRoot, "data.db.bak");
        await File.WriteAllTextAsync(dataFile,   "current database content");
        await File.WriteAllTextAsync(backupFile, "stale backup");

        try
        {
            var pathProvider = Substitute.For<IAppDataPathProvider>();
            pathProvider.AppDataDirectory.Returns(TempRoot);

            var sut = new DbBackupService(pathProvider);

            // Act
            var result = await sut.BackupAsync();

            // Assert — success and .bak now matches current data.db
            result.ShouldBeOfType<Result<bool, ErrorResponse>.Ok>();
            var backupContent = await File.ReadAllTextAsync(backupFile);
            backupContent.ShouldBe("current database content");
        }
        finally
        {
            Directory.Delete(TempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task NotTriggerBackupOnConstruction()
    {
        // Arrange — backup is NOT triggered at construction time; only on explicit call
        Directory.CreateDirectory(TempRoot);
        var backupFile = Path.Combine(TempRoot, "data.db.bak");

        try
        {
            var pathProvider = Substitute.For<IAppDataPathProvider>();
            pathProvider.AppDataDirectory.Returns(TempRoot);

            // Act — construct without calling BackupAsync
            _ = new DbBackupService(pathProvider);

            // Assert — no backup file created passively
            File.Exists(backupFile).ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(TempRoot, recursive: true);
        }
    }
}
