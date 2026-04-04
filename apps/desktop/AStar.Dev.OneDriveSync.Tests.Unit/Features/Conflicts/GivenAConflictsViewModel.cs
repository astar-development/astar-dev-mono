using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Conflict.Resolution.Domain;
using AStar.Dev.Conflict.Resolution.Features.Persistence;
using AStar.Dev.Conflict.Resolution.Features.Resolution;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Features.Conflicts;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Conflicts;

public sealed class GivenAConflictsViewModel : IDisposable
{
    private readonly IConflictStore _store = Substitute.For<IConflictStore>();
    private readonly IConflictResolver _resolver = Substitute.For<IConflictResolver>();
    private readonly ICascadeService _cascade = Substitute.For<ICascadeService>();
    private readonly Subject<ConflictQueueChanged> _subject = new();

    public GivenAConflictsViewModel()
        => _store.ConflictQueueChanged.Returns(_subject);

    [Fact]
    public async Task when_loaded_with_pending_conflicts_then_conflicts_list_is_populated()
    {
        var conflict = BuildConflict();
        _store.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok([conflict]));

        var sut = CreateSut();
        await sut.LoadAsync(TestContext.Current.CancellationToken);

        sut.Conflicts.ShouldNotBeEmpty();
        sut.Conflicts[0].ConflictId.ShouldBe(conflict.Id);
    }

    [Fact]
    public async Task when_loaded_then_badge_count_equals_number_of_pending_conflicts()
    {
        var conflicts = new List<ConflictRecord> { BuildConflict(), BuildConflict(), BuildConflict() };
        _store.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok(conflicts));

        var sut = CreateSut();
        await sut.LoadAsync(TestContext.Current.CancellationToken);

        sut.BadgeCount.ShouldBe(3);
    }

    [Fact]
    public void when_a_conflict_added_event_arrives_then_badge_count_increments()
    {
        _store.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok([]));

        var sut = CreateSut();

        _subject.OnNext(new ConflictQueueChanged(Guid.NewGuid(), ConflictQueueChangeType.ConflictAdded, 1));

        sut.BadgeCount.ShouldBe(1);
    }

    [Fact]
    public void when_a_conflict_resolved_event_arrives_then_badge_count_decrements()
    {
        _store.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok([]));

        var sut = CreateSut();
        _subject.OnNext(new ConflictQueueChanged(Guid.NewGuid(), ConflictQueueChangeType.ConflictAdded, 2));

        _subject.OnNext(new ConflictQueueChanged(Guid.NewGuid(), ConflictQueueChangeType.ConflictResolved, 1));

        sut.BadgeCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_select_all_is_invoked_then_all_conflicts_are_selected()
    {
        var conflicts = new List<ConflictRecord> { BuildConflict(), BuildConflict() };
        _store.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok(conflicts));

        var sut = CreateSut();
        await sut.LoadAsync(TestContext.Current.CancellationToken);
        sut.SelectAllCommand.Execute().Subscribe();

        sut.Conflicts.ShouldAllBe(item => item.IsSelected);
    }

    public void Dispose() => _subject.Dispose();

    private ConflictsViewModel CreateSut() => new(_store, _resolver, _cascade);

    private static ConflictRecord BuildConflict()
        => ConflictRecordFactory.Create(Guid.NewGuid(), "/files/document.txt", DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow, ConflictType.BothModified);
}
