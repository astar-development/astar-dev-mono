using System;
using System.Linq;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Features.Activity;
using AStar.Dev.Sync.Engine.Features.Activity;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Activity;

public sealed class GivenAnActivityFeedService : IDisposable
{
    private readonly ActivityFeedService _sut = new();

    [Fact]
    public void when_51_items_are_reported_then_snapshot_contains_exactly_50()
    {
        for (var i = 0; i < 51; i++)
            _sut.Report("acc-1", ActivityActionType.Downloaded, $"/file-{i}.txt");

        var result = _sut.GetSnapshot();

        _ = result.Match(
            onSome: items => { items.Count.ShouldBe(50); return 0; },
            onNone: () => { throw new InvalidOperationException("Expected Some but got None."); });
    }

    [Fact]
    public void when_51_items_are_reported_then_newest_item_is_first()
    {
        for (var i = 0; i < 51; i++)
            _sut.Report("acc-1", ActivityActionType.Downloaded, $"/file-{i}.txt");

        var result = _sut.GetSnapshot();

        _ = result.Match(
            onSome: items => { items[0].FilePath.ShouldBe("/file-50.txt"); return 0; },
            onNone: () => { throw new InvalidOperationException("Expected Some but got None."); });
    }

    [Fact]
    public void when_no_items_have_been_reported_then_snapshot_is_none()
    {
        var result = _sut.GetSnapshot();

        result.ShouldBe(Option<System.Collections.Generic.IReadOnlyList<ActivityItem>>.None.Instance);
    }

    [Fact]
    public void when_item_is_reported_then_activity_stream_emits_the_item()
    {
        ActivityItem? received = null;

        using var _ = _sut.ActivityStream.Subscribe(item => received = item);

        _sut.Report("acc-1", ActivityActionType.Uploaded, "/doc.docx");

        received.ShouldNotBeNull();
        received.AccountId.ShouldBe("acc-1");
        received.ActionType.ShouldBe(ActivityActionType.Uploaded);
        received.FilePath.ShouldBe("/doc.docx");
    }

    [Fact]
    public void when_item_is_reported_then_snapshot_contains_it()
    {
        _sut.Report("acc-1", ActivityActionType.Downloaded, "/photo.jpg");

        var result = _sut.GetSnapshot();

        _ = result.Match(
            onSome: items => { items.ShouldContain(item => item.FilePath == "/photo.jpg"); return 0; },
            onNone: () => { throw new InvalidOperationException("Expected Some but got None."); });
    }

    public void Dispose() => _sut.Dispose();
}
