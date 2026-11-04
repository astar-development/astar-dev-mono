using System.Globalization;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.Localization.TestsUnit;

public sealed class GivenServiceCollectionExtensions
{
    [Fact]
    public void when_the_localization_service_is_added_then_it_resolves_strings_from_the_supplied_assembly()
    {
        using var provider = new ServiceCollection().AddLocalizationService(typeof(GivenServiceCollectionExtensions).Assembly, new CultureInfo("en-GB")).BuildServiceProvider();

        var localization = provider.GetRequiredService<ILocalizationService>();

        localization.GetLocal("App.Title").ShouldBe("Localisation test app");
    }

    [Fact]
    public void when_the_localization_service_is_added_then_the_requested_culture_is_active()
    {
        using var provider = new ServiceCollection().AddLocalizationService(typeof(GivenServiceCollectionExtensions).Assembly, new CultureInfo("en-GB")).BuildServiceProvider();

        var localization = provider.GetRequiredService<ILocalizationService>();

        localization.CurrentCulture.Name.ShouldBe("en-GB");
    }

    [Fact]
    public void when_the_localization_service_is_added_then_it_is_registered_as_a_singleton()
    {
        var services = new ServiceCollection().AddLocalizationService(typeof(GivenServiceCollectionExtensions).Assembly, new CultureInfo("en-GB"));

        services.Single(service => service.ServiceType == typeof(ILocalizationService)).Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }
}
