using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Persistence;

public sealed class GivenADatabaseReadyForBackup : IAsyncLifetime
{
    private readonly string _tempRoot =
        Path.Combine(Path.GetTempPath(), "AStar.Dev.OneDriveSync.Tests", Guid.NewGuid().ToString());

    public ValueTask InitializeAsync()
    {
        _ = Directory.CreateDirectory(_tempRoot);

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Directory.Delete(_tempRoot, recursive: true);

        return ValueTask.CompletedTask;
    }

    private DbBackupService CreateSut()
    {
        var pathProvider = Substitute.For<IAppDataPathProvider>();
        _ = pathProvider.AppDataDirectory.Returns(_tempRoot);
        var logger = Substitute.For<ILogger<DbBackupService>>();

        return new DbBackupService(pathProvider, logger, new FileSystem());
    }

    [Fact]
    public async Task when_backup_is_requested_then_a_dot_bak_file_is_created_alongside_the_database()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempRoot, "file-data.db"), "SQLite placeholder", TestContext.Current.CancellationToken);

        var result = await CreateSut().BackupAsync(TestContext.Current.CancellationToken);

        _ = result.ShouldBeOfType<Result<bool, ErrorResponse>.Ok>();
        File.Exists(Path.Combine(_tempRoot, "file-data.db.bak")).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_database_file_is_absent_then_backup_returns_a_failure_result()
    {
        var result = await CreateSut().BackupAsync(TestContext.Current.CancellationToken);

        _ = result.ShouldBeOfType<Result<bool, ErrorResponse>.Error>();
    }

    [Fact]
    public async Task when_a_stale_backup_exists_then_it_is_replaced_with_the_current_database()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempRoot, "file-data.db"), "current database content", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_tempRoot, "file-data.db.bak"), "stale backup", TestContext.Current.CancellationToken);

        var result = await CreateSut().BackupAsync(TestContext.Current.CancellationToken);

        _ = result.ShouldBeOfType<Result<bool, ErrorResponse>.Ok>();
        string backupContent = await File.ReadAllTextAsync(Path.Combine(_tempRoot, "file-data.db.bak"), TestContext.Current.CancellationToken);
        backupContent.ShouldBe("current database content");
    }

    [Fact]
    public void when_constructed_then_no_backup_is_created_passively()
    {
        _ = CreateSut();

        File.Exists(Path.Combine(_tempRoot, "file-data.db.bak")).ShouldBeFalse();
    }
}
