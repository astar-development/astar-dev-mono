using AStar.Dev.Wallpaper.Scrapper.Support;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenALogBroadcaster
{
    private const string TestMessage = "test-message";

    [Fact]
    public void when_broadcast_is_called_then_message_logged_event_fires_with_exact_message()
    {
        var sut = new LogBroadcaster();
        string? received = null;
        sut.MessageLogged += msg => received = msg;

        sut.Broadcast(TestMessage);

        received.ShouldBe(TestMessage);
    }

    [Fact]
    public void when_broadcast_is_called_with_multiple_subscribers_then_all_subscribers_are_notified()
    {
        var sut = new LogBroadcaster();
        var callCount = 0;
        sut.MessageLogged += _ => callCount++;
        sut.MessageLogged += _ => callCount++;

        sut.Broadcast(TestMessage);

        callCount.ShouldBe(2);
    }

    [Fact]
    public void when_broadcast_is_called_with_no_subscribers_then_no_exception_is_thrown()
    {
        var sut = new LogBroadcaster();

        var act = () => sut.Broadcast(TestMessage);

        act.ShouldNotThrow();
    }

    [Fact]
    public void when_subscriber_is_added_after_first_broadcast_then_it_does_not_receive_earlier_messages()
    {
        var sut = new LogBroadcaster();
        sut.Broadcast(TestMessage);

        string? received = null;
        sut.MessageLogged += msg => received = msg;

        received.ShouldBeNull();
    }

    [Fact]
    public void when_subscriber_is_removed_then_it_no_longer_receives_messages()
    {
        var sut = new LogBroadcaster();
        string? received = null;
        Action<string> handler = msg => received = msg;
        sut.MessageLogged += handler;
        sut.MessageLogged -= handler;

        sut.Broadcast(TestMessage);

        received.ShouldBeNull();
    }
}
