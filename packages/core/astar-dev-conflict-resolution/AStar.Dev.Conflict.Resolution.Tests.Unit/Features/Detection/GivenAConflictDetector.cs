using System;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using AStar.Dev.Conflict.Resolution.Domain;
using AStar.Dev.Conflict.Resolution.Features.Detection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStar.Dev.Conflict.Resolution.Tests.Unit.Features.Detection;

public sealed class GivenAConflictDetector
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly DateTimeOffset LastSyncTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task when_both_local_and_remote_were_modified_since_last_sync_then_conflict_is_detected()
    {
        const string filePath = "/files/document.txt";
        var remoteLastModified = LastSyncTime.AddMinutes(10);
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(filePath, new MockFileData("local content") { LastWriteTime = LastSyncTime.AddMinutes(5).DateTime });
        var sut = CreateSut(fileSystem);

        var result = await sut.DetectAsync(AccountId, filePath, remoteLastModified, 100, false, LastSyncTime, TestContext.Current.CancellationToken);

        var conflict = ((AStar.Dev.Functional.Extensions.Result<ConflictRecord?, ConflictDetectionError>.Ok)result).Value;
        conflict.ShouldNotBeNull();
        conflict!.ConflictType.ShouldBe(ConflictType.BothModified);
        conflict.FilePath.ShouldBe(filePath);
        conflict.AccountId.ShouldBe(AccountId);
    }

    [Fact]
    public async Task when_remote_is_deleted_and_local_file_exists_then_conflict_is_detected()
    {
        const string filePath = "/files/document.txt";
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(filePath, new MockFileData("local content"));
        var sut = CreateSut(fileSystem);

        var result = await sut.DetectAsync(AccountId, filePath, null, null, true, LastSyncTime, TestContext.Current.CancellationToken);

        var conflict = ((AStar.Dev.Functional.Extensions.Result<ConflictRecord?, ConflictDetectionError>.Ok)result).Value;
        conflict.ShouldNotBeNull();
        conflict!.ConflictType.ShouldBe(ConflictType.DeletedOnOneSide);
    }

    [Fact]
    public async Task when_only_remote_is_modified_and_local_is_unchanged_then_no_conflict_is_detected()
    {
        const string filePath = "/files/document.txt";
        var remoteLastModified = LastSyncTime.AddMinutes(10);
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(filePath, new MockFileData("local content") { LastWriteTime = LastSyncTime.AddMinutes(-30).DateTime });
        var sut = CreateSut(fileSystem);

        var result = await sut.DetectAsync(AccountId, filePath, remoteLastModified, 100, false, LastSyncTime, TestContext.Current.CancellationToken);

        var conflict = ((AStar.Dev.Functional.Extensions.Result<ConflictRecord?, ConflictDetectionError>.Ok)result).Value;
        conflict.ShouldBeNull();
    }

    [Fact]
    public async Task when_local_file_does_not_exist_then_no_conflict_is_detected()
    {
        const string filePath = "/files/document.txt";
        var remoteLastModified = LastSyncTime.AddMinutes(10);
        var fileSystem = new MockFileSystem();
        var sut = CreateSut(fileSystem);

        var result = await sut.DetectAsync(AccountId, filePath, remoteLastModified, 100, false, LastSyncTime, TestContext.Current.CancellationToken);

        var conflict = ((AStar.Dev.Functional.Extensions.Result<ConflictRecord?, ConflictDetectionError>.Ok)result).Value;
        conflict.ShouldBeNull();
    }

    [Fact]
    public async Task when_remote_is_deleted_and_local_does_not_exist_then_no_conflict_is_detected()
    {
        const string filePath = "/files/document.txt";
        var fileSystem = new MockFileSystem();
        var sut = CreateSut(fileSystem);

        var result = await sut.DetectAsync(AccountId, filePath, null, null, true, LastSyncTime, TestContext.Current.CancellationToken);

        var conflict = ((AStar.Dev.Functional.Extensions.Result<ConflictRecord?, ConflictDetectionError>.Ok)result).Value;
        conflict.ShouldBeNull();
    }

    private static ConflictDetector CreateSut(MockFileSystem fileSystem)
        => new(fileSystem, NullLogger<ConflictDetector>.Instance);
}
