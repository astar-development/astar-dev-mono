using AStar.Dev.Clock.Updates;
using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.Clock.TestsUnit.Updates;

public sealed class GivenVelopackServiceCollectionExtensions
{
    [Fact]
    public void when_velopack_update_services_are_added_then_the_velopack_update_service_is_registered()
    {
        var services = CreateServices();

        services.ShouldContain(service => service.ServiceType == typeof(IVelopackUpdateService));
    }

    [Fact]
    public void when_velopack_update_services_are_added_then_the_update_notification_service_is_registered()
    {
        var services = CreateServices();

        services.ShouldContain(service => service.ServiceType == typeof(IUpdateNotificationService));
    }

    [Fact]
    public void when_velopack_update_services_are_added_then_the_plain_update_dialog_text_provider_is_registered()
    {
        var services = CreateServices();

        var descriptor = services.Single(service => service.ServiceType == typeof(IUpdateDialogTextProvider));
        descriptor.ImplementationType.ShouldBe(typeof(PlainUpdateDialogTextProvider));
    }

    private static IServiceCollection CreateServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Updates:GithubRepositoryUrl"] = "https://github.com/astar-development/astar-dev-mono" })
            .Build();

        return new ServiceCollection().AddVelopackUpdateServices(configuration);
    }
}
