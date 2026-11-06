using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using System.Reflection;

namespace AStarDev.LoggingExtensions.TestsUnit;

public sealed class GivenLoggingExtensions
{
    private const string TestServiceName = "the-test-service";

    private const string TestConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost/";

    [Fact]
    public void when_add_o_tel_logging_is_called_with_a_connection_string_then_the_resource_carries_the_service_name()
    {
        using var app = BuildWebApplicationWithConnectionString();

        ResourceAttribute(app.Services, "service.name").ShouldBe(TestServiceName);
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_with_a_connection_string_then_the_resource_carries_the_entry_assembly_version_without_build_metadata()
    {
        using var app = BuildWebApplicationWithConnectionString();
        var expectedVersion = Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];

        ResourceAttribute(app.Services, "service.version").ShouldBe(expectedVersion);
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_with_a_connection_string_then_the_resource_carries_a_service_instance_id()
    {
        using var app = BuildWebApplicationWithConnectionString();

        ResourceAttribute(app.Services, "service.instance.id").ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_with_a_connection_string_then_the_resource_carries_the_host_environment_in_lower_case()
    {
        using var app = BuildWebApplicationWithConnectionString(Environments.Staging);

        ResourceAttribute(app.Services, "deployment.environment.name").ShouldBe("staging");
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_on_a_web_application_builder_then_it_returns_the_same_builder_for_chaining()
    {
        var builder = WebApplication.CreateBuilder();

        var result = builder.AddOTelLogging(TestServiceName);

        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_on_a_web_application_builder_then_a_usable_logger_is_produced()
    {
        var builder = WebApplication.CreateBuilder();
        _ = builder.AddOTelLogging(TestServiceName);
        using var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<GivenLoggingExtensions>>();

        logger.ShouldNotBeNull();
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_on_a_web_application_builder_then_i_logger_astar_resolves_to_a_star_logger()
    {
        var builder = WebApplication.CreateBuilder();
        _ = builder.AddOTelLogging(TestServiceName);
        using var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILoggerAstar<GivenLoggingExtensions>>();

        logger.ShouldBeOfType<AStarLogger<GivenLoggingExtensions>>();
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_on_a_web_application_builder_then_log_messages_can_be_written_without_throwing()
    {
        var builder = WebApplication.CreateBuilder();
        _ = builder.AddOTelLogging(TestServiceName);
        using var app = builder.Build();
        var logger = app.Services.GetRequiredService<ILogger<GivenLoggingExtensions>>();

        Action act = () => logger.LogInformation("the expected message was logged");

        act.ShouldNotThrow();
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_on_a_host_application_builder_then_it_returns_the_same_builder_for_chaining()
    {
        var builder = Host.CreateApplicationBuilder();

        var result = builder.AddOTelLogging(TestServiceName);

        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_on_a_host_application_builder_then_a_usable_logger_is_produced()
    {
        var builder = Host.CreateApplicationBuilder();
        _ = builder.AddOTelLogging(TestServiceName);
        using var host = builder.Build();

        var logger = host.Services.GetRequiredService<ILogger<GivenLoggingExtensions>>();

        logger.ShouldNotBeNull();
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_on_a_host_application_builder_then_log_messages_can_be_written_without_throwing()
    {
        var builder = Host.CreateApplicationBuilder();
        _ = builder.AddOTelLogging(TestServiceName);
        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<GivenLoggingExtensions>>();

        Action act = () => logger.LogInformation("the expected message was logged");

        act.ShouldNotThrow();
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_with_an_external_settings_file_then_it_is_merged_into_configuration()
    {
        var builder = WebApplication.CreateBuilder();

        _ = builder.AddOTelLogging(TestServiceName, externalSettingsFile: "appsettings.tests.json");

        builder.Configuration["SomeTestKey"].ShouldBe("some-test-value");
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_with_no_external_settings_file_then_configuration_is_unaffected()
    {
        var builder = WebApplication.CreateBuilder();

        _ = builder.AddOTelLogging(TestServiceName);

        builder.Configuration["SomeTestKey"].ShouldBeNull();
    }

    [Fact]
    public async Task when_add_o_tel_logging_is_called_on_a_web_application_builder_with_no_connection_string_then_starting_the_host_does_not_throw()
    {
        var builder = WebApplication.CreateBuilder();
        _ = builder.AddOTelLogging(TestServiceName);
        await using var app = builder.Build();

        async Task Act()
        {
            await app.StartAsync();
            await app.StopAsync();
        }

        await Should.NotThrowAsync(Act);
    }

    [Fact]
    public async Task when_add_o_tel_logging_is_called_on_a_host_application_builder_with_no_connection_string_then_starting_the_host_does_not_throw()
    {
        var builder = Host.CreateApplicationBuilder();
        _ = builder.AddOTelLogging(TestServiceName);
        using var host = builder.Build();

        async Task Act()
        {
            await host.StartAsync();
            await host.StopAsync();
        }

        await Should.NotThrowAsync(Act);
    }

    private static WebApplication BuildWebApplicationWithConnectionString(string environmentName = "Production")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environmentName });
        builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] = TestConnectionString;
        _ = builder.AddOTelLogging(TestServiceName);

        return builder.Build();
    }

    private static string ResourceAttribute(IServiceProvider services, string key)
        => services.GetRequiredService<TracerProvider>().GetResource().Attributes
            .Single(attribute => attribute.Key == key).Value.ToString()!;

    [Fact]
    public void when_a_failure_is_logged_with_azure_monitor_configured_then_error_type_reaches_later_processors()
    {
        using var capture = new ErrorTypeCapturingProcessor();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] = TestConnectionString;
        _ = builder.AddOTelLogging(TestServiceName);
        _ = builder.Services.ConfigureOpenTelemetryLoggerProvider(provider => provider.AddProcessor(capture));
        using var app = builder.Build();

        LogMessage.ExceptionCaught(app.Services.GetRequiredService<ILogger<GivenLoggingExtensions>>(), "the-location", "System.InvalidOperationException");

        capture.ErrorTypes.ShouldBe(["System.InvalidOperationException"]);
    }

    private sealed class ErrorTypeCapturingProcessor : BaseProcessor<LogRecord>
    {
        public List<string?> ErrorTypes { get; } = [];

        public override void OnEnd(LogRecord data) => ErrorTypes.Add(data.Attributes?.LastOrDefault(attribute => attribute.Key == "error.type").Value?.ToString());
    }
}
