using AStarDev.LoggingExtensions.TestsUnit.Helpers;

namespace AStarDev.LoggingExtensions.TestsUnit;

[TestSubject(typeof(LogMessage))]
public sealed class GivenAnExceptionLoggedByType
{
    private const string Email = "jane.doe@example.com";
    private const string Token = "eyJhbGciOiJIUzI1NiJ9.secret-token";

    private static readonly InvalidOperationException ExceptionWithPii = new($"Request for {Email} failed with bearer {Token}");

    [Fact]
    public void when_the_error_type_of_an_exception_is_read_then_it_is_the_full_type_name()
    {
        string errorType = ExceptionWithPii.ErrorType();

        errorType.ShouldBe("System.InvalidOperationException");
    }

    [Fact]
    public void when_an_exception_is_caught_and_logged_then_only_its_type_name_is_recorded()
    {
        var logger = new FakeLogger();

        LogMessage.ExceptionCaught(logger, "ContactSender", ExceptionWithPii.ErrorType());

        var log = logger.Logs.ShouldHaveSingleItem();
        log.Level.ShouldBe(LogLevel.Error);
        log.EventId.Id.ShouldBe(5010);
        log.EventId.Name.ShouldBe("exception");
        log.Message.ShouldBe("ContactSender encountered System.InvalidOperationException");
        log.Exception.ShouldBeNull();
        AssertNoPii(logger);
    }

    [Fact]
    public void when_an_error_with_an_exception_type_is_logged_then_only_its_type_name_is_recorded()
    {
        var logger = new FakeLogger();

        LogMessage.Error(logger, "Could not save the account.", ExceptionWithPii.ErrorType());

        var log = logger.Logs.ShouldHaveSingleItem();
        log.Level.ShouldBe(LogLevel.Error);
        log.EventId.Id.ShouldBe(507);
        log.Message.ShouldBe("Error occurred : `Could not save the account.` (System.InvalidOperationException)");
        log.Exception.ShouldBeNull();
        logger.States[0].ShouldContain(new KeyValuePair<string, object?>("ErrorType", "System.InvalidOperationException"));
        AssertNoPii(logger);
    }

    private static void AssertNoPii(FakeLogger logger)
    {
        string recorded = string.Join("|", logger.Logs.Select(log => log.Message).Concat(logger.States.SelectMany(state => state.Select(pair => $"{pair.Key}={pair.Value}"))));

        recorded.ShouldNotContain(Email);
        recorded.ShouldNotContain(Token);
    }
}
