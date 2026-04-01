using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Infrastructure.Persistence;

public sealed class GivenADbBackupService
{
    private const string AppDataDirectory = "/data/onedriveSync";
    private const string DatabaseFileName = "file-data.db";
    private const string BackupFileName   = "file-data.db.bak";

    private static readonly string DatabaseFilePath = Path.Combine(AppDataDirectory, DatabaseFileName);
    private static readonly string BackupFilePath   = Path.Combine(AppDataDirectory, BackupFileName);

    [Fact]
    public async Task when_database_file_does_not_exist_then_backup_returns_error()
    {
        var fileSystem    = Substitute.For<IFileSystem>();
        var pathProvider  = Substitute.For<IAppDataPathProvider>();
        var logger        = Substitute.For<ILogger<DbBackupService>>();

        pathProvider.AppDataDirectory.Returns(AppDataDirectory);
        fileSystem.File.Exists(DatabaseFilePath).Returns(false);

        var sut = new DbBackupService(pathProvider, logger, fileSystem);

        var result = await sut.BackupAsync();

        result.ShouldBeOfType<Result<bool, ErrorResponse>.Error>();
    }

    [Fact]
    public async Task when_database_file_exists_then_backup_returns_ok_true()
    {
        var fileSystem    = Substitute.For<IFileSystem>();
        var pathProvider  = Substitute.For<IAppDataPathProvider>();
        var logger        = Substitute.For<ILogger<DbBackupService>>();

        pathProvider.AppDataDirectory.Returns(AppDataDirectory);
        fileSystem.File.Exists(DatabaseFilePath).Returns(true);

        var sourceStream = new MemoryStream([1, 2, 3]);
        var destStream   = new MemoryStream();
        fileSystem.File.OpenRead(DatabaseFilePath).Returns(sourceStream);
        fileSystem.File.Open(BackupFilePath, FileMode.Create, FileAccess.Write).Returns(destStream);

        var sut = new DbBackupService(pathProvider, logger, fileSystem);

        var result = await sut.BackupAsync();

        result.ShouldBeOfType<Result<bool, ErrorResponse>.Ok>();
    }

    [Fact]
    public async Task when_database_file_exists_then_file_system_open_is_called_for_backup_path()
    {
        var fileSystem    = Substitute.For<IFileSystem>();
        var pathProvider  = Substitute.For<IAppDataPathProvider>();
        var logger        = Substitute.For<ILogger<DbBackupService>>();

        pathProvider.AppDataDirectory.Returns(AppDataDirectory);
        fileSystem.File.Exists(DatabaseFilePath).Returns(true);

        var sourceStream = new MemoryStream([1, 2, 3]);
        var destStream   = new MemoryStream();
        fileSystem.File.OpenRead(DatabaseFilePath).Returns(sourceStream);
        fileSystem.File.Open(BackupFilePath, FileMode.Create, FileAccess.Write).Returns(destStream);

        var sut = new DbBackupService(pathProvider, logger, fileSystem);

        _ = await sut.BackupAsync();

        fileSystem.File.Received(1).Open(BackupFilePath, FileMode.Create, FileAccess.Write);
    }
}
