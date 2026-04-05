using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Features.Activity;
using AStar.Dev.Sync.Engine.Features.Activity;
using Microsoft.Reactive.Testing;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Activity;

public sealed class GivenAnActivityViewModel : IDisposable
{
    private readonly IScheduler _originalMainScheduler = RxApp.MainThreadScheduler;
    private readonly IActivityFeedService _feedService = Substitute.For<IActivityFeedService>();
    private readonly Subject<ActivityItem> _subject = new();
    private readonly TestScheduler _testScheduler = new();

    public GivenAnActivityViewModel()
    {
        RxApp.MainThreadScheduler = ImmediateScheduler.Instance;

        _feedService.ActivityStream.Returns(_subject);
        _feedService.GetSnapshot().Returns(Option<IReadOnlyList<ActivityItem>>.None.Instance);
    }

    [Fact]
    public void when_constructed_with_empty_feed_then_items_collection_is_empty()
    {
        var sut = CreateSut();

        sut.Items.ShouldBeEmpty();
    }

    [Fact]
    public void when_constructed_with_empty_feed_then_is_empty_is_true()
    {
        var sut = CreateSut();

        sut.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void when_item_arrives_via_stream_then_items_collection_is_updated()
    {
        var sut = CreateSut();

        _subject.OnNext(ActivityItemFactory.Create("acc-1", DateTimeOffset.UtcNow, ActivityActionType.Downloaded, "/file.txt"));
        _testScheduler.AdvanceBy(TimeSpan.FromMilliseconds(200).Ticks);

        sut.Items.ShouldNotBeEmpty();
    }

    [Fact]
    public void when_item_arrives_via_stream_then_is_empty_becomes_false()
    {
        var sut = CreateSut();

        _subject.OnNext(ActivityItemFactory.Create("acc-1", DateTimeOffset.UtcNow, ActivityActionType.Downloaded, "/file.txt"));
        _testScheduler.AdvanceBy(TimeSpan.FromMilliseconds(200).Ticks);

        sut.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void when_snapshot_contains_items_then_items_collection_is_pre_populated()
    {
        var existingItem = ActivityItemFactory.Create("acc-1", DateTimeOffset.UtcNow.AddMinutes(-5), ActivityActionType.Uploaded, "/old.txt");
        _feedService.GetSnapshot().Returns(new Option<IReadOnlyList<ActivityItem>>.Some([existingItem]));

        var sut = CreateSut();

        sut.Items.Count.ShouldBe(1);
    }

    [Fact]
    public void when_new_item_arrives_then_it_is_prepended_before_existing_items()
    {
        var older = ActivityItemFactory.Create("acc-1", DateTimeOffset.UtcNow.AddMinutes(-5), ActivityActionType.Uploaded, "/old.txt");
        _feedService.GetSnapshot().Returns(new Option<IReadOnlyList<ActivityItem>>.Some([older]));

        var sut = CreateSut();

        var newer = ActivityItemFactory.Create("acc-1", DateTimeOffset.UtcNow, ActivityActionType.Downloaded, "/new.txt");
        _subject.OnNext(newer);
        _testScheduler.AdvanceBy(TimeSpan.FromMilliseconds(200).Ticks);

        sut.Items[0].FilePath.ShouldBe("/new.txt");
    }

    [Fact]
    public void when_disposed_then_subsequent_stream_items_do_not_update_collection()
    {
        var sut = CreateSut();

        sut.Dispose();
        _subject.OnNext(ActivityItemFactory.Create("acc-1", DateTimeOffset.UtcNow, ActivityActionType.Downloaded, "/after-dispose.txt"));
        _testScheduler.AdvanceBy(TimeSpan.FromMilliseconds(200).Ticks);

        sut.Items.ShouldBeEmpty();
    }

    public void Dispose()
    {
        RxApp.MainThreadScheduler = _originalMainScheduler;
        _subject.Dispose();
    }

    private ActivityViewModel CreateSut() => new(_feedService, _testScheduler);
}
