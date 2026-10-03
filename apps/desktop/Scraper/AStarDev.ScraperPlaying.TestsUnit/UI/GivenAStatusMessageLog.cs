using AStarDev.ScraperPlaying.UI;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenAStatusMessageLog
{
    [Fact]
    public void when_a_single_message_is_appended_then_text_is_that_message()
    {
        var log = new StatusMessageLog(maximumMessages: 100);

        _ = log.Append("first message");

        log.Text.ShouldBe("first message");
    }

    [Fact]
    public void when_multiple_messages_are_appended_then_text_joins_them_with_newlines_oldest_first()
    {
        var log = new StatusMessageLog(maximumMessages: 100);

        _ = log.Append("first message");
        _ = log.Append("second message");
        _ = log.Append("third message");

        log.Text.ShouldBe($"first message{Environment.NewLine}second message{Environment.NewLine}third message");
    }

    [Fact]
    public void when_appending_beyond_capacity_then_the_oldest_message_is_discarded()
    {
        var log = new StatusMessageLog(maximumMessages: 2);

        _ = log.Append("first message");
        _ = log.Append("second message");
        _ = log.Append("third message");

        log.Text.ShouldBe($"second message{Environment.NewLine}third message");
    }

    [Fact]
    public void when_the_text_is_read_repeatedly_without_appending_then_the_same_text_is_returned_without_being_rebuilt()
    {
        var log = new StatusMessageLog(maximumMessages: 100);
        _ = log.Append("first message");
        _ = log.Append("second message");

        var first = log.Text;
        var second = log.Text;

        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void when_a_message_is_appended_after_the_text_was_read_then_the_text_includes_it()
    {
        var log = new StatusMessageLog(maximumMessages: 100);
        _ = log.Append("first message");
        _ = log.Text;

        _ = log.Append("second message");

        log.Text.ShouldBe($"first message{Environment.NewLine}second message");
    }

    [Fact]
    public void when_the_first_message_is_appended_then_a_refresh_is_required()
    {
        var log = new StatusMessageLog(maximumMessages: 100);

        var refreshRequired = log.Append("first message");

        refreshRequired.ShouldBeTrue();
    }

    [Fact]
    public void when_messages_are_appended_before_the_text_is_read_then_only_the_first_requires_a_refresh()
    {
        var log = new StatusMessageLog(maximumMessages: 100);

        var required = new[] { log.Append("first message"), log.Append("second message"), log.Append("third message") };

        required.ShouldBe([true, false, false]);
    }

    [Fact]
    public void when_the_text_is_read_then_the_next_append_requires_a_refresh_again()
    {
        var log = new StatusMessageLog(maximumMessages: 100);
        _ = log.Append("first message");
        _ = log.Text;

        var refreshRequired = log.Append("second message");

        refreshRequired.ShouldBeTrue();
    }

    [Fact]
    public async Task when_messages_are_appended_from_many_threads_then_the_newest_messages_up_to_capacity_are_retained()
    {
        var log = new StatusMessageLog(maximumMessages: 10);

        await Task.WhenAll(Enumerable.Range(0, 8).Select(thread => Task.Run(() =>
        {
            for (var message = 0; message < 500; message++)
            {
                _ = log.Append($"thread {thread} message {message}");
            }
        }, TestContext.Current.CancellationToken)));

        log.Text.Split(Environment.NewLine).Length.ShouldBe(10);
    }
}
