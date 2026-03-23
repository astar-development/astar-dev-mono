using AStar.Dev.Utilities;
using Microsoft.ApplicationInsights.Extensibility;
using NSubstitute;
using Serilog;

namespace AStar.Dev.Logging.Extensions.Tests.Unit;

[TestSubject(typeof(SerilogConfigure))]
public class SerilogConfigureShould
{
    [Fact]
    public void Configure_ShouldConfigureLogger_WithValidParameters()
    {
        var loggerConfiguration = new LoggerConfiguration();
        var configurationMock   = new ConfigurationBuilder();
        configurationMock.AddInMemoryCollection();
        var telemetryConfiguration = new TelemetryConfiguration();

        var result = loggerConfiguration.Configure(configurationMock.Build(), telemetryConfiguration);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<LoggerConfiguration>();
        result.WriteTo.ShouldNotBeNull();
        result.ReadFrom.ShouldNotBeNull();
        result.ToJson().ShouldMatchApproved();
    }

    [Fact]
    public void Configure_ShouldThrowNullReferenceException_WhenLoggerConfigurationIsNull()
    {
        LoggerConfiguration? loggerConfiguration = null;
        var                  configurationMock   = new ConfigurationBuilder();
        configurationMock.AddInMemoryCollection();
        var telemetryConfiguration = new TelemetryConfiguration();

        Should.Throw<NullReferenceException>(() => loggerConfiguration!.Configure(configurationMock.Build(), telemetryConfiguration));
    }

    [Fact]
    public void Configure_ShouldThrowNullReferenceException_WhenConfigurationIsNull()
    {
        var             loggerConfiguration    = new LoggerConfiguration();
        IConfiguration? configuration          = null;
        var             telemetryConfiguration = new TelemetryConfiguration();

        Should.Throw<NullReferenceException>(() => loggerConfiguration.Configure(configuration!, telemetryConfiguration));
    }

    [Fact]
    public void Configure_ShouldThrowInvalidOperationException_WhenTelemetryConfigurationIsNull()
    {
        var                     loggerConfiguration    = new LoggerConfiguration();
        var                     configurationMock      = Substitute.For<IConfiguration>();
        TelemetryConfiguration? telemetryConfiguration = null;

        Should.Throw<InvalidOperationException>(() => loggerConfiguration.Configure(configurationMock, telemetryConfiguration!));
    }

    [Fact]
    public void Configure_ShouldHandleEmptyConfiguration()
    {
        var loggerConfiguration = new LoggerConfiguration();
        var configurationMock   = new ConfigurationBuilder();
        configurationMock.AddInMemoryCollection();
        var telemetryConfiguration = new TelemetryConfiguration();

        var result = loggerConfiguration.Configure(configurationMock.Build(), telemetryConfiguration);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void Configure_ShouldHandleNullSectionsInsideConfiguration()
    {
        var loggerConfiguration = new LoggerConfiguration();
        var configurationMock   = new ConfigurationBuilder();
        configurationMock.AddInMemoryCollection();
        var telemetryConfiguration = new TelemetryConfiguration();

        var result = loggerConfiguration.Configure(configurationMock.Build(), telemetryConfiguration);

        result.ShouldNotBeNull();
    }
}