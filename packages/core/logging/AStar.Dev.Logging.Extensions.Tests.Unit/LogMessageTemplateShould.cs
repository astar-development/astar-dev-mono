namespace AStar.Dev.Logging.Extensions.Tests.Unit;

[TestSubject(typeof(LogMessage))]
public class LogMessageTemplateShould
{
    [Fact]
    public void LogBadRequest_WithValidPath_ShouldLogCorrectMessage()
    {
        var          logger = new FakeLogger();
        const string path   = "/test-path";

        LogMessage.BadRequest(logger, path);

        logger.Logs.Count.ShouldBe(1);
        var log = logger.Logs[0];
        log.Level.ShouldBe(LogLevel.Warning);
        log.EventId.Id.ShouldBe(400);
        log.Message.ShouldBe($"Bad Request (400) for `{path}`");
        log.Exception.ShouldBeNull();
    }

    [Fact]
    public void LogUnauthorized_WithValidPath_ShouldLogCorrectMessage()
    {
        var          logger = new FakeLogger();
        const string path   = "/unauthorized-path";

        LogMessage.Unauthorized(logger, path);

        logger.Logs.Count.ShouldBe(1);
        var log = logger.Logs[0];
        log.Level.ShouldBe(LogLevel.Warning);
        log.EventId.Id.ShouldBe(401);
        log.Message.ShouldBe($"Unauthorized (401) for `{path}`");
        log.Exception.ShouldBeNull();
    }

    [Fact]
    public void LogForbidden_WithValidPathAndUser_ShouldLogCorrectMessage()
    {
        var          logger = new FakeLogger();
        const string path   = "/forbidden-path";
        const string user   = "test-user";

        LogMessage.Forbidden(logger, path, user);

        logger.Logs.Count.ShouldBe(1);
        var log = logger.Logs[0];
        log.Level.ShouldBe(LogLevel.Warning);
        log.EventId.Id.ShouldBe(403);
        log.Message.ShouldBe($"Forbidden (403) for `{path}` for user `{user}`");
        log.Exception.ShouldBeNull();
    }

    [Fact]
    public void LogNotFound_WithValidPath_ShouldLogCorrectMessage()
    {
        var          logger = new FakeLogger();
        const string path   = "/not-found-path";

        LogMessage.NotFound(logger, path);

        logger.Logs.Count.ShouldBe(1);
        var log = logger.Logs[0];
        log.Level.ShouldBe(LogLevel.Warning);
        log.EventId.Id.ShouldBe(404);
        log.Message.ShouldBe($"Not Found (404) for `{path}`");
        log.Exception.ShouldBeNull();
    }

    [Fact]
    public void LogConflict_WithValidPath_ShouldLogCorrectMessage()
    {
        var          logger = new FakeLogger();
        const string path   = "/conflict-path";

        LogMessage.Conflict(logger, path);

        logger.Logs.Count.ShouldBe(1);
        var log = logger.Logs[0];
        log.Level.ShouldBe(LogLevel.Warning);
        log.EventId.Id.ShouldBe(409);
        log.Message.ShouldBe($"Conflict (409) for `{path}`");
        log.Exception.ShouldBeNull();
    }

    [Fact]
    public void LogUnprocessableEntity_WithValidPath_ShouldLogCorrectMessage()
    {
        var          logger = new FakeLogger();
        const string path   = "/unprocessable-entity-path";

        LogMessage.UnprocessableEntity(logger, path);

        logger.Logs.Count.ShouldBe(1);
        var log = logger.Logs[0];
        log.Level.ShouldBe(LogLevel.Warning);
        log.EventId.Id.ShouldBe(422);
        log.Message.ShouldBe($"Unprocessable Entity (422) for `{path}`");
        log.Exception.ShouldBeNull();
    }

    [Fact]
    public void LogTooManyRequests_WithValidPath_ShouldLogCorrectMessage()
    {
        var          logger = new FakeLogger();
        const string path   = "/too-many-requests";

        LogMessage.TooManyRequests(logger, path);

        logger.Logs.Count.ShouldBe(1);
        var log = logger.Logs[0];
        log.Level.ShouldBe(LogLevel.Warning);
        log.EventId.Id.ShouldBe(429);
        log.Message.ShouldBe($"Too Many Requests (429) for `{path}`");
        log.Exception.ShouldBeNull();
    }

    [Fact]
    public void LogInternalServerError_WithValidPath_ShouldLogCorrectMessage()
    {
        var          logger = new FakeLogger();
        const string path   = "/internal-server-error";

        LogMessage.InternalServerError(logger, path);

        logger.Logs.Count.ShouldBe(1);
        var log = logger.Logs[0];
        log.Level.ShouldBe(LogLevel.Error);
        log.EventId.Id.ShouldBe(500);
        log.Message.ShouldBe($"Internal Server Error (500) for `{path}`");
        log.Exception.ShouldBeNull();
    }

    [Fact]
    public void LogBadGateway_WithValidPath_ShouldLogCorrectMessage()
    {
        var          logger = new FakeLogger();
        const string path   = "/bad-gateway";

        LogMessage.BadGateway(logger, path);

        logger.Logs.Count.ShouldBe(1);
        var log = logger.Logs[0];
        log.Level.ShouldBe(LogLevel.Error);
        log.EventId.Id.ShouldBe(502);
        log.Message.ShouldBe($"Bad Gateway (502) for `{path}`");
        log.Exception.ShouldBeNull();
    }

    [Fact]
    public void LogServiceUnavailable_WithValidPath_ShouldLogCorrectMessage()
    {
        var          logger = new FakeLogger();
        const string path   = "/service-unavailable";

        LogMessage.ServiceUnavailable(logger, path);

        logger.Logs.Count.ShouldBe(1);
        var log = logger.Logs[0];
        log.Level.ShouldBe(LogLevel.Error);
        log.EventId.Id.ShouldBe(503);
        log.Message.ShouldBe($"Service Unavailable (503) for `{path}`");
        log.Exception.ShouldBeNull();
    }

    [Fact]
    public void LogGatewayTimeout_WithValidPath_ShouldLogCorrectMessage()
    {
        var          logger = new FakeLogger();
        const string path   = "/gateway-timeout";

        LogMessage.GatewayTimeout(logger, path);

        logger.Logs.Count.ShouldBe(1);
        var log = logger.Logs[0];
        log.Level.ShouldBe(LogLevel.Error);
        log.EventId.Id.ShouldBe(504);
        log.Message.ShouldBe($"Gateway Timeout (504) for `{path}`");
        log.Exception.ShouldBeNull();
    }
}