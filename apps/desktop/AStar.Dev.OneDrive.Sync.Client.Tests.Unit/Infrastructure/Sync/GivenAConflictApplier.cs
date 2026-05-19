using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;
using ReactiveUnit = global::System.Reactive.Unit;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenAConflictApplier
{
    private readonly IGraphService   _graphService   = Substitute.For<IGraphService>();
    private readonly IHttpDownloader _httpDownloader = Substitute.For<IHttpDownloader>();
    private readonly IFileSystem     _fileSystem     = Substitute.For<IFileSystem>();

    private ConflictApplier CreateSut() => new(_httpDownloader, _graphService, _fileSystem);

    private static SyncConflict CreateUseRemoteConflict() => new()
    {
        Remote   = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId("item-1")),
        Target   = SyncFileTargetFactory.Create("/local/path/file.txt", "file.txt"),
        Snapshot = ConflictSnapshotFactory.Create(DateTimeOffset.UtcNow.AddMinutes(-5), 0L, DateTimeOffset.UtcNow, 0L)
    };

    [Fact]
    public void when_constructed_then_instance_is_not_null()
    {
        var sut = CreateSut();

        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_outcome_is_use_remote_and_url_resolution_succeeds_and_download_succeeds_then_returns_true()
    {
        _graphService.GetDownloadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<string, string>.Ok("https://example.com/file.txt"));
        _httpDownloader.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<ReactiveUnit, string>.Ok(ReactiveUnit.Default));

        var result = await CreateSut().ApplyAsync(CreateUseRemoteConflict(), ConflictOutcome.UseRemote, "user-1", "token", TestContext.Current.CancellationToken);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task when_outcome_is_use_remote_and_url_resolution_fails_then_returns_false()
    {
        _graphService.GetDownloadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<string, string>.Error("url-resolution-failed"));

        var result = await CreateSut().ApplyAsync(CreateUseRemoteConflict(), ConflictOutcome.UseRemote, "user-1", "token", TestContext.Current.CancellationToken);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task when_outcome_is_use_remote_and_download_fails_then_returns_false()
    {
        _graphService.GetDownloadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<string, string>.Ok("https://example.com/file.txt"));
        _httpDownloader.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<ReactiveUnit, string>.Error("download-failed"));

        var result = await CreateSut().ApplyAsync(CreateUseRemoteConflict(), ConflictOutcome.UseRemote, "user-1", "token", TestContext.Current.CancellationToken);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task when_outcome_is_use_remote_and_url_resolution_fails_then_downloader_is_not_called()
    {
        _graphService.GetDownloadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<string, string>.Error("url-resolution-failed"));

        await CreateSut().ApplyAsync(CreateUseRemoteConflict(), ConflictOutcome.UseRemote, "user-1", "token", TestContext.Current.CancellationToken);

        await _httpDownloader.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_outcome_is_keep_both_and_local_file_exists_then_file_is_moved()
    {
        var mockPath = Substitute.For<IPath>();
        mockPath.GetDirectoryName(Arg.Any<string>()).Returns("/local/path");
        mockPath.GetFileNameWithoutExtension(Arg.Any<string>()).Returns("file");
        mockPath.GetExtension(Arg.Any<string>()).Returns(".txt");
        mockPath.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns("/local/path/file (local 2024-01-01 00-00).txt");
        _fileSystem.Path.Returns(mockPath);

        var mockFile = Substitute.For<IFile>();
        mockFile.Exists(Arg.Any<string>()).Returns(true);
        _fileSystem.File.Returns(mockFile);

        var conflict = new SyncConflict
        {
            Remote   = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty)),
            Target   = SyncFileTargetFactory.Create("/local/path/file.txt", "file.txt"),
            Snapshot = ConflictSnapshotFactory.Create(DateTimeOffset.UtcNow, 0L, DateTimeOffset.UtcNow, 0L)
        };

        var result = await CreateSut().ApplyAsync(conflict, ConflictOutcome.KeepBoth, "user-1", "token", TestContext.Current.CancellationToken);

        result.ShouldBeTrue();
        mockFile.Received(1).Move(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task when_outcome_is_keep_both_and_local_file_does_not_exist_then_file_is_not_moved()
    {
        var mockPath = Substitute.For<IPath>();
        mockPath.GetDirectoryName(Arg.Any<string>()).Returns("/local/path");
        mockPath.GetFileNameWithoutExtension(Arg.Any<string>()).Returns("file");
        mockPath.GetExtension(Arg.Any<string>()).Returns(".txt");
        mockPath.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns("/local/path/file (local 2024-01-01 00-00).txt");
        _fileSystem.Path.Returns(mockPath);

        var mockFile = Substitute.For<IFile>();
        mockFile.Exists(Arg.Any<string>()).Returns(false);
        _fileSystem.File.Returns(mockFile);

        var conflict = new SyncConflict
        {
            Remote   = RemoteItemRefFactory.Create(new AccountId("user-1"), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty)),
            Target   = SyncFileTargetFactory.Create("/local/path/file.txt", "file.txt"),
            Snapshot = ConflictSnapshotFactory.Create(DateTimeOffset.UtcNow, 0L, DateTimeOffset.UtcNow, 0L)
        };

        var result = await CreateSut().ApplyAsync(conflict, ConflictOutcome.KeepBoth, "user-1", "token", TestContext.Current.CancellationToken);

        result.ShouldBeTrue();
        mockFile.DidNotReceive().Move(Arg.Any<string>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData(ConflictOutcome.UseLocal)]
    [InlineData(ConflictOutcome.Skip)]
    public async Task when_outcome_is_not_requiring_action_then_returns_true(ConflictOutcome outcome)
    {
        var result = await CreateSut().ApplyAsync(CreateUseRemoteConflict(), outcome, "user-1", "token", TestContext.Current.CancellationToken);

        result.ShouldBeTrue();
    }
}
