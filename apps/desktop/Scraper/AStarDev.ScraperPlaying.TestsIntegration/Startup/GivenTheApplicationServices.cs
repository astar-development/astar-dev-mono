using System.Net;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Startup;
using AStarDev.ScraperPlaying.UI;
using AStarDev.ControlDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.Startup;

/// <summary>Builds the real registrations and resolves the main window's collaborators, so a missing registration fails here rather than when the application starts.</summary>
public sealed class GivenTheApplicationServices : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-scraperplaying-applicationservices-{Guid.CreateVersion7():N}.db");
    private readonly ServiceProvider serviceProvider;
    private bool disposed;

    public GivenTheApplicationServices()
    {
        var configuration = new ConfigurationBuilder().Build();
        serviceProvider = new ServiceCollection()
            .AddConfigurationServices(configuration)
            .AddDataServices(databasePath)
            .AddInfrastructureServices()
            .AddApplicationServices(configuration)
            .AddLogging()
            .BuildServiceProvider();
    }

    [Fact]
    public void when_the_real_registrations_are_validated_then_no_service_captures_a_shorter_lived_dependency()
    {
        var configuration = new ConfigurationBuilder().Build();
        using var validated = new ServiceCollection()
            .AddConfigurationServices(configuration)
            .AddDataServices(databasePath)
            .AddInfrastructureServices()
            .AddApplicationServices(configuration)
            .AddLogging()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        validated.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_the_database_context_is_created_from_a_scope_validated_provider_then_it_is_created()
    {
        using var validated = BuildValidated(out _);

        await using var context = await validated.GetRequiredService<IDbContextFactory<ControlDbContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken);

        context.ShouldNotBeNull();
    }

    // MainWindow is excluded: it is an Avalonia window and cannot be constructed without a windowing platform.
    [Fact]
    public void when_every_registered_service_is_resolved_from_a_scope_validated_provider_then_none_fails_to_resolve()
    {
        using var validated = BuildValidated(out var services);
        using var scope = validated.CreateScope();

        var failures = services
            .Where(descriptor => !descriptor.ServiceType.ContainsGenericParameters && !descriptor.IsKeyedService && descriptor.ServiceType != typeof(MainWindow))
            .Select(descriptor => descriptor.Lifetime is ServiceLifetime.Singleton ? validated : scope.ServiceProvider)
            .Zip(services.Where(descriptor => !descriptor.ServiceType.ContainsGenericParameters && !descriptor.IsKeyedService && descriptor.ServiceType != typeof(MainWindow)))
            .Select(pair => TryResolve(pair.First, pair.Second.ServiceType))
            .OfType<string>()
            .Distinct()
            .ToList();

        failures.ShouldBeEmpty();
    }

    [Fact]
    public void when_the_wallhaven_client_is_created_then_it_uses_the_client_timeout_that_outlasts_the_rate_limit_retries()
    {
        using var client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(ApplicationConstants.WallhavenHttpClientName);

        client.Timeout.ShouldBe(ScrapeTimeouts.Default.Client);
    }

    [Fact]
    public void when_the_main_window_collaborators_are_resolved_then_each_is_a_single_shared_instance()
    {
        var resolved = new object[]
        {
            serviceProvider.GetRequiredService<StatusReporter>(),
            serviceProvider.GetRequiredService<ApplicationReadiness>(),
            serviceProvider.GetRequiredService<UserOperationRunner>(),
            serviceProvider.GetRequiredService<ConfigurationBrowser>(),
            serviceProvider.GetRequiredService<TagsBrowser>(),
            serviceProvider.GetRequiredService<ScrapeRunner>(),
            serviceProvider.GetRequiredService<MainWindowActions>(),
            serviceProvider.GetRequiredService<ImageDisplayCoordinator>()
        };

        resolved.ShouldBe(
        [
            serviceProvider.GetRequiredService<StatusReporter>(),
            serviceProvider.GetRequiredService<ApplicationReadiness>(),
            serviceProvider.GetRequiredService<UserOperationRunner>(),
            serviceProvider.GetRequiredService<ConfigurationBrowser>(),
            serviceProvider.GetRequiredService<TagsBrowser>(),
            serviceProvider.GetRequiredService<ScrapeRunner>(),
            serviceProvider.GetRequiredService<MainWindowActions>(),
            serviceProvider.GetRequiredService<ImageDisplayCoordinator>()
        ]);
    }

    [Fact]
    public async Task when_the_real_wallhaven_client_sends_an_api_request_and_an_image_request_then_only_the_api_request_carries_the_api_key()
    {
        using var capture = new CapturingHandler();
        var configuration = new ConfigurationBuilder().Build();
        using var provider = new ServiceCollection()
            .AddConfigurationServices(configuration)
            .AddDataServices(databasePath)
            .AddInfrastructureServices()
            .AddApplicationServices(configuration)
            .AddLogging()
            .AddHttpClient(ApplicationConstants.WallhavenHttpClientName).ConfigurePrimaryHttpMessageHandler(() => capture).Services
            .BuildServiceProvider();
        using var client = provider.GetRequiredService<IWallhavenClientFactory>().Create(new WallhavenConnection("secret-key", new Uri("https://wallhaven.cc/")));

        using var apiResponse = await client.GetAsync(new Uri("https://wallhaven.cc/api/v1/search?page=1"), TestContext.Current.CancellationToken);
        using var imageResponse = await client.GetAsync(new Uri("https://w.wallhaven.cc/full/ab/wallhaven-abc123.jpg"), TestContext.Current.CancellationToken);

        capture.ApiKeysByHost.ShouldBe(["wallhaven.cc=secret-key", "w.wallhaven.cc="]);
    }

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        serviceProvider.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }

    private ServiceProvider BuildValidated(out IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder().Build();
        services = new ServiceCollection()
            .AddConfigurationServices(configuration)
            .AddDataServices(databasePath)
            .AddInfrastructureServices()
            .AddApplicationServices(configuration)
            .AddLogging();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private static string? TryResolve(IServiceProvider provider, Type serviceType)
    {
        try
        {
            _ = provider.GetService(serviceType);

            return null;
        }
        catch (InvalidOperationException ex)
        {
            return $"{serviceType.Name}: {ex.Message}";
        }
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public List<string> ApiKeysByHost { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ApiKeysByHost.Add($"{request.RequestUri!.Host}={(request.Headers.TryGetValues("X-API-Key", out var values) ? string.Join(",", values) : string.Empty)}");

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
