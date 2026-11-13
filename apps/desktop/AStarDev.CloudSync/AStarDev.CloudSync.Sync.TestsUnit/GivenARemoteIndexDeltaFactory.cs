namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenARemoteIndexDeltaFactory
{
    [Fact]
    public void when_no_changes_are_given_then_the_delta_is_empty()
    {
        var delta = RemoteIndexDeltaFactory.Create(null);

        delta.Changes.ShouldBeEmpty();
    }

    [Fact]
    public void when_changes_are_given_then_they_are_kept_in_order()
    {
        var first = RemoteChangeFactory.CreateDeleted(RemoteIds.Of("one"));
        var second = RemoteChangeFactory.CreateDeleted(RemoteIds.Of("two"));

        var delta = RemoteIndexDeltaFactory.Create([first, second]);

        delta.Changes.ShouldBe([first, second]);
    }
}
