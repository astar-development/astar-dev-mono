using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Infrastructure.Persistence;

public sealed class GivenADbBackupService
{
    private const string AppDataDirectory = "/data/onedriveSync";
    private const string DatabaseFileName = "file-data.db";
    private const string BackupFileName   = "file-data.db.bak";

    private static readonly string _databaseFilePath = Path.Combine(AppDataDirectory, DatabaseFileName);
    private static readonly string _backupFilePath   = Path.Combine(AppDataDirectory, BackupFileName);

    [Fact]
    public async Task when_database_file_does_not_exist_then_backup_returns_error()
    {
        var fileSystem    = Substitute.For<IFileSystem>();
        var pathProvider  = Substitute.For<IAppDataPathProvider>();
        var logger        = Substitute.For<ILogger<DbBackupService>>();

        pathProvider.AppDataDirectory.Returns(AppDataDirectory);
        fileSystem.File.Exists(_databaseFilePath).Returns(false);

        var sut = new DbBackupService(pathProvider, logger, fileSystem);

        var result = await sut.BackupAsync(TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<bool, ErrorResponse>.Error>();
    }

    [Fact]
    public async Task when_database_file_exists_then_backup_returns_ok_true()
    {
        var fileSystem    = Substitute.For<IFileSystem>();
        var pathProvider  = Substitute.For<IAppDataPathProvider>();
        var logger        = Substitute.For<ILogger<DbBackupService>>();

        pathProvider.AppDataDirectory.Returns(AppDataDirectory);
        fileSystem.File.Exists(_databaseFilePath).Returns(true);

        var sourceStream = Substitute.For<FileSystemStream>(new MemoryStream([1, 2, 3]), _databaseFilePath, false);
        var destStream   = Substitute.For<FileSystemStream>(new MemoryStream(), _backupFilePath, false);
        fileSystem.File.OpenRead(_databaseFilePath).Returns(sourceStream);
        fileSystem.File.Open(_backupFilePath, FileMode.Create, FileAccess.Write).Returns(destStream);

        var sut = new DbBackupService(pathProvider, logger, fileSystem);

        var result = await sut.BackupAsync(TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<bool, ErrorResponse>.Ok>();
    }

    [Fact]
    public async Task when_database_file_exists_then_file_system_open_is_called_for_backup_path()
    {
        var fileSystem    = Substitute.For<IFileSystem>();
        var pathProvider  = Substitute.For<IAppDataPathProvider>();
        var logger        = Substitute.For<ILogger<DbBackupService>>();

        pathProvider.AppDataDirectory.Returns(AppDataDirectory);
        fileSystem.File.Exists(_databaseFilePath).Returns(true);

        var sourceStream = Substitute.For<FileSystemStream>(new MemoryStream([1, 2, 3]), _databaseFilePath, false);
        var destStream   = Substitute.For<FileSystemStream>(new MemoryStream(), _backupFilePath, false);
        fileSystem.File.OpenRead(_databaseFilePath).Returns(sourceStream);
        fileSystem.File.Open(_backupFilePath, FileMode.Create, FileAccess.Write).Returns(destStream);

        var sut = new DbBackupService(pathProvider, logger, fileSystem);

        _ = await sut.BackupAsync(TestContext.Current.CancellationToken);

        fileSystem.File.Received(1).Open(_backupFilePath, FileMode.Create, FileAccess.Write);
    }
}
