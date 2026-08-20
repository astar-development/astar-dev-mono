using System.Globalization;
using AStarDev.WallpaperScraper.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.WallpaperScraper.Startup;

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
