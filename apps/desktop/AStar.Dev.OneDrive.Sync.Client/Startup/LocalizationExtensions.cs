using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Startup;

public static class LocalizationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddLocalizationServices()
        {
#pragma warning disable CA1859
            ILocalizationService locService = new LocalizationService();
#pragma warning restore CA1859
            locService.Initialise(new CultureInfo("en-GB"));
            services.AddSingleton(locService);

            return services;
        }
    }
}
