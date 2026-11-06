using AStarDev.LoggingExtensions.TestsUnit.Helpers;

namespace AStarDev.LoggingExtensions.TestsUnit;

[TestSubject(typeof(AStarLogger<>))]
public sealed class GivenAnAStarLogger
{
    [Fact]
    public void when_a_page_view_is_logged_then_the_page_view_template_is_used_with_the_page_name()
    {
        var innerLogger = new FakeLogger<GivenAnAStarLogger>();
        var astarLogger = new AStarLogger<GivenAnAStarLogger>(innerLogger);

        astarLogger.LogPageView("Home");

        innerLogger.Logs.Count.ShouldBe(1);
        var log = innerLogger.Logs[0];
        log.Level.ShouldBe(LogLevel.Information);
        log.EventId.Id.ShouldBe(200);
        log.Message.ShouldBe("Page `Home` viewed.");
    }
}
