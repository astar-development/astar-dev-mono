using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using AStar.Dev.OneDriveSync.Infrastructure.Startup;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Startup;

public sealed class GivenADatabaseMigrationStartupTask : IAsyncLifetime
{
    private readonly string _testRoot =
        Path.Combine(Path.GetTempPath(), "AStar.Dev.OneDriveSync.Tests", Guid.NewGuid().ToString());

    // null! is safe: all fields are assigned in InitializeAsync, which xUnit
    // IAsyncLifetime guarantees runs before any test body executes.
    private SqliteConnection _inMemoryConnection = null!;
    private ServiceProvider  _serviceProvider    = null!;
    private string           _newAppDataDir      = null!;
    private string           _legacyParentDir    = null!;
    private string           _legacyDbPath       = null!;
    private string           _newDbPath          = null!;

    public ValueTask InitializeAsync()
    {
        _newAppDataDir   = Path.Combine(_testRoot, "new-app-data");
        _legacyParentDir = Path.Combine(_testRoot, "legacy-root");

        string legacyAppDataDir = Path.Combine(_legacyParentDir, "AStar.Dev.OneDriveSync");
        _legacyDbPath = Path.Combine(legacyAppDataDir, "file-data.db");
        _newDbPath    = Path.Combine(_newAppDataDir,   "file-data.db");

        _ = Directory.CreateDirectory(_newAppDataDir);
        _ = Directory.CreateDirectory(legacyAppDataDir);

        _inMemoryConnection = new SqliteConnection("DataSource=:memory:");
        _inMemoryConnection.Open();

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceProvider.DisposeAsync().ConfigureAwait(false);
        await _inMemoryConnection.DisposeAsync().ConfigureAwait(false);
        if (Directory.Exists(_testRoot))
            Directory.Delete(_testRoot, recursive: true);
    }

    private DatabaseMigrationStartupTask CreateSut(SqliteConnection connection)
    {
        var pathProvider = Substitute.For<IAppDataPathProvider>();
        pathProvider.AppDataDirectory.Returns(_newAppDataDir);

        var folderResolver = Substitute.For<ISpecialFolderResolver>();
        folderResolver.GetLocalApplicationDataPath().Returns(_legacyParentDir);

        var services = new ServiceCollection();
        services.AddSingleton(pathProvider);
        services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(connection));
        _serviceProvider = services.BuildServiceProvider();

        return new DatabaseMigrationStartupTask(_serviceProvider, Substitute.For<ILogger<DatabaseMigrationStartupTask>>(), new FileSystem(), folderResolver);
    }

    [Fact]
    public async Task when_neither_legacy_nor_new_database_exists_then_no_file_is_copied()
    {
        var sut = CreateSut(_inMemoryConnection);

        await sut.RunAsync(CancellationToken.None);

        File.Exists(_newDbPath).ShouldBeFalse();
    }

    [Fact]
    public async Task when_new_database_already_exists_then_legacy_file_content_is_not_overwritten()
    {
        await File.WriteAllTextAsync(_newDbPath, "existing-db", CancellationToken.None);
        await File.WriteAllTextAsync(_legacyDbPath, "legacy-db", CancellationToken.None);
        var sut = CreateSut(_inMemoryConnection);

        await sut.RunAsync(CancellationToken.None);

        string newContent = await File.ReadAllTextAsync(_newDbPath, CancellationToken.None);
        newContent.ShouldBe("existing-db");
    }

    [Fact]
    public async Task when_legacy_file_exists_and_new_file_does_not_then_legacy_file_is_copied()
    {
        await File.WriteAllTextAsync(_legacyDbPath, "legacy-content", CancellationToken.None);
        File.Delete(_newDbPath);
        var sut = CreateSut(_inMemoryConnection);

        await sut.RunAsync(CancellationToken.None);

        File.Exists(_newDbPath).ShouldBeTrue();
    }

    [Fact]
    public async Task when_legacy_file_is_copied_then_copied_file_has_identical_content()
    {
        string legacyContent = "legacy-content-to-copy";
        await File.WriteAllTextAsync(_legacyDbPath, legacyContent, CancellationToken.None);
        File.Delete(_newDbPath);
        var sut = CreateSut(_inMemoryConnection);

        await sut.RunAsync(CancellationToken.None);

        string copiedContent = await File.ReadAllTextAsync(_newDbPath, CancellationToken.None);
        copiedContent.ShouldBe(legacyContent);
    }

    [Fact]
    public async Task when_run_async_completes_then_database_migrations_have_been_applied()
    {
        var sut = CreateSut(_inMemoryConnection);

        await sut.RunAsync(CancellationToken.None);

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_inMemoryConnection).Options;
        await using var ctx = new AppDbContext(options);
        var appliedMigrations = await ctx.Database.GetAppliedMigrationsAsync(CancellationToken.None);

        appliedMigrations.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task when_run_async_completes_then_wal_journal_mode_is_enabled()
    {
        string dbFilePath = Path.Combine(_testRoot, "wal-test.db");
        await using var fileConnection = new SqliteConnection($"DataSource={dbFilePath}");
        fileConnection.Open();
        var sut = CreateSut(fileConnection);

        await sut.RunAsync(CancellationToken.None);

        await using var cmd = fileConnection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode";
        string? mode = (string?)cmd.ExecuteScalar();

        mode.ShouldBe("wal");
    }
}
