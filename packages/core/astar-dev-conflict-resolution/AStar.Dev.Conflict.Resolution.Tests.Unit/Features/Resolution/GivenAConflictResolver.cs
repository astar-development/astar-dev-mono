using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using AStar.Dev.Conflict.Resolution.Domain;
using AStar.Dev.Conflict.Resolution.Features.Persistence;
using AStar.Dev.Conflict.Resolution.Features.Resolution;
using AStar.Dev.Functional.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStar.Dev.Conflict.Resolution.Tests.Unit.Features.Resolution;

public sealed class GivenAConflictResolver
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private const string FilePath = "/files/report.docx";

    [Fact]
    public void when_building_keep_both_path_then_timestamp_format_is_locale_invariant_utc()
    {
        var detectedAt = new DateTimeOffset(2026, 3, 15, 14, 30, 45, TimeSpan.Zero);

        var result = ConflictResolver.BuildKeepBothPath("/files/report.docx", detectedAt);

        result.ShouldBe(Path.Combine("/files", "report-(2026-03-15T143045Z).docx"));
    }

    [Fact]
    public async Task when_keep_both_is_applied_then_the_local_file_is_renamed()
    {
        var conflict = BuildConflict();
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(FilePath, new MockFileData("local content"));
        var store = BuildPassthroughStore(conflict);
        var sut = new ConflictResolver(store, fileSystem, NullLogger<ConflictResolver>.Instance);

        await sut.ResolveAsync(conflict, ResolutionStrategy.KeepBoth, TestContext.Current.CancellationToken);

        fileSystem.File.Exists(FilePath).ShouldBeFalse();
    }

    [Fact]
    public async Task when_skip_is_applied_then_conflict_remains_in_queue()
    {
        var conflict = BuildConflict();
        var store = Substitute.For<IConflictStore>();
        var sut = new ConflictResolver(store, new MockFileSystem(), NullLogger<ConflictResolver>.Instance);

        var result = await sut.ResolveAsync(conflict, ResolutionStrategy.Skip, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Result<ConflictRecord, ConflictResolverError>.Ok>();
        await store.DidNotReceive().ResolveAsync(Arg.Any<Guid>(), Arg.Any<ResolutionStrategy>(), Arg.Any<System.Threading.CancellationToken>());
        conflict.IsResolved.ShouldBeFalse();
    }

    [Fact]
    public async Task when_local_wins_is_applied_then_store_resolve_is_called()
    {
        var conflict = BuildConflict();
        var store = BuildPassthroughStore(conflict);
        var sut = new ConflictResolver(store, new MockFileSystem(), NullLogger<ConflictResolver>.Instance);

        await sut.ResolveAsync(conflict, ResolutionStrategy.LocalWins, TestContext.Current.CancellationToken);

        await store.Received(1).ResolveAsync(conflict.Id, ResolutionStrategy.LocalWins, Arg.Any<System.Threading.CancellationToken>());
    }

    [Fact]
    public async Task when_remote_wins_is_applied_then_store_resolve_is_called()
    {
        var conflict = BuildConflict();
        var store = BuildPassthroughStore(conflict);
        var sut = new ConflictResolver(store, new MockFileSystem(), NullLogger<ConflictResolver>.Instance);

        await sut.ResolveAsync(conflict, ResolutionStrategy.RemoteWins, TestContext.Current.CancellationToken);

        await store.Received(1).ResolveAsync(conflict.Id, ResolutionStrategy.RemoteWins, Arg.Any<System.Threading.CancellationToken>());
    }

    private static ConflictRecord BuildConflict()
        => ConflictRecordFactory.Create(AccountId, FilePath, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow, ConflictType.BothModified);

    private static IConflictStore BuildPassthroughStore(ConflictRecord conflict)
    {
        var store = Substitute.For<IConflictStore>();
        store.ResolveAsync(Arg.Any<Guid>(), Arg.Any<ResolutionStrategy>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(new Result<ConflictRecord, ConflictStoreError>.Ok(conflict));

        return store;
    }
}
