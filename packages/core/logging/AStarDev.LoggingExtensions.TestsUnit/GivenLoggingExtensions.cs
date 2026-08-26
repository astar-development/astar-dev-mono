using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AStar.Dev.Logging.Extensions.TestsUnit;

public sealed class GivenLoggingExtensions
{
    [Fact]
    public void when_add_o_tel_logging_is_called_on_a_web_application_builder_then_it_returns_the_same_builder_for_chaining()
    {
        var builder = WebApplication.CreateBuilder();

        var result = builder.AddOTelLogging();

        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_on_a_web_application_builder_then_a_usable_logger_is_produced()
    {
        var builder = WebApplication.CreateBuilder();
        _ = builder.AddOTelLogging();
        using var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<GivenLoggingExtensions>>();

        logger.ShouldNotBeNull();
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_on_a_web_application_builder_then_i_logger_astar_resolves_to_a_star_logger()
    {
        var builder = WebApplication.CreateBuilder();
        _ = builder.AddOTelLogging();
        using var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILoggerAstar<GivenLoggingExtensions>>();

        logger.ShouldBeOfType<AStarLogger<GivenLoggingExtensions>>();
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_on_a_web_application_builder_then_log_messages_can_be_written_without_throwing()
    {
        var builder = WebApplication.CreateBuilder();
        _ = builder.AddOTelLogging();
        using var app = builder.Build();
        var logger = app.Services.GetRequiredService<ILogger<GivenLoggingExtensions>>();

        Action act = () => logger.LogInformation("the expected message was logged");

        act.ShouldNotThrow();
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_on_a_host_application_builder_then_it_returns_the_same_builder_for_chaining()
    {
        var builder = Host.CreateApplicationBuilder();

        var result = builder.AddOTelLogging();

        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_on_a_host_application_builder_then_a_usable_logger_is_produced()
    {
        var builder = Host.CreateApplicationBuilder();
        _ = builder.AddOTelLogging();
        using var host = builder.Build();

        var logger = host.Services.GetRequiredService<ILogger<GivenLoggingExtensions>>();

        logger.ShouldNotBeNull();
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_on_a_host_application_builder_then_log_messages_can_be_written_without_throwing()
    {
        var builder = Host.CreateApplicationBuilder();
        _ = builder.AddOTelLogging();
        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<GivenLoggingExtensions>>();

        Action act = () => logger.LogInformation("the expected message was logged");

        act.ShouldNotThrow();
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_with_an_external_settings_file_then_it_is_merged_into_configuration()
    {
        var builder = WebApplication.CreateBuilder();

        _ = builder.AddOTelLogging("appsettings.tests.json");

        builder.Configuration["SomeTestKey"].ShouldBe("some-test-value");
    }

    [Fact]
    public void when_add_o_tel_logging_is_called_with_no_external_settings_file_then_configuration_is_unaffected()
    {
        var builder = WebApplication.CreateBuilder();

        _ = builder.AddOTelLogging();

        builder.Configuration["SomeTestKey"].ShouldBeNull();
    }

    [Fact]
    public async Task when_add_o_tel_logging_is_called_on_a_web_application_builder_with_no_connection_string_then_starting_the_host_does_not_throw()
    {
        var builder = WebApplication.CreateBuilder();
        _ = builder.AddOTelLogging();
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
        _ = builder.AddOTelLogging();
        using var host = builder.Build();

        async Task Act()
        {
            await host.StartAsync();
            await host.StopAsync();
        }

        await Should.NotThrowAsync(Act);
    }
}
