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
            var locService = new LocalizationService();
            locService.Initialise(new CultureInfo("en-GB"));
            services.AddSingleton<ILocalizationService>(locService);

            return services;
        }
    }
}
