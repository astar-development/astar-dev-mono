using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace AStarDev.LoggingOTel.TestsUnit;

public sealed class GivenAnErrorTypeLogProcessor
{
    [Theory]
    [InlineData("ErrorType", "System.InvalidOperationException", "System.InvalidOperationException")]
    [InlineData("ExceptionType", "Microsoft.Identity.Client.MsalServiceException", "Microsoft.Identity.Client.MsalServiceException")]
    [InlineData("ErrorType", " ", "_OTHER")]
    public void when_a_failure_is_logged_with_an_error_type_attribute_then_it_is_exported_as_error_type(string key, string value, string expected)
    {
        var exported = LogAndCapture(logger => LogFailure(logger, key, value));

        exported["error.type"].ShouldBe(expected);
        exported[key].ShouldBe(value);
    }

    [Fact]
    public void when_a_success_is_logged_without_an_error_type_attribute_then_no_error_type_is_exported()
    {
        var exported = LogAndCapture(logger => logger.LogInformation("The operation completed in {DurationMs} ms", 12));

        exported.ShouldNotContainKey("error.type");
    }

    private static void LogFailure(ILogger logger, string key, string value)
    {
        if (key == "ErrorType")
            logger.LogError("The operation failed ({ErrorType})", value);
        else
            logger.LogError("The operation failed ({ExceptionType})", value);
    }

    private static Dictionary<string, string> LogAndCapture(Action<ILogger> log)
    {
        using var capture = new CapturingProcessor();
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.ConfigureOTelLogging(new ConfigurationBuilder().Build(), "the-test-service").AddOpenTelemetry(options => options.AddProcessor(capture)));
        using (var serviceProvider = services.BuildServiceProvider())
            log(serviceProvider.GetRequiredService<ILogger<GivenAnErrorTypeLogProcessor>>());

        return capture.Attributes.ShouldHaveSingleItem();
    }

    private sealed class CapturingProcessor : BaseProcessor<LogRecord>
    {
        public List<Dictionary<string, string>> Attributes { get; } = [];

        public override void OnEnd(LogRecord data) => Attributes.Add((data.Attributes ?? []).GroupBy(attribute => attribute.Key).ToDictionary(group => group.Key, group => group.Last().Value?.ToString() ?? string.Empty));
    }
}
