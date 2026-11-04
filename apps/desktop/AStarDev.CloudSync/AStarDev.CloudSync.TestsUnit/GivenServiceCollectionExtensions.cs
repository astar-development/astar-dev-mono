using AStarDev.CloudSync.ViewModels;
using AStarDev.Localization;
using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.TestsUnit;

public class GivenServiceCollectionExtensions
{
    [Fact]
    public void when_application_services_are_added_then_the_main_window_view_model_is_registered()
    {
        var services = CreateServices();

        services.ShouldContain(service => service.ServiceType == typeof(MainWindowViewModel));
    }

    [Fact]
    public void when_application_services_are_added_then_the_velopack_update_service_is_registered()
    {
        var services = CreateServices();

        services.ShouldContain(service => service.ServiceType == typeof(IVelopackUpdateService));
    }

    [Fact]
    public void when_application_services_are_added_then_the_update_notification_service_is_registered()
    {
        var services = CreateServices();

        services.ShouldContain(service => service.ServiceType == typeof(IUpdateNotificationService));
    }

    [Fact]
    public void when_application_services_are_added_then_the_localized_update_dialog_text_provider_is_registered()
    {
        var services = CreateServices();

        var descriptor = services.Single(service => service.ServiceType == typeof(IUpdateDialogTextProvider));
        descriptor.ImplementationType.ShouldBe(typeof(LocalizedUpdateDialogTextProvider));
    }

    [Fact]
    public void when_application_services_are_added_then_the_localisation_service_reads_the_cloudsync_strings()
    {
        using var provider = CreateServices().BuildServiceProvider();

        var localization = provider.GetRequiredService<ILocalizationService>();

        localization.GetLocal("App.Title").ShouldBe("AStar Dev CloudSync");
    }

    [Theory]
    [InlineData("Update.Title")]
    [InlineData("Update.Message")]
    [InlineData("Update.ReleaseNotesLabel")]
    [InlineData("Update.RestartNow")]
    [InlineData("Update.Later")]
    [InlineData("Update.Downloading")]
    public void when_application_services_are_added_then_the_update_dialog_keys_are_localised(string key)
    {
        using var provider = CreateServices().BuildServiceProvider();

        var localization = provider.GetRequiredService<ILocalizationService>();

        localization.GetLocal(key).ShouldNotBe(key);
    }

    private static IServiceCollection CreateServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Updates:GithubRepositoryUrl"] = "https://github.com/astar-development/astar-dev-mono", ["Updates:ChannelPrefix"] = "cloudsync" })
            .Build();

        return new ServiceCollection().AddApplicationServices(configuration);
    }
}
