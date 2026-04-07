using System;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Features.Activity;
using AStar.Dev.Sync.Engine.Features.Activity;
using Shouldly;
using Xunit;

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
    public void when_item_is_reported_then_activity_stream_emits_a_non_null_item()
    {
        ActivityItem? received = null;

        using var subscription = _sut.ActivityStream.Subscribe(item => received = item);

        _sut.Report("acc-1", ActivityActionType.Uploaded, "/doc.docx");

        received.ShouldNotBeNull();
    }

    [Fact]
    public void when_item_is_reported_then_emitted_item_has_correct_account_id()
    {
        ActivityItem? received = null;

        using var subscription = _sut.ActivityStream.Subscribe(item => received = item);

        _sut.Report("acc-1", ActivityActionType.Uploaded, "/doc.docx");

        received!.AccountId.ShouldBe("acc-1");
    }

    [Fact]
    public void when_item_is_reported_then_emitted_item_has_correct_action_type()
    {
        ActivityItem? received = null;

        using var subscription = _sut.ActivityStream.Subscribe(item => received = item);

        _sut.Report("acc-1", ActivityActionType.Uploaded, "/doc.docx");

        received!.ActionType.ShouldBe(ActivityActionType.Uploaded);
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

    [Fact]
    public void when_report_is_called_with_null_account_id_then_throws_argument_exception()
        => Should.Throw<ArgumentException>(() => _sut.Report(null!, ActivityActionType.Downloaded, "/file.txt"));

    [Fact]
    public void when_report_is_called_with_whitespace_account_id_then_throws_argument_exception()
        => Should.Throw<ArgumentException>(() => _sut.Report("   ", ActivityActionType.Downloaded, "/file.txt"));

    [Fact]
    public void when_report_is_called_with_null_file_path_then_throws_argument_exception()
        => Should.Throw<ArgumentException>(() => _sut.Report("acc-1", ActivityActionType.Downloaded, null!));

    [Fact]
    public void when_report_is_called_with_whitespace_file_path_then_throws_argument_exception()
        => Should.Throw<ArgumentException>(() => _sut.Report("acc-1", ActivityActionType.Downloaded, "   "));

    public void Dispose() => _sut.Dispose();
}
