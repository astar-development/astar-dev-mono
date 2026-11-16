using AStarDev.LoggingExtensions.TestsUnit.Helpers;

namespace AStarDev.LoggingExtensions.TestsUnit;

public sealed class GivenTheWarningWithErrorTypeTemplate
{
    [Fact]
    public void when_a_warning_with_an_error_type_is_logged_then_the_level_event_id_and_message_follow_the_template()
    {
        var logger = new FakeLogger();

        LogMessage.Warning(logger, "Somewhere", "Something did not finish", "System.InvalidOperationException");

        var log = logger.Logs.ShouldHaveSingleItem();
        (log.Level, log.EventId.Id, log.Message, log.Exception).ShouldBe((LogLevel.Warning, 405, "Somewhere has raised: `Something did not finish` (System.InvalidOperationException).", null));
    }
}
