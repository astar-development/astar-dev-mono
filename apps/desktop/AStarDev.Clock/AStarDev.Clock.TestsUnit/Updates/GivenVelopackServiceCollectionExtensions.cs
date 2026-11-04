using AStarDev.Clock.Updates;
using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.Clock.TestsUnit.Updates;

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
    public void when_velopack_update_services_are_added_then_the_localized_update_dialog_text_provider_is_registered()
    {
        var services = CreateServices();

        var descriptor = services.Single(service => service.ServiceType == typeof(IUpdateDialogTextProvider));
        descriptor.ImplementationType.ShouldBe(typeof(LocalizedUpdateDialogTextProvider));
    }

    [Fact]
    public void when_the_update_dialog_text_provider_is_resolved_then_the_labels_come_from_the_en_gb_localisation_file()
    {
        using var provider = CreateServices().BuildServiceProvider();

        var textProvider = provider.GetRequiredService<IUpdateDialogTextProvider>();

        textProvider.Title.ShouldBe("Update available");
        textProvider.ReleaseNotesLabel.ShouldBe("What's new");
        textProvider.RestartNowLabel.ShouldBe("Restart now");
        textProvider.LaterLabel.ShouldBe("Later");
        textProvider.DownloadingLabel.ShouldBe("Downloading...");
    }

    [Fact]
    public void when_the_update_dialog_text_provider_is_resolved_then_the_message_comes_from_the_en_gb_localisation_file()
    {
        using var provider = CreateServices().BuildServiceProvider();

        var textProvider = provider.GetRequiredService<IUpdateDialogTextProvider>();

        textProvider.GetMessage("1.2.3").ShouldBe("Version 1.2.3 is ready to install.");
    }

    private static IServiceCollection CreateServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Updates:GithubRepositoryUrl"] = "https://github.com/astar-development/astar-dev-mono" })
            .Build();

        return new ServiceCollection().AddVelopackUpdateServices(configuration);
    }
}
