using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;

namespace AStarDev.LoggingOTel.TestsUnit;

public sealed class GivenOTelTelemetryConfigurator
{
    private static IConfigurationRoot Configuration(params (string Key, string Value)[] settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings.Select(setting => new KeyValuePair<string, string?>(setting.Key, setting.Value))).Build();

    [Fact]
    public void when_telemetry_is_configured_then_the_named_meter_and_activity_source_are_listened_to_until_disposed()
    {
        using var meter = new Meter("the-test-meter");
        using var source = new ActivitySource("the-test-source");
        var counter = meter.CreateCounter<long>("the-test-counter");

        var providers = OTelTelemetryConfigurator.ConfigureOTelTelemetry(Configuration(("OpenTelemetry:OtlpEndpoint", "http://localhost:4317")), "the-test-service", "the-test-meter", "the-test-source");
        var whileRunning = (counter.Enabled, source.HasListeners());
        providers.Dispose();

        (whileRunning, counter.Enabled, source.HasListeners()).ShouldBe(((true, true), false, false));
    }

    [Fact]
    public void when_an_otlp_endpoint_and_a_connection_string_are_configured_then_telemetry_starts_without_throwing()
    {
        var configuration = Configuration(("OpenTelemetry:OtlpEndpoint", "http://localhost:4317"), ("ApplicationInsights:ConnectionString", "InstrumentationKey=00000000-0000-0000-0000-000000000000"));

        var start = () => OTelTelemetryConfigurator.ConfigureOTelTelemetry(configuration, "the-test-service", "the-test-meter", "the-test-source").Dispose();

        start.ShouldNotThrow();
    }

    [Fact]
    public void when_other_meters_exist_then_they_are_not_listened_to()
    {
        using var other = new Meter("another-meter");
        var counter = other.CreateCounter<long>("another-counter");

        using var providers = OTelTelemetryConfigurator.ConfigureOTelTelemetry(Configuration(("OpenTelemetry:OtlpEndpoint", "http://localhost:4317")), "the-test-service", "the-test-meter", "the-test-source");

        counter.Enabled.ShouldBeFalse();
    }
}
