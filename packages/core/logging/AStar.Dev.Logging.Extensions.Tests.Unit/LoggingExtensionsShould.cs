using NSubstitute;

namespace AStar.Dev.Logging.Extensions.Tests.Unit;

public sealed class LoggingExtensionsShould
{
    [Theory]
    [InlineData("This is not a valid filename for a lot of reasons")]
    [InlineData(@"c:\This is not a valid filename\as the path\and filename\do not exist.what.did.you.expect.lol")]
    public void ThrowExceptionWhenAddSerilogLoggingIsCalledButConfigIsntValid(string? fileNameWithPath)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        void action()
        {
            builder.AddSerilogLogging(fileNameWithPath!);
        }

        Should.NotThrow(action);
    }

    [Fact]
    public void AddSerilogLogging_WebApplicationBuilder_ValidExternalSettingsFile_ShouldConfigureSerilog()
    {
        IConfigurationRoot configMock = new ConfigurationBuilder().AddInMemoryCollection().Build();
        WebApplicationBuilder builder    = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(configMock);
        string externalSettingsFile = "testsettings.json";

        ITelemetryClient? telemetryClient = Substitute.For<ITelemetryClient>();
        IServiceProvider? telemetryMock   = Substitute.For<IServiceProvider>();
        telemetryMock.GetService(typeof(ITelemetryClient)).Returns(telemetryClient);
        builder.Services.AddSingleton(telemetryMock);

        WebApplicationBuilder configuredBuilder = builder.AddSerilogLogging(externalSettingsFile);

        configuredBuilder.ShouldNotBeNull();
        configuredBuilder.ShouldBe(builder);
    }

    [Fact]
    public void AddSerilogLogging_WebApplicationBuilder_NullExternalSettingsFile_ShouldNotLoadJsonFile()
    {
        IConfiguration? configMock = Substitute.For<IConfiguration>();
        WebApplicationBuilder builder    = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(configMock);

        string? externalSettingsFile = null;

        ITelemetryClient? telemetryClient = Substitute.For<ITelemetryClient>();
        IServiceProvider? telemetryMock   = Substitute.For<IServiceProvider>();
        telemetryMock.GetService(typeof(ITelemetryClient)).Returns(telemetryClient);
        builder.Services.AddSingleton(telemetryMock);

        WebApplicationBuilder configuredBuilder = builder.AddSerilogLogging(externalSettingsFile!);

        configuredBuilder.ShouldNotBeNull();
        configuredBuilder.ShouldBe(builder);
    }

    [Fact]
    public void AddSerilogLogging_HostApplicationBuilder_ValidExternalSettingsFile_ShouldConfigureSerilog()
    {
        IConfigurationRoot configMock = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var builder    = new HostApplicationBuilder();
        builder.Configuration.AddConfiguration(configMock);
        string externalSettingsFile = "testsettings.json";

        ITelemetryClient? telemetryClient = Substitute.For<ITelemetryClient>();
        IServiceProvider? telemetryMock   = Substitute.For<IServiceProvider>();
        telemetryMock.GetService(typeof(ITelemetryClient)).Returns(telemetryClient);
        builder.Services.AddSingleton(telemetryMock);

        HostApplicationBuilder configuredBuilder = builder.AddSerilogLogging(externalSettingsFile);

        configuredBuilder.ShouldNotBeNull();
        configuredBuilder.ShouldBe(builder);
    }

    [Fact]
    public void AddSerilogLogging_HostApplicationBuilder_NullExternalSettingsFile_ShouldNotLoadJsonFile()
    {
        IConfiguration? configMock = Substitute.For<IConfiguration>();
        var builder    = new HostApplicationBuilder();
        builder.Configuration.AddConfiguration(configMock);
        string? externalSettingsFile = null;

        ITelemetryClient? telemetryClient = Substitute.For<ITelemetryClient>();
        IServiceProvider? telemetryMock   = Substitute.For<IServiceProvider>();
        telemetryMock.GetService(typeof(ITelemetryClient)).Returns(telemetryClient);
        builder.Services.AddSingleton(telemetryMock);

        HostApplicationBuilder configuredBuilder = builder.AddSerilogLogging(externalSettingsFile!);

        configuredBuilder.ShouldNotBeNull();
        configuredBuilder.ShouldBe(builder);
    }
}
