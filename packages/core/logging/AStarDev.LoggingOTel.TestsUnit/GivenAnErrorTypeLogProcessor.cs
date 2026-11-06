using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace AStarDev.LoggingOTel.TestsUnit;

public sealed class GivenAnErrorTypeLogProcessor
{
    [Theory]
    [InlineData("System.InvalidOperationException", "System.InvalidOperationException")]
    [InlineData(" ", "_OTHER")]
    [InlineData(null, "_OTHER")]
    public void when_a_failure_is_logged_with_an_error_type_attribute_then_it_is_exported_as_error_type(string? value, string expected)
    {
        var exported = LogAndCapture(logger => logger.LogError("The operation failed ({ErrorType})", value));

        exported["error.type"].ShouldBe(expected);
    }

    [Fact]
    public void when_a_failure_is_logged_with_an_exception_type_attribute_then_it_is_exported_as_error_type()
    {
        var exported = LogAndCapture(logger => logger.LogError("The operation failed ({ExceptionType})", "Microsoft.Identity.Client.MsalServiceException"));

        exported["error.type"].ShouldBe("Microsoft.Identity.Client.MsalServiceException");
        exported["ExceptionType"].ShouldBe("Microsoft.Identity.Client.MsalServiceException");
    }

    [Fact]
    public void when_a_failure_carries_both_error_type_and_exception_type_then_the_first_attribute_wins()
    {
        var exported = LogAndCapture(logger => logger.LogError("Failed with {ExceptionType} then {ErrorType}", "First.Exception", "Second.Error"));

        exported["error.type"].ShouldBe("First.Exception");
    }

    [Fact]
    public void when_a_success_is_logged_without_an_error_type_attribute_then_no_error_type_is_exported()
    {
        var exported = LogAndCapture(logger => logger.LogInformation("The operation completed in {DurationMs} ms", 12));

        exported.ShouldNotContainKey("error.type");
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
