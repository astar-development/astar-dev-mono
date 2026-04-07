using System;
using System.Linq;
using System.Threading.Tasks;
using AStar.Dev.Conflict.Resolution.Domain;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Features.Conflicts;
using AStar.Dev.OneDriveSync.Tests.Integration.Helpers;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Persistence;

public sealed class GivenAConflictStore
{
    [Fact]
    public async Task when_a_conflict_is_added_then_it_appears_in_the_pending_queue()
    {
        await using var factory = AppDbContextFactory.Create();
        var store = new ConflictStore(new TestDbContextFactory(factory));
        var conflict = new ConflictRecordBuilder().Build();

        await store.AddAsync(conflict, TestContext.Current.CancellationToken);

        var pendingResult = await store.GetPendingAsync(TestContext.Current.CancellationToken);
        var pending = ((Result<System.Collections.Generic.IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok)pendingResult).Value;
        pending.ShouldHaveSingleItem();
        pending[0].Id.ShouldBe(conflict.Id);
    }

    [Fact]
    public async Task when_a_conflict_is_resolved_then_the_queue_is_empty()
    {
        await using var factory = AppDbContextFactory.Create();
        var store = new ConflictStore(new TestDbContextFactory(factory));
        var conflict = new ConflictRecordBuilder().Build();
        await store.AddAsync(conflict, TestContext.Current.CancellationToken);

        await store.ResolveAsync(conflict.Id, ResolutionStrategy.LocalWins, TestContext.Current.CancellationToken);

        var pendingResult = await store.GetPendingAsync(TestContext.Current.CancellationToken);
        var pending = ((Result<System.Collections.Generic.IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok)pendingResult).Value;
        pending.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_one_of_many_conflicts_is_resolved_then_only_that_conflict_is_removed_from_queue()
    {
        await using var factory = AppDbContextFactory.Create();
        var store = new ConflictStore(new TestDbContextFactory(factory));
        var conflict1 = new ConflictRecordBuilder().WithFilePath("/files/a.txt").Build();
        var conflict2 = new ConflictRecordBuilder().WithFilePath("/files/b.txt").Build();
        await store.AddAsync(conflict1, TestContext.Current.CancellationToken);
        await store.AddAsync(conflict2, TestContext.Current.CancellationToken);

        await store.ResolveAsync(conflict1.Id, ResolutionStrategy.RemoteWins, TestContext.Current.CancellationToken);

        var pendingResult = await store.GetPendingAsync(TestContext.Current.CancellationToken);
        var pending = ((Result<System.Collections.Generic.IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok)pendingResult).Value;
        pending.ShouldHaveSingleItem();
        pending[0].Id.ShouldBe(conflict2.Id);
    }

    [Fact]
    public async Task when_pending_conflicts_are_queried_then_newest_is_first()
    {
        await using var factory = AppDbContextFactory.Create();
        var store = new ConflictStore(new TestDbContextFactory(factory));
        var conflict1 = new ConflictRecordBuilder().WithFilePath("/files/older.txt").Build();
        await Task.Delay(10, TestContext.Current.CancellationToken);
        var conflict2 = new ConflictRecordBuilder().WithFilePath("/files/newer.txt").Build();
        await store.AddAsync(conflict1, TestContext.Current.CancellationToken);
        await store.AddAsync(conflict2, TestContext.Current.CancellationToken);

        var pendingResult = await store.GetPendingAsync(TestContext.Current.CancellationToken);
        var pending = ((Result<System.Collections.Generic.IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok)pendingResult).Value;

        pending[0].Id.ShouldBe(conflict2.Id);
    }

    [Fact]
    public async Task when_queue_survives_simulated_crash_then_pending_conflicts_are_recoverable()
    {
        await using var factory = AppDbContextFactory.Create();
        var store = new ConflictStore(new TestDbContextFactory(factory));
        var conflict = new ConflictRecordBuilder().Build();
        await store.AddAsync(conflict, TestContext.Current.CancellationToken);

        var secondStore = new ConflictStore(new TestDbContextFactory(factory));
        var pendingResult = await secondStore.GetPendingAsync(TestContext.Current.CancellationToken);
        var pending = ((Result<System.Collections.Generic.IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok)pendingResult).Value;

        pending.ShouldHaveSingleItem();
        pending[0].Id.ShouldBe(conflict.Id);
    }

    [Fact]
    public async Task when_get_by_file_path_is_called_then_only_conflicts_for_that_path_are_returned()
    {
        const string targetPath = "/files/target.txt";
        await using var factory = AppDbContextFactory.Create();
        var store = new ConflictStore(new TestDbContextFactory(factory));
        var targetConflict = new ConflictRecordBuilder().WithFilePath(targetPath).Build();
        var otherConflict  = new ConflictRecordBuilder().WithFilePath("/files/other.txt").Build();
        await store.AddAsync(targetConflict, TestContext.Current.CancellationToken);
        await store.AddAsync(otherConflict, TestContext.Current.CancellationToken);

        var result = await store.GetByFilePathAsync(targetPath, TestContext.Current.CancellationToken);
        var conflicts = ((Result<System.Collections.Generic.IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok)result).Value;

        conflicts.ShouldHaveSingleItem();
        conflicts[0].FilePath.ShouldBe(targetPath);
    }
}
