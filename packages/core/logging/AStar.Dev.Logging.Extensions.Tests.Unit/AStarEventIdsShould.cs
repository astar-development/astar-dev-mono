namespace AStar.Dev.Logging.Extensions.Tests.Unit;

[TestSubject(typeof(AStarEventIds))]
public class AStarEventIdsShould
{
    [Fact]
    public void PageView_HasExpectedIdAndName()
    {
        const int    expectedId   = 1000;
        const string expectedName = "Page view";

        var eventId = AStarEventIds.PageView;

        eventId.Id.ShouldBe(expectedId);
        eventId.Name.ShouldBe(expectedName);
    }
}