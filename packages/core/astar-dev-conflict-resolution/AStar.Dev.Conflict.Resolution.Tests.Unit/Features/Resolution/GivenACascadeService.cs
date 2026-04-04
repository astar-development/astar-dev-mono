using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AStar.Dev.Conflict.Resolution.Domain;
using AStar.Dev.Conflict.Resolution.Features.Persistence;
using AStar.Dev.Conflict.Resolution.Features.Resolution;
using AStar.Dev.Functional.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStar.Dev.Conflict.Resolution.Tests.Unit.Features.Resolution;

public sealed class GivenACascadeService
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private const string FilePath = "/files/shared.xlsx";

    [Fact]
    public async Task when_a_conflict_is_resolved_then_cascade_applies_to_all_matching_pending_conflicts()
    {
        var resolvedConflict = BuildConflict();
        var otherConflict1   = BuildConflict();
        var otherConflict2   = BuildConflict();

        var store = Substitute.For<IConflictStore>();
        store.GetByFilePathAsync(FilePath, Arg.Any<System.Threading.CancellationToken>())
            .Returns(new Result<IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok([resolvedConflict, otherConflict1, otherConflict2]));
        store.ResolveAsync(Arg.Any<Guid>(), Arg.Any<ResolutionStrategy>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(new Result<ConflictRecord, ConflictStoreError>.Ok(resolvedConflict));

        var sut = new CascadeService(store, NullLogger<CascadeService>.Instance);

        var result = await sut.ApplyCascadeAsync(resolvedConflict.Id, FilePath, ResolutionStrategy.LocalWins, TestContext.Current.CancellationToken);

        var count = ((Result<int, ConflictStoreError>.Ok)result).Value;
        count.ShouldBe(2);
    }

    [Fact]
    public async Task when_no_other_pending_conflicts_share_the_same_file_path_then_cascade_count_is_zero()
    {
        var resolvedConflict = BuildConflict();

        var store = Substitute.For<IConflictStore>();
        store.GetByFilePathAsync(FilePath, Arg.Any<System.Threading.CancellationToken>())
            .Returns(new Result<IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok([resolvedConflict]));

        var sut = new CascadeService(store, NullLogger<CascadeService>.Instance);

        var result = await sut.ApplyCascadeAsync(resolvedConflict.Id, FilePath, ResolutionStrategy.RemoteWins, TestContext.Current.CancellationToken);

        var count = ((Result<int, ConflictStoreError>.Ok)result).Value;
        count.ShouldBe(0);
    }

    [Fact]
    public async Task when_cascading_then_the_triggering_conflict_itself_is_excluded()
    {
        var resolvedConflict = BuildConflict();
        var otherConflict    = BuildConflict();

        var store = Substitute.For<IConflictStore>();
        store.GetByFilePathAsync(FilePath, Arg.Any<System.Threading.CancellationToken>())
            .Returns(new Result<IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok([resolvedConflict, otherConflict]));
        store.ResolveAsync(Arg.Any<Guid>(), Arg.Any<ResolutionStrategy>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(new Result<ConflictRecord, ConflictStoreError>.Ok(otherConflict));

        var sut = new CascadeService(store, NullLogger<CascadeService>.Instance);

        await sut.ApplyCascadeAsync(resolvedConflict.Id, FilePath, ResolutionStrategy.KeepBoth, TestContext.Current.CancellationToken);

        await store.DidNotReceive().ResolveAsync(resolvedConflict.Id, Arg.Any<ResolutionStrategy>(), Arg.Any<System.Threading.CancellationToken>());
    }

    private static ConflictRecord BuildConflict()
        => ConflictRecordFactory.Create(AccountId, FilePath, DateTimeOffset.UtcNow.AddMinutes(-10), DateTimeOffset.UtcNow, ConflictType.BothModified);
}
